namespace PackageIndexer;

public sealed class PackageEntry
{
    public static PackageEntry Create(string id, string version, string repo, IList<string> frameworks)
    {
        return new PackageEntry(id, version, repo, frameworks);
    }

    private PackageEntry(string id, string version, string repo, IList<string> frameworks)
    {
        Name = id;
        Version = version;
        Repository = repo;
        Frameworks = frameworks;
    }

    //public Guid Fingerprint { get; }
    public string Name { get; }
    public string Version { get; }
    public string Repository {  get; }
    public IList<string> Frameworks { get; }

    public void Write(Stream stream)
    {
        XmlEntryFormat.WritePackageEntry(stream, this);
    }
}
