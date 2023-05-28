using System.Text;
using System.Xml;
using System.Xml.Linq;

internal class DocsCommentsContainer
{
    private DirectoryInfo DocsXmlDir { get; set; }

    public readonly Dictionary<string, DocsType> Types = new();
    public readonly Dictionary<string, DocsMember> Members = new();

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

    internal void LoadDocsFile(XDocument xDoc, string filePath, Encoding encoding)
    {
        if (IsXmlMalformed(xDoc, filePath))
        {
            return;
        }

        DocsType docsType = new(filePath, xDoc, xDoc.Root!, encoding);

        bool add = false;
        bool addedAsInterface = false;

        bool containsForbiddenAssembly = docsType.AssemblyInfos.Any(assemblyInfo =>
                                            Config.ExcludedAssemblies.Any(excluded => assemblyInfo.AssemblyName.StartsWith(excluded)) ||
                                            Config.ExcludedNamespaces.Any(excluded => assemblyInfo.AssemblyName.StartsWith(excluded)));

        if (!containsForbiddenAssembly)
        {
            // If it's an interface, always add it if the user wants to detect EIIs,
            // even if it's in an assembly that was not included but was not explicitly excluded
            addedAsInterface = false;
            if (!Config.SkipInterfaceImplementations)
            {
                // Interface files start with I, and have an 2nd alphabetic character
                addedAsInterface = docsType.Name.Length >= 2 && docsType.Name[0] == 'I' && docsType.Name[1] >= 'A' && docsType.Name[1] <= 'Z';
                add |= addedAsInterface;
            }

            bool containsAllowedAssembly = docsType.AssemblyInfos.Any(assemblyInfo =>
                                                Config.IncludedAssemblies.Any(included => assemblyInfo.AssemblyName.StartsWith(included)) ||
                                                Config.IncludedNamespaces.Any(included => assemblyInfo.AssemblyName.StartsWith(included)));

            if (containsAllowedAssembly)
            {
                // If it was already added above as an interface, skip this part
                // Otherwise, find out if the type belongs to the included assemblies, and if specified, to the included (and not excluded) types
                // This includes interfaces even if user wants to skip EIIs - They will be added if they belong to this namespace or to the list of
                // included (and not exluded) types, but will not be used for EII, but rather as normal types whose comments should be ported
                if (!addedAsInterface)
                {
                    // Either the user didn't specify namespace filtering (allow all namespaces) or specified particular ones to include/exclude
                    if (!Config.IncludedNamespaces.Any() ||
                            (Config.IncludedNamespaces.Any(included => docsType.Namespace.StartsWith(included)) &&
                             !Config.ExcludedNamespaces.Any(excluded => docsType.Namespace.StartsWith(excluded))))
                    {
                        // Can add if the user didn't specify type filtering (allow all types), or specified particular ones to include/exclude
                        add = !Config.IncludedTypes.Any() ||
                                (Config.IncludedTypes.Contains(docsType.Name) &&
                                 !Config.ExcludedTypes.Contains(docsType.Name));
                    }
                }
            }
        }

        if (add)
        {
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
            if (addedAsInterface)
            {
                Log.Magenta("[Interface] - " + message);
            }
            else if (totalMembersAdded == 0)
            {
                Log.Warning(message);
            }
            else
            {
                Log.Success(message);
            }
        }
    }

    private static bool HasAllowedDirName(DirectoryInfo dirInfo) =>
        !Configuration.ForbiddenBinSubdirectories.Contains(dirInfo.Name) && !dirInfo.Name.EndsWith(".Tests", StringComparison.InvariantCultureIgnoreCase);

    private static bool HasAllowedFileName(FileInfo fileInfo) =>
        !fileInfo.Name.StartsWith("ns-") &&
            fileInfo.Name != "index.xml" &&
            fileInfo.Name != "_filter.xml";

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