using System.Collections.Frozen;

namespace PackageIndexer;

internal static class PlatformPackageDefinition
{
    private static FrozenSet<string> s_packageIds;

    public static readonly List<string> packagesWithoutDocs =
    [
        "Microsoft.Extensions.DependencyModel",
        "Microsoft.Extensions.FileSystemGlobbing",
        "Microsoft.NETCore.Platforms",
        "Microsoft.Win32.Registry.AccessControl",
        "Microsoft.Win32.SystemEvents",
        "System.CodeDom",
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
        "System.Diagnostics.EventLog",
        "System.Diagnostics.PerformanceCounter",
        "System.DirectoryServices",
        "System.DirectoryServices.AccountManagement",
        "System.DirectoryServices.Protocols",
        "System.IO.Hashing",
        "System.IO.Packaging",
        "System.IO.Ports",
        "System.Management",
        "System.Memory.Data",
        "System.Net.Http.WinHttpHandler",
        "System.Reflection.Context",
        "System.Reflection.MetadataLoadContext",
        "System.Resources.Extensions",
        "System.Runtime.Caching",
        "System.Security.Cryptography.Cose",
        "System.Security.Cryptography.Pkcs",
        "System.Security.Cryptography.ProtectedData",
        "System.Security.Cryptography.Xml",
        "System.Security.Permissions",
        "System.ServiceModel.Syndication",
        "System.ServiceProcess.ServiceController",
        "System.Speech",
        "System.Windows.Extensions"
    ];

    public static FrozenSet<string> Owners = FrozenSet.ToFrozenSet(
    [
        "aspnet",
        "dotnetframework"
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

    // For ASP.NET, include these packages:
    //        PackageFilterExpression.Parse("Microsoft.AspNetCore*"),
    //        PackageFilterExpression.Parse("Microsoft.Authentication.WebAssembly.Msal"),
    //        PackageFilterExpression.Parse("Microsoft.JSInterop*"),
    //        PackageFilterExpression.Parse("Microsoft.Net.Http.Headers"),
    //        PackageFilterExpression.Parse("Microsoft.Extensions.ApiDescription.Server"),
    //        PackageFilterExpression.Parse("Microsoft.Extensions.Features"),
    //        PackageFilterExpression.Parse("Microsoft.Extensions.Identity.Core"),
    //        PackageFilterExpression.Parse("Microsoft.Extensions.Identity.Stores")
}
