using System.ComponentModel.DataAnnotations;
using System.Management.Automation.Language;
using System.Text;
using System.Xml.Linq;

namespace XmlDocConflictResolver;

internal class ConflictChecker
{
    // The default boilerplate string for what the docs platform
    // considers an empty (undocumented) API element.
    public static readonly string ToBeAdded = "To be added.";

    private readonly DirectoryInfo _intelliSenseXmlDir;
    private readonly DirectoryInfo _docsXmlDir;
    private readonly DocsCommentsContainer _docsComments;
    private readonly IntelliSenseXmlCommentsContainer _intelliSenseXmlComments;

    internal ConflictChecker(DirectoryInfo intelliSenseXmlDir, DirectoryInfo docsXmlDir)
    {
        _intelliSenseXmlDir = intelliSenseXmlDir;
        _docsXmlDir = docsXmlDir;
        _docsComments = new DocsCommentsContainer(docsXmlDir);
        _intelliSenseXmlComments = new IntelliSenseXmlCommentsContainer(intelliSenseXmlDir);
    }

    internal void CollectFiles()
    {
        Log.Info("Looking for IntelliSense xml files...");

        foreach (FileInfo fileInfo in _intelliSenseXmlComments.EnumerateFiles())
        {
            XDocument? xDoc = null;
            Encoding? encoding = null;
            try
            {
                var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                using (StreamReader sr = new(fileInfo.FullName, utf8NoBom, detectEncodingFromByteOrderMarks: true))
                {
                    xDoc = XDocument.Load(sr);
                    encoding = sr.CurrentEncoding;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load '{fileInfo.FullName}'. {ex}");
            }

            if (xDoc != null)
            {
                _intelliSenseXmlComments.ParseIntellisenseXmlDoc(xDoc, fileInfo.FullName, encoding!);
            }
        }
        Log.Success("Finished looking for IntelliSense xml files.");
        Log.Line();

        Log.Info("Looking for Docs xml files...");

        // Find a matching ECMAXML file for each type.
        foreach (IntelliSenseXmlMember ixmlMember in _intelliSenseXmlComments.Members.Values)
        {
            // Ignore members and assume they've been loaded with their containing type.
            if (!ixmlMember.Name.StartsWith('T'))
                continue;

            // Check if we've already loaded an ECMAXML file for the containing type.
            if (!_docsComments.Types.ContainsKey(ixmlMember.Name))
            {
                FileInfo? docsFile = GetDocsFileForType(ixmlMember);
                if (docsFile is null)
                {
                    Log.Error($"No docs file found for type '{ixmlMember.Name}'.");
                    continue;
                }

                // Load the docs file.
                XDocument? xDoc = null;
                try
                {
                    var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                    var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                    using (StreamReader sr = new(docsFile.FullName, utf8NoBom, detectEncodingFromByteOrderMarks: true))
                    {
                        xDoc = XDocument.Load(sr);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to load '{docsFile.FullName}'. {ex}");
                }

                if (xDoc != null)
                {
                    _docsComments.LoadDocsFile(xDoc, docsFile.FullName);
                }
            }

            // Enumerates all XML files.
            //foreach (FileInfo fileInfo in DocsComments.EnumerateFiles())
            //{
            //    XDocument? xDoc = null;
            //    Encoding? encoding = null;
            //    try
            //    {
            //        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            //        var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            //        using (StreamReader sr = new(fileInfo.FullName, utf8NoBom, detectEncodingFromByteOrderMarks: true))
            //        {
            //            xDoc = XDocument.Load(sr);
            //            encoding = sr.CurrentEncoding;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Log.Error($"Failed to load '{fileInfo.FullName}'. {ex}");
            //    }

            //    if (xDoc != null && encoding != null)
            //    {
            //        DocsComments.LoadDocsFile(xDoc, fileInfo.FullName, encoding);
            //    }
            //}
            //Log.Success("Finished looking for Docs xml files.");
            //Log.Line();
        }
    }

    internal void Start()
    {
        if (!_intelliSenseXmlComments.Members.Any())
        {
            Log.Error("No IntelliSense XML comments found.");
            return;
        }

        if (!_docsComments.Types.Any())
        {
            Log.Error("No docs type APIs found.");
            return;
        }

        InsertConflictingText();
    }

    private void InsertConflictingText()
    {
        Log.Info("Looking for IntelliSense xml comments that differ from the docs...");

        foreach (IntelliSenseXmlMember member in _intelliSenseXmlComments.Members.Values)
        {
            CheckForConflictingTextForMember(member);
        }
    }

    private void CheckForConflictingTextForMember(IntelliSenseXmlMember ixmlMember)
    {
        bool foundDocsApi;
        IDocsAPI? ecmaxmlApi;

        // Find docs type or member.
        if (ixmlMember.IsType())
            foundDocsApi = _docsComments.Types.TryGetValue(ixmlMember.Name, out ecmaxmlApi);
        else
            foundDocsApi = _docsComments.Members.TryGetValue(ixmlMember.Name, out ecmaxmlApi);

        if (foundDocsApi && ecmaxmlApi != null)
        {
            if (!ecmaxmlApi.Summary.IsDocsEmpty() &&
                string.Compare(ecmaxmlApi.Summary, ixmlMember.Summary) != 0)
            {
                ixmlMember.Summary = ecmaxmlApi.Summary;
                ixmlMember.XmlFile.Changed = true;
            }

            if (!ecmaxmlApi.Returns.IsDocsEmpty() &&
                string.Compare(ecmaxmlApi.Returns, ixmlMember.Returns) != 0)
            {
                ixmlMember.Returns = ecmaxmlApi.Returns;
                ixmlMember.XmlFile.Changed = true;
            }

            if (!ecmaxmlApi.Remarks.IsDocsEmpty() &&
                string.Compare(ecmaxmlApi.Remarks, ixmlMember.Remarks) != 0)
            {
                ixmlMember.Remarks = ecmaxmlApi.Remarks;
                ixmlMember.XmlFile.Changed = true;
            }

            //foreach (IntelliSenseXmlParam ixmlParam in ixmlMember.Params)
            for (int i = 0; i < ixmlMember.Params.Count; i++)
            {
                IntelliSenseXmlParam ixmlParam = ixmlMember.Params[i];

                // Find matching param from docs XML.
                if (!ixmlParam.Value.IsDocsEmpty())
                {
                    DocsParam? ecmaxmlParam = ecmaxmlApi.Params.FirstOrDefault(p => p.Name == ixmlParam.Name);
                    if (ecmaxmlParam is not null &&
                        string.Compare(ecmaxmlParam.Value, ixmlParam.Value) != 0)
                    {
                        ixmlParam.Value = ecmaxmlParam.Value;
                        ixmlMember.XmlFile.Changed = true;
                    }
                }
            }

            foreach (IntelliSenseXmlTypeParam ixmlTypeParam in ixmlMember.TypeParams)
            {
                // Find matching typeParam from docs XML.
                if (!ixmlTypeParam.Value.IsDocsEmpty())
                {
                    DocsTypeParam? ecmaxmlTypeParam =
                        ecmaxmlApi.TypeParams.FirstOrDefault(tp => tp.Name == ixmlTypeParam.Name);
                    if (ecmaxmlTypeParam is not null &&
                        string.Compare(ecmaxmlTypeParam.Value, ixmlTypeParam.Value) != 0)
                    {
                        ixmlTypeParam.Value = ecmaxmlTypeParam.Value;
                        ixmlMember.XmlFile.Changed = true;
                    }
                }
            }

            // Ignoring: altmember, seealso, related tags.

            // These XML tags are only for non-type APIs.
            if (ecmaxmlApi.Kind == APIKind.Member)
            {
                // Property value.
                DocsMember ecmaxmlMember = (DocsMember)ecmaxmlApi;
                if (!ecmaxmlMember.Value.IsDocsEmpty() &&
                    string.Compare(ecmaxmlMember.Value, ixmlMember.Value) != 0)
                {
                    ixmlMember.Value = ecmaxmlMember.Value;
                    ixmlMember.XmlFile.Changed = true;
                }

                // Exceptions.
                foreach (IntelliSenseXmlException ixmlException in ixmlMember.Exceptions)
                {
                    // Find matching exception from docs XML.
                    if (!ixmlException.Value.IsDocsEmpty())
                    {
                        DocsException? ecmaxmlException = ecmaxmlMember.Exceptions.FirstOrDefault(e => e.Cref == ixmlException.Cref);
                        if (ecmaxmlException is not null &&
                            string.Compare(ecmaxmlException.Value, ixmlException.Value) != 0)
                        {
                            ixmlException.Value = ecmaxmlException.Value;
                            ixmlMember.XmlFile.Changed = true;
                        }
                    }
                }
            }
        }
    }

    private FileInfo? GetDocsFileForType(IntelliSenseXmlMember ixmlMember)
    {
        bool nestedType = false;

        // Files are named by type and in a folder with the name of the namespace.
        var directories = _docsXmlDir.EnumerateDirectories(ixmlMember.Namespace, SearchOption.AllDirectories);
        if (!directories.Any())
        {
            // This could be because it's a nested class and the namespace was calculated incorrectly.
            // So look for a directory with one less period-separated token on the end.
            if (!ixmlMember.Namespace.Contains('.'))
                return null;

            string newNamespace = ixmlMember.Namespace[..ixmlMember.Namespace.LastIndexOf('.')];
            directories = _docsXmlDir.EnumerateDirectories(newNamespace, SearchOption.AllDirectories);
            if (!directories.Any())
                return null;
            else
                nestedType = true;
        }

        // Get just the type name.
        // Nested types handling:
        // For example, for T:System.TimeZoneInfo.TransitionTime,
        // the docs file is System > TimeZoneInfo+TransitionTime.xml.
        string typeName;
        if (nestedType)
        {
            string[] nameParts = ixmlMember.Name.Split('.');
            if (nameParts.Length > 2)
                typeName = string.Join("+", nameParts[(nameParts.Length - 2)..]);
            else
                return null;
        }
        else
            typeName = ixmlMember.Name[(ixmlMember.Name.LastIndexOf('.') + 1)..];

        IEnumerable<FileInfo>? files = null;
        foreach (var directory in directories)
        {
            // Look for typename.xml.
            files = directory.EnumerateFiles(string.Concat(typeName, ".xml"), SearchOption.TopDirectoryOnly);
            if (files.Any())
                break;
        }

        if (files is null || !files.Any())
        {
            return null;
        }

        return files!.First();
    }

    internal void SaveToDisk() => _intelliSenseXmlComments.SaveToDisk();

    internal void AddConflictMarkers() => _intelliSenseXmlComments.AddConflictMarkers();

    internal void CleanUpFiles() => _intelliSenseXmlComments.CleanUpFiles();
}

internal static class Extensions
{
    // Checks if the passed string is considered "empty" according to the Docs repo rules.
    public static bool IsDocsEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) || s == ConflictChecker.ToBeAdded;

    public static string ConvertToUnixFilePath(this string filePath)
    {
        string[] filePathTokens = filePath.Replace('\\', '/').Split(':');
        return string.Concat(
            "/",
            filePathTokens[0],
            "/",
            filePathTokens[1][1..]); // Removes colon.
    }
}
