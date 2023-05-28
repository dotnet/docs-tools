// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.Linq;

internal class IntelliSenseXmlCommentsContainer
{
    private DirectoryInfo IntelliSenseXmlDir { get; set; }

    // The IntelliSense xml files do not separate types
    // from members like ECMA xml files - everything is a member.
    public Dictionary<string, IntelliSenseXmlMember> Members = new();
    public Dictionary<string, IntelliSenseXmlFile> Files = new();

    public IntelliSenseXmlCommentsContainer(DirectoryInfo intellisenseXmlDir) => IntelliSenseXmlDir = intellisenseXmlDir;

    internal IEnumerable<FileInfo> EnumerateFiles()
    {
        // 1) Find all the xml files inside all the subdirectories inside the IntelliSense xml directory
        foreach (DirectoryInfo subDir in IntelliSenseXmlDir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
        {
            foreach (FileInfo fileInfo in subDir.EnumerateFiles("*.xml", SearchOption.AllDirectories))
            {
                yield return fileInfo;
            }
        }

        // 2) Find all the xml files in the top directory
        foreach (FileInfo fileInfo in IntelliSenseXmlDir.EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly))
        {
            yield return fileInfo;
        }
    }

    internal void ParseIntellisenseXmlDoc(XDocument xDoc, string filePath, Encoding fileEncoding)
    {
        IntelliSenseXmlFile xmlFile = new IntelliSenseXmlFile(xDoc, filePath, fileEncoding);
        Files.Add(filePath, xmlFile);

        if (!TryGetAssemblyName(xDoc, filePath, out string? assembly))
        {
            return;
        }

        int totalAdded = 0;
        if (XmlHelper.TryGetChildElement(xDoc.Root!, "members", out XElement? xeMembers) 
            && xeMembers != null)
        {
            foreach (XElement xeMember in xeMembers.Elements("member"))
            {
                IntelliSenseXmlMember member = new(xeMember, xmlFile);

                totalAdded++;
                Members.TryAdd(member.Name, member);
            }
        }

        if (totalAdded > 0)
        {
            Log.Success($"{totalAdded} IntelliSense xml member(s) added from xml file '{filePath}'");
        }
    }

    // Verifies the file is properly formed while attempting to retrieve the assembly name.
    private static bool TryGetAssemblyName(XDocument? xDoc, string fileName, [NotNullWhen(returnValue: true)] out string? assembly)
    {
        assembly = null;

        if (xDoc == null)
        {
            Log.Error($"The XDocument was null: {fileName}");
            return false;
        }

        if (xDoc.Root == null)
        {
            Log.Error($"The IntelliSense xml file does not contain a root element: {fileName}");
            return false;
        }

        if (xDoc.Root.Name == "linker" || xDoc.Root.Name == "FileList")
        {
            // This is a linker suppression file or a framework list
            return false;
        }

        if (xDoc.Root.Name != "doc")
        {
            Log.Error($"The IntelliSense xml file does not contain a doc element: {fileName}");
            return false;
        }

        if (!xDoc.Root.HasElements)
        {
            Log.Error($"The IntelliSense xml file doc element not have any children: {fileName}");
            return false;
        }

        if (xDoc.Root.Elements("assembly").Count() != 1)
        {
            Log.Error($"The IntelliSense xml file does not contain exactly 1 'assembly' element: {fileName}");
            return false;
        }

        if (xDoc.Root.Elements("members").Count() != 1)
        {
            Log.Error($"The IntelliSense xml file does not contain exactly 1 'members' element: {fileName}");
            return false;
        }

        XElement? xAssembly = xDoc.Root.Element("assembly");
        if (xAssembly == null)
        {
            Log.Error($"The assembly xElement is null: {fileName}");
            return false;
        }
        if (xAssembly.Elements("name").Count() != 1)
        {
            Log.Error($"The IntelliSense xml file assembly element does not contain exactly 1 'name' element: {fileName}");
            return false;
        }

        assembly = xAssembly.Element("name")!.Value;
        if (string.IsNullOrEmpty(assembly))
        {
            Log.Error($"The IntelliSense xml file assembly string is null or empty: {fileName}");
            return false;
        }

        // The System.Private.CoreLib xml file should be mapped to the System.Runtime assembly
        if (assembly.ToUpperInvariant() == "SYSTEM.PRIVATE.CORELIB")
        {
            assembly = "System.Runtime";
        }

        return true;
    }

    public void SaveToDisk()
    {
        // TODO...
        List<string> savedFiles = new();
        foreach (IntelliSenseXmlFile xmlFile in Files.Values.Where(x => x.Changed))
        {
            Log.Info(false, $"Saving changes for {xmlFile.FilePath} ... ");

            try
            {
                // These settings prevent the addition of the <xml> element on
                // the first line and will preserve indentation+endlines.
                XmlWriterSettings xws = new()
                {
                    Encoding = xmlFile.FileEncoding,
                    OmitXmlDeclaration = true,
                    Indent = true,
                    CheckCharacters = false
                };

                using (XmlWriter xw = XmlWriter.Create(xmlFile.FilePath.Replace(".xml", ".docsversion.xml"), xws))
                {
                    xmlFile.Xdoc.Save(xw);
                }

                // Workaround to delete the annoying endline added by XmlWriter.Save
                string fileData = File.ReadAllText(xmlFile.FilePath);
                if (!fileData.EndsWith(Environment.NewLine))
                {
                    File.WriteAllText(xmlFile.FilePath, fileData + Environment.NewLine, xmlFile.FileEncoding);
                }

                Log.Success(" [Saved]");
            }
            catch (Exception e)
            {
                Log.Error("Failed to write to {0}. {1}", xmlFile.FilePath, e.Message);
                Log.Error(e.StackTrace ?? string.Empty);
                if (e.InnerException != null)
                {
                    Log.Line();
                    Log.Error(e.InnerException.Message);
                    Log.Line();
                    Log.Error(e.InnerException.StackTrace ?? string.Empty);
                }
            }
        }
    }
}
