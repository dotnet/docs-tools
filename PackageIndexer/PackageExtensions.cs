using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace PackageIndexer;

internal static class PackageExtensions
{
    public static string GetRepository(this PackageArchiveReader root)
    {
        RepositoryMetadata repo = root.NuspecReader.GetRepositoryMetadata();
        return repo.Url;
    }

    public static IEnumerable<FrameworkSpecificGroup> GetCatalogReferenceGroups(this PackageArchiveReader root)
    {
        // NOTE: We're not using root.GetReferenceItems() because it apparently doesn't always
        //       return items from the ref folder. One package where this reproduces is
        //       System.Security.Cryptography.Csp 4.3.0

        var tfms = new HashSet<string>();

        foreach (FrameworkSpecificGroup group in root.GetItems("ref").Concat(root.GetItems("lib")))
        {
            if (tfms.Add(group.TargetFramework.GetShortFolderName()))
                yield return group;
        }
    }

    public static FrameworkSpecificGroup GetCatalogReferenceGroup(this PackageArchiveReader root, NuGetFramework current)
    {
        IEnumerable<FrameworkSpecificGroup> referenceItems = root.GetCatalogReferenceGroups();
        FrameworkSpecificGroup referenceGroup = NuGetFrameworkUtility.GetNearest(referenceItems, current);
        return referenceGroup;
    }
}
