using System.Collections.Frozen;

namespace PackageIndexer;

internal static class PlatformPackageDefinition
{
    private static FrozenSet<string> s_packageIds;

    public static readonly List<string> otherPackagesWithoutDocs =
    [
        // See https://github.com/dotnet/dotnet-api-docs/pull/10395#discussion_r1758128787.
        "Microsoft.Extensions.Diagnostics.ResourceMonitoring",
        // From WinForms and doesn't use compiler-generated XML docs.
        "System.Drawing.Common",
        // WCF
        "System.ServiceModel.Federation"
    ];

    public static readonly List<string> runtimePackagesWithoutDocs =
    [
        "Microsoft.Extensions.DependencyModel",
        "Microsoft.Extensions.FileSystemGlobbing",
        "Microsoft.NETCore.Platforms",
        "Microsoft.Win32.Registry.AccessControl",
        "Microsoft.Win32.SystemEvents",
        "System.CodeDom",
        "System.Collections.Immutable",
        "System.ComponentModel.Composition",
        "System.ComponentModel.Composition.Registration",
        "System.Composition.AttributedModel",
        "System.Composition.Convention",
        "System.Composition.Hosting",
        "System.Composition.Runtime",
        "System.Composition.TypedParts",
        "System.Configuration.ConfigurationManager",
        "System.Data.Odbc",
        "System.Data.OleDb",
        "System.Diagnostics.DiagnosticSource",
        "System.Diagnostics.EventLog",
        "System.Diagnostics.PerformanceCounter",
        "System.DirectoryServices.AccountManagement",
        "System.DirectoryServices",
        "System.DirectoryServices.Protocols",
        "System.IO.Hashing",
        "System.IO.Packaging",
        "System.IO.Pipelines",
        "System.IO.Ports",
        "System.Management",
        "System.Memory.Data",
        "System.Net.Http.Json",
        "System.Net.Http.WinHttpHandler",
        "System.Reflection.Context",
        "System.Reflection.Metadata",
        "System.Reflection.MetadataLoadContext",
        "System.Resources.Extensions",
        "System.Runtime.Caching",
        "System.Runtime.Serialization.Formatters",
        "System.Security.Cryptography.Cose",
        "System.Security.Cryptography.Pkcs",
        "System.Security.Cryptography.ProtectedData",
        "System.Security.Cryptography.Xml",
        "System.Security.Permissions",
        "System.ServiceModel.Syndication",
        "System.ServiceProcess.ServiceController",
        "System.Speech",
        "System.Text.Encoding.CodePages",
        "System.Text.Encodings.Web",
        "System.Text.Json",
        "System.Threading.AccessControl",
        "System.Threading.Channels",
        "System.Threading.Tasks.Dataflow",
        "System.Windows.Extensions"
    ];

    public static FrozenSet<string> Owners = FrozenSet.ToFrozenSet(
    [
        "aspnet",
        "dotnetframework"
    ], StringComparer.OrdinalIgnoreCase);

    // Repository URLs to exclude
    public static readonly FrozenSet<string> ExcludedRepositories = FrozenSet.ToFrozenSet(
    [
        "https://github.com/dotnet/maintenance-packages"
    ], StringComparer.OrdinalIgnoreCase);

    public static PackageFilter Filter { get; } = new(
        includes:
        [
            PackageFilterExpression.Parse("Microsoft.Bcl.*"),
            PackageFilterExpression.Parse("Microsoft.Extensions.*"),
            PackageFilterExpression.Parse("Microsoft.IO.Redist"),
            PackageFilterExpression.Parse("Microsoft.Win32.*"),
            PackageFilterExpression.Parse("System.*")
        ],
        excludes:
        [
            PackageFilterExpression.Parse("System.Private.ServiceModel"),
            PackageFilterExpression.Parse("System.Runtime.WindowsRuntime"),
            PackageFilterExpression.Parse("System.Runtime.WindowsRuntime.UI.Xaml"),
            // Documented under Azure SDK for .NET moniker.
            PackageFilterExpression.Parse("System.ClientModel"),
            // Documented under ASP.NET Core moniker.
            PackageFilterExpression.Parse("System.Threading.RateLimiting"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Caching.Hybrid"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Features"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Identity.Core"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Identity.Stores"),
            // Documented under ML.NET moniker.
            PackageFilterExpression.Parse("Microsoft.Extensions.ML"),
            // Documented under .NET Aspire moniker.
            PackageFilterExpression.Parse("Microsoft.Extensions.ServiceDiscovery*"),
            // Old R9 packages.
            PackageFilterExpression.Parse("Microsoft.Extensions.DependencyInjection.NamedService"),
            PackageFilterExpression.Parse("Microsoft.Extensions.DependencyInjection.Pools"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Diagnostics.HealthChecks.Core"),
            PackageFilterExpression.Parse("Microsoft.Extensions.EnumStrings"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Hosting.Testing.StartupInitialization"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Http.AutoClient"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Http.Telemetry"),
            PackageFilterExpression.Parse("Microsoft.Extensions.HttpClient.SocketHandling"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Options.ValidateOnStart"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Options.Validation"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Telemetry.Console"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Telemetry.Testing"),
            PackageFilterExpression.Parse("System.Cloud.DocumentDb.Abstractions"),
            PackageFilterExpression.Parse("System.Cloud.Messaging"),
            PackageFilterExpression.Parse("System.Cloud.Messaging.Abstractions"),
            // Test APIs.
            PackageFilterExpression.Parse("Microsoft.Extensions.DependencyInjection.Specification.Tests"),
            // No longer built in runtime repo.
            PackageFilterExpression.Parse("System.Reflection.Emit"),
            PackageFilterExpression.Parse("System.ComponentModel.Annotations"),
            PackageFilterExpression.Parse("System.Data.DataSetExtensions"),
            PackageFilterExpression.Parse("System.Security.AccessControl"),
            PackageFilterExpression.Parse("System.Security.Cryptography.Cng"),
            PackageFilterExpression.Parse("System.Security.Principal.Windows"),
            // Maintenance packages.
            PackageFilterExpression.Parse("Microsoft.IO.Redist"),
            PackageFilterExpression.Parse("System.Buffers"),
            PackageFilterExpression.Parse("System.Data.SqlClient"),
            PackageFilterExpression.Parse("System.Json"),
            PackageFilterExpression.Parse("System.Memory"),
            PackageFilterExpression.Parse("System.Net.WebSockets.WebSocketProtocol"),
            PackageFilterExpression.Parse("System.Numerics.Vectors"),
            PackageFilterExpression.Parse("System.Reflection.DispatchProxy"),
            PackageFilterExpression.Parse("System.Runtime.CompilerServices.Unsafe"),
            PackageFilterExpression.Parse("System.Threading.Tasks.Extensions"),
            PackageFilterExpression.Parse("System.ValueTuple"),
            PackageFilterExpression.Parse("System.Xml.XPath.XmlDocument"),
            // Suffixes.
            PackageFilterExpression.Parse("*.cs"),
            PackageFilterExpression.Parse("*.de"),
            PackageFilterExpression.Parse("*.es"),
            PackageFilterExpression.Parse("*.fr"),
            PackageFilterExpression.Parse("*.it"),
            PackageFilterExpression.Parse("*.ja"),
            PackageFilterExpression.Parse("*.ko"),
            PackageFilterExpression.Parse("*.pl"),
            PackageFilterExpression.Parse("*.pt-br"),
            PackageFilterExpression.Parse("*.ru"),
            PackageFilterExpression.Parse("*.tr"),
            PackageFilterExpression.Parse("*.zh-Hans"),
            PackageFilterExpression.Parse("*.zh-Hant"),
        ]
    );

    public static bool IsRepositoryExcluded(string? repositoryUrl)
    {
        // Exclude package if the repository URL is missing,
        // or if the repo is in the excluded list.
        return (string.IsNullOrWhiteSpace(repositoryUrl) ||
               ExcludedRepositories.Contains(repositoryUrl));
    }
}
