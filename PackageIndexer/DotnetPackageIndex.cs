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
        Dictionary<string, string[]> ownerInformation = await feed.GetOwnerMappingAsync();

        string[] packageIds = ownerInformation.Keys
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            .Where(id => IsOwnedByDotNet(ownerInformation, id) &&
                         PackageFilter.Default.IsMatch(id))
            .ToArray();

        Console.WriteLine($"Found {packageIds.Length:N0} package IDs owned by .NET.");

        // Are these packages already filtered somehow? Yes - see PackageFilter.cs.
        // https://www.nuget.org/packages/Microsoft.NETCore.App.Ref
        // https://www.nuget.org/packages/Microsoft.WindowsDesktop.App.Ref

        Console.WriteLine("Getting versions...");

        ConcurrentBag<(PackageIdentity, bool)> identities = [];

        await Parallel.ForEachAsync(packageIds, async (packageId, _) =>
        {
            IReadOnlyList<(NuGetVersion, bool)> versions = await feed.GetAllVersionsAsync(packageId);

            foreach ((NuGetVersion, bool) version in versions)
            {
                var identity = new PackageIdentity(packageId, version.Item1);
                identities.Add((identity, version.Item2));
            }
        });

        Console.WriteLine($"Found {identities.Count:N0} package versions.");

        return identities.ToArray();
    }

    private static async Task<IReadOnlyList<PackageIdentity>> GetPackagesFromOtherGalleryAsync(NuGetFeed feed)
    {
        Console.WriteLine("Enumerating feed...");

        IReadOnlyList<PackageIdentity> identities = await feed.GetAllPackagesAsync();

        identities = identities.Where(i => PackageFilter.Default.IsMatch(i.Id)).ToArray();

        Console.WriteLine($"Found {identities.Count:N0} package versions.");

        return identities.ToArray();
    }

    private static IReadOnlyList<PackageIdentity> GetLatestVersions(
        IReadOnlyList<(PackageIdentity, bool)> identities,
        bool usePreviewVersions
        )
    {
        var result = new List<PackageIdentity>();

        IEnumerable<IGrouping<string, (PackageIdentity, bool)>> groups = identities.GroupBy(i => i.Item1.Id).OrderBy(g => g.Key);

        foreach (IGrouping<string, (PackageIdentity, bool)> group in groups)
        {
            string packageId = group.Key;
            IOrderedEnumerable<(PackageIdentity, bool)> versions =
                group.OrderByDescending(p => p.Item1.Version, VersionComparer.VersionReleaseMetadata);

            (PackageIdentity, bool) latestStable = versions.FirstOrDefault(i => !i.Item1.Version.IsPrerelease);
            (PackageIdentity, bool) latestPrerelease = versions.FirstOrDefault(i => i.Item1.Version.IsPrerelease);

            // If the latest version is deprecated, don't include any version.
            if (latestStable.Item2)
                latestStable = default;
            if (latestPrerelease.Item2)
                latestPrerelease = default;

            if (latestStable != default && latestPrerelease != default)
            {
                bool stableIsNewer = VersionComparer.VersionReleaseMetadata.Compare(latestPrerelease.Item1.Version, latestStable.Item1.Version) <= 0;
                if (stableIsNewer)
                    latestPrerelease = default;
            }

            // Comment this out for preview-only versions.
            if (latestStable != default)
            {
                result.Add(latestStable.Item1);
            }

            if (usePreviewVersions)
            {
                // Make sure it's a .NET 9 preview version.
                if ((latestPrerelease != default) && latestPrerelease.Item1.Version.Major == 9)
                    result.Add(latestPrerelease.Item1);
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
