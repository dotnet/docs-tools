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

        // For ASP.NET.
        //IReadOnlyList<PackageIdentity> latestVersions = GetLatestVersions(packages, usePreviewVersions, 10);

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
        using System.Data.IDataReader reader = queryProvider.ExecuteQuery(databaseName, query, null);

        List<string> packageIds = [];

        while (reader.Read())
        {
            string packageId = reader.GetString(0);
            if (PlatformPackageDefinition.Filter.IsMatch(packageId))
                packageIds.Add(packageId);
        }

        // Special case since not owned by dotnetframework (yet)
        packageIds.Add("Microsoft.Extensions.VectorData.Abstractions");

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

    // Only includes the latest version.
    private static IReadOnlyList<PackageIdentity> GetLatestVersions(
        IReadOnlyList<(PackageIdentity packageId, bool isDeprecated)> identities,
        bool usePreviewVersions,
        int prereleaseVersionPrefix = -1
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

            (PackageIdentity packageId, bool isDeprecated) latestPrerelease;
            if (prereleaseVersionPrefix != -1)
            {
                // Filter out prerelease versions that don't match the prefix.
                latestPrerelease = versions.FirstOrDefault(i => i.packageId.Version.IsPrerelease &&
                                        i.packageId.Version.Major == prereleaseVersionPrefix);
            }
            else
                latestPrerelease = versions.FirstOrDefault(i => i.packageId.Version.IsPrerelease);

            // If the latest version is deprecated, don't include any version.
            if (latestStable.isDeprecated)
                latestStable = default;
            if (latestPrerelease.isDeprecated)
                latestPrerelease = default;

            if (!usePreviewVersions && latestStable != default)
            {
                result.Add(latestStable.packageId);
                continue;
            }

            if (usePreviewVersions)
            {
                // Use whichever version (stable or prerelease) is newer.
                if (latestStable != default && latestPrerelease != default)
                {
                    bool stableIsNewer = VersionComparer.VersionReleaseMetadata.Compare(
                        latestPrerelease.packageId.Version,
                        latestStable.packageId.Version
                        ) <= 0;

                    if (stableIsNewer)
                        result.Add(latestStable.packageId);
                    else
                        result.Add(latestPrerelease.packageId);
                }
                else if (latestStable != default)
                    result.Add(latestStable.packageId);
                else if (latestPrerelease != default)
                    result.Add(latestPrerelease.packageId);
                // else don't add the package at all.
            }
        }

        return result;
    }
}
