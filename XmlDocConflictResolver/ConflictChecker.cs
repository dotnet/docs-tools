using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void CollectFiles()
        {
            Log.Info("Looking for IntelliSense xml files...");

            foreach (FileInfo fileInfo in IntelliSenseXmlComments.EnumerateFiles())
            {
                XDocument? xDoc = null;
                Encoding? encoding = null;
                try
                {
                    var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                    var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
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
                    IntelliSenseXmlComments.ParseIntellisenseXmlDoc(xDoc, fileInfo.FullName, encoding);
                }
            }
            Log.Success("Finished looking for IntelliSense xml files.");
            Log.Line();

            Log.Info("Looking for Docs xml files...");

            foreach (FileInfo fileInfo in DocsComments.EnumerateFiles())
            {
                XDocument? xDoc = null;
                Encoding? encoding = null;
                try
                {
                    var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                    var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
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

                if (xDoc != null && encoding != null)
                {
                    DocsComments.LoadDocsFile(xDoc, fileInfo.FullName, encoding);
                }
            }
            Log.Success("Finished looking for Docs xml files.");
            Log.Line();
        }

        public void Start()
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
                //PortMissingCommentsForMember(dMemberToUpdate);
            }
        }

        private void CheckForConflictingTextForMember(IntelliSenseXmlMember ixmlMember)
        {
            //bool foundDifferences = false;

            if (DocsComments.Members.TryGetValue(ixmlMember.Name,
                out DocsMember? ecmaxmlMember) && ecmaxmlMember != null)
            {
                if (!ecmaxmlMember.Summary.IsDocsEmpty() &&
                    String.Compare(ecmaxmlMember.Summary, ixmlMember.Summary) != 0)
                {
                    ixmlMember.Summary = ecmaxmlMember.Summary;
                    ixmlMember.xmlFile.Changed = true;
                }

                // TODO...
                //if (isMethod)
                //{
                //    mc.Returns = ecmaxmlMember.Returns;
                //}

                //mc.Remarks = ecmaxmlMember.Remarks;
                //if (isProperty)
                //{
                //    mc.Property = GetPropertyValue(ecmaxmlMember.Value, ecmaxmlMember.Returns);
                //}

                //foreach (DocsParam tsParam in ecmaxmlMember.Params)
                //{
                //    mc.Params.Add(tsParam.Name, tsParam.Value);
                //}

                //foreach (DocsTypeParam tsTypeParam in ecmaxmlMember.TypeParams)
                //{
                //    mc.TypeParams.Add(tsTypeParam.Name, tsTypeParam.Value);
                //}

                //TryPortMissingSummaryForAPI(dMemberToUpdate, mc.Summary, mc.IsEII);
                //TryPortMissingRemarksForAPI(dMemberToUpdate, mc.Remarks, mc.IsEII);
                //TryPortMissingParamsForAPI(dMemberToUpdate, ecmaxmlMember, dInterfacedMember);
                //TryPortMissingTypeParamsForAPI(dMemberToUpdate, ecmaxmlMember, dInterfacedMember);
                //TryPortMissingExceptionsForMember(dMemberToUpdate, ecmaxmlMember);

                //if (isProperty)
                //{
                //    TryPortMissingPropertyForMember(dMemberToUpdate, mc.Property, mc.IsEII);
                //}
                //else if (isMethod)
                //{
                //    TryPortMissingReturnsForMember(dMemberToUpdate, mc.Returns, mc.IsEII);
                //}

                //if (dMemberToUpdate.Changed)
                //{
                //    ModifiedAPIs.Add(dMemberToUpdate.DocId);
                //    ModifiedFiles.Add(dMemberToUpdate.FilePath);
                //}
            }
        }

        public void SaveToDisk() => IntelliSenseXmlComments.SaveToDisk();
    }

    static class Extensions
    {
        // Checks if the passed string is considered "empty" according to the Docs repo rules.
        public static bool IsDocsEmpty(this string? s) =>
            string.IsNullOrWhiteSpace(s) || s == ConflictChecker.ToBeAdded;
    }
}
