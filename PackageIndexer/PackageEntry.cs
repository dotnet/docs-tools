namespace PackageIndexer;

public sealed class PackageEntry
{
    public static PackageEntry Create(string id, string version, IList<FrameworkEntry> entries)
    {
        return new PackageEntry(id, version, entries);
    }

    private PackageEntry(string id, string version, IList<FrameworkEntry> entries)
    {
        Name = id;
        Version = version;
        FrameworkEntries = entries;
    }

    //public Guid Fingerprint { get; }
    public string Name { get; }
    public string Version { get; }
    public IList<FrameworkEntry> FrameworkEntries { get; }

    public void Write(Stream stream)
    {
        XmlEntryFormat.WritePackageEntry(stream, this);
    }
}