using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Text;
using System.Xml.Linq;

namespace XmlDocConflictResolver
{
    internal class ConflictChecker
    {
        // The default boilerplate string for what the docs platform
        // considers an empty (undocumented) API element.
        public static readonly string ToBeAdded = "To be added.";

        private readonly DirectoryInfo IntelliSenseXmlDir;
        private readonly DirectoryInfo DocsXmlDir;
        private readonly DocsCommentsContainer DocsComments;
        private readonly IntelliSenseXmlCommentsContainer IntelliSenseXmlComments;

        internal ConflictChecker(DirectoryInfo intelliSenseXmlDir, DirectoryInfo docsXmlDir)
        {
            IntelliSenseXmlDir = intelliSenseXmlDir;
            DocsXmlDir = docsXmlDir;
            DocsComments = new DocsCommentsContainer(docsXmlDir);
            IntelliSenseXmlComments = new IntelliSenseXmlCommentsContainer(intelliSenseXmlDir);
        }

        internal void CollectFiles()
        {
            Log.Info("Looking for IntelliSense xml files...");

            foreach (FileInfo fileInfo in IntelliSenseXmlComments.EnumerateFiles())
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
                    IntelliSenseXmlComments.ParseIntellisenseXmlDoc(xDoc, fileInfo.FullName, encoding!);
                }
            }
            Log.Success("Finished looking for IntelliSense xml files.");
            Log.Line();

            Log.Info("Looking for Docs xml files...");

            // Find a matching ECMAXML file for each type.
            foreach (IntelliSenseXmlMember ixmlMember in IntelliSenseXmlComments.Members.Values)
            {
                string typeDocId;
                if (ixmlMember.Name.StartsWith('T'))
                    typeDocId = ixmlMember.Name;
                else
                {
                    // Construct the DocId for the containing type.
                    // From "M:System.Formats.Cbor.CborWriter.WriteTag(System.Formats.Cbor.CborTag)"
                    // to "T:System.Formats.Cbor.CborWriter"

                    // First remove parameters, if any.
                    typeDocId = ixmlMember.Name.Split('(')[0];
                    // Chop off prefix and member name.
                    typeDocId = typeDocId[1..typeDocId.LastIndexOf('.')];
                    // Add "T" prefix.
                    typeDocId = String.Concat("T", typeDocId);
                }

                // Check if we've already loaded an ECMAXML file for the containing type.
                if (!DocsComments.Types.ContainsKey(typeDocId))
                {
                    FileInfo? docsFile = GetDocsFileForMember(ixmlMember);
                    if (docsFile is null)
                        continue;

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
                        DocsComments.LoadDocsFile(xDoc, docsFile.FullName);
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
            if (!IntelliSenseXmlComments.Members.Any())
            {
                Log.Error("No IntelliSense XML comments found.");
                return;
            }

            if (!DocsComments.Types.Any())
            {
                Log.Error("No docs type APIs found.");
                return;
            }

            InsertConflictingText();
        }

        private void InsertConflictingText()
        {
            Log.Info("Looking for IntelliSense xml comments that differ from the docs...");

            foreach (IntelliSenseXmlMember member in IntelliSenseXmlComments.Members.Values)
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
                foundDocsApi = DocsComments.Types.TryGetValue(ixmlMember.Name, out ecmaxmlApi);
            else
                foundDocsApi = DocsComments.Members.TryGetValue(ixmlMember.Name, out ecmaxmlApi);

            if (foundDocsApi && ecmaxmlApi != null)
            {
                if (!ecmaxmlApi.Summary.IsDocsEmpty() &&
                    String.Compare(ecmaxmlApi.Summary, ixmlMember.Summary) != 0)
                {
                    ixmlMember.Summary = ecmaxmlApi.Summary;
                    ixmlMember.XmlFile.Changed = true;
                }

                if (!ecmaxmlApi.Returns.IsDocsEmpty() &&
                    String.Compare(ecmaxmlApi.Returns, ixmlMember.Returns) != 0)
                {
                    ixmlMember.Returns = ecmaxmlApi.Returns;
                    ixmlMember.XmlFile.Changed = true;
                }

                //foreach (IntelliSenseXmlParam ixmlParam in ixmlMember.Params)
                for (int i = 0; i < ixmlMember.Params.Count; i++)
                {
                    IntelliSenseXmlParam ixmlParam = ixmlMember.Params[i];

                    // Find matching param from docs XML.
                    if (!ixmlParam.Value.IsDocsEmpty())
                    {
                        DocsParam? ecmaxmlParam = ecmaxmlApi.Params.Where(p => p.Name == ixmlParam.Name).FirstOrDefault();
                        if (ecmaxmlParam is not null && 
                            String.Compare(ecmaxmlParam.Value, ixmlParam.Value) != 0)
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
                        DocsTypeParam? ecmaxmlTypeParam = ecmaxmlApi.TypeParams.Where(tp => tp.Name == ixmlTypeParam.Name).FirstOrDefault();
                        if (ecmaxmlTypeParam is not null && 
                            String.Compare(ecmaxmlTypeParam.Value, ixmlTypeParam.Value) != 0)
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
                    DocsMember ecmaxmlMember = (DocsMember)ecmaxmlApi;
                    if (!ecmaxmlMember.Value.IsDocsEmpty() &&
                        String.Compare(ecmaxmlMember.Value, ixmlMember.Value) != 0)
                    {
                        ixmlMember.Value = ecmaxmlMember.Value;
                        ixmlMember.XmlFile.Changed = true;
                    }

                    foreach (IntelliSenseXmlException ixmlException in ixmlMember.Exceptions)
                    {
                        // Find matching exception from docs XML.
                        if (!ixmlException.Value.IsDocsEmpty())
                        {
                            DocsException? ecmaxmlException = ecmaxmlMember.Exceptions.Where(e => e.Cref == ixmlException.Cref).FirstOrDefault();
                            if (ecmaxmlException is not null &&
                                String.Compare(ecmaxmlException.Value, ixmlException.Value) != 0)
                            {
                                ixmlException.Value = ecmaxmlException.Value;
                                ixmlMember.XmlFile.Changed = true;
                            }
                        }
                    }
                }
            }
        }

        private FileInfo? GetDocsFileForMember(IntelliSenseXmlMember ixmlMember)
        {
            // Files are named by type and in a folder with the name of the namespace.
            var directories = DocsXmlDir.EnumerateDirectories(ixmlMember.Namespace, SearchOption.AllDirectories);
            if (!directories.Any())
            {
                Log.Error($"No docs directory found for namespace '{ixmlMember.Namespace}'.");
                // This could be because it's a nested class and the namespace was calculated incorrectly.
                // So look for a directory with one less period-separated token on the end.
                string newNamespace = ixmlMember.Namespace[..ixmlMember.Namespace.LastIndexOf('.')];
                directories = DocsXmlDir.EnumerateDirectories(newNamespace, SearchOption.AllDirectories);
                if (!directories.Any())
                    return null;
            }

            // Get just the type name.
            // TODO: This won't work for nested types.
            string typeName;
            if (ixmlMember.Name.StartsWith('T'))
                typeName = ixmlMember.Name[(ixmlMember.Name.LastIndexOf('.') + 1)..];
            else
            {
                string[] tokens = ixmlMember.Name.Split('.');
                typeName = tokens[tokens.Length - 2];
            }

            IEnumerable<FileInfo>? files = null;
            foreach (var directory in directories)
            {
                // Look for typename.xml.
                files = directory.EnumerateFiles(String.Concat(typeName, ".xml"), SearchOption.TopDirectoryOnly);
                if (files.Any())
                    break;
            }

            if (files is null || !files.Any())
            {
                Log.Error($"No docs file found for type '{typeName}'.");
                return null;
            }

            return files!.First();
        }

        internal void SaveToDisk() => IntelliSenseXmlComments.SaveToDisk();

        internal void AddConflictMarkers() => IntelliSenseXmlComments.AddConflictMarkers();

        internal void CleanUpFiles() => IntelliSenseXmlComments.CleanUpFiles();
    }

    internal static class Extensions
    {
        // Checks if the passed string is considered "empty" according to the Docs repo rules.
        public static bool IsDocsEmpty(this string? s) =>
            string.IsNullOrWhiteSpace(s) || s == ConflictChecker.ToBeAdded;

        public static string ConvertToUnixFilePath(this string filePath)
        {
            string[] filePathTokens = filePath.Replace('\\', '/').Split(':');
            return String.Concat(
                "/",
                filePathTokens[0],
                "/",
                filePathTokens[1][1..]); // Removes colon.
        }
    }
}
