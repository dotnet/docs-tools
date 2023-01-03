using CleanRepo.Extensions;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// Publish command:
// dotnet publish -c release -p:PublishSingleFile=true --no-self-contained -r win10-x64 c:\users\gewarren\cleanrepo\CleanRepo\CleanRepo.csproj

namespace CleanRepo
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Annoying")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Annoying")]
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            //args = new[] { "--trim-redirects", "--docset-root=c:\\users\\gewarren\\dotnet-docs\\docs", "--lookback-days=90", "--output-file=c:\\users\\gewarren\\desktop\\clicks.txt" };
            //args = new[] { "--remove-hops" };
            //args = new[] { "--replace-redirects" };
            args = new[] { "--orphaned-snippets" };
#endif

            Parser.Default.ParseArguments<Options>(args).WithParsed(RunOptions);
        }

        static void RunOptions(Options options)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Find orphaned topics
            if (options.FindOrphanedTopics)
            {
                if (String.IsNullOrEmpty(options.InputDirectory))
                {
                    Console.WriteLine("\nEnter the path to the directory that you want to check for orphans:\n");
                    options.InputDirectory = Console.ReadLine();
                }
                if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                {
                    Console.WriteLine("\nThat directory doesn't exist.");
                    return;
                }

                if (options.Delete is null)
                {
                    options.Delete = false;
                    Console.WriteLine("\nDo you want to delete orphans (y or n)?");
                    var info = Console.ReadKey();
                    if (info.KeyChar == 'y' || info.KeyChar == 'Y')
                    {
                        options.Delete = true;
                    }
                }

                Console.WriteLine($"\nSearching the '{options.InputDirectory}' directory and its subdirectories for orphaned topics...");

                List<FileInfo> tocFiles = GetTocFiles(options.InputDirectory);
                List<FileInfo> markdownFiles = GetMarkdownFiles(options.InputDirectory);

                if (tocFiles is null || markdownFiles is null)
                {
                    return;
                }

                ListOrphanedTopics(tocFiles, markdownFiles, options.Delete.Value);
            }
            // Find orphaned images
            else if (options.FindOrphanedImages)
            {
                // Get input directory.
                if (String.IsNullOrEmpty(options.InputDirectory))
                {
                    Console.WriteLine("\nEnter the path to the directory that you want to check for orphans:\n");
                    options.InputDirectory = Console.ReadLine();
                }
                if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                {
                    Console.WriteLine("\nThat directory doesn't exist.");
                    return;
                }

                DocFxRepo repo;
                try
                {
                    repo = new DocFxRepo(options.InputDirectory);
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }

                // Check that this directory or one of its ancestors has a docfx.json file.
                // I.e. we don't want it to be a parent directory of a docfx.json directory, so single docset only.
                if (repo.DocFxDirectory == null)
                {
                    Console.WriteLine("\nCould not find the docfx.json file for this directory.");
                    return;
                }

                // Get the base path URL for the docset.
                if (repo.BasePathUrl == null)
                {
                    Console.WriteLine("\nCould not find the base path URL for this directory.");
                    return;
                }

                if (options.Delete is null)
                {
                    options.Delete = false;
                    Console.WriteLine("\nDo you want to delete orphans (y or n)?");
                    var info = Console.ReadKey();
                    if (info.KeyChar == 'y' || info.KeyChar == 'Y')
                    {
                        options.Delete = true;
                    }
                }

                Console.WriteLine($"\nSearching the '{options.InputDirectory}' directory recursively for orphaned .png/.jpg/.gif/.svg files...\n");

                repo.ListOrphanedImages(options.Delete.Value);
            }
            else if (options.CatalogImages)
            {
                // Get input directory.
                if (String.IsNullOrEmpty(options.InputDirectory))
                {
                    Console.WriteLine("\nEnter the path to the directory where you want to catalog image links:\n");
                    options.InputDirectory = Console.ReadLine();
                }
                if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                {
                    Console.WriteLine("\nThat directory doesn't exist.");
                    return;
                }

                DocFxRepo repo;
                try
                {
                    repo = new DocFxRepo(options.InputDirectory);
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }

                // Check that this directory or one of its ancestors has a docfx.json file.
                // I.e. we don't want it to be a parent directory of a docfx.json directory, so single docset only.
                if (repo.DocFxDirectory == null)
                {
                    Console.WriteLine("\nCould not find the docfx.json file for this directory.");
                    return;
                }

                // Get the base path URL for the docset.
                if (repo.BasePathUrl == null)
                {
                    Console.WriteLine("\nCould not find the base path URL for this directory.");
                    return;
                }

                Console.WriteLine($"\nCataloging the images in the '{options.InputDirectory}' directory...\n");

                repo.OutputImageReferences();
            }
            // Find orphaned include-type files
            else if (options.FindOrphanedIncludes)
            {
                if (String.IsNullOrEmpty(options.InputDirectory))
                {
                    Console.WriteLine("\nEnter the path to the directory that you want to check for orphans:\n");
                    options.InputDirectory = Console.ReadLine();
                }
                if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                {
                    Console.WriteLine("\nThat directory doesn't exist.");
                    return;
                }

                if (options.Delete is null)
                {
                    options.Delete = false;
                    Console.WriteLine("\nDo you want to delete orphans (y or n)?");
                    var info = Console.ReadKey();
                    if (info.KeyChar == 'y' || info.KeyChar == 'Y')
                    {
                        options.Delete = true;
                    }
                }

                Console.WriteLine($"\nSearching the '{options.InputDirectory}' directory recursively for orphaned .md files " +
                    $"in directories named 'includes' or '_shared'.");

                Dictionary<string, int> includeFiles = GetIncludeFiles(options.InputDirectory);

                if (includeFiles.Count == 0)
                {
                    Console.WriteLine("\nNo .md files were found in any directory named 'includes' or '_shared'.");
                    return;
                }

                ListOrphanedIncludes(options.InputDirectory, includeFiles, options.Delete.Value);
            }
            // Find orphaned .cs and .vb files
            else if (options.FindOrphanedSnippets)
            {
                if (String.IsNullOrEmpty(options.InputDirectory))
                {
                    Console.WriteLine("\nEnter the path to the directory that you want to check for orphans:\n");
                    options.InputDirectory = Console.ReadLine();
                }
                if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                {
                    Console.WriteLine("\nThat directory doesn't exist.");
                    return;
                }

                if (options.Delete is null)
                {
                    options.Delete = false;
                    Console.WriteLine("\nDo you want to delete orphans (y or n)?");
                    var info = Console.ReadKey();
                    if (info.KeyChar == 'y' || info.KeyChar == 'Y')
                    {
                        options.Delete = true;
                    }
                }

                Console.WriteLine($"\nSearching the '{options.InputDirectory}' directory recursively for orphaned .cs and .vb files.");

                List<string> snippetFiles = GetSnippetFiles(options.InputDirectory);

                if (snippetFiles.Count == 0)
                {
                    Console.WriteLine("\nNo .cs or .vb files were found.");
                    return;
                }

                ListOrphanedSnippets(options.InputDirectory, snippetFiles, options.Delete.Value);
            }
            // Replace links to topics that are redirected in the master redirection file.
            else if (options.ReplaceRedirectTargets)
            {
                // Get the directory in which to replace links.
                if (String.IsNullOrEmpty(options.InputDirectory))
                {
                    Console.WriteLine("\nEnter the path to the directory that you want to replace links in:\n");
                    options.InputDirectory = Console.ReadLine();
                }
                if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                {
                    Console.WriteLine("\nThat directory doesn't exist.");
                    return;
                }

                // Check that this directory or one of its ancestors has a docfx.json file.
                // I.e. we don't want it to be a parent directory of a docfx.json directory, so single docset only.
                var directory = new DirectoryInfo(options.InputDirectory);
                DirectoryInfo docfxDir = GetDirectory(directory, "docfx.json");
                if (docfxDir == null)
                {
                    Console.WriteLine("\nCould not find the docfx.json file for this directory.");
                    return;
                }

                // Get the base path URL for the docset.
                string urlBasePath = GetUrlBasePath(docfxDir);
                if (urlBasePath == null)
                {
                    Console.WriteLine("\nCould not find the base path URL for this directory.");
                    return;
                }

                // Get the OPS config file.
                FileInfo opsConfigFile = GetFileHereOrInParent(options.InputDirectory, ".openpublishing.publish.config.json");
                if (opsConfigFile == null)
                {
                    Console.WriteLine($"\nCouldn't find OPS config file for directory '{options.InputDirectory}'.");
                    return;
                }
                if (opsConfigFile.DirectoryName == null)
                {
                    Console.WriteLine("Could not determine directory name for OPS config file.");
                    return;
                }

                Console.WriteLine($"\nSearching the '{options.InputDirectory}' directory for links to redirected topics...\n");

                // Get a list of all redirection files.
                List<string> redirectionFiles = GetRedirectionFiles(opsConfigFile);

                // Gather all the redirects.
                List<Redirect> redirects = new List<Redirect>();
                foreach (string redirectionFile in redirectionFiles)
                {
                    FileInfo redirectsFile = new FileInfo(Path.Combine(opsConfigFile.DirectoryName, redirectionFile));
                    if (redirectsFile == null)
                    {
                        Console.WriteLine($"\nCould not find redirection file '{redirectionFile}'.");
                        continue;
                    }

                    redirects.AddRange(GetAllRedirectedFiles(redirectsFile, opsConfigFile.DirectoryName));
                }

                // Get all the markdown and YAML files.
                List<FileInfo> linkingFiles = GetMarkdownFiles(options.InputDirectory);
                linkingFiles.AddRange(GetYAMLFiles(options.InputDirectory));

                // Check all links, including in toc.yml, to files in the redirects list.
                // Replace links to redirected files.
                ReplaceRedirectedLinks(redirects, linkingFiles, urlBasePath);

                Console.WriteLine("\nFinished replacing links.");
            }
            // Replace site-relative links to *this* repo with file-relative links.
            else if (options.ReplaceWithRelativeLinks)
            {
                if (String.IsNullOrEmpty(options.InputDirectory))
                {
                    Console.WriteLine("\nEnter the path to the directory where you want to replace redirected links:\n");
                    options.InputDirectory = Console.ReadLine();
                }
                if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                {
                    Console.WriteLine("\nThat directory doesn't exist.");
                    return;
                }

                // Get the OPS config file.
                FileInfo opsConfigFile = GetFileHereOrInParent(options.InputDirectory, ".openpublishing.publish.config.json");
                if (opsConfigFile == null)
                {
                    Console.WriteLine($"\nCouldn't find OPS config file for directory '{options.InputDirectory}'.");
                    return;
                }

                // Check that this isn't the root directory of the repo. The code doesn't handle that case currently
                // because it can't always determine the base path of the docset (e.g. for dotnet/docs repo).
                if (String.Equals(opsConfigFile.DirectoryName, options.InputDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"\nYou entered the repo root directory. Please enter a subdirectory in which to replace links.");
                    return;
                }

                // Check that this directory or one of its ancestors has a docfx.json file.
                // I.e. we don't want it to be a parent directory of a docfx.json directory, so single docset only.
                var directory = new DirectoryInfo(options.InputDirectory);
                DirectoryInfo docfxDir = GetDirectory(directory, "docfx.json");
                if (docfxDir == null)
                {
                    Console.WriteLine("\nCould not find the docfx.json file for this directory.");
                    return;
                }

                // Get the absolute path to the base directory for this docset.
                string rootDirectory = GetDocsetAbsolutePath(docfxDir.FullName, directory);

                // Find the docset and its URL base path.
                // Get all docsets for the OPS config file.
                Dictionary<string, string> docsets = GetDocsetInfo(opsConfigFile);
                // Find the specific docset info for the input directory.
                string relativePathToDocset = Path.GetRelativePath(opsConfigFile.DirectoryName, rootDirectory);
                string urlBasePath = null;
                foreach (var docset in docsets)
                {
                    if (docset.Key == relativePathToDocset)
                    {
                        // Bingo!
                        urlBasePath = docset.Value;
                        break;
                    }
                }

                Console.WriteLine($"\nReplacing site-relative links to '{urlBasePath}/' in " +
                    $"the '{options.InputDirectory}' directory with file-relative links.\n");

                // Get all the markdown and YAML files.
                List<FileInfo> linkingFiles = GetMarkdownFiles(options.InputDirectory);
                linkingFiles.AddRange(GetYAMLFiles(options.InputDirectory));

                // Check all links in these files.
                ReplaceLinks(linkingFiles, urlBasePath, rootDirectory);
            }
            // Remove hops/daisy chains in a redirection file.
            else if (options.RemoveRedirectHops)
            {
                // Get the directory that represents the docset.
                if (String.IsNullOrEmpty(options.InputDirectory))
                {
                    Console.WriteLine("\nEnter the path to any directory in the docset:\n");
                    options.InputDirectory = Console.ReadLine();
                }
                if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                {
                    Console.WriteLine("\nThat directory doesn't exist.");
                    return;
                }

                // Check that this directory or one of its ancestors has a docfx.json file.
                // I.e. we don't want it to be a parent directory of a docfx.json directory, so single docset only.
                var directory = new DirectoryInfo(options.InputDirectory);
                DirectoryInfo docfxDir = GetDirectory(directory, "docfx.json");
                if (docfxDir == null)
                {
                    Console.WriteLine("\nCould not find the docfx.json file for this directory.");
                    return;
                }

                // Get the OPS config file.
                FileInfo opsConfigFile = GetFileHereOrInParent(options.InputDirectory, ".openpublishing.publish.config.json");
                if (opsConfigFile == null)
                {
                    Console.WriteLine($"\nCouldn't find OPS config file for directory '{options.InputDirectory}'.");
                    return;
                }
                if (opsConfigFile.DirectoryName == null)
                {
                    Console.WriteLine("Could not determine directory name for OPS config file.");
                    return;
                }

                // Get all docsets for the OPS config file.
                Dictionary<string, string> docsets = GetDocsetInfo(opsConfigFile);

                // Get a list of all redirection files.
                List<string> redirectionFiles = GetRedirectionFiles(opsConfigFile);

                // Remove hops within each file.
                foreach (string redirectionFile in redirectionFiles)
                {
                    FileInfo redirectsFile = new FileInfo(Path.Combine(opsConfigFile.DirectoryName, redirectionFile));
                    if (redirectsFile == null)
                    {
                        Console.WriteLine($"\nCould not find redirection file '{redirectionFile}'.");
                        continue;
                    }

                    Console.WriteLine($"\nRemoving hops from the '{redirectionFile}' redirection file.\n");
                    RemoveRedirectHops(redirectsFile, docsets, opsConfigFile.DirectoryName);
                }
            }
            // Nothing to do.
            else
            {
                Console.WriteLine("\nYou did not specify which function to perform. To see options, use 'CleanRepo.exe -?'.");
                return;
            }

            stopwatch.Stop();
            Console.WriteLine($"\nElapsed time: {stopwatch.Elapsed.ToHumanReadableString()}");
        }

        #region Replace site-relative links
        private static void ReplaceLinks(List<FileInfo> linkingFiles, string urlBasePath, string rootDirectory)
        {
            // Strip preceding / off urlBasePath, if it exists.
            urlBasePath = urlBasePath.TrimStart('/');

            foreach (var linkingFile in linkingFiles)
            {
                // Read the whole file up front because we might change the file mid-flight.
                string originalFileText = File.ReadAllText(linkingFile.FullName);

                // Test strings:
                // ![VisualizerIcon](/test-repo/debugger/dbg-tips.png "Visualizer icon")
                // ![VisualizerIcon](/test-repo/debugger/dbg-tips.png)
                // For more information, see [this page](/test-repo/debugger/dbg-tips).

                // Find links that look like [link text](/docsetName/some other text)
                string pattern1 = @"\]\((/" + urlBasePath + @"/([^\)\s]*))";

                foreach (Match match in Regex.Matches(originalFileText, pattern1, RegexOptions.IgnoreCase))
                {
                    // Get the first capture group, which is the part of the path after the docset name.
                    string siteRelativePath = match.Groups[2].Value;

                    // If the path contains a ?, ignore this link as replacing it might not be ideal.
                    // For example, if the link is to a specific version like "?view=vs-2015".
                    if (siteRelativePath.IndexOf('?') >= 0)
                        continue;

                    ReplaceLinkText(siteRelativePath, rootDirectory, linkingFile, match.Groups[0].Value, match.Groups[1].Value);
                }

                // Find links that look like <img src="/azure/mydocs/media/pic3.png">
                string pattern2 = "<img[^>]*?src[ ]*=[ ]*\"(/" + urlBasePath + "/([^>]*?.(png|gif|jpg|svg)))[ ]*\"";

                foreach (Match match in Regex.Matches(originalFileText, pattern2, RegexOptions.IgnoreCase))
                {
                    // Get the first capture group, which is the part of the path after the docset name.
                    string siteRelativePath = match.Groups[2].Value;

                    ReplaceLinkText(siteRelativePath, rootDirectory, linkingFile, match.Groups[0].Value, match.Groups[1].Value);
                }

                // Find links that look like [0]: /azure/mydocs/media/pic1.png
                string pattern3 = @"\[.*\]:[ ]*(/" + urlBasePath + @"/(.*\.(png|gif|jpg|svg)))";

                foreach (Match match in Regex.Matches(originalFileText, pattern3, RegexOptions.IgnoreCase))
                {
                    // Get the first capture group, which is the part of the path after the docset name.
                    string siteRelativePath = match.Groups[2].Value;

                    ReplaceLinkText(siteRelativePath, rootDirectory, linkingFile, match.Groups[0].Value, match.Groups[1].Value);
                }

                // Find links that look like imageSrc: /azure/mydocs/media/pic1.png
                string pattern4 = @"imageSrc:[ ]*(/" + urlBasePath + @"/([^:]*\.(png|gif|jpg|svg)))";

                foreach (Match match in Regex.Matches(originalFileText, pattern4, RegexOptions.IgnoreCase))
                {
                    // Get the first capture group, which is the part of the path after the docset name.
                    string siteRelativePath = match.Groups[2].Value;

                    ReplaceLinkText(siteRelativePath, rootDirectory, linkingFile, match.Groups[0].Value, match.Groups[1].Value);
                }

                // Find links that look like :::image type="complex" source="/azure/mydocs/media/pic1.png" alt-text="Screenshot.":::
                string pattern5 = @":::image[^:]*source=""(/" + urlBasePath + @"/([^:]*\.(png|gif|jpg|svg)))""[^:]*:::";

                foreach (Match match in Regex.Matches(originalFileText, pattern5, RegexOptions.IgnoreCase))
                {
                    // Get the first capture group, which is the part of the path after the docset name.
                    string siteRelativePath = match.Groups[2].Value;

                    ReplaceLinkText(siteRelativePath, rootDirectory, linkingFile, match.Groups[0].Value, match.Groups[1].Value);
                }
            }
        }

        private static void ReplaceLinkText(string siteRelativePath, string rootDirectory, FileInfo linkingFile, string originalMatch, string originalLink)
        {
            // If the link contains a bookmark, trim it off and add it back later.
            // If there are two hash characters, this pattern is greedy and finds the last one.
            string bookmarkPattern = @"(.*)(#.*)";
            string bookmark = null;
            if (Regex.IsMatch(siteRelativePath, bookmarkPattern))
            {
                Match bookmarkMatch = Regex.Match(siteRelativePath, bookmarkPattern);
                siteRelativePath = bookmarkMatch.Groups[1].Value;
                bookmark = bookmarkMatch.Groups[2].Value;
            }

            // Build an absolute path to this file.
            string absolutePath = Path.Combine(rootDirectory, siteRelativePath.Trim());

            // Clean up the path by replacing forward slashes with back slashes, removing extra dots, etc.
            absolutePath = Path.GetFullPath(absolutePath);

            FileInfo file = new FileInfo(absolutePath);
            if (String.IsNullOrEmpty(file.Extension) || !string.Equals(file.Extension, ".md") || !string.Equals(file.Extension, ".yml"))
            {
                // Look for a file of this name in the same directory to obtain its extension.
                try
                {
                    FileInfo[] files = file.Directory.GetFiles(file.Name + ".*");
                    if (files.Length > 0)
                    {
                        // Since site-relative image links still require a file extension,
                        // and this link didn't include an extension, favor a non-image extension first.
                        if (files.Any(f => f.Extension == ".md"))
                            absolutePath = files.First(f => f.Extension == ".md").FullName;
                        else if (files.Any(f => f.Extension == ".yml"))
                            absolutePath = files.First(f => f.Extension == ".yml").FullName;
                        else
                            absolutePath = files[0].FullName;
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    // This can happen if files from a different repo map to the same docset.
                    // For example, the C# language specification: [C# Language Specification](/dotnet/csharp/language-reference/language-specification/introduction)
                    return;
                }
            }

            // Check that the link is valid in the local repo.
            if (!File.Exists(absolutePath))
            {
                return;
            }

            if (absolutePath != null)
            {
                // Determine the file-relative path to absolutePath.
                string fileRelativePath = Path.GetRelativePath(linkingFile.DirectoryName, absolutePath);

                // Replace backslashes with forward slashes.
                fileRelativePath = fileRelativePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (fileRelativePath != null)
                {
                    // Add the bookmark back onto the end, if there is one.
                    if (!String.IsNullOrEmpty(bookmark))
                    {
                        fileRelativePath = fileRelativePath + bookmark;
                    }

                    string newText = originalMatch.Replace(originalLink, fileRelativePath);

                    // Replace the link.
                    Console.WriteLine($"Replacing '{originalMatch}' with '{newText}' in file '{linkingFile.FullName}'.");

                    // If a previous link was found and replaced, the text may have changed, so reread the file.
                    string currentFileText = File.ReadAllText(linkingFile.FullName);

                    File.WriteAllText(linkingFile.FullName, currentFileText.Replace(originalMatch, newText));
                }
            }
        }
        #endregion

        #region Orphaned includes
        /// TODO: Improve the perf of this method using the following pseudo code:
        /// For each include file
        ///    For each markdown file
        ///       Do a RegEx search for the include file
        ///          If found, BREAK to the next include file
        private static void ListOrphanedIncludes(string inputDirectory, Dictionary<string, int> includeFiles, bool deleteOrphanedIncludes)
        {
            // Get all files that could possibly link to the include files
            var files = GetAllMarkdownFiles(inputDirectory, out DirectoryInfo rootDirectory);

            if (files is null)
            {
                return;
            }

            // Gather up all the include references and increment the count for that include file in the Dictionary.
            //foreach (var markdownFile in files)
            Parallel.ForEach(files, markdownFile =>
            {
                foreach (string line in File.ReadAllLines(markdownFile.FullName))
                {
                    // Example include references:
                    // [!INCLUDE [DotNet Restore Note](../includes/dotnet-restore-note.md)]
                    // [!INCLUDE[DotNet Restore Note](~/includes/dotnet-restore-note.md)]
                    // [!INCLUDE [temp](../_shared/assign-to-sprint.md)]

                    // An include file referenced from another include file won't have "includes" or "_shared" in the path.
                    // E.g. [!INCLUDE [P2S FAQ All](vpn-gateway-faq-p2s-all-include.md)]

                    // RegEx pattern to match
                    string includeLinkPattern = @"\[!INCLUDE[ ]?\[[^\]]*?\]\((.*?\.md)";

                    // There could be more than one INCLUDE reference on the line, hence the foreach loop.
                    foreach (Match match in Regex.Matches(line, includeLinkPattern, RegexOptions.IgnoreCase))
                    {
                        // Get the first capture group, which is the relative path ending in '.md'.
                        string relativePath = match.Groups[1].Value.Trim();

                        if (relativePath != null)
                        {
                            string fullPath;

                            // Path could start with a tilde e.g. ~/includes/dotnet-restore-note.md
                            if (relativePath.StartsWith("~/"))
                            {
                                fullPath = Path.Combine(rootDirectory.FullName, relativePath.TrimStart('~', '/'));
                            }
                            else
                            {
                                // Construct the full path to the referenced INCLUDE file
                                fullPath = Path.Combine(markdownFile.DirectoryName, relativePath);
                            }

                            // Clean up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                            fullPath = Path.GetFullPath(fullPath);

                            if (fullPath != null)
                            {
                                // Increment the count for this INCLUDE file in our dictionary
                                try
                                {
                                    // TODO - is this okay to have in a Parallel.ForEach loop?
                                    includeFiles[fullPath.ToLower()]++;
                                }
                                catch (KeyNotFoundException)
                                {
                                    // No need to do anything.
                                }
                            }
                        }
                    }
                }
            });

            int count = 0;

            // Print out the INCLUDE files that have zero references.
            StringBuilder output = new StringBuilder();
            foreach (var includeFile in includeFiles)
            {
                if (includeFile.Value == 0)
                {
                    count++;
                    output.AppendLine(Path.GetFullPath(includeFile.Key));
                }
            }

            if (deleteOrphanedIncludes)
            {
                // Delete orphaned image files
                foreach (var includeFile in includeFiles)
                {
                    if (includeFile.Value == 0)
                    {
                        File.Delete(includeFile.Key);
                    }
                }
            }

            string deleted = deleteOrphanedIncludes ? "and deleted " : "";

            Console.WriteLine($"\nFound {deleted}{count} orphaned INCLUDE files:\n");
            Console.WriteLine(output.ToString());
            Console.WriteLine("DONE");
        }

        /// <summary>
        /// Returns a collection of *.md files in the current directory, and optionally subdirectories,
        /// if the directory name is 'includes' or '_shared'.
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, int> GetIncludeFiles(string inputDirectory)
        {
            DirectoryInfo dir = new DirectoryInfo(inputDirectory);

            Dictionary<string, int> includeFiles = new Dictionary<string, int>();

            if (String.Compare(dir.Name, "includes", true) == 0 || String.Compare(dir.Name, "_shared", true) == 0)
            {
                // This is a folder that is likely to contain "include"-type files, i.e. files that aren't in the TOC.

                foreach (var file in dir.EnumerateFiles("*.md"))
                {
                    includeFiles.Add(file.FullName.ToLower(), 0);
                }
            }

            // Search in subdirectories.
            foreach (var subDirectory in dir.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                if (String.Compare(subDirectory.Name, "includes", true) == 0 || String.Compare(subDirectory.Name, "_shared", true) == 0)
                {
                    // This is a folder that is likely to contain "include"-type files, i.e. files that aren't in the TOC.

                    foreach (var file in subDirectory.EnumerateFiles("*.md"))
                    {
                        includeFiles.Add(file.FullName.ToLower(), 0);
                    }
                }
            }

            return includeFiles;
        }
        #endregion

        #region Orphaned snippets
        /// <summary>
        /// Returns a list of *.cs and *.vb files in the current directory, and optionally subdirectories.
        /// </summary>
        private static List<string> GetSnippetFiles(string inputDirectory)
        {
            DirectoryInfo dir = new DirectoryInfo(inputDirectory);

            List<string> snippetFiles = new List<string>();

            foreach (var file in dir.EnumerateFiles("*.cs"))
            {
                snippetFiles.Add(file.FullName.ToLower());
            }
            foreach (var file in dir.EnumerateFiles("*.vb"))
            {
                snippetFiles.Add(file.FullName.ToLower());
            }

            foreach (var subDirectory in dir.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                foreach (var file in subDirectory.EnumerateFiles("*.cs"))
                {
                    snippetFiles.Add(file.FullName.ToLower());
                }
                foreach (var file in subDirectory.EnumerateFiles("*.vb"))
                {
                    snippetFiles.Add(file.FullName.ToLower());
                }
            }

            return snippetFiles;
        }

        private static void ListOrphanedSnippets(string inputDirectory, List<string> snippetFiles, bool deleteOrphanedSnippets)
        {
            // Get all files that could possibly link to the snippet files
            var files = GetAllMarkdownFiles(inputDirectory, out DirectoryInfo rootDirectory);

            if (files is null)
            {
                return;
            }

            Console.WriteLine($"Checking {snippetFiles.Count} snippet files.");

            int countOfOrphans = 0;
            // Prints out the snippet files that have zero references.
            StringBuilder output = new StringBuilder();

            // Keep track of which directories need to be deleted.
            // We can't delete them as we go because then our list of snippet files
            // will be inaccurate.
            List<string> directoriesToDelete = new List<string>();

            // Keep track of directories we know we have to preserve.
            List<string> directoriesToKeep = new List<string>();

            foreach (var snippetFile in snippetFiles)
            {
                FileInfo fi = new FileInfo(snippetFile);
                string snippetFileName = fi.Name;

                bool foundSnippetReference = false;

                // Check if there's a .csproj or .vbproj file in its ancestry.
                bool partOfProject = false;
                DirectoryInfo projectDir = GetDirectory(new DirectoryInfo(fi.DirectoryName), "*.??proj");
                if (projectDir != null)
                    partOfProject = true;

                if (!partOfProject)
                {
                    foreach (FileInfo markdownFile in files)
                    {
                        // Matches the following types of snippet syntax:
                        // :::code language="csharp" source="snippets/EventCounters/MinimalEventCounterSource.cs":::
                        // [!code-csharp[Violation#1](../code-quality/codesnippet/ca1010.cs)]
                        // [!code-csharp[Violation#1](../code-quality/codesnippet/ca1010.cs#snippet1)]

                        string regex = @"(\(|"")([^\)""\n]*\/" + snippetFileName + @")#?\w*(\)|"")";

                        foreach (Match match in Regex.Matches(File.ReadAllText(markdownFile.FullName), regex, RegexOptions.IgnoreCase))
                        {
                            if (!(match is null) && match.Length > 0)
                            {
                                string relativePath = match.Groups[2].Value.Trim();

                                if (relativePath != null)
                                {
                                    string fullPath;

                                    // Path could start with a tilde e.g. ~/snippets/ca1010.cs
                                    if (relativePath.StartsWith("~/"))
                                    {
                                        fullPath = Path.Combine(rootDirectory.FullName, relativePath.TrimStart('~', '/'));
                                    }
                                    else
                                    {
                                        // Construct the full path to the referenced snippet file
                                        fullPath = Path.Combine(markdownFile.DirectoryName, relativePath);
                                    }

                                    // Clean up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                                    fullPath = Path.GetFullPath(fullPath);

                                    if (String.Equals(snippetFile, fullPath, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        // This snippet file is not orphaned.
                                        foundSnippetReference = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (foundSnippetReference)
                            break;
                        // else check the next Markdown file.
                    }

                    if (!foundSnippetReference)
                    {
                        // The snippet file is orphaned (not used anywhere).
                        countOfOrphans++;
                        output.AppendLine(Path.GetFullPath(snippetFile));

                        if (deleteOrphanedSnippets)
                        {
                            File.Delete(snippetFile);
                        }
                    }
                }
                else
                {
                    // The code file is part of a project.
                    // If any descendants of the project file directory
                    // are referenced, then don't delete anything in the project file directory.

                    // If we already know this project directory is orphaned or unorphaned, move on.
                    if (directoriesToDelete.Contains(projectDir.FullName)
                        || directoriesToKeep.Contains(projectDir.FullName))
                        continue;

                    foreach (FileInfo markdownFile in files)
                    {
                        // Matches the following types of snippet syntax:
                        // :::code language="csharp" source="snippets/EventCounters/MinimalEventCounterSource.cs":::
                        // [!code-csharp[Violation#1](../code-quality/codesnippet/CSharp/ca1010.cs)]

                        // Search for a reference that includes the project directory name.
                        string regex = @"(\(|"")([^\)""\n]*" + projectDir.Name + @")\/[^\)""\n]*(\)|"")";

                        // Loop through all the matches in the file.
                        MatchCollection matches = Regex.Matches(File.ReadAllText(markdownFile.FullName), regex, RegexOptions.IgnoreCase);
                        foreach (Match match in matches)
                        {
                            if (!(match is null) && match.Length > 0)
                            {
                                string relativePath = match.Groups[2].Value.Trim();

                                if (relativePath != null)
                                {
                                    string fullPath;

                                    // Path could start with a tilde e.g. ~/snippets/stuff
                                    if (relativePath.StartsWith("~/"))
                                    {
                                        fullPath = Path.Combine(rootDirectory.FullName, relativePath.TrimStart('~', '/'));
                                    }
                                    else
                                    {
                                        // Construct the full path to the referenced directory.
                                        fullPath = Path.Combine(markdownFile.DirectoryName, relativePath);
                                    }

                                    // Clean up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                                    fullPath = Path.GetFullPath(fullPath);

                                    if (String.Equals(projectDir.FullName, fullPath, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        // This snippet file is not orphaned.
                                        foundSnippetReference = true;

                                        // Add the project directory to the known list of directories to keep (saves searching again).
                                        if (!directoriesToKeep.Contains(projectDir.FullName))
                                            directoriesToKeep.Add(projectDir.FullName);

                                        break;
                                    }
                                }
                            }
                        }

                        if (foundSnippetReference)
                            break;
                        // else check the next Markdown file.
                    }

                    if (!foundSnippetReference)
                    {
                        // The snippet file and its project directory is orphaned (not used anywhere).
                        if (!directoriesToDelete.Contains(projectDir.FullName))
                        {
                            directoriesToDelete.Add(projectDir.FullName);
                        }
                    }
                }
            }

            // Delete orphaned directories.
            Console.WriteLine($"Found {directoriesToDelete.Count} orphaned directories:\n");

            if (deleteOrphanedSnippets)
            {
                foreach (var directory in directoriesToDelete)
                {
                    Console.WriteLine(directory);
                    Directory.Delete(directory, true);
                }
            }

            Console.WriteLine($"\nFound {countOfOrphans} orphaned snippet files:\n");
            Console.WriteLine(output.ToString());
            Console.WriteLine("DONE");
        }
        #endregion

        private static string TryGetFullPath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch (PathTooLongException)
            {
                Console.WriteLine($"Unable to get path because it's too long: {path}\n");
                return null;
            }
        }

        #region Orphaned articles
        /// <summary>
        /// Lists the markdown files that aren't referenced from a TOC file.
        /// </summary>
        private static void ListOrphanedTopics(List<FileInfo> tocFiles, List<FileInfo> markdownFiles, bool deleteOrphanedTopics)
        {
            Dictionary<string, int> filesToKeep = new Dictionary<string, int>();

            Console.WriteLine("\nTopics not in any TOC file (that are also not includes or shared or misc):\n");

            bool IsTopicFile(FileInfo file) =>
                !file.FullName.Contains("\\includes\\") &&
                !file.FullName.Contains("\\_shared\\") &&
                !file.FullName.Contains("\\misc\\") &&
                String.Compare(file.Name, "TOC.md", true) != 0 &&
                String.Compare(file.Name, "index.md", true) != 0;

            List<FileInfo> orphanedFiles = new List<FileInfo>();

            Parallel.ForEach(markdownFiles.Where(IsTopicFile), markdownFile =>
            {
                var found = tocFiles.Any(tocFile => IsFileLinkedFromTocFile(markdownFile, tocFile));
                if (!found)
                {
                    orphanedFiles.Add(markdownFile);
                    Console.WriteLine(markdownFile.FullName);
                }
            });

            Console.WriteLine($"\nFound {orphanedFiles.Count} .md files that aren't referenced in a TOC.");

            // Delete files if the option is set.
            if (deleteOrphanedTopics)
            {
                Parallel.ForEach(markdownFiles, linkingFile =>
                {
                    CheckFileLinks(orphanedFiles, linkingFile, ref filesToKeep);
                });

                // Delete files that aren't linked to.
                foreach (var orphanedFile in orphanedFiles)
                {
                    if (!filesToKeep.ContainsKey(orphanedFile.FullName))
                        File.Delete(orphanedFile.FullName);
                }

                if (filesToKeep.Count > 0)
                {
                    Console.Write($"\nThe following {filesToKeep.Count} files *were not deleted* " +
                        $"because they're referenced in one or more files:\n\n");
                    foreach (var fileName in filesToKeep)
                    {
                        Console.WriteLine(fileName);
                    }
                }
                else
                {
                    Console.WriteLine($"\nDeleted {orphanedFiles.Count} files.");
                }
            }
        }

        private static bool IsFileLinkedFromTocFile(FileInfo linkedFile, FileInfo tocFile)
        {
            string text = File.ReadAllText(tocFile.FullName);

            // Example links .yml/.md:
            // href: ide/managing-external-tools.md
            // # [Managing External Tools](ide/managing-external-tools.md)

            string linkRegEx = tocFile.Extension.ToLower() == ".yml" ?
                @"href:(.*?" + linkedFile.Name + ")" :
                @"\]\((([^\)])*?" + linkedFile.Name + @")";

            // For each link that contains the file name...
            foreach (Match match in Regex.Matches(text, linkRegEx, RegexOptions.IgnoreCase))
            {
                // Get the file-relative path to the linked file.
                string relativePath = match.Groups[1].Value.Trim();

                // Remove any quotation marks
                relativePath = relativePath.Replace("\"", "");

                if (relativePath.StartsWith("/") || relativePath.StartsWith("http:") || relativePath.StartsWith("https:"))
                {
                    // The file is in a different repo, so ignore it.
                    continue;

                    // TODO - For links that start with "/", check if they are in the same repo.
                }

                if (relativePath != null)
                {
                    // Construct the full path to the referenced file
                    string fullPath = Path.Combine(tocFile.DirectoryName, relativePath);

                    // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                    fullPath = Path.GetFullPath(fullPath);

                    if (fullPath != null)
                    {
                        // See if our constructed path matches the actual file we think it is
                        if (String.Compare(fullPath, linkedFile.FullName, true) == 0)
                        {
                            return true;
                        }
                        else
                        {
                            // If we get here, the file name matched but the full path did not.
                        }
                    }
                }
            }

            // We did not find this file linked in the specified file.
            return false;
        }

        /// <summary>
        /// If linkingFile contains a link to any file in linkedFiles, add the file to filesToKeep.
        /// </summary>
        private static void CheckFileLinks(List<FileInfo> linkedFiles, FileInfo linkingFile, ref Dictionary<string, int> filesToKeep)
        {
            if (!File.Exists(linkingFile.FullName))
            {
                return;
            }

            string fileContents = File.ReadAllText(linkingFile.FullName);

            // Example links .yml/.md:
            // href: ide/managing-external-tools.md
            // [Managing External Tools](ide/managing-external-tools.md)

            foreach (var linkedFile in linkedFiles)
            {
                string linkRegEx = linkingFile.Extension.ToLower() == ".yml" ?
                        @"href:(.*?" + linkedFile.Name + ")" :
                        @"\]\((([^\)])*?" + linkedFile.Name + @")";

                // For each link that contains the file name...
                foreach (Match match in Regex.Matches(fileContents, linkRegEx, RegexOptions.IgnoreCase))
                {
                    // Get the file-relative path to the linked file.
                    string relativePath = match.Groups[1].Value.Trim();

                    // Remove any quotation marks
                    relativePath = relativePath.Replace("\"", "");

                    if (relativePath != null)
                    {
                        string fullPath;
                        try
                        {
                            // Construct the full path to the referenced file
                            fullPath = Path.Combine(linkingFile.DirectoryName, relativePath);
                        }
                        catch (ArgumentException e)
                        {
                            Console.WriteLine($"\nCaught exception while constructing full path " +
                                $"for '{relativePath}' in '{linkingFile.FullName}': {e.Message}");
                            throw;
                        }

                        // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                        fullPath = Path.GetFullPath(fullPath);
                        if (fullPath != null)
                        {
                            // See if our constructed path matches the actual file we think it is
                            if (String.Compare(fullPath, linkedFile.FullName, true) == 0)
                            {
                                // File is linked from another file.
                                if (filesToKeep.ContainsKey(linkedFile.FullName))
                                {
                                    // Increment the count of links to this file.
                                    filesToKeep[linkedFile.FullName]++;
                                }
                                else
                                {
                                    filesToKeep.Add(linkedFile.FullName, 1);
                                }
                            }
                            else
                            {
                                // This link did not match the full file name.
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Redirected files

        private static RedirectionFile LoadRedirectJson(FileInfo redirectsFile)
        {
            using (StreamReader reader = new StreamReader(redirectsFile.FullName))
            {
                string json = reader.ReadToEnd();

                try
                {
                    return JsonSerializer.Deserialize<RedirectionFile>(json);
                }
                catch (JsonException e)
                {
                    Console.WriteLine($"Caught exception while reading the {redirectsFile.FullName} file: {e.Message} {e.InnerException?.Message}");
                    return null;
                }
            }
        }

        private static void FormatRedirectionFile(FileInfo redirectsFileInfo)
        {
            // Deserialize the redirect entries.
            RedirectionFile RedirectionFile = LoadRedirectJson(redirectsFileInfo);
            if (RedirectionFile is null)
            {
                Console.WriteLine("Deserialization failed.");
                return;
            }

            // Serialize the redirects back to the file.
            WriteRedirectJson(redirectsFileInfo.FullName, RedirectionFile);
        }

        private static void WriteRedirectJson(string filePath, RedirectionFile redirects)
        {
            JsonSerializerOptions options = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(redirects, options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// For each source path in a redirect entry, check if it's been clicked in any locale
        /// in the last X days. If not, remove the redirect entry from the redirection file.
        /// </summary>
        private static void TrimRedirectEntries(FileInfo redirectsFileInfo, Dictionary<string, string> docsets, int lookbackDays, string outputFile)
        {
            throw new NotImplementedException();

            //RedirectionFile RedirectionFile = LoadRedirectJson(redirectsFileInfo);
            //if (RedirectionFile is null)
            //{
            //    Console.WriteLine("Deserialization of redirection file failed.");
            //    return;
            //}

            //// Set up Kusto client.
            //KustoConnectionStringBuilder builder = new KustoConnectionStringBuilder("https://cgadataout.kusto.windows.net/;Fed=true", "CustomerTouchPoint");
            //var client = Kusto.Data.Net.Client.KustoClientFactory.CreateCslQueryProvider(builder);

            //// Tracks which redirects to remove.
            //var noClickRedirects = new List<Redirect>();
            //// For link-click output.
            //var sb = new StringBuilder();

            ////for (int i = 0; i < RedirectionFile.redirections.Count && i < 50; i++)
            //for (int i = 0; i < RedirectionFile.redirections.Count; i++)
            //{
            //    // Output for long-running jobs.
            //    if ((i % 50 == 0) && (i > 0))
            //    {
            //        Console.WriteLine($"Progress update: Checked {i} of {RedirectionFile.redirections.Count} redirects.");
            //    }

            //    Redirect redirect = RedirectionFile.redirections[i];

            //    // If the redirect has a moniker, we don't know how to construct the URL, so ignore it.
            //    // TODO: To query page views on redirects with a moniker, 
            //    // probably just add "?view=<moniker>" to the end of the URL.
            //    // But what if there are multiple monikers?
            //    if (redirect.monikers != null)
            //        continue;

            //    // Trim off the file extension.
            //    string trimmedPath = redirect.source_path[0..redirect.source_path.LastIndexOf('.')];

            //    // If the file name is exactly "index", remove it from the URL.
            //    if (String.Compare(trimmedPath[(trimmedPath.LastIndexOf('/') + 1)..].ToLowerInvariant(), "index") == 0)
            //    {
            //        trimmedPath = trimmedPath[0..(trimmedPath.LastIndexOf('/') + 1)];
            //    }

            //    string urlBasePath = null;

            //    // Trim off the beginning of the path and obtain the corresponding base path for the URL.
            //    foreach (var docset in docsets)
            //    {
            //        if (trimmedPath.StartsWith(docset.Key))
            //        {
            //            trimmedPath = trimmedPath[(docset.Key.Length + 1)..];
            //            urlBasePath = docset.Value;
            //            break;
            //        }
            //    }

            //    if (String.IsNullOrEmpty(urlBasePath))
            //    {
            //        // Ignore this redirect.
            //        continue;
            //    }

            //    // Construct the URL to the article.
            //    string sourcePathUrl = $"{urlBasePath}/{trimmedPath}";

            //    long clicks = -1;
            //    try
            //    {
            //        clicks = NumberOfClicks(sourcePathUrl);
            //    }
            //    catch (Exception ex)
            //    {
            //        if (ex is KustoClientException || ex is KustoServiceException)
            //        {
            //            // Finish up with the data we have and then exit.
            //            Console.WriteLine("Caught a KustoClientException or KustoServiceException exception. Exiting the program.");
            //            break;
            //        }

            //        throw;
            //    }

            //    // Differentiate between invalid URL and no page views
            //    if (clicks == 0)
            //    {
            //        string liveUrl = String.Concat("https://docs.microsoft.com/en-us", sourcePathUrl);
            //        bool isValidUrl = false;

            //        try
            //        {
            //            isValidUrl = IsUrlValid(liveUrl);
            //        }
            //        catch (Exception ex)
            //        {
            //            if (ex is KustoClientException || ex is KustoServiceException)
            //            {
            //                // Finish up with the data we have and then exit.
            //                Console.WriteLine("Caught a KustoClientException or KustoServiceException exception. Exiting the program.");
            //                break;
            //            }

            //            throw;
            //        }

            //        if (isValidUrl)
            //        {
            //            // It's a valid URL with no recent page views.
            //            noClickRedirects.Add(redirect);
            //        }
            //        else
            //        {
            //            // Invalid URL, so don't delete redirect entry
            //            // in case we constructed the URL incorrectly.
            //            clicks = -1;
            //        }
            //    }

            //    sb.AppendLine($"{sourcePathUrl}\t{clicks}");
            //}

            //// Remove any defunct redirects.
            //foreach (var redirect in noClickRedirects)
            //{
            //    RedirectionFile.redirections.Remove(redirect);
            //}

            //// Serialize the new list of redirects to the file.
            //WriteRedirectJson(redirectsFileInfo.FullName, RedirectionFile);

            //// Write the link-click output to a file.
            //File.WriteAllText(outputFile, sb.ToString());

            //Console.WriteLine($"\nRemoved a total of {noClickRedirects.Count} inactive redirect entries. Tab-separated page view data written to {outputFile}.");

            //long NumberOfClicks(string url)
            //{
            //    string query = @"PageView | where Site == ""docs.microsoft.com"" | where StartDateTime > ago(" + lookbackDays +
            //        @"d) | where Url endswith """ + url + @""" | summarize PageViews=dcount(PageViewId) by ContentId";

            //    IDataReader reader = null;
            //    try
            //    {
            //        reader = client.ExecuteQuery(query);
            //    }
            //    catch (DataTableIncompleteDataStreamException)
            //    {
            //        // Just ignore this redirect for now.
            //        Console.WriteLine("Caught DataTableIncompleteDataStreamException. Will continue on with the next redirect.");
            //        return -1;
            //    }

            //    long numClicks = 0;

            //    if (reader != null && reader.FieldCount == 2)
            //    {
            //        if (reader.Read())
            //        {
            //            numClicks = reader.GetInt64(reader.GetOrdinal("PageViews"));
            //        }

            //        reader.Close();
            //    }

            //    return numClicks;
            //}

            //bool IsUrlValid(string LiveUrl)
            //{
            //    string query = @"TopicMetadata | where LiveUrl == """ + LiveUrl + @""" | distinct ContentId";

            //    IDataReader reader = null;
            //    try
            //    {
            //        reader = client.ExecuteQuery(query);
            //    }
            //    catch (DataTableIncompleteDataStreamException)
            //    {
            //        // Could still be valid, but return false just in case.
            //        return false;
            //    }

            //    bool isValid = false;
            //    if (reader != null && reader.FieldCount == 1)
            //    {
            //        if (reader.Read())
            //        {
            //            isValid = true;
            //        }

            //        reader.Close();
            //    }

            //    return isValid;
            //}
        }

        /// <summary>
        /// For each target URL, see if it's a source_path somewhere else.
        /// If so, replace the original target URL with the new target URL.
        /// </summary>
        private static void RemoveRedirectHops(FileInfo redirectsFile, Dictionary<string, string> docsets, string rootPath)
        {
            RedirectionFile RedirectionFile = LoadRedirectJson(redirectsFile);
            if (RedirectionFile is null)
            {
                Console.WriteLine("Deserialization of redirection file failed.");
                return;
            }

            string fileText = File.ReadAllText(redirectsFile.FullName);

            // Load the sources and targets into a dictionary for easier look up.
            Dictionary<string, string> redirectsLookup = new Dictionary<string, string>(RedirectionFile.redirections.Count);
            foreach (Redirect redirect in RedirectionFile.redirections)
            {
                string fullPath = null;
                if (redirect.source_path != null)
                {
                    // Construct the full path to the redirected file
                    fullPath = Path.Combine(redirectsFile.DirectoryName, redirect.source_path);
                }
                else if (redirect.source_path_from_root != null)
                {
                    // Construct the full path to the redirected file
                    fullPath = Path.Combine(rootPath, redirect.source_path_from_root.Substring(1));
                }

                redirectsLookup.Add(Path.GetFullPath(fullPath), redirect.redirect_url);
            }

            foreach (var redirectPair in redirectsLookup)
            {
                string currentTarget = redirectPair.Value;

                string docsetRootFolderName = null;
                string basePathUrl = null;

                foreach (var docset in docsets)
                {
                    if (currentTarget.StartsWith(docset.Value + "/"))
                    {
                        docsetRootFolderName = docset.Key;
                        basePathUrl = docset.Value;
                        break;
                    }
                }

                if (docsetRootFolderName == null)
                {
                    // Redirect URL is in a different docset/repo, so ignore it.
                    continue;
                }

                // Formulate the full path for the redirect URL (so it matches the dictionary key format).
                string targetPath = currentTarget.Remove(0, basePathUrl.Length) + ".md";
                string normalizedTargetPath = Path.GetFullPath(Path.Combine(rootPath, docsetRootFolderName, targetPath[1..] /* Removes the initial forward slash */));

                // If we enter this loop, the target of a redirect is also the source of one or more redirects.
                // Keep looping till you find the final target.
                while (redirectsLookup.ContainsKey(normalizedTargetPath))
                {
                    // Avoid an infinite loop by checking that this isn't the same key/value pair.
                    if (String.Equals(redirectsLookup[normalizedTargetPath], currentTarget))
                    {
                        Console.WriteLine($"\nWARNING: {normalizedTargetPath} REDIRECTS TO ITSELF. PLEASE FIND A DIFFERENT REDIRECT URL.\n");
                        break;
                    }

                    currentTarget = redirectsLookup[normalizedTargetPath];

                    targetPath = currentTarget.Remove(0, basePathUrl.Length) + ".md";
                    normalizedTargetPath = Path.GetFullPath(Path.Combine(rootPath, docsetRootFolderName, targetPath[1..] /* Removes the initial forward slash */));
                }

                if (redirectPair.Value != currentTarget)
                {
                    Console.WriteLine($"Replacing target URL '{redirectPair.Value}' with '{currentTarget}'.");

                    fileText = fileText.Replace($"\"redirect_url\": \"{redirectPair.Value}\"", $"\"redirect_url\": \"{currentTarget}\"");
                }
            }

            // Write the redirects back to the file.
            File.WriteAllText(redirectsFile.FullName, fileText);
        }

        private static IList<Redirect> GetAllRedirectedFiles(FileInfo redirectsFile, string rootPath)
        {
            RedirectionFile RedirectionFile = LoadRedirectJson(redirectsFile);

            if (RedirectionFile is null)
            {
                Console.WriteLine("Deserialization failed.");
                return null;
            }

            foreach (Redirect redirect in RedirectionFile.redirections)
            {
                if (redirect.source_path != null)
                {
                    // Construct the full path to the redirected file
                    string fullPath = Path.Combine(redirectsFile.DirectoryName, redirect.source_path);

                    // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                    fullPath = Path.GetFullPath(fullPath);

                    redirect.source_path_absolute = fullPath;
                }
                else if (redirect.source_path_from_root != null)
                {
                    // Construct the full path to the redirected file
                    string fullPath = Path.Combine(rootPath, redirect.source_path_from_root.Substring(1));

                    // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                    fullPath = Path.GetFullPath(fullPath);

                    redirect.source_path_absolute = fullPath;
                }
            }

            return RedirectionFile.redirections;
        }

        private static void ReplaceRedirectedLinks(IList<Redirect> redirects, List<FileInfo> linkingFiles, string docsetName)
        {
            Dictionary<string, Redirect> redirectLookup = Enumerable.ToDictionary<Redirect, string>(redirects, r => r.source_path_absolute);

            // For each file...
            foreach (var linkingFile in linkingFiles)
            {
                bool foundOldLink = false;
                StringBuilder output = new StringBuilder($"FILE '{linkingFile.FullName}' contains the following link(s) to redirected files:\n\n");

                string text = File.ReadAllText(linkingFile.FullName);

                // Matches link with optional #bookmark on the end.
                string linkRegEx = linkingFile.Extension.ToLower() == ".yml" ?
                    @"href:(.*\.md)(#[\w-]+)?" :
                    @"\]\(([^\)]*\.md)(#[\w-]+)?\)";

                // For each link in the file...
                foreach (Match match in Regex.Matches(text, linkRegEx, RegexOptions.IgnoreCase))
                {
                    // Get the file-relative path to the linked file.
                    string relativePath = match.Groups[1].Value.Trim();

                    if (relativePath.StartsWith("http"))
                    {
                        // This could be an absolute URL to a file in the repo, so check.
                        string httpRegex = @"https?:\/\/docs.microsoft.com\/([A-z][A-z]-[A-z][A-z]\/)?" + docsetName + @"\/";
                        var httpMatch = Regex.Match(relativePath, httpRegex, RegexOptions.IgnoreCase);

                        if (!httpMatch.Success)
                        {
                            // The file is in a different repo, so ignore it.
                            continue;
                        }

                        // Chop off the https://docs.microsoft.com/docset/ part of the path.
                        relativePath = relativePath.Substring(httpMatch.Value.Length);
                    }

                    // Remove any quotation marks
                    relativePath = relativePath.Replace("\"", "");

                    string fullPath = null;
                    try
                    {
                        // Construct the full path to the linked file.
                        fullPath = Path.Combine(linkingFile.DirectoryName, relativePath);
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine($"Ignoring the link {relativePath} due to possibly invalid format.\n");
                        continue;
                    }

                    // Clean up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                    try
                    {
                        fullPath = Path.GetFullPath(fullPath);
                    }
                    catch (NotSupportedException)
                    {
                        //Console.WriteLine($"Found a possibly malformed link '{match.Groups[0].Value}' in '{linkingFile.FullName}'.\n");
                        break;
                    }

                    if (fullPath != null)
                    {
                        // See if our constructed path matches a source file in the dictionary of redirects.
                        if (redirectLookup.ContainsKey(fullPath))
                        {
                            foundOldLink = true;
                            output.AppendLine($"'{relativePath}'");

                            string redirectURL = redirectLookup[fullPath].redirect_url;

                            // Add the bookmark back on, in case it applies to the new target.
                            if (!String.IsNullOrEmpty(match.Groups[2].Value))
                                redirectURL = redirectURL + match.Groups[2].Value;

                            output.AppendLine($"REPLACING '({relativePath})' with '({redirectURL})'.");

                            // Replace the link.
                            if (linkingFile.Extension.ToLower() == ".md")
                            {
                                text = text.Replace(match.Groups[0].Value, $"]({redirectURL})");
                            }
                            else // .yml file
                            {
                                text = text.Replace(match.Groups[0].Value, $"href: {redirectURL}");
                            }
                            File.WriteAllText(linkingFile.FullName, text);
                        }
                    }
                }

                if (foundOldLink)
                {
                    Console.WriteLine(output.ToString());
                }
            }
        }
        #endregion

        #region Popular files
        /// <summary>
        /// Finds topics that appear more than once, either in one TOC.md file, or multiple TOC.md files.
        /// </summary>
        private static void ListPopularFiles(List<FileInfo> tocFiles, List<FileInfo> markdownFiles)
        {
            bool found = false;
            StringBuilder output = new StringBuilder("The following files appear in more than one TOC file:\n\n");

            // Keep a hash table of each topic path with the number of times it's referenced
            Dictionary<string, int> topics = markdownFiles.ToDictionary<FileInfo, string, int>(mf => mf.FullName, mf => 0);

            foreach (var markdownFile in markdownFiles)
            {
                // If the file is in the Includes directory, ignore it
                if (markdownFile.FullName.Contains("\\includes\\"))
                    continue;

                foreach (var tocFile in tocFiles)
                {
                    if (IsFileLinkedFromFile(markdownFile, tocFile))
                    {
                        topics[markdownFile.FullName]++;
                    }
                }
            }

            // Now spit out the topics that appear more than once.
            foreach (var topic in topics)
            {
                if (topic.Value > 1)
                {
                    found = true;
                    output.AppendLine(topic.Key);
                }
            }

            // Only write the StringBuilder to the console if we found a topic referenced from more than one TOC file.
            if (found)
            {
                Console.Write(output.ToString());
            }
        }
        #endregion

        #region Generic helper methods
        /// <summary>
        /// Looks for the specified file in the specified directory, and if not found,
        /// in all parent directories up to the disk root directory.
        /// </summary>
        private static FileInfo GetFileHereOrInParent(string inputDirectory, string fileName)
        {
            DirectoryInfo dir = new DirectoryInfo(inputDirectory);

            try
            {
                while (dir.GetFiles(fileName, SearchOption.TopDirectoryOnly).Length == 0)
                {
                    dir = dir.Parent;

                    // Loop exit condition.
                    if (dir == dir.Root)
                        return null;
                }

                return dir.GetFiles(fileName, SearchOption.TopDirectoryOnly)[0];
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"Could not find directory {dir.FullName}");
                throw;
            }
        }

        private static string GetDocsetAbsolutePath(string docfxFilePath, DirectoryInfo inputDirectory)
        {
            // Deserialize the docfx.json file.
            DocFx docfx = LoadDocfxFile(Path.Combine(docfxFilePath, "docfx.json"));
            if (docfx == null)
            {
                return null;
            }

            string docsetPath = null;

            // If there's more than one docset, choose the one that includes the input directory.
            foreach (var entry in docfx.build.content)
            {
                if (entry.src == null)
                    continue;

                if (entry.src.TrimEnd('/') == ".")
                    docsetPath = docfxFilePath;
                else
                    docsetPath = Path.GetFullPath(entry.src, docfxFilePath);

                // Check that it's a parent (or the same) directory as the input directory.
                if (inputDirectory.FullName.StartsWith(docsetPath))
                    break;
            }

            return docsetPath;
        }

        internal static string GetUrlBasePath(DirectoryInfo docFxDirectory)
        {
            string docfxFilePath = Path.Combine(docFxDirectory.FullName, "docfx.json");
            string urlBasePath = null;

            // Deserialize the docfx.json file.
            DocFx docfx = LoadDocfxFile(docfxFilePath);
            if (docfx == null)
            {
                return null;
            }

            // Hack: Parse URL base path out of breadcrumbPath. Examples:
            // "breadcrumb_path": "/visualstudio/_breadcrumb/toc.json"
            // "breadcrumb_path": "/windows/uwp/breadcrumbs/toc.json"
            // "breadcrumb_path": "/dotnet/breadcrumb/toc.json"
            // "breadcrumb_path": "breadcrumb/toc.yml"  <--Need to handle this.

            string? breadcrumbPath = docfx.build.globalMetadata.breadcrumb_path;

            if (breadcrumbPath is not null)
            {
                // Remove everything after and including the second last / character.
                if (breadcrumbPath.Contains('/'))
                {
                    breadcrumbPath = breadcrumbPath[0..breadcrumbPath.LastIndexOf('/')];
                    if (breadcrumbPath.Contains('/'))
                    {
                        urlBasePath = breadcrumbPath[0..breadcrumbPath.LastIndexOf('/')];
                    }
                }
            }

            if (!String.IsNullOrEmpty(urlBasePath))
            {
                Console.WriteLine($"Is '{urlBasePath}' the correct URL base path for your docs (y or n)?");
                char key = Console.ReadKey().KeyChar;

                if (key == 'y' || key == 'Y')
                    return urlBasePath;
            }

            Console.WriteLine($"\nWhat's the URL base path for articles in the `{docFxDirectory.FullName}` directory? (Example: /aspnet/core)");
            return Console.ReadLine();
        }

        private static List<string> GetRedirectionFiles(FileInfo opsConfigFile)
        {
            // Deserialize the OPS config file.
            OPSConfig config = LoadOPSJson(opsConfigFile);
            if (config == null || config.redirection_files == null)
            {
                return new List<string>() { ".openpublishing.redirection.json" };
            }
            else
                return config.redirection_files;
        }

        /// <summary>
        /// Pulls docset information, including URL base path, from the OPS config and docfx.json files.
        /// </summary>
        private static Dictionary<string, string> GetDocsetInfo(FileInfo opsConfigFile)
        {
            // Deserialize the OPS config file to get build source folders.
            OPSConfig config = LoadOPSJson(opsConfigFile);
            if (config == null)
            {
                Console.WriteLine("Could not deserialize OPS config file.");
                return null;
            }

            var mappingInfo = new Dictionary<string, string>();
            foreach (var sourceFolder in config.docsets_to_publish)
            {
                // Deserialize the corresponding docfx.json file.
                string docfxFilePath = Path.Combine(opsConfigFile.DirectoryName, sourceFolder.build_source_folder, "docfx.json");
                DocFx docfx = LoadDocfxFile(docfxFilePath);
                if (docfx == null)
                {
                    continue;
                }

                string? breadcrumbPath = docfx.build.globalMetadata.breadcrumb_path;
                string basePath = "";

                if (breadcrumbPath is not null)
                {
                    // Remove everything after and including the second last / character.
                    breadcrumbPath = breadcrumbPath[0..breadcrumbPath.LastIndexOf('/')];
                    basePath = breadcrumbPath[0..breadcrumbPath.LastIndexOf('/')];
                }

                if (breadcrumbPath is null || basePath.StartsWith('~'))
                {
                    // We can't get the URL base path automatically, so ask the user.
                    Console.WriteLine($"What's the URL base path for articles in the `{sourceFolder.build_source_folder}` directory? (Example: /aspnet/core)");
                    basePath = Console.ReadLine();
                }

                // There can be more than one "src" path.
                foreach (var item in docfx.build.content)
                {
                    // Examples:
                    // "src": "./vs-2015"
                    // "src": "./"

                    // Construct the full path to where the docset files are located.
                    string docsetFilePath = sourceFolder.build_source_folder;

                    if (item.src != null)
                    {
                        if (item.src != ".")
                        {
                            // Trim "./" off the beginning, if it's there.
                            if (item.src.StartsWith("./"))
                                item.src = item.src[2..];

                            if (item.src.Length > 0)
                            {
                                if (docsetFilePath == ".")
                                    docsetFilePath = item.src;
                                else
                                    docsetFilePath = String.Concat(docsetFilePath, "/", item.src);
                            }
                        }

                        if (!mappingInfo.ContainsKey(docsetFilePath))
                            mappingInfo.Add(docsetFilePath, basePath);
                    }
                }
            }

            return mappingInfo;
        }

        // Classes for deserialization.
        class OPSConfig
        {
            public List<Docset> docsets_to_publish { get; set; }
            public List<string> redirection_files { get; set; }
        }
        class Docset
        {
            public string build_source_folder { get; set; }
        }
        class DocFx
        {
            public Build build { get; set; }
        }
        class Build
        {
            public GlobalMetadata globalMetadata { get; set; }
            public List<Content> content { get; set; }
        }
        class Content
        {
            public string src { get; set; }
            public string dest { get; set; }
        }
        class GlobalMetadata
        {
            public string breadcrumb_path { get; set; }
        }

        /// <summary>
        /// Deserialize OPS config file.
        /// </summary>
        private static OPSConfig LoadOPSJson(FileInfo opsFile)
        {
            using (StreamReader reader = new StreamReader(opsFile.FullName))
            {
                string json = reader.ReadToEnd();

                try
                {
                    return JsonSerializer.Deserialize<OPSConfig>(json);
                }
                catch (JsonException e)
                {
                    Console.WriteLine($"Caught exception while reading OPS config file: {e.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Deserialize docfx.json file.
        /// </summary>
        private static DocFx LoadDocfxFile(string docfxFilePath)
        {
            using (StreamReader reader = new StreamReader(docfxFilePath))
            {
                string json = reader.ReadToEnd();

                try
                {
                    return JsonSerializer.Deserialize<DocFx>(json);
                }
                catch (JsonException e)
                {
                    Console.WriteLine($"Caught exception while reading docfx.json file: {e.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Checks if the specified file path is referenced in the specified file.
        /// </summary>
        private static bool IsFileLinkedFromFile(FileInfo linkedFile, FileInfo linkingFile)
        {
            if (!File.Exists(linkingFile.FullName))
            {
                return false;
            }

            foreach (var line in File.ReadAllLines(linkingFile.FullName))
            {
                // Example links .yml/.md:
                // href: ide/managing-external-tools.md
                // [Managing External Tools](ide/managing-external-tools.md)

                string linkRegEx = linkingFile.Extension.ToLower() == ".yml" ?
                    @"href:(.*?" + linkedFile.Name + ")" :
                    @"\]\((([^\)])*?" + linkedFile.Name + @")";

                // For each link that contains the file name...
                foreach (Match match in Regex.Matches(line, linkRegEx, RegexOptions.IgnoreCase))
                {
                    // Get the file-relative path to the linked file.
                    string relativePath = match.Groups[1].Value.Trim();

                    // Remove any quotation marks
                    relativePath = relativePath.Replace("\"", "");

                    if (relativePath != null)
                    {
                        string fullPath;
                        try
                        {
                            // Construct the full path to the referenced file
                            fullPath = Path.Combine(linkingFile.DirectoryName, relativePath);
                        }
                        catch (ArgumentException e)
                        {
                            Console.WriteLine($"\nCaught exception while constructing full path " +
                                $"for '{relativePath}' in '{linkingFile.FullName}': {e.Message}");
                            throw;
                        }

                        // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                        fullPath = Path.GetFullPath(fullPath);
                        if (fullPath != null)
                        {
                            // See if our constructed path matches the actual file we think it is
                            if (String.Compare(fullPath, linkedFile.FullName, true) == 0)
                            {
                                return true;
                            }
                            else
                            {
                                // If we get here, the file name matched but the full path did not.
                            }
                        }
                    }
                }
            }

            // We did not find this file linked in the specified file.
            return false;
        }

        /// <summary>
        /// Gets all *.md files recursively, starting in the specified directory.
        /// </summary>
        private static List<FileInfo> GetMarkdownFiles(string directoryPath)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);
            return dir.EnumerateFiles("*.md", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Gets all *.yml files recursively, starting in the specified directory.
        /// </summary>
        private static List<FileInfo> GetYAMLFiles(string directoryPath)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);
            return dir.EnumerateFiles("*.yml", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Gets all *.yml files recursively, starting in the ancestor directory that contains docfx.json.
        /// </summary>
        internal static List<FileInfo> GetAllYamlFiles(string directoryPath, out DirectoryInfo rootDirectory)
        {
            // Look further up the path until we find docfx.json
            rootDirectory = GetDirectory(new DirectoryInfo(directoryPath), "docfx.json");

            if (rootDirectory is null)
                return null;

            return rootDirectory.EnumerateFiles("*.yml", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Gets all *.md files recursively, starting in the ancestor directory that contains docfx.json.
        /// </summary>
        internal static List<FileInfo> GetAllMarkdownFiles(string directoryPath, out DirectoryInfo rootDirectory)
        {
            // Look further up the path until we find docfx.json
            rootDirectory = GetDirectory(new DirectoryInfo(directoryPath), "docfx.json");

            if (rootDirectory is null)
                return null;

            return rootDirectory.EnumerateFiles("*.md", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Gets all TOC.* files recursively, starting in the specified directory if it contains "docfx.json" file.
        /// Otherwise it looks up the directory path until it finds a "docfx.json" file. Then it starts the recursive search
        /// for TOC.* files from that directory.
        /// </summary>
        private static List<FileInfo> GetTocFiles(string directoryPath)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);

            // Look further up the path until we find docfx.json
            dir = GetDirectory(dir, "docfx.json");

            if (dir is null)
                return null;

            return dir.EnumerateFiles("TOC.*", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Returns the specified directory if it contains a file with the specified name.
        /// Otherwise returns the nearest parent directory that contains a file with the specified name.
        /// </summary>
        internal static DirectoryInfo GetDirectory(DirectoryInfo dir, string fileName)
        {
            try
            {
                while (dir.GetFiles(fileName, SearchOption.TopDirectoryOnly).Length == 0)
                {
                    dir = dir.Parent;

                    if (dir == dir?.Root)
                    {
                        return null;
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"\nCould not find directory {dir.FullName}");
                return null;
            }

            return dir;
        }
        #endregion
    }
}