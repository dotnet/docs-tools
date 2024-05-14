using NuGet.Frameworks;
using NuGet.Packaging;

namespace PackageIndexer;

public sealed class PackageIndexer(NuGetStore store)
{
    private readonly NuGetStore _store = store;

    public async Task<PackageEntry> Index(string id, string version)
    {
        string repo;
        var dependencies = new Dictionary<string, PackageArchiveReader>();
        var frameworkEntries = new List<FrameworkEntry>();
        try
        {
            using (PackageArchiveReader root = await _store.GetPackageAsync(id, version))
            {
                repo = root.GetRepository();

                var targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (FrameworkSpecificGroup item in root.GetCatalogReferenceGroups())
                    targetNames.Add(item.TargetFramework.GetShortFolderName());

                NuGetFramework[] targets = targetNames.Select(NuGetFramework.Parse).ToArray();

                if (targets.Length == 0)
                    return null;

                foreach (NuGetFramework target in targets)
                {
                    frameworkEntries.Add(FrameworkEntry.Create(target.GetShortFolderName()));
                }
            }

            return PackageEntry.Create(id, version, repo, frameworkEntries);
        }
        finally
        {
            foreach (PackageArchiveReader package in dependencies.Values)
                package.Dispose();
        }
    }
}
