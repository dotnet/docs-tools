using NuGet.Frameworks;
using NuGet.Packaging;

namespace PackageIndexer;

public sealed class PackageIndexer
{
    private readonly NuGetStore _store;
    //private readonly IReadOnlyList<FrameworkLocator> _frameworkLocators;

    public PackageIndexer(NuGetStore store)
    {
        _store = store;
    }

    //public PackageIndexer(NuGetStore store, IEnumerable<FrameworkLocator> frameworkLocators)
    //{
    //    _store = store;
    //    _frameworkLocators = frameworkLocators.ToArray();
    //}

    public async Task<PackageEntry> Index(string id, string version)
    {
        var dependencies = new Dictionary<string, PackageArchiveReader>();
        var frameworkEntries = new List<FrameworkEntry>();
        try
        {
            using (var root = await _store.GetPackageAsync(id, version))
            {
                var targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in root.GetCatalogReferenceGroups())
                    targetNames.Add(item.TargetFramework.GetShortFolderName());

                var targets = targetNames.Select(NuGetFramework.Parse).ToArray();

                if (!targets.Any())
                    return null;

                foreach (var target in targets)
                {
                    //var referenceGroup = root.GetCatalogReferenceGroup(target);

                    //Debug.Assert(referenceGroup is not null);

                    //await GetDependenciesAsync(dependencies, root, target);

                    // Add framework

                    //var platformPaths = GetPlatformSet(target);

                    //if (platformPaths is null)
                    //{
                    //    if (!IsKnownUnsupportedPlatform(target))
                    //        Console.WriteLine($"error: can't resolve platform references for {target}");
                    //    continue;
                    //}

                    frameworkEntries.Add(FrameworkEntry.Create(target.GetShortFolderName()));
                }
            }

            return PackageEntry.Create(id, version, frameworkEntries);
        }
        finally
        {
            foreach (var package in dependencies.Values)
                package.Dispose();
        }
    }

    private async Task GetDependenciesAsync(Dictionary<string, PackageArchiveReader> packages, PackageArchiveReader root, NuGetFramework target)
    {
        var dependencies = root.GetPackageDependencies();
        var dependencyGroup = NuGetFrameworkUtility.GetNearest(dependencies, target);
        if (dependencyGroup is not null)
        {
            foreach (var d in dependencyGroup.Packages)
            {
                if (packages.TryGetValue(d.Id, out var existingPackage))
                {
                    if (d.VersionRange.MinVersion > existingPackage.NuspecReader.GetVersion())
                    {
                        existingPackage.Dispose();
                        packages.Remove(d.Id);
                        existingPackage = null;
                    }
                }

                if (existingPackage is not null)
                    continue;

                var dependency = await _store.ResolvePackageAsync(d.Id, d.VersionRange);
                if (dependency is null)
                {
                    Console.WriteLine($"error: can't resolve dependency {d.Id} {d.VersionRange}");
                    continue;
                }

                packages.Add(d.Id, dependency);
                await GetDependenciesAsync(packages, dependency, target);
            }
        }
    }

    //private string[] GetPlatformSet(NuGetFramework framework)
    //{
    //    foreach (var l in _frameworkLocators)
    //    {
    //        var paths = l.Locate(framework);
    //        if (paths is not null)
    //            return paths;
    //    }

    //    return null;
    //}
}