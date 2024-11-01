using System.Collections.Concurrent;
using System.Xml.Linq;
using Kusto.Data.Net.Client;
using Kusto.Data;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace PackageIndexer;

public static class DotnetPackageIndex
{
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
            return await GetPackagesFromKustoAsync(feed);
        else
            //return await GetPackagesFromOtherGalleryAsync(feed);
            throw new ApplicationException("NuGetOrg should be the only feed.");
    }

    private static async Task<IReadOnlyList<(PackageIdentity, bool)>> GetPackagesFromKustoAsync(NuGetFeed feed)
    {
        Console.WriteLine("Fetching package IDs...");

        var cluster = "ddteldata.kusto.windows.net";
        var databaseName = "ClientToolsInsights";
        var predicate = string.Join(" or ", PlatformPackageDefinition.Owners.Select(n => $"set_has_element(Owners, \"{n}\")"));
        var query = $"""
            NiPackageOwners
            | where {predicate}
            | project Id
            | order by Id asc
            """;

        var connectionString = new KustoConnectionStringBuilder(cluster).WithAadUserPromptAuthentication();
        using var queryProvider = KustoClientFactory.CreateCslQueryProvider(connectionString);
        using var reader = queryProvider.ExecuteQuery(databaseName, query, null);

        List<string> packageIds = [];

        while (reader.Read())
        {
            string packageId = reader.GetString(0);
            if (PlatformPackageDefinition.Filter.IsMatch(packageId))
                packageIds.Add(packageId);
        }

        Console.WriteLine($"Found {packageIds.Count:N0} package IDs owned by .NET.");

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
}
