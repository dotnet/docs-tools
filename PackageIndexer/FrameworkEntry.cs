namespace PackageIndexer;

public sealed class FrameworkEntry
{
    public static FrameworkEntry Create(string frameworkName)
    {
        return new FrameworkEntry(frameworkName);
    }

    private FrameworkEntry(string frameworkName)
    {
        FrameworkName = frameworkName;
    }

    public string FrameworkName { get; }

    public void Write(Stream stream)
    {
        XmlEntryFormat.WriteFrameworkEntry(stream, this);
    }
}