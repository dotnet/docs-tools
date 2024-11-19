using System.Collections.Frozen;

namespace PackageIndexer;

internal static class PlatformPackageDefinition
{
    private static FrozenSet<string> s_packageIds;

    public static readonly List<string> PackagesWithTruthDocs =
    [
        // Don't include docs from Microsoft.Bcl* packages.
        // For example, Ms.Bcl.Memory includes the System types Index and Range,
        // and some very bad docs for these types. (Convo with Carlos 11/18/24.)
        //"Microsoft.Bcl.AsyncInterfaces",
        //"Microsoft.Bcl.Cryptography",
        //"Microsoft.Bcl.Memory",
        //"Microsoft.Bcl.Numerics",
        //"Microsoft.Bcl.TimeProvider",
        "Microsoft.Extensions.Caching.Abstractions",
        "Microsoft.Extensions.Caching.Memory",
        "Microsoft.Extensions.Configuration",
        "Microsoft.Extensions.Configuration.Abstractions",
        "Microsoft.Extensions.Configuration.Binder",
        "Microsoft.Extensions.Configuration.CommandLine",
        "Microsoft.Extensions.Configuration.EnvironmentVariables",
        "Microsoft.Extensions.Configuration.FileExtensions",
        "Microsoft.Extensions.Configuration.Ini",
        "Microsoft.Extensions.Configuration.Json",
        "Microsoft.Extensions.Configuration.UserSecrets",
        "Microsoft.Extensions.Configuration.Xml",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.DependencyInjection.Specification.Tests",
        "Microsoft.Extensions.Diagnostics",
        "Microsoft.Extensions.Diagnostics.Abstractions",
        "Microsoft.Extensions.FileProviders.Abstractions",
        "Microsoft.Extensions.FileProviders.Composite",
        "Microsoft.Extensions.FileProviders.Physical",
        "Microsoft.Extensions.HostFactoryResolver.Sources",
        "Microsoft.Extensions.Hosting",
        "Microsoft.Extensions.Hosting.Abstractions",
        "Microsoft.Extensions.Hosting.Systemd",
        "Microsoft.Extensions.Hosting.WindowsServices",
        "Microsoft.Extensions.Http",
        "Microsoft.Extensions.Logging",
        "Microsoft.Extensions.Logging.Abstractions",
        "Microsoft.Extensions.Logging.Configuration",
        "Microsoft.Extensions.Logging.Console",
        "Microsoft.Extensions.Logging.Debug",
        "Microsoft.Extensions.Logging.EventLog",
        "Microsoft.Extensions.Logging.EventSource",
        "Microsoft.Extensions.Logging.TraceSource",
        "Microsoft.Extensions.Options",
        "Microsoft.Extensions.Options.ConfigurationExtensions",
        "Microsoft.Extensions.Options.DataAnnotations",
        "Microsoft.Extensions.Primitives",
        "System.Composition",
        "System.Diagnostics.EventLog.Messages",
        "System.Formats.Asn1",
        "System.Formats.Cbor",
        "System.Formats.Nrbf",
        "System.Net.ServerSentEvents",
        "System.Numerics.Tensors",
        "System.Runtime.Serialization.Schema",
        "System.Speech"
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
            PackageFilterExpression.Parse("Microsoft.Extensions.Features"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Identity.Core"),
            PackageFilterExpression.Parse("Microsoft.Extensions.Identity.Stores"),
            // Documented under ML.NET moniker.
            PackageFilterExpression.Parse("Microsoft.Extensions.ML"),
            // Documented under .NET Aspire moniker.
            PackageFilterExpression.Parse("Microsoft.Extensions.ServiceDiscovery*"),
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
}
