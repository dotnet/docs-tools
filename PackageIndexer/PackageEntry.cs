namespace PackageIndexer;

public sealed class PackageEntry
{
    public static PackageEntry Create(string id, string version, string repo, IList<FrameworkEntry> entries)
    {
        return new PackageEntry(id, version, repo, entries);
    }

    private PackageEntry(string id, string version, string repo, IList<FrameworkEntry> entries)
    {
        Name = id;
        Version = version;
        Repository = repo;
        FrameworkEntries = entries;
    }

    //public Guid Fingerprint { get; }
    public string Name { get; }
    public string Version { get; }
    public string Repository {  get; }
    public IList<FrameworkEntry> FrameworkEntries { get; }

    public void Write(Stream stream)
    {
        XmlEntryFormat.WritePackageEntry(stream, this);
    }
}
