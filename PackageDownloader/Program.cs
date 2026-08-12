using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using NuGet.Versioning;

const string NuGetIndexUrl = "https://packagefeedproxy.microsoft.io/nuget/v3/index.json";
const string NetCoreRefPackageId = "Microsoft.NETCore.App.Ref";
const string WindowsDesktopRefPackageId = "Microsoft.WindowsDesktop.App.Ref";
const string RefTargetFramework = "net11.0";
const string ShimReferencesUrl = "https://raw.githubusercontent.com/dotnet/runtime/v7.0.0-preview.1.22076.8/src/libraries/shims/netfxreference.props";
const string dotnetDir = @"C:\Users\gewarren\binaries\dotnet";
const string versionDir = "net-11.0";
const string windowsDesktopDir = "windowsdesktop-11.0";
string downloadDir = Path.Combine(Path.GetTempPath(), "ref-packages");

using HttpClient httpClient = new();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PackageDownloader/1.0");

ClearDirectory(downloadDir);

string packageBaseAddress = await GetPackageBaseAddressAsync(httpClient);
HashSet<string> shimDllExclusions = await GetShimDllExclusionsAsync(httpClient);
HashSet<string> xmlFilesToCopy = BuildFileNameSet(
    "Microsoft.Extensions.Caching.Abstractions.xml",
    "Microsoft.Extensions.Configuration.Abstractions.xml",
    "Microsoft.Extensions.DependencyInjection.Abstractions.xml",
    "Microsoft.Extensions.Diagnostics.Abstractions.xml",
    "Microsoft.Extensions.FileProviders.Abstractions.xml",
    "Microsoft.Extensions.Hosting.Abstractions.xml",
    "Microsoft.Extensions.Logging.Abstractions.xml",
    "Microsoft.Extensions.Options.xml",
    "Microsoft.Extensions.Primitives.xml",
    "System.Formats.Asn1.xml",
    "System.Linq.AsyncEnumerable.xml",
    "System.Net.ServerSentEvents.xml",
    "System.Reflection.DispatchProxy.xml",
    "System.Text.RegularExpressions.xml");

string netCoreExtractDir = await DownloadAndExtractLatestPackageAsync(httpClient, packageBaseAddress, NetCoreRefPackageId, downloadDir);
string netCoreRefDir = GetRefDirectory(netCoreExtractDir);
string netCoreDestination = Path.Combine(dotnetDir, versionDir);
ClearDirectory(netCoreDestination);
CopyFiles(netCoreRefDir, netCoreDestination, "*.dll", shimDllExclusions);
CopyNamedFiles(netCoreRefDir, netCoreDestination, xmlFilesToCopy);

string windowsDesktopExtractDir = await DownloadAndExtractLatestPackageAsync(httpClient, packageBaseAddress, WindowsDesktopRefPackageId, downloadDir);
string windowsDesktopRefDir = GetRefDirectory(windowsDesktopExtractDir);
string windowsDesktopDestination = Path.Combine(dotnetDir, windowsDesktopDir);
ClearDirectory(windowsDesktopDestination);
CopyFiles(
    windowsDesktopRefDir,
    windowsDesktopDestination,
    "*.dll",
    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.VisualBasic.dll",
        "System.Drawing.dll"
    });

// Copy System.Security.Cryptography.dll to the dependencies directory for WindowsDesktop.
string cryptoSource = Path.Combine(netCoreDestination, "System.Security.Cryptography.dll");
if (!File.Exists(cryptoSource))
{
    throw new FileNotFoundException("Could not find the copied System.Security.Cryptography.dll dependency.", cryptoSource);
}

string dependenciesDestination = Path.Combine(dotnetDir, "dependencies", windowsDesktopDir);
Directory.CreateDirectory(dependenciesDestination);
File.Copy(cryptoSource, Path.Combine(dependenciesDestination, Path.GetFileName(cryptoSource)), overwrite: true);
Console.WriteLine($"Copied {Path.GetFileName(cryptoSource)} to {dependenciesDestination}.");

static async Task<string> GetPackageBaseAddressAsync(HttpClient httpClient)
{
    using JsonDocument serviceIndex = await GetJsonDocumentAsync(httpClient, NuGetIndexUrl);

    foreach (JsonElement resource in serviceIndex.RootElement.GetProperty("resources").EnumerateArray())
    {
        if (!resource.TryGetProperty("@type", out JsonElement typeElement) ||
            !resource.TryGetProperty("@id", out JsonElement idElement))
        {
            continue;
        }

        string? resourceType = typeElement.GetString();
        string? resourceId = idElement.GetString();
        if (resourceType?.Contains("PackageBaseAddress/3.0.0", StringComparison.Ordinal) == true &&
            !string.IsNullOrWhiteSpace(resourceId))
        {
            return resourceId.TrimEnd('/') + "/";
        }
    }

    throw new InvalidOperationException($"Could not find a PackageBaseAddress resource in {NuGetIndexUrl}.");
}

static async Task<HashSet<string>> GetShimDllExclusionsAsync(HttpClient httpClient)
{
    string propsXml = await httpClient.GetStringAsync(ShimReferencesUrl);
    XDocument props = XDocument.Parse(propsXml);

    HashSet<string> exclusions = new(StringComparer.OrdinalIgnoreCase);
    foreach (XElement reference in props.Descendants("NetFxReference"))
    {
        string? include = reference.Attribute("Include")?.Value;
        if (!string.IsNullOrWhiteSpace(include))
        {
            exclusions.Add(include + ".dll");
        }
    }

    if (exclusions.Count == 0)
    {
        throw new InvalidOperationException($"No NetFxReference entries were found in {ShimReferencesUrl}.");
    }

    return exclusions;
}

static async Task<string> DownloadAndExtractLatestPackageAsync(
    HttpClient httpClient,
    string packageBaseAddress,
    string packageId,
    string downloadDir)
{
    string packageIdLower = packageId.ToLowerInvariant();
    string versionsUrl = $"{packageBaseAddress}{packageIdLower}/index.json";
    using JsonDocument versionsDocument = await GetJsonDocumentAsync(httpClient, versionsUrl);

    List<(string Original, NuGetVersion Parsed)> versions = [];
    foreach (JsonElement versionElement in versionsDocument.RootElement.GetProperty("versions").EnumerateArray())
    {
        string? version = versionElement.GetString();
        if (string.IsNullOrWhiteSpace(version))
        {
            continue;
        }

        if (!NuGetVersion.TryParse(version, out NuGetVersion? parsedVersion))
        {
            throw new InvalidOperationException($"The feed returned an invalid NuGet version for {packageId}: {version}");
        }

        versions.Add((version, parsedVersion));
    }

    if (versions.Count == 0)
    {
        throw new InvalidOperationException($"No versions were found for {packageId} at {versionsUrl}.");
    }

    string latestVersion = versions.MaxBy(version => version.Parsed)!.Original;
    string packageDir = Path.Combine(downloadDir, packageIdLower, latestVersion);
    Directory.CreateDirectory(packageDir);

    string packagePath = Path.Combine(packageDir, $"{packageIdLower}.{latestVersion}.nupkg");
    string packageUrl = $"{packageBaseAddress}{packageIdLower}/{latestVersion}/{packageIdLower}.{latestVersion}.nupkg";

    await using (Stream packageStream = await httpClient.GetStreamAsync(packageUrl))
    await using (FileStream packageFile = File.Create(packagePath))
    {
        await packageStream.CopyToAsync(packageFile);
    }

    string extractDir = Path.Combine(packageDir, "extracted");
    ClearDirectory(extractDir);
    ZipFile.ExtractToDirectory(packagePath, extractDir, overwriteFiles: true);

    Console.WriteLine($"Downloaded and extracted {packageId} {latestVersion}.");
    return extractDir;
}

static async Task<JsonDocument> GetJsonDocumentAsync(HttpClient httpClient, string url)
{
    await using Stream stream = await httpClient.GetStreamAsync(url);
    return await JsonDocument.ParseAsync(stream);
}

static string GetRefDirectory(string extractDir)
{
    string refDir = Path.Combine(extractDir, "ref", RefTargetFramework);
    if (!Directory.Exists(refDir))
    {
        throw new DirectoryNotFoundException($"The package does not contain a ref/{RefTargetFramework} directory: {extractDir}");
    }

    return refDir;
}

static void CopyFiles(string sourceDir, string destinationDir, string searchPattern, ISet<string> excludedFileNames)
{
    Directory.CreateDirectory(destinationDir);

    int copiedCount = 0;
    foreach (string sourcePath in Directory.EnumerateFiles(sourceDir, searchPattern, SearchOption.TopDirectoryOnly))
    {
        string fileName = Path.GetFileName(sourcePath);
        if (excludedFileNames.Contains(fileName))
        {
            continue;
        }

        File.Copy(sourcePath, Path.Combine(destinationDir, fileName), overwrite: true);
        copiedCount++;
    }

    Console.WriteLine($"Copied {copiedCount} {searchPattern} files from {sourceDir} to {destinationDir}.");
}

static void CopyNamedFiles(string sourceDir, string destinationDir, ISet<string> fileNamesToCopy)
{
    Directory.CreateDirectory(destinationDir);

    int copiedCount = 0;
    foreach (string fileName in fileNamesToCopy)
    {
        string sourcePath = Path.Combine(sourceDir, fileName);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Could not find an expected XML reference file.", sourcePath);
        }

        File.Copy(sourcePath, Path.Combine(destinationDir, fileName), overwrite: true);
        copiedCount++;
    }

    Console.WriteLine($"Copied {copiedCount} named files from {sourceDir} to {destinationDir}.");
}

static HashSet<string> BuildFileNameSet(params string[] fileNames)
{
    HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
    foreach (string fileName in fileNames)
    {
        names.Add(fileName);
        if (Path.GetExtension(fileName).Length == 0)
        {
            names.Add(fileName + ".xml");
        }
    }

    return names;
}

static void ClearDirectory(string directory)
{
    if (Directory.Exists(directory))
    {
        Directory.Delete(directory, recursive: true);
    }

    Directory.CreateDirectory(directory);
}
