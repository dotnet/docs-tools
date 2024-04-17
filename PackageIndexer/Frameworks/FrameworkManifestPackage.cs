namespace PackageIndexer;

public sealed class FrameworkManifestPackage
{
    public FrameworkManifestPackage(string id,
                                    string version)
    {
        Id = id;
        Version = version;
        //References = references.Order().ToArray();
    }

    public string Id { get; }

    public string Version { get; }

    //public IReadOnlyList<string> References { get; }
}