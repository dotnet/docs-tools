using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace PackageIndexer;

public class NuGetStore
{
    private readonly NuGetFeed[] _feeds;

    public NuGetStore(string packagesCachePath, params NuGetFeed[] feeds)
    {
        ArgumentException.ThrowIfNullOrEmpty(packagesCachePath);
        ArgumentNullException.ThrowIfNull(feeds);

        if (feeds.Length == 0)
            throw new ArgumentException("must have at least one feed", nameof(feeds));

        PackagesCachePath = packagesCachePath;
        _feeds = feeds;
    }

    public string PackagesCachePath { get; }

    public Task<PackageArchiveReader> GetPackageAsync(PackageIdentity identity)
    {
        return GetPackageAsync(identity.Id, identity.Version.ToNormalizedString());
    }

    public async Task<PackageArchiveReader> GetPackageAsync(string id, string version)
    {
        string path = GetPackagePath(id, version);
        if (File.Exists(path))
            return new PackageArchiveReader(path);

        var identity = new PackageIdentity(id, NuGetVersion.Parse(version));
        string directory = Path.GetDirectoryName(path);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await using (FileStream fileStream = File.Create(path))
        {
            bool success = false;

            foreach (NuGetFeed feed in _feeds)
            {
                if (await feed.TryCopyPackageStreamAsync(identity, fileStream))
                {
                    success = true;
                    break;
                }
            }

            if (!success)
                throw new Exception($"Can't resolve package {id} {version}");
        }

        return new PackageArchiveReader(path);
    }

    public void DeleteFromCache(string id, string version)
    {
        string path = GetPackagePath(id, version);
        if (path is not null)
            File.Delete(path);
    }

    private string GetPackagePath(string id, string version)
    {
        return Path.Combine(PackagesCachePath, $"{id}.{version}.nupkg");
    }
}
