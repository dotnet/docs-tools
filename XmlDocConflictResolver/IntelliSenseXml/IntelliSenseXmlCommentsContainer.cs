// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using XmlDocConflictResolver;

internal class IntelliSenseXmlCommentsContainer
{
    private DirectoryInfo IntelliSenseXmlDir { get; set; }

    private readonly string _docsVersionSuffix = ".docs.xml";
    private readonly string _mergedVersionSuffix = ".merged.xml";

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
        List<string> savedFiles = new();
        foreach (IntelliSenseXmlFile xmlFile in Files.Values.Where(x => x.Changed))
        {
            string docsVersionFileName = xmlFile.FilePath.Replace(".xml", _docsVersionSuffix);
            Log.Info(false, $"Saving changes to '{docsVersionFileName}' ...");

            try
            {
                XmlWriterSettings xws = new()
                {
                    Encoding = xmlFile.FileEncoding,
                    Indent = true,
                    CheckCharacters = false,
                    IndentChars = "    "
                };

                using (XmlWriter xw = XmlWriter.Create(docsVersionFileName, xws))
                {
                    xmlFile.Xdoc.Save(xw);
                }

                // Remove the encoding from the XML declaration.
                string contents = File.ReadAllText(docsVersionFileName);
                contents = contents.Replace(@"<?xml version=""1.0"" encoding=""utf-8""?>", @"<?xml version=""1.0""?>");

                // Replace &lt; and &gt;.
                contents = contents.Replace("&lt;", "<");
                contents = contents.Replace("&gt;", ">");

                // Add a newline at the end, if necessary.
                if (!contents.EndsWith(Environment.NewLine))
                {
                    contents = contents + Environment.NewLine;
                }

                File.WriteAllText(docsVersionFileName, contents, xmlFile.FileEncoding);

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

    internal void AddConflictMarkers()
    {
        using PowerShell powershell = PowerShell.Create();

        // Foreach IntelliSense XML file that had changes,
        // create a new file with conflict markers using diff.exe.
        foreach (IntelliSenseXmlFile xmlFile in Files.Values.Where(x => x.Changed))
        {
            Log.Info(false, $"Adding conflict markers to '{xmlFile.FilePath}' ...");

            string modifiedIntelliSenseFile = xmlFile.FilePath.Replace(".xml", _docsVersionSuffix);
            if (!File.Exists(modifiedIntelliSenseFile))
            {
                Log.Error($"IntelliSense XML file '{xmlFile.FilePath}' had " +
                    $"changes but no docs version file '{modifiedIntelliSenseFile}' was found.");
                continue;
            }

            // Convert the file paths to Linux file system paths.
            string originalIntelliSenseFile = xmlFile.FilePath.ConvertToUnixFilePath();
            modifiedIntelliSenseFile = modifiedIntelliSenseFile.ConvertToUnixFilePath();
            string mergedFile = originalIntelliSenseFile.Replace(".xml", _mergedVersionSuffix);

            string diffCommand = $"diff --unchanged-group-format=\"%=\" --old-group-format=\"\" --new-group-format=\"%>\" --changed-group-format=\"<<<<<<<%c'\\\\12'%<=======%c'\\\\12'%>>>>>>>>%c'\\\\12'\" {originalIntelliSenseFile} {modifiedIntelliSenseFile} > {mergedFile}";

            string scriptPath = "diff_script.sh";
            File.WriteAllText(scriptPath, diffCommand);

            // TODO: Don't hardcode the path to bash.exe.
            powershell.AddScript($"& 'C:\\Program Files\\Git\\bin\\bash.exe' {scriptPath}");

            Collection<PSObject> results = powershell.Invoke();

            Log.Success(" [Saved]");
        }
    }

    internal void CleanUpFiles()
    {
        foreach (IntelliSenseXmlFile xmlFile in Files.Values.Where(x => x.Changed))
        {
            // Delete all files that end with docsVersionSuffix.
            string modifiedIntelliSenseFile = xmlFile.FilePath.Replace(".xml", _docsVersionSuffix);
            if (!File.Exists(modifiedIntelliSenseFile))
            {
                Log.Error($"IntelliSense XML file '{xmlFile.FilePath}' had " +
                    $"changes but no docs version file '{modifiedIntelliSenseFile}' was found.");
            }
            else
                File.Delete(modifiedIntelliSenseFile);

            // "Move" the merged files to the original IntelliSense XML files.
            string mergedFile = xmlFile.FilePath.Replace(".xml", _mergedVersionSuffix);
            if (!File.Exists(mergedFile))
            {
                Log.Error($"IntelliSense XML file '{xmlFile.FilePath}' had " +
                    $"changes but no merged file '{mergedFile}' was found.");
            }
            else
                File.Move(mergedFile, xmlFile.FilePath, true);
        }

        Log.Info("Finished cleaning up files.");
    }
}
