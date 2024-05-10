using System.Xml.Linq;

namespace PackageIndexer;

internal static class XmlEntryFormat
{
    public static void WriteFrameworkEntry(Stream stream, FrameworkEntry frameworkEntry)
    {
        var document = new XDocument();
        var root = new XElement("framework", new XAttribute("name", frameworkEntry.FrameworkName));
        document.Add(root);

        document.Save(stream);
    }

    public static void WritePackageEntry(Stream stream, PackageEntry packageEntry)
    {
        var document = new XDocument();
        var root = new XElement("package",
            new XAttribute("id", packageEntry.Name),
            new XAttribute("version", packageEntry.Version),
            new XAttribute("repository", packageEntry.Repository)
        );
        document.Add(root);

        foreach (FrameworkEntry fx in packageEntry.FrameworkEntries)
        {
            root.Add(new XElement("framework", fx.FrameworkName));
        }

        document.Save(stream);
    }

    public static PackageEntry ReadPackageEntry(string packageIndexFile)
    {
        XDocument doc = XDocument.Load(packageIndexFile);

        XElement packageElement = doc.Element("package");

        string id = packageElement.Attribute("id").Value;
        string version = packageElement.Attribute("version").Value;
        string repo = packageElement.Attribute("repository")?.Value;

        IEnumerable<XElement> frameworkElements = packageElement.Elements("framework");

        IList<FrameworkEntry> frameworks = [];
        foreach (XElement frameworkElement in frameworkElements) 
        {
            frameworks.Add(FrameworkEntry.Create(frameworkElement.Value));
        }

        return PackageEntry.Create(id, version, repo, frameworks);
    }
}
