using System.Xml.Linq;

namespace PackageIndexer;

internal static class XmlEntryFormat
{
    public static void WriteFrameworkEntry(Stream stream, string framework)
    {
        var document = new XDocument();
        var root = new XElement("framework", new XAttribute("name", framework));
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

        foreach (string fx in packageEntry.Frameworks)
        {
            root.Add(new XElement("framework", fx));
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

        IList<string> frameworks = [];
        foreach (XElement frameworkElement in frameworkElements) 
        {
            frameworks.Add(frameworkElement.Value);
        }

        return PackageEntry.Create(id, version, repo, frameworks);
    }
}
