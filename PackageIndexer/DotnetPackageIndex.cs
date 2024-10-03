using System.Collections.Concurrent;
using System.Xml.Linq;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace PackageIndexer;

public static class DotnetPackageIndex
{
    private static readonly string[] s_dotnetPlatformOwners = [
        "aspnet",
        "dotnetframework"
    ];

    public static async Task CreateAsync(string packageListPath, bool usePreviewVersions)
    {
        // TODO - use nightly feed??
        IReadOnlyList<PackageIdentity> packages =
            await GetPackagesAsync(usePreviewVersions, NuGetFeeds.NuGetOrg); //, NuGetFeeds.NightlyLatest);

        var packageDocument = new XDocument();
        var root = new XElement("packages");
        packageDocument.Add(root);

        foreach (PackageIdentity item in packages)
        {
            var e = new XElement("package",
                new XAttribute("id", item.Id),
                new XAttribute("version", item.Version)
            );

            root.Add(e);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(packageListPath));
        packageDocument.Save(packageListPath);
    }

    private static async Task<IReadOnlyList<PackageIdentity>> GetPackagesAsync(
        bool usePreviewVersions,
        params string[] feedUrls
        )
    {
        var packages = new List<(PackageIdentity, bool)>();

        foreach (string feedUrl in feedUrls)
        {
            var feed = new NuGetFeed(feedUrl);
            IReadOnlyList<(PackageIdentity, bool)> feedPackages = await GetPackagesAsync(feed);
            packages.AddRange(feedPackages);
        }

        Console.WriteLine($"Found {packages.Count:N0} package versions across {feedUrls.Length} feeds.");

        IReadOnlyList<PackageIdentity> latestVersions = GetLatestVersions(packages, usePreviewVersions);

        Console.WriteLine($"Found {latestVersions.Count:N0} latest package versions.");

        return latestVersions;
    }

    private static async Task<IReadOnlyList<(PackageIdentity, bool)>> GetPackagesAsync(NuGetFeed feed)
    {
        Console.WriteLine($"Getting packages from {feed.FeedUrl}...");

        if (feed.FeedUrl == NuGetFeeds.NuGetOrg)
            return await GetPackagesFromNuGetOrgAsync(feed);
        else
            //return await GetPackagesFromOtherGalleryAsync(feed);
            throw new ApplicationException("NuGetOrg should be the only feed.");
    }

    private static async Task<IReadOnlyList<(PackageIdentity, bool)>> GetPackagesFromNuGetOrgAsync(NuGetFeed feed)
    {
        Console.WriteLine("Fetching owner information...");

        // TODO
        // 9/11/24 - Commented out until I can get owner info from Kusto.
        //Dictionary<string, string[]> ownerInformation = await feed.GetOwnerMappingAsync();

        //string[] packageIds = ownerInformation.Keys
        //    .ToHashSet(StringComparer.OrdinalIgnoreCase)
        //    .Where(id => IsOwnedByDotNet(ownerInformation, id) &&
        //                 PackageFilter.Default.IsMatch(id))
        //    .ToArray();

        // Temporarily hard-code the .NET package IDs.
        string[] packageIds = [
            "Microsoft.Bcl.AsyncInterfaces",
            "Microsoft.Bcl.Build",
            "Microsoft.Bcl.Cryptography",
            "Microsoft.Bcl.HashCode",
            "Microsoft.Bcl.Numerics",
            "Microsoft.Bcl.TimeProvider",
            "Microsoft.Extensions.AmbientMetadata.Application",
            "Microsoft.Extensions.ApiDescription.Client",
            "Microsoft.Extensions.ApiDescription.Server",
            "Microsoft.Extensions.AsyncState",
            "Microsoft.Extensions.AuditReports",
            "Microsoft.Extensions.Caching.Abstractions",
            "Microsoft.Extensions.Caching.Hybrid",
            "Microsoft.Extensions.Caching.Memory",
            "Microsoft.Extensions.Caching.SqlServer",
            "Microsoft.Extensions.Caching.StackExchangeRedis",
            "Microsoft.Extensions.Compliance.Abstractions",
            "Microsoft.Extensions.Compliance.Redaction",
            "Microsoft.Extensions.Compliance.Testing",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.Configuration.Abstractions",
            "Microsoft.Extensions.Configuration.Binder",
            "Microsoft.Extensions.Configuration.CommandLine",
            "Microsoft.Extensions.Configuration.EnvironmentVariables",
            "Microsoft.Extensions.Configuration.FileExtensions",
            "Microsoft.Extensions.Configuration.Ini",
            "Microsoft.Extensions.Configuration.Json",
            "Microsoft.Extensions.Configuration.KeyPerFile",
            "Microsoft.Extensions.Configuration.UserSecrets",
            "Microsoft.Extensions.Configuration.Xml",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.DependencyInjection.AutoActivation",
            "Microsoft.Extensions.DependencyInjection.Specification.Tests",
            "Microsoft.Extensions.DependencyModel",
            "Microsoft.Extensions.DiagnosticAdapter",
            "Microsoft.Extensions.Diagnostics",
            "Microsoft.Extensions.Diagnostics.Abstractions",
            "Microsoft.Extensions.Diagnostics.ExceptionSummarization",
            "Microsoft.Extensions.Diagnostics.HealthChecks",
            "Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions",
            "Microsoft.Extensions.Diagnostics.HealthChecks.Common",
            "Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore",
            "Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilization",
            "Microsoft.Extensions.Diagnostics.Probes",
            "Microsoft.Extensions.Diagnostics.ResourceMonitoring",
            "Microsoft.Extensions.Diagnostics.Testing",
            "Microsoft.Extensions.FileProviders.Abstractions",
            "Microsoft.Extensions.FileProviders.Composite",
            "Microsoft.Extensions.FileProviders.Embedded",
            "Microsoft.Extensions.FileProviders.Physical",
            "Microsoft.Extensions.FileSystemGlobbing",
            "Microsoft.Extensions.Hosting",
            "Microsoft.Extensions.Hosting.Abstractions",
            "Microsoft.Extensions.Hosting.Systemd",
            "Microsoft.Extensions.Hosting.Testing",
            "Microsoft.Extensions.Hosting.WindowsServices",
            "Microsoft.Extensions.Http",
            "Microsoft.Extensions.Http.Diagnostics",
            "Microsoft.Extensions.Http.Polly",
            "Microsoft.Extensions.Http.Resilience",
            "Microsoft.Extensions.Localization",
            "Microsoft.Extensions.Localization.Abstractions",
            "Microsoft.Extensions.Logging",
            "Microsoft.Extensions.Logging.Abstractions",
            "Microsoft.Extensions.Logging.AzureAppServices",
            "Microsoft.Extensions.Logging.Configuration",
            "Microsoft.Extensions.Logging.Console",
            "Microsoft.Extensions.Logging.Debug",
            "Microsoft.Extensions.Logging.EventLog",
            "Microsoft.Extensions.Logging.EventSource",
            "Microsoft.Extensions.Logging.TraceSource",
            "Microsoft.Extensions.ObjectPool",
            "Microsoft.Extensions.ObjectPool.DependencyInjection",
            "Microsoft.Extensions.Options",
            "Microsoft.Extensions.Options.ConfigurationExtensions",
            "Microsoft.Extensions.Options.Contextual",
            "Microsoft.Extensions.Options.DataAnnotations",
            "Microsoft.Extensions.Primitives",
            "Microsoft.Extensions.Resilience",
            "Microsoft.Extensions.StaticAnalysis",
            "Microsoft.Extensions.Telemetry",
            "Microsoft.Extensions.Telemetry.Abstractions",
            "Microsoft.Extensions.TimeProvider.Testing",
            "Microsoft.Extensions.WebEncoders",
            "Microsoft.Win32.Primitives",
            "Microsoft.Win32.Registry",
            "Microsoft.Win32.Registry.AccessControl",
            "Microsoft.Win32.SystemEvents",
            "System.AppContext",
            "System.Buffers",
            "System.CodeDom",
            "System.Collections",
            "System.Collections.Concurrent",
            "System.Collections.Immutable",
            "System.Collections.NonGeneric",
            "System.Collections.Specialized",
            "System.ComponentModel",
            "System.ComponentModel.Annotations",
            "System.ComponentModel.Composition",
            "System.ComponentModel.Composition.Registration",
            "System.ComponentModel.EventBasedAsync",
            "System.ComponentModel.Primitives",
            "System.ComponentModel.TypeConverter",
            "System.Composition",
            "System.Composition.AttributedModel",
            "System.Composition.Convention",
            "System.Composition.Hosting",
            "System.Composition.Runtime",
            "System.Composition.TypedParts",
            "System.Configuration.ConfigurationManager",
            "System.Console",
            "System.Data.Common",
            "System.Data.DataSetExtensions",
            "System.Data.Odbc",
            "System.Data.OleDb",
            "System.Data.SqlClient",
            "System.Diagnostics.Contracts",
            "System.Diagnostics.Debug",
            "System.Diagnostics.DiagnosticSource",
            "System.Diagnostics.EventLog",
            "System.Diagnostics.FileVersionInfo",
            "System.Diagnostics.PerformanceCounter",
            "System.Diagnostics.Process",
            "System.Diagnostics.StackTrace",
            "System.Diagnostics.TextWriterTraceListener",
            "System.Diagnostics.Tools",
            "System.Diagnostics.TraceSource",
            "System.Diagnostics.Tracing",
            "System.DirectoryServices",
            "System.DirectoryServices.AccountManagement",
            "System.DirectoryServices.Protocols",
            "System.Drawing.Common",
            "System.Drawing.Primitives",
            "System.Dynamic.Runtime",
            "System.Formats.Asn1",
            "System.Formats.Cbor",
            "System.Formats.Nrbf",
            "System.Globalization",
            "System.Globalization.Calendars",
            "System.Globalization.Extensions",
            "System.IO",
            "System.IO.Compression",
            "System.IO.Compression.clrcompression-arm",
            "System.IO.Compression.clrcompression-x64",
            "System.IO.Compression.clrcompression-x86",
            "System.IO.Compression.ZipFile",
            "System.IO.FileSystem",
            "System.IO.FileSystem.AccessControl",
            "System.IO.FileSystem.DriveInfo",
            "System.IO.FileSystem.Primitives",
            "System.IO.FileSystem.Watcher",
            "System.IO.Hashing",
            "System.IO.IsolatedStorage",
            "System.IO.MemoryMappedFiles",
            "System.IO.Packaging",
            "System.IO.Pipelines",
            "System.IO.Pipes",
            "System.IO.Pipes.AccessControl",
            "System.IO.Ports",
            "System.IO.UnmanagedMemoryStream",
            "System.Json",
            "System.Linq",
            "System.Linq.Expressions",
            "System.Linq.Parallel",
            "System.Linq.Queryable",
            "System.Management",
            "System.Memory",
            "System.Memory.Data",
            "System.Net.Http",
            "System.Net.Http.Json",
            "System.Net.Http.Rtc",
            "System.Net.Http.WinHttpHandler",
            "System.Net.NameResolution",
            "System.Net.NetworkInformation",
            "System.Net.Ping",
            "System.Net.Primitives",
            "System.Net.Requests",
            "System.Net.Security",
            "System.Net.ServerSentEvents",
            "System.Net.Sockets",
            "System.Net.WebHeaderCollection",
            "System.Net.WebSockets",
            "System.Net.WebSockets.Client",
            "System.Net.WebSockets.WebSocketProtocol",
            "System.Numerics.Tensors",
            "System.Numerics.Vectors",
            "System.Numerics.Vectors.WindowsRuntime",
            "System.ObjectModel",
            "System.Private.DataContractSerialization",
            "System.Private.Networking",
            "System.Private.Uri",
            "System.Reflection",
            "System.Reflection.Context",
            "System.Reflection.DispatchProxy",
            "System.Reflection.Emit",
            "System.Reflection.Emit.ILGeneration",
            "System.Reflection.Emit.Lightweight",
            "System.Reflection.Extensions",
            "System.Reflection.Metadata",
            "System.Reflection.MetadataLoadContext",
            "System.Reflection.Primitives",
            "System.Reflection.TypeExtensions",
            "System.Resources.Extensions",
            "System.Resources.Reader",
            "System.Resources.ResourceManager",
            "System.Resources.Writer",
            "System.Runtime",
            "System.Runtime.Caching",
            "System.Runtime.CompilerServices.Unsafe",
            "System.Runtime.CompilerServices.VisualC",
            "System.Runtime.Experimental",
            "System.Runtime.Extensions",
            "System.Runtime.Handles",
            "System.Runtime.InteropServices",
            "System.Runtime.InteropServices.NFloat.Internal",
            "System.Runtime.InteropServices.RuntimeInformation",
            "System.Runtime.InteropServices.WindowsRuntime",
            "System.Runtime.Loader",
            "System.Runtime.Numerics",
            "System.Runtime.Serialization.Formatters",
            "System.Runtime.Serialization.Json",
            "System.Runtime.Serialization.Primitives",
            "System.Runtime.Serialization.Schema",
            "System.Runtime.Serialization.Xml",
            "System.Security.AccessControl",
            "System.Security.Claims",
            "System.Security.Cryptography.Algorithms",
            "System.Security.Cryptography.Cng",
            "System.Security.Cryptography.Cose",
            "System.Security.Cryptography.Csp",
            "System.Security.Cryptography.Encoding",
            "System.Security.Cryptography.OpenSsl",
            "System.Security.Cryptography.Pkcs",
            "System.Security.Cryptography.Primitives",
            "System.Security.Cryptography.ProtectedData",
            "System.Security.Cryptography.X509Certificates",
            "System.Security.Cryptography.Xml",
            "System.Security.Permissions",
            "System.Security.Principal",
            "System.Security.Principal.Windows",
            "System.Security.SecureString",
            "System.ServiceModel.Duplex",
            "System.ServiceModel.Federation",
            "System.ServiceModel.Http",
            "System.ServiceModel.NetFramingBase",
            "System.ServiceModel.NetNamedPipe",
            "System.ServiceModel.NetTcp",
            "System.ServiceModel.Primitives",
            "System.ServiceModel.Security",
            "System.ServiceModel.Syndication",
            "System.ServiceModel.UnixDomainSocket",
            "System.ServiceProcess.ServiceController",
            "System.Speech",
            "System.Text.Encoding",
            "System.Text.Encoding.CodePages",
            "System.Text.Encoding.Extensions",
            "System.Text.Encodings.Web",
            "System.Text.Json",
            "System.Text.RegularExpressions",
            "System.Threading",
            "System.Threading.AccessControl",
            "System.Threading.Channels",
            "System.Threading.Overlapped",
            "System.Threading.Tasks",
            "System.Threading.Tasks.Dataflow",
            "System.Threading.Tasks.Extensions",
            "System.Threading.Tasks.Parallel",
            "System.Threading.Thread",
            "System.Threading.ThreadPool",
            "System.Threading.Timer",
            "System.ValueTuple",
            "System.Web.Http.Common",
            "System.Web.Services.Description",
            "System.Windows.Extensions",
            "System.Xml.ReaderWriter",
            "System.Xml.XDocument",
            "System.Xml.XmlDocument",
            "System.Xml.XmlSerializer",
            "System.Xml.XPath",
            "System.Xml.XPath.XDocument",
            "System.Xml.XPath.XmlDocument"
        ];

        Console.WriteLine($"Found {packageIds.Length:N0} package IDs owned by .NET.");

        Console.WriteLine("Getting versions...");

        ConcurrentBag<(PackageIdentity, bool)> identities = [];

        await Parallel.ForEachAsync(packageIds, async (packageId, _) =>
        {
            IReadOnlyList<(NuGetVersion, bool)> versions = await feed.GetAllVersionsAsync(packageId);

            foreach ((NuGetVersion version, bool isDeprecated) version in versions)
            {
                var identity = new PackageIdentity(packageId, version.version);
                identities.Add((identity, version.isDeprecated));
            }
        });

        Console.WriteLine($"Found {identities.Count:N0} package versions.");

        return identities.ToArray();
    }

    private static IReadOnlyList<PackageIdentity> GetLatestVersions(
        IReadOnlyList<(PackageIdentity packageId, bool isDeprecated)> identities,
        bool usePreviewVersions
        )
    {
        var result = new List<PackageIdentity>();

        IEnumerable<IGrouping<string, (PackageIdentity, bool)>> groups =
            identities.GroupBy(i => i.packageId.Id).OrderBy(g => g.Key);

        foreach (IGrouping<string, (PackageIdentity packageId, bool isDeprecated)> group in groups)
        {
            string packageId = group.Key;
            IOrderedEnumerable<(PackageIdentity packageId, bool isDeprecated)> versions =
                group.OrderByDescending(p => p.packageId.Version, VersionComparer.VersionReleaseMetadata);

            (PackageIdentity packageId, bool isDeprecated) latestStable =
                versions.FirstOrDefault(i => !i.packageId.Version.IsPrerelease);
            (PackageIdentity packageId, bool isDeprecated) latestPrerelease =
                versions.FirstOrDefault(i => i.packageId.Version.IsPrerelease);

            // If the latest version is deprecated, don't include any version.
            if (latestStable.isDeprecated)
                latestStable = default;
            if (latestPrerelease.isDeprecated)
                latestPrerelease = default;

            // If the latest stable version is newer than the latest
            // prerelease version, don't include the prerelease version.
            if (latestStable != default && latestPrerelease != default)
            {
                bool stableIsNewer = VersionComparer.VersionReleaseMetadata.Compare(
                    latestPrerelease.packageId.Version,
                    latestStable.packageId.Version
                    ) <= 0;
                if (stableIsNewer)
                    latestPrerelease = default;
            }

            // Comment this out for preview-only versions.
            if (latestStable != default)
            {
                result.Add(latestStable.packageId);
            }

            if (usePreviewVersions)
            {
                // Make sure it's a .NET 9 preview version.
                if ((latestPrerelease != default) && latestPrerelease.packageId.Version.Major == 9)
                    result.Add(latestPrerelease.packageId);
            }
        }

        return result;
    }

    private static bool IsOwnedByDotNet(Dictionary<string, string[]> ownerInformation, string id)
    {
        if (ownerInformation.TryGetValue(id, out string[] owners))
        {
            foreach (string owner in owners)
            {
                foreach (string platformOwner in s_dotnetPlatformOwners)
                {
                    if (string.Equals(owner, platformOwner, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }

        return false;
    }
}
