using System.Xml.Linq;

namespace XmlDocConflictResolver;

internal class DocsCommentsContainer
{
    private DirectoryInfo DocsXmlDir { get; set; }

    public readonly Dictionary<string, IDocsAPI> Types = new();
    public readonly Dictionary<string, IDocsAPI> Members = new();

    public DocsCommentsContainer(DirectoryInfo docsXmlDir) => DocsXmlDir = docsXmlDir;

    internal IEnumerable<FileInfo> EnumerateFiles()
    {
        foreach (DirectoryInfo subDir in DocsXmlDir.EnumerateDirectories($"*", SearchOption.TopDirectoryOnly))
        {
            foreach (FileInfo fileInfo in subDir.EnumerateFiles("*.xml", SearchOption.AllDirectories))
            {
                // LoadFile will determine if the Type is allowed or not
                yield return fileInfo;
            }
        }

        // Find interfaces only inside System.* folders.
        // Including Microsoft.* folders reaches the max limit of files to include in a list,
        // plus there are no essential interfaces there.
        foreach (DirectoryInfo subDir in DocsXmlDir.EnumerateDirectories("System*", SearchOption.AllDirectories))
        {
            // Ensure including interface files that start with I and
            // then an uppercase letter, and prevent including files like 'Int'
            foreach (FileInfo fileInfo in subDir.EnumerateFiles("I*.xml", SearchOption.AllDirectories))
            {
                if (fileInfo.Name[1] >= 'A' || fileInfo.Name[1] <= 'Z')
                {
                    yield return fileInfo;
                }
            }
        }
    }

    internal void LoadDocsFile(XDocument xDoc, string filePath)
    {
        if (IsXmlMalformed(xDoc, filePath))
        {
            return;
        }

        DocsType docsType = new(filePath, xDoc, xDoc.Root!);

        int totalMembersAdded = 0;
        Types.TryAdd(docsType.DocId, docsType); // is it OK this encounters duplicates?

        if (XmlHelper.TryGetChildElement(xDoc.Root!, "Members", out XElement? xeMembers) && xeMembers != null)
        {
            foreach (XElement xeMember in xeMembers.Elements("Member"))
            {
                DocsMember member = new(filePath, docsType, xeMember);
                totalMembersAdded++;
                Members.TryAdd(member.DocId, member); // is it OK this encounters duplicates?
            }
        }

        string message = $"Type '{docsType.DocId}' added with {totalMembersAdded} member(s) included: {filePath}";

        if (totalMembersAdded == 0)
        {
            Log.Warning(message);
        }
        else
        {
            Log.Success(message);
        }
    }

    private static bool IsXmlMalformed(XDocument? xDoc, string fileName)
    {
        if (xDoc == null)
        {
            Log.Error($"XDocument is null: {fileName}");
            return true;
        }
        if (xDoc.Root == null)
        {
            Log.Error($"Docs xml file does not have a root element: {fileName}");
            return true;
        }

        if (xDoc.Root.Name == "Namespace")
        {
            Log.Error($"Skipping namespace file (should have been filtered already): {fileName}");
            return true;
        }

        if (xDoc.Root.Name != "Type")
        {
            Log.Error($"Docs xml file does not have a 'Type' root element: {fileName}");
            return true;
        }

        if (!xDoc.Root.HasElements)
        {
            Log.Error($"Docs xml file Type element does not have any children: {fileName}");
            return true;
        }

        if (xDoc.Root.Elements("Docs").Count() != 1)
        {
            Log.Error($"Docs xml file Type element does not have a Docs child: {fileName}");
            return true;
        }

        return false;
    }
}