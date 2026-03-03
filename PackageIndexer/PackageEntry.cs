namespace PackageIndexer;

public sealed class PackageEntry
{
    public static PackageEntry Create(
        string id,
        string version,
        string repo,
        IList<string> frameworks,
        bool includeXmlDocs = true)
    {
        return new PackageEntry(id, version, repo, frameworks, includeXmlDocs);
    }

    private PackageEntry(
        string id,
        string version,
        string repo,
        IList<string> frameworks,
        bool includeXmlDocs)
    {
        Name = id;
        Version = version;
        Repository = repo;
        Frameworks = frameworks;
        IncludeXmlDocs = includeXmlDocs;
    }

    public string Name { get; }
    public string Version { get; }
    public string Repository {  get; }
    public IList<string> Frameworks { get; }
    public bool IncludeXmlDocs { get; set; }

    public void Write(Stream stream)
    {
        XmlEntryFormat.WritePackageEntry(stream, this);
    }
}
