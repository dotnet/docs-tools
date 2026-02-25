using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AssemblyCopier;

internal static class Program
{
    // Excluded DLLs from netfxreference.props
    // (https://github.com/dotnet/runtime/blob/v7.0.0-preview.1.22076.8/src/libraries/shims/netfxreference.props)
    static readonly HashSet<string> s_excludedDlls = new(StringComparer.OrdinalIgnoreCase)
        {
            "mscorlib.dll",
            "Microsoft.VisualBasic.dll",
            "System.dll",
            "System.ComponentModel.DataAnnotations.dll",
            "System.Configuration.dll",
            "System.Core.dll",
            "System.Data.dll",
            "System.Drawing.dll",
            "System.IO.Compression.FileSystem.dll",
            "System.Net.dll",
            "System.Numerics.dll",
            "System.Runtime.Serialization.dll",
            "System.Security.dll",
            "System.ServiceProcess.dll",
            "System.ServiceModel.Web.dll",
            "System.Transactions.dll",
            "System.Web.dll",
            "System.Windows.dll",
            "System.Xml.dll",
            "System.Xml.Serialization.dll",
            "System.Xml.Linq.dll"
        };

    private static async Task<int> Main(string[] args)
    {
#if DEBUG
        args = [@"c:\users\gewarren\binaries\dotnet", "10.0"];
#endif

        if (args.Length != 2)
        {
            string exeName = Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location);
            Console.Error.Write("Error: Incorrect number of arguments");
            Console.Error.Write($"\nUsage: {exeName} <dotnet-binaries-directory> <version>");
            return -1;
        }

        string rootPath = args[0];
        string version = args[1];

        bool success = true;

        try
        {
            await RunAsync(rootPath, version);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            success = false;
        }

        return success ? 0 : -1;
    }

    private static async Task RunAsync(string rootPath, string version)
    {
        await CopyNETCoreAppDlls(rootPath, version);
        await CopyWindowsDesktopAppDlls(rootPath, version);
        CopyCryptographyDllToDependencies(rootPath, version);
    }

    private static async Task CopyNETCoreAppDlls(string rootPath, string version)
    {
        // XML files to copy
        string[] xmlFilesToCopy =
        {
            "System.Formats.Asn1.xml",
            "System.Linq.AsyncEnumerable.xml",
            "System.Net.ServerSentEvents.xml",
            "System.Reflection.DispatchProxy.xml"
        };

        await CopyDlls(
            rootPath,
            "net",
            "Microsoft.NETCore.App.Ref",
            version,
            "WindowsBase.dll",
            xmlFilesToCopy);
    }

    private static async Task CopyWindowsDesktopAppDlls(string rootPath, string version)
    {
        await CopyDlls(
            rootPath,
            "windowsdesktop",
            "Microsoft.WindowsDesktop.App.Ref",
            version);
    }

    private static async Task CopyDlls(
        string rootPath,
        string destDirRootName,
        string packageName,
        string version,
        string addlDllToExclude = "",
        string[]? xmlFilesToCopy = null)
    {
        Console.WriteLine($"Searching for {packageName} package version {version}.*...");

        string lowerCasePackageName = packageName.ToLowerInvariant();

        // Download the NuGet package.
        using HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AssemblyCopier/1.0");

        // Query NuGet API for the package versions.
        string nugetApiUrl = $"https://api.nuget.org/v3-flatcontainer/{lowerCasePackageName}/index.json";
        NuGetVersionsResponse? versionResponse = await client.GetFromJsonAsync<NuGetVersionsResponse>(nugetApiUrl);

        if (versionResponse?.Versions == null || versionResponse.Versions.Length == 0)
        {
            throw new Exception("Failed to retrieve package versions from NuGet.");
        }

        // Find the latest version matching the major version.
        string? latestVersion = versionResponse.Versions
            .Where(v => v.StartsWith($"{version}."))
            .OrderByDescending(v => v)
            .First(); // Throws InvalidOperationException if none found.
        Console.WriteLine($"Found version: {latestVersion}");

        // Download the package.
        string packageUrl = $"https://api.nuget.org/v3-flatcontainer/{lowerCasePackageName}/{latestVersion}/{lowerCasePackageName}.{latestVersion}.nupkg";
        Console.WriteLine($"Downloading package from {packageUrl}...");

        byte[] packageData = await client.GetByteArrayAsync(packageUrl);

        // Create temporary directory for extraction.
        string tempDir = Path.Combine(Path.GetTempPath(), $"{packageName}-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Save and extract the package (it's a ZIP file).
            string packagePath = Path.Combine(tempDir, "package.zip");
            await File.WriteAllBytesAsync(packagePath, packageData);

            Console.WriteLine("Extracting package...");
            ZipFile.ExtractToDirectory(packagePath, tempDir);

            // Find the ref folder with the version.
            string refFolder = Path.Combine(tempDir, "ref", $"net{version}");
            if (!Directory.Exists(refFolder))
            {
                throw new Exception($"Reference folder not found: {refFolder}");
            }

            Console.WriteLine($"Found reference folder: {refFolder}");

            // Prepare destination directory.
            string destinationDir = Path.Combine(rootPath, $"{destDirRootName}-{version}");

            if (Directory.Exists(destinationDir))
            {
                Console.WriteLine($"Cleaning destination directory: {destinationDir}");
                Directory.Delete(destinationDir, true);
            }

            Directory.CreateDirectory(destinationDir);
            Console.WriteLine($"Created destination directory: {destinationDir}");

            // Copy DLL files (excluding the ones in the exclusion list).
            string[] allDlls = Directory.GetFiles(refFolder, "*.dll");
            int copiedCount = 0;
            int excludedCount = 0;

            foreach (string dllPath in allDlls)
            {
                string fileName = Path.GetFileName(dllPath);

                if (s_excludedDlls.Contains(fileName) || (string.Compare(fileName, addlDllToExclude, true) == 0))
                {
                    Console.WriteLine($"Excluding: {fileName}");
                    excludedCount++;
                    continue;
                }

                string destPath = Path.Combine(destinationDir, fileName);
                File.Copy(dllPath, destPath, true);
                copiedCount++;
            }

            Console.WriteLine($"Copied {copiedCount} DLL files (excluded {excludedCount})");

            if (xmlFilesToCopy != null)
            {
                int xmlCopiedCount = 0;
                foreach (string xmlFile in xmlFilesToCopy)
                {
                    string xmlSourcePath = Path.Combine(refFolder, xmlFile);
                    if (File.Exists(xmlSourcePath))
                    {
                        string xmlDestPath = Path.Combine(destinationDir, xmlFile);
                        File.Copy(xmlSourcePath, xmlDestPath, true);
                        Console.WriteLine($"Copied XML file: {xmlFile}");
                        xmlCopiedCount++;
                    }
                    else
                    {
                        Console.WriteLine($"Warning: XML file not found: {xmlFile}");
                    }
                }

                Console.WriteLine($"Copied {xmlCopiedCount} XML files");
            }

            Console.WriteLine("Operation completed successfully!");
        }
        finally
        {
            // Clean up temporary directory.
            Directory.Delete(tempDir, true);
        }
    }

    private static void CopyCryptographyDllToDependencies(string rootPath, string version)
    {
        // Copy System.Security.Cryptography.dll from /<rootPath>/net-<version>
        // to /<rootPath>/dependencies/windowsdesktop-<version> (overwrite old copy if necessary).
        string sourceFile = Path.Combine(rootPath, $"net-{version}", "System.Security.Cryptography.dll");
        string destDir = Path.Combine(rootPath, "dependencies", $"windowsdesktop-{version}");
        string destFile = Path.Combine(destDir, "System.Security.Cryptography.dll");

        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException($"Source file not found: {sourceFile}");
        }

        Directory.CreateDirectory(destDir);
        File.Copy(sourceFile, destFile, overwrite: true);
        Console.WriteLine($"Copied System.Security.Cryptography.dll to {destDir}");
    }

    private record NuGetVersionsResponse
    {
        [JsonPropertyName("versions")]
        public string[] Versions { get; init; } = [];
    }
}

