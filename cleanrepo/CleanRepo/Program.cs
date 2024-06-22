using CleanRepo.Extensions;
using CommandLine;
using Microsoft.Build.Construction;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tesseract;
using static System.Net.Mime.MediaTypeNames;

namespace CleanRepo;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Annoying")]
static class Program
{
    static void Main(string[] args)
    {
#if DEBUG
        // TODO: Consider using launchSettings.json with command line args instead:
        //
        //   {
        //     "profiles": {
        //       "CleanRepo": {
        //         "commandName": "Project",
        //         "commandLineArgs": "--orphaned-images\r\n--base-path=\"/dotnet\""
        //       }
        //     }
        //   }
        //
        // ... to avoid hardcoded values in DEBUG preprocessor directives like this:
        args = new[] {
        "--filter-images-for-text",
        "--filter-text-json-file=c:\\Users\\diberry\\repos\\filter-text.json",
        "--url-base-path=/azure/developer/javascript",
        "--ocr-model-directory=c:\\Users\\diberry\\repos\\temp\\tesseract\\tessdata_fast",
        "--articles-directory=c:\\Users\\diberry\\repos\\writing\\docs\\azure-dev-docs-pr-2\\articles",
        "--media-directory=c:\\Users\\diberry\\repos\\writing\\docs\\azure-dev-docs-pr-2\\articles\\javascript\\media"};
        //args = new[] { "--orphaned-snippets", "--relative-links", "--remove-hops", "--replace-redirects", "--orphaned-includes", "--orphaned-articles", "--orphaned-images",
        //"--articles-directory=c:\\users\\gewarren\\docs\\docs\\fundamentals", "--media-directory=c:\\users\\gewarren\\docs\\docs\\core",
        //"--includes-directory=c:\\users\\gewarren\\docs\\includes", "--snippets-directory=c:\\users\\gewarren\\docs\\samples\\snippets\\csharp\\vs_snippets_clr",
        //"--docfx-directory=c:\\users\\gewarren\\docs", "--url-base-path=/dotnet", "--delete=false"};
#endif

        /*
         Catalog images with text

        args = new[] {
        "--catalog-images-with-text", 
        "--url-base-path=/azure/developer/javascript",
        "--ocr-model-directory=c:\\Users\\diberry\\repos\\temp\\tesseract\\tessdata_fast",
        "--articles-directory=c:\\Users\\diberry\\repos\\writing\\docs\\azure-dev-docs-pr-2\\articles",
        "--media-directory=c:\\Users\\diberry\\repos\\writing\\docs\\azure-dev-docs-pr-2\\articles\\javascript\\media"};

         */

        /*
         Filter images for text

        args = new[] {
        "--filter-images-for-text",
        "--filter-text-json-file=c:\\Users\\diberry\\repos\\filter-text.json",
        "--url-base-path=/azure/developer/javascript",
        "--ocr-model-directory=c:\\Users\\diberry\\repos\\temp\\tesseract\\tessdata_fast",
        "--articles-directory=c:\\Users\\diberry\\repos\\writing\\docs\\azure-dev-docs-pr-2\\articles",
        "--media-directory=c:\\Users\\diberry\\repos\\writing\\docs\\azure-dev-docs-pr-2\\articles\\javascript\\media"};

 */




        Parser.Default.ParseArguments<Options>(args).WithParsed(RunOptions);
    }

    static void RunOptions(Options options)
    {
        // Nothing to do.
        if (options.FindOrphanedArticles is false &&
            options.FindOrphanedImages is false &&
            options.CatalogImages is false &&
            options.FindOrphanedIncludes is false &&
            options.FindOrphanedSnippets is false &&
            options.ReplaceRedirectTargets is false &&
            options.ReplaceWithRelativeLinks is false &&
            options.CatalogImagesWithText is false &&
            options.FilterImagesForText is false &&
            options.RemoveRedirectHops is false)
        {
            Console.WriteLine("\nYou didn't specify which function to perform. To see options, use 'CleanRepo.exe -?'.");
            return;
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        string? startDirectory;
        if (!string.IsNullOrEmpty(options.DocFxDirectory))
            startDirectory = options.DocFxDirectory;
        else if (!string.IsNullOrEmpty(options.ArticlesDirectory))
            startDirectory = options.ArticlesDirectory;
        else if (!string.IsNullOrEmpty(options.MediaDirectory))
            startDirectory = options.MediaDirectory;
        else if (!string.IsNullOrEmpty(options.SnippetsDirectory))
            startDirectory = options.SnippetsDirectory;
        else if (!string.IsNullOrEmpty(options.IncludesDirectory))
            startDirectory = options.IncludesDirectory;
        else
        {
            Console.WriteLine("\nEnter the path to the directory that contains the docfx.json file for the docset:\n");
            options.DocFxDirectory = Console.ReadLine();
            startDirectory = options.DocFxDirectory;
        }

        if (string.IsNullOrEmpty(startDirectory) || !Directory.Exists(startDirectory))
        {
            Console.WriteLine($"\nThe {startDirectory} directory doesn't exist.");
            return;
        }

        if (string.IsNullOrEmpty(options.UrlBasePath))
        {
            Console.WriteLine("\nEnter the URL base path for this docset, for example, '/dotnet' or '/windows/uwp':\n");
            options.UrlBasePath = Console.ReadLine();
        }

        // Initialize the DocFxRepo object for all options.
        var docFxRepo = new DocFxRepo(startDirectory, options.UrlBasePath!);
        if (docFxRepo.DocFxDirectory is null)
        {
            Console.WriteLine($"\nCouldn't find docfx.json file in '{startDirectory}' or an ancestor directory...exiting.");
            return;
        }

        // Determine if we're to delete orphans (or just report them).
        if (options.FindOrphanedImages
            || options.FindOrphanedSnippets
            || options.FindOrphanedIncludes
            || options.FindOrphanedArticles)
        {
            if (options.Delete is null)
            {
                options.Delete = false;
                Console.WriteLine("\nDo you want to delete orphans (y or n)?");
                ConsoleKeyInfo info = Console.ReadKey();
                if (info.KeyChar == 'y' || info.KeyChar == 'Y')
                    options.Delete = true;
            }
        }

        // Find orphaned articles.
        if (options.FindOrphanedArticles)
        {
            if (string.IsNullOrEmpty(options.ArticlesDirectory))
            {
                Console.WriteLine("\nEnter the path to the directory that you want to check for orphaned articles:\n");
                options.ArticlesDirectory = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(options.ArticlesDirectory) || !Directory.Exists(options.ArticlesDirectory))
            {
                Console.WriteLine($"\nThe {options.ArticlesDirectory} directory doesn't exist.");
                return;
            }

            // Make sure the searchable directory is part of the same DocFx docset.
            if (!options.ArticlesDirectory.IsSubdirectoryOf(docFxRepo.DocFxDirectory!.FullName))
            {
                Console.WriteLine($"'{options.ArticlesDirectory}' is not a child of the docfx.json file's directory '{docFxRepo.DocFxDirectory}'.");
                return;
            }

            Console.WriteLine($"\nSearching the '{options.ArticlesDirectory}' directory and its subdirectories for orphaned articles...");

            List<FileInfo> markdownFiles = GetMarkdownFiles(options.ArticlesDirectory, "snippets");

            if (docFxRepo.AllTocFiles is null || markdownFiles is null)
                return;

            ListOrphanedArticles(docFxRepo.AllTocFiles, markdownFiles, options.Delete!.Value);
        }

        // Find orphaned images
        if (options.FindOrphanedImages)
        {
            if (string.IsNullOrEmpty(options.MediaDirectory))
            {
                Console.WriteLine("\nEnter the path to the directory that you want to check for orphaned media files:\n");
                options.MediaDirectory = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(options.MediaDirectory) || !Directory.Exists(options.MediaDirectory))
            {
                Console.WriteLine($"\nThe {options.MediaDirectory} directory doesn't exist.");
                return;
            }

            // Make sure the searchable directory is part of the same DocFx docset.
            if (!options.MediaDirectory.IsSubdirectoryOf(docFxRepo.DocFxDirectory!.FullName))
            {
                Console.WriteLine($"'{options.MediaDirectory}' is not a child of the docfx.json file's directory '{docFxRepo.DocFxDirectory}'.");
                return;
            }

            // Add regex to find image refs similar to 'social_image_url: "/dotnet/media/logo.png"'
            // This is done here (dynamically) because it relies on knowing the base path URL.
            docFxRepo._imageLinkRegExes.Add($"social_image_url: ?\"?(?<path>{docFxRepo.UrlBasePath}.*?(\\.(png|jpg|gif|svg))+)");

            // Gather media file names.
            if (docFxRepo._imageRefs is null)
                docFxRepo._imageRefs = GetMediaFiles(options.MediaDirectory);

            Console.WriteLine($"\nSearching the '{options.MediaDirectory}' directory recursively for orphaned .png/.jpg/.gif/.svg files...\n");

            docFxRepo.ListOrphanedImages(options.Delete!.Value, "snippets");
        }

        // Catalog images
        if (options.CatalogImages || options.CatalogImagesWithText || options.FilterImagesForText)
        {


            if (string.IsNullOrEmpty(options.MediaDirectory))
            {
                Console.WriteLine("\nEnter the path to the directory where you want to catalog media files:\n");
                options.MediaDirectory = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(options.MediaDirectory) || !Directory.Exists(options.MediaDirectory))
            {
                Console.WriteLine($"\nThe {options.MediaDirectory} directory doesn't exist.");
                return;
            }

            // Make sure the searchable directory is part of the same DocFx docset.
            if (!options.MediaDirectory.IsSubdirectoryOf(docFxRepo.DocFxDirectory!.FullName))
            {
                Console.WriteLine($"'{options.MediaDirectory}' is not a child of the docfx.json file's directory '{docFxRepo.DocFxDirectory}'.");
                return;
            }
            if (options.CatalogImagesWithText && string.IsNullOrEmpty(options.OcrModelDirectory))
            {
                Console.WriteLine($"'--ocr-model-directory' directory was not provided.");
                return;
            }

            // Add regex to find image refs similar to 'social_image_url: "/dotnet/media/logo.png"'
            // This is done here (dynamically) because it relies on knowing the base path URL.
            docFxRepo._imageLinkRegExes.Add($"social_image_url: ?\"?(?<path>{docFxRepo.UrlBasePath}.*?(\\.(png|jpg|gif|svg))+)");

            // Gather media file names.
            if (docFxRepo._imageRefs is null)
                docFxRepo._imageRefs = GetMediaFiles(options.MediaDirectory);

            Console.WriteLine($"\nCataloging '{docFxRepo._imageRefs.Count}' images (recursively) in the '{options.MediaDirectory}' directory...\n");

            if (options.CatalogImagesWithText)
            {
                // Extract hash keys from the dictionary
                List<string> mediaFilesList = docFxRepo._imageRefs.Keys.ToList();

                // Pass hash keys to ScanMediaFiles
                docFxRepo._ocrRefs = ScanMediaFiles(mediaFilesList, options.OcrModelDirectory);



                docFxRepo.OutputImageReferences(true);
            }
            else if (options.FilterImagesForText)
            {

                if (String.IsNullOrEmpty(options.FilterTextJsonFile))
                {
                    Console.WriteLine($"\nThe filterTextJsonFile can't be empty when requesting FilterImagesForText.");
                    return;
                }
                if (!File.Exists(options.FilterTextJsonFile))
                {
                    Console.WriteLine($"\nThe filterTextJsonFile '{options.FilterTextJsonFile}' doesn't exist.");
                    return;
                }

                List<string> searchTerms;
                try
                {
                    string jsonContent = File.ReadAllText(options.FilterTextJsonFile);
                    searchTerms = JsonSerializer.Deserialize<List<string>>(jsonContent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError reading or deserializing '{options.FilterTextJsonFile}': {ex.Message}");
                    return;
                }
                if (searchTerms.Count == 0)
                {
                    Console.WriteLine($"\nNo search terms found in '{options.FilterTextJsonFile}'.");
                    return;
                }

                // Extract hash keys from the dictionary
                List<string> mediaFilesList = docFxRepo._imageRefs.Keys.ToList();

                // Pass hash keys to ScanMediaFiles
                Dictionary<string, string> unfilteredResults = ScanMediaFiles(mediaFilesList, options.OcrModelDirectory);

                // Filter results
                docFxRepo._ocrFilteredRefs = FilterMediaFiles(unfilteredResults, searchTerms);

                docFxRepo.OutputImageReferences(true, true);
            }
            else
            {
                docFxRepo.OutputImageReferences();
            }


        }


        // Find orphaned include-type files
        if (options.FindOrphanedIncludes)
        {
            if (string.IsNullOrEmpty(options.IncludesDirectory))
            {
                Console.WriteLine("\nEnter the path to the directory that you want to check for orphaned include files:\n");
                options.IncludesDirectory = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(options.IncludesDirectory) || !Directory.Exists(options.IncludesDirectory))
            {
                Console.WriteLine($"\nThe {options.IncludesDirectory} directory doesn't exist.");
                return;
            }

            // Make sure the searchable directory is part of the same DocFx docset.
            if (!options.IncludesDirectory.IsSubdirectoryOf(docFxRepo.DocFxDirectory!.FullName))
            {
                Console.WriteLine($"'{options.IncludesDirectory}' is not a child of the docfx.json file's directory '{docFxRepo.DocFxDirectory}'.");
                return;
            }

            Console.WriteLine($"\nSearching the '{options.IncludesDirectory}' directory recursively for orphaned .md files " +
                $"in directories or subdirectories of a directory named 'includes'.");

            Dictionary<string, int> includeFiles = GetIncludeFiles(options.IncludesDirectory);

            if (includeFiles.Count == 0)
            {
                Console.WriteLine("\nNo .md files were found in any directories or subdirectories of a directory named 'includes'.");
                return;
            }
            else
                Console.WriteLine($"\nChecking {includeFiles.Count} include files.");

            ListOrphanedIncludes(options.IncludesDirectory, includeFiles, options.Delete!.Value);
        }

        // Find orphaned snippet files
        if (options.FindOrphanedSnippets)
        {
            if (string.IsNullOrEmpty(options.SnippetsDirectory))
            {
                Console.WriteLine("\nEnter the path to the directory that you want to check for orphaned snippet files:\n");
                options.SnippetsDirectory = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(options.SnippetsDirectory) || !Directory.Exists(options.SnippetsDirectory))
            {
                Console.WriteLine($"\nThe {options.SnippetsDirectory} directory doesn't exist.");
                return;
            }

            // Make sure the searchable directory is part of the same DocFx docset.
            if (!options.SnippetsDirectory.IsSubdirectoryOf(docFxRepo.DocFxDirectory!.FullName))
            {
                Console.WriteLine($"'{options.SnippetsDirectory}' is not a child of the docfx.json file's directory '{docFxRepo.DocFxDirectory}'.");
                return;
            }

            Console.WriteLine($"\nSearching the '{options.SnippetsDirectory}' directory recursively for orphaned snippet files.");

            // Get all snippet files.
            List<(string, string?)> snippetFiles = GetSnippetFiles(options.SnippetsDirectory);
            if (snippetFiles.Count == 0)
            {
                Console.WriteLine("\nNo files with matching extensions were found.");
                return;
            }

            // Associate snippet files to a project (where applicable).
            AddProjectInfo(ref snippetFiles);

            // Catalog all the solution files and the project (directories) they reference.
            List<(string, List<string?>)> solutionFiles = GetSolutionFiles(options.SnippetsDirectory);

            ListOrphanedSnippets(options.SnippetsDirectory, snippetFiles, solutionFiles,
                options.Delete!.Value, options.XmlSource);
        }

        // Replace links to articles that are redirected in the master redirection files.
        if (options.ReplaceRedirectTargets)
        {
            // Get the directory that represents the docset.
            if (string.IsNullOrEmpty(options.ArticlesDirectory))
            {
                Console.WriteLine("\nEnter the path to the directory that contains the articles with links to fix up:\n");
                options.ArticlesDirectory = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(options.ArticlesDirectory) || !Directory.Exists(options.ArticlesDirectory))
            {
                Console.WriteLine($"\nThe {options.ArticlesDirectory} directory doesn't exist.");
                return;
            }

            // Make sure the searchable directory is part of the same DocFx docset.
            if (!options.ArticlesDirectory.IsSubdirectoryOf(docFxRepo.DocFxDirectory!.FullName))
            {
                Console.WriteLine($"'{options.ArticlesDirectory}' is not a child of the docfx.json file's directory '{docFxRepo.DocFxDirectory}'.");
                return;
            }

            Console.WriteLine($"\nSearching the '{options.ArticlesDirectory}' directory for links to redirected topics...\n");

            // Gather all the redirects.
            List<Redirect> redirects = docFxRepo.GetAllRedirects();

            // Get all the markdown and YAML files.
            List<FileInfo> linkingFiles = GetMarkdownFiles(options.ArticlesDirectory);
            linkingFiles.AddRange(GetYAMLFiles(options.ArticlesDirectory));

            // Check all links, including in toc.yml, to files in the redirects list.
            // Replace links to redirected files.
            docFxRepo.ReplaceRedirectedLinks(redirects, linkingFiles);

            Console.WriteLine("\nFinished replacing redirected links.");
        }

        // Replace site-relative links to *this* repo with file-relative links.
        if (options.ReplaceWithRelativeLinks)
        {
            // Get the directory that represents the docset.
            if (string.IsNullOrEmpty(options.ArticlesDirectory))
            {
                Console.WriteLine("\nEnter the path to the directory that contains the articles with links to fix up:\n");
                options.ArticlesDirectory = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(options.ArticlesDirectory) || !Directory.Exists(options.ArticlesDirectory))
            {
                Console.WriteLine($"\nThe {options.ArticlesDirectory} directory doesn't exist.");
                return;
            }

            // Make sure the searchable directory is part of the same DocFx docset.
            if (!options.ArticlesDirectory.IsSubdirectoryOf(docFxRepo.DocFxDirectory!.FullName))
            {
                Console.WriteLine($"'{options.ArticlesDirectory}' is not a child of the docfx.json file's directory '{docFxRepo.DocFxDirectory}'.");
                return;
            }

            // Check that this isn't the root directory of the repo. The code doesn't handle that case currently
            // because it can't always determine the base path of the docset (e.g. for dotnet/docs repo).
            if (string.Equals(docFxRepo.OpsConfigFile.DirectoryName, options.ArticlesDirectory, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"\nYou entered the repo root directory. Please enter a subdirectory in which to replace links.");
                return;
            }

            // Get the absolute path to the base directory for this docset.
            string? rootDirectory = docFxRepo.GetDocsetAbsolutePath(options.ArticlesDirectory);

            if (rootDirectory is null)
            {
                Console.WriteLine($"\nThe docfx.json file for {options.ArticlesDirectory} is invalid.");
                return;
            }

            Console.WriteLine($"\nReplacing site-relative links to '{docFxRepo.UrlBasePath}/' in " +
                $"the '{options.ArticlesDirectory}' directory with file-relative links.\n");

            // Get all the markdown and YAML files in the search directory.
            List<FileInfo> linkingFiles = GetMarkdownFiles(options.ArticlesDirectory);
            linkingFiles.AddRange(GetYAMLFiles(options.ArticlesDirectory));

            // Check all links in these files.
            ReplaceLinks(linkingFiles, docFxRepo.UrlBasePath, rootDirectory);

            Console.WriteLine("\nFinished fixing relative links.");
        }

        // Remove hops/daisy chains in a redirection file.        
        if (options.RemoveRedirectHops)
        {
            // Get the directory that represents the docset.
            if (string.IsNullOrEmpty(options.DocFxDirectory))
            {
                Console.WriteLine("\nEnter the path to the directory that contains the docfx.json file:\n");
                options.DocFxDirectory = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(options.DocFxDirectory) || !Directory.Exists(options.DocFxDirectory))
            {
                Console.WriteLine($"\nThe {options.DocFxDirectory} directory doesn't exist.");
                return;
            }

            // Make sure the searchable directory is part of the same DocFx docset.
            // These can be different if docFxRepo was constructed using a different directory (e.g. articles/media/snippets/include).
            if (!options.DocFxDirectory!.IsSubdirectoryOf(docFxRepo.DocFxDirectory!.FullName))
            {
                Console.WriteLine($"'{options.DocFxDirectory}' is not a child of the docfx.json file's directory '{docFxRepo.DocFxDirectory}'.");
                return;
            }

            docFxRepo.RemoveAllRedirectHops();

            Console.WriteLine("\nFinished removing redirect hops.");
        }

        stopwatch.Stop();
        Console.WriteLine($"Elapsed time: {stopwatch.Elapsed.ToHumanReadableString()}");
    }

    #region Replace site-relative links
    private static void ReplaceLinks(List<FileInfo> linkingFiles, string urlBasePath, string rootDirectory)
    {
        // Strip preceding / off urlBasePath, if it exists.
        urlBasePath = urlBasePath.TrimStart('/');

        List<string> regexes =
            [
                @"\]\(<?(/" + urlBasePath + @"/([^\)\s]*)>?)\)",                                    // [link text](/basepath/some other text)
                @"\]:\s(/" + urlBasePath + @"/([^\s]*))",                                           // [ref link]: /basepath/some other text
                "<img[^>]*?src[ ]*=[ ]*\"(/" + urlBasePath + "/([^>]*?.(png|gif|jpg|svg)))[ ]*\"",  // <img src="/azure/mydocs/media/pic3.png">
                @"\[.*\]:[ ]*(/" + urlBasePath + @"/(.*\.(png|gif|jpg|svg)))",                      // [0]: /azure/mydocs/media/pic1.png
                @"imageSrc:[ ]*(/" + urlBasePath + @"/([^:]*\.(png|gif|jpg|svg)))",                 // imageSrc: /azure/mydocs/media/pic1.png
                @":::image[^:]*source=""(/" + urlBasePath + @"/([^:]*\.(png|gif|jpg|svg)))""[^:]*:::" // :::image type="complex" source="/azure/mydocs/media/pic1.png" alt-text="Screenshot.":::
            ];

        foreach (FileInfo linkingFile in linkingFiles)
        {
            // Read the whole file up front because we might change the file mid-flight.
            string originalFileText = File.ReadAllText(linkingFile.FullName);

            // Test strings:
            // ![VisualizerIcon](/test-repo/debugger/dbg-tips.png "Visualizer icon")
            // ![VisualizerIcon](/test-repo/debugger/dbg-tips.png)
            // For more information, see [this page](/test-repo/debugger/dbg-tips).

            foreach (string regex in regexes)
            {
                // Regex ignores case.
                foreach (Match match in Regex.Matches(originalFileText, regex, RegexOptions.IgnoreCase))
                {
                    // Get the first capture group, which is the part of the path after the docset name.
                    string siteRelativePath = match.Groups[2].Value;

                    // If the path contains a ?, ignore this link as replacing it might not be ideal.
                    // For example, if the link is to a specific version like "?view=vs-2015".
                    if (siteRelativePath.Contains('?'))
                        continue;

                    ReplaceLinkText(siteRelativePath, rootDirectory, linkingFile, match.Groups[0].Value, match.Groups[1].Value);
                }
            }
        }
    }

    private static void ReplaceLinkText(
        string siteRelativePath,
        string rootDirectory,
        FileInfo linkingFile,
        string originalMatch,
        string originalLink
        )
    {
        // If the link contains a bookmark, trim it off and add it back later.
        // If there are two hash characters, this pattern is greedy and finds the last one.
        string bookmarkPattern = @"(.*)(#.*)";
        string? bookmark = null;
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

        // Get the actual casing of the file on the file system.
        try
        {
            absolutePath = GetActualCaseForFilePath(absolutePath);
        }
        catch (FileNotFoundException)
        {
            // This can happen if files from a different repo map to the same docset.
            // For example, the C# language specification: [C# Language Specification](/dotnet/csharp/language-reference/language-specification/introduction)
            return;
        }

        if (absolutePath != null)
        {
            // Determine the file-relative path to absolutePath.
            string fileRelativePath = Path.GetRelativePath(linkingFile.DirectoryName!, absolutePath);

            // Replace any backslashes with forward slashes.
            fileRelativePath = fileRelativePath.Replace('\\', '/');

            if (fileRelativePath != null)
            {
                // Add the bookmark back onto the end, if there is one.
                if (!string.IsNullOrEmpty(bookmark))
                {
                    fileRelativePath += bookmark;
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
        List<FileInfo>? files = GetAllMarkdownFiles(inputDirectory, out DirectoryInfo? rootDirectory);

        if (files is null || rootDirectory is null)
            return;

        // Gather up all the include references and increment the count for that include file in the Dictionary.
        //foreach (var markdownFile in files)
        Parallel.ForEach(files, markdownFile =>
        {
            foreach (string line in File.ReadAllLines(markdownFile.FullName))
            {
                // Example include references:
                // [!INCLUDE [DotNet Restore Note](../includes/dotnet-restore-note.md)]
                // [!INCLUDE[DotNet Restore Note](~/includes/dotnet-restore-note.md)]
                // [!INCLUDE [temp](../dir1/includes/assign-to-sprint.md)]

                // An include file referenced from another include file won't have "includes" in the path.
                // E.g. [!INCLUDE [P2S FAQ All](vpn-gateway-faq-p2s-all-include.md)]

                // RegEx pattern to match
                string includeLinkPattern = @"\[!INCLUDE[ ]?\[[^\]]*?\]\(<?(.*?\.md)";

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
                            fullPath = Path.Combine(markdownFile.DirectoryName!, relativePath);
                        }

                        // Clean up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                        fullPath = Path.GetFullPath(fullPath);

                        if (fullPath != null)
                        {
                            // Increment the count for this INCLUDE file in our dictionary
                            if (includeFiles.TryGetValue(fullPath, out int value))
                                includeFiles[fullPath] = ++value;
                        }
                    }
                }
            }
        });

        int count = 0;

        // Print out the INCLUDE files that have zero references.
        StringBuilder output = new();
        foreach (KeyValuePair<string, int> includeFile in includeFiles)
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
            foreach (KeyValuePair<string, int> includeFile in includeFiles)
            {
                if (includeFile.Value == 0)
                    File.Delete(includeFile.Key);
            }
        }

        string deleted = deleteOrphanedIncludes ? "and deleted " : "";

        Console.WriteLine($"\nFound {deleted}{count} orphaned INCLUDE files:\n");
        Console.WriteLine(output.ToString());
        Console.WriteLine("DONE");
    }

    /// <summary>
    /// Returns a collection of *.md files in the current directory or its subdirectories
    /// that have an ancestor directory named "includes".
    /// </summary>
    /// <returns></returns>
    private static Dictionary<string, int> GetIncludeFiles(string inputDirectory)
    {
        const string includesDirectoryName = "includes";

        DirectoryInfo dir = new(inputDirectory);

        // Create the dictionary with a case-insensitive comparer,
        // because links in Markdown don't have to match the actual file path casing.
        Dictionary<string, int> includeFiles = new(StringComparer.InvariantCultureIgnoreCase);

        // Determine if this directory or one of its ancestors is named "includes".
        bool startDirIsIncludesDir = false;
        DirectoryInfo? dirIterator = dir;
        while (dirIterator != null)
        {
            if (string.Compare(dirIterator.Name, includesDirectoryName, true) == 0)
            {
                startDirIsIncludesDir = true;
                break;
            }

            dirIterator = dirIterator.Parent ?? null;
        }

        if (startDirIsIncludesDir)
        {
            foreach (FileInfo file in dir.EnumerateFiles("*.md", SearchOption.AllDirectories))
            {
                includeFiles.Add(file.FullName, 0);
            }
        }
        else
        {
            foreach (DirectoryInfo subdirectory in dir.EnumerateDirectories(includesDirectoryName, SearchOption.AllDirectories))
            {
                foreach (FileInfo file in subdirectory.EnumerateFiles("*.md", SearchOption.AllDirectories))
                {
                    try
                    {
                        includeFiles.Add(file.FullName, 0);
                    }
                    catch (ArgumentException)
                    {
                        // System.ArgumentException: An item with the same key has already been added.
                        // This can happen if an "includes" directory has an ancestor named "includes".
                    }
                }
            }
        }

        return includeFiles;
    }
    #endregion

    #region Orphaned snippets
    /// <summary>
    /// Returns a list of code files in the specified directory and its subdirectories.
    /// </summary>
    private static List<(string, string?)> GetSnippetFiles(string inputDirectory)
    {
        List<string> fileExtensions = [".cs", ".vb", ".fs", ".cpp", ".xaml"];

        var dir = new DirectoryInfo(inputDirectory);
        var snippetFiles = new List<(string, string?)>();

        foreach (string extension in fileExtensions)
        {
            foreach (FileInfo file in dir.EnumerateFiles($"*{extension}"))
            {
                snippetFiles.Add((file.FullName, null));
            }
        }

        foreach (DirectoryInfo subDirectory in dir.EnumerateDirectories("*", SearchOption.AllDirectories))
        {
            foreach (string extension in fileExtensions)
            {
                foreach (FileInfo file in subDirectory.EnumerateFiles($"*{extension}"))
                {
                    snippetFiles.Add((file.FullName, null));
                }
            }
        }

        return snippetFiles;
    }

    /// <summary>
    /// Adds an associated project file to each applicable snippet file in the specified list.
    /// </summary>
    private static void AddProjectInfo(ref List<(string, string?)> snippetFiles)
    {
        //foreach (var snippetFile in snippetFiles)
        for (int i = 0; i < snippetFiles.Count; i++)
        {
            string filePath = snippetFiles[i].Item1;
            var fi = new FileInfo(filePath);

            string projExtension = GetProjectExtension(filePath);

            DirectoryInfo? projectDir = GetDirectory(new DirectoryInfo(fi.DirectoryName!), $"*{projExtension}");
            if (projectDir != null)
                snippetFiles[i] = (filePath, projectDir.FullName);
        }
    }

    private static string GetProjectExtension(string filePath) => Path.GetExtension(filePath) switch
    {
        ".cs" => ".csproj",
        ".vb" => ".vbproj",
        ".fs" => ".fsproj",
        ".cpp" => ".vcxproj",
        ".xaml" => ".*proj",
        _ => throw new ArgumentException($"Unexpected file extension.", filePath)
    };

    /// <summary>
    /// Builds a list of solution files and all the (unique) project directories they reference (using full paths).
    /// </summary>
    private static List<(string, List<string?>)> GetSolutionFiles(string startDirectory)
    {
        List<(string, List<string?>)> solutionFiles = [];

        DirectoryInfo dir = new(startDirectory);
        foreach (FileInfo slnFile in dir.EnumerateFiles("*.sln", SearchOption.AllDirectories))
        {
            SolutionFile solutionFile = SolutionFile.Parse(slnFile.FullName);
            List<string?> projectFiles = solutionFile.ProjectsInOrder.Select(p => Path.GetDirectoryName(p.AbsolutePath)).Distinct().ToList();

            solutionFiles.Add((slnFile.FullName, projectFiles));
        }

        return solutionFiles;
    }

    private static void ListOrphanedSnippets(
        string inputDirectory,
        List<(string, string?)> snippetFiles,
        List<(string, List<string?>)> solutionFiles,
        bool deleteOrphanedSnippets
        ) => ListOrphanedSnippets(inputDirectory, snippetFiles, solutionFiles, deleteOrphanedSnippets, false);

    private static void ListOrphanedSnippets(string inputDirectory,
        List<(string, string?)> snippetFiles,
        List<(string, List<string?>)> solutionFiles,
        bool deleteOrphanedSnippets,
        bool searchEcmaXmlFiles)
    {
        // Get all files that could possibly link to the snippet files.
        List<FileInfo>? files;
        DirectoryInfo? rootDirectory;
        if (searchEcmaXmlFiles)
            files = GetAllEcmaXmlFiles(inputDirectory, out rootDirectory);
        else
            files = GetAllMarkdownFiles(inputDirectory, out rootDirectory);

        if (files is null || rootDirectory is null)
            return;

        Console.WriteLine($"Checking {snippetFiles.Count} snippet files.");

        int countOfOrphans = 0;
        // Prints out the snippet files that have zero references.
        StringBuilder output = new();

        // Keep track of which directories are referenced/unreferenced.
        var projectDirectories = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

        foreach ((string, string?) snippetFile in snippetFiles)
        {
            FileInfo fi = new(snippetFile.Item1);
            string regexSnippetFileName = fi.Name.Replace(".", "\\.").Replace("[", "\\[").Replace("]", "\\]");

            bool foundSnippetReference = false;

            // Check if there's a .csproj or .vbproj file in its ancestry.
            bool isPartOfProject = false;
            string? projectPath = snippetFile.Item2;
            if (projectPath is not null)
            {
                // It's part of a project.
                isPartOfProject = true;

                // Add the project directory to the list of project directories.
                // Initialize it with 0 references.
                projectDirectories.TryAdd(projectPath, 0);
            }

            // If we've already determined this project directory isn't orphaned,
            // move on to the next snippet file.
            if (projectPath is not null && projectDirectories.TryGetValue(projectPath, out int value) && (value > 0))
                continue;

            // First try to find a reference to the actual snippet file.
            foreach (FileInfo mdOrXmlFile in files)
            {
                // Matches the following types of snippet syntax:
                // :::code language="csharp" source="snippets/EventCounters/MinimalEventCounterSource.cs":::
                // [!code-csharp[Violation#1](../code-quality/codesnippet/ca1010.cs)]
                // [!code-csharp[Violation#1](../code-quality/codesnippet/ca1010.cs#snippet1)]
                // [!code-csharp[Hi](./code/code.cs?highlight=1,6)]
                // [!code-csharp[FxCop.Usage#1](./code/code.cs?range=3-6)]

                string regex = @"(\(|"")([^\)""\n]*\/" + regexSnippetFileName + @")(#\w*)?(\?\w*=(\d|,|-)*)?(\)|"")";

                // Ignores case.
                string fileText = File.ReadAllText(mdOrXmlFile.FullName);
                foreach (Match match in Regex.Matches(fileText, regex, RegexOptions.IgnoreCase))
                {
                    if (match is not null && match.Length > 0)
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
                                fullPath = Path.Combine(mdOrXmlFile.DirectoryName!, relativePath);
                            }

                            // Clean up the path.
                            fullPath = Path.GetFullPath(fullPath);

                            if (string.Equals(snippetFile.Item1, fullPath, StringComparison.InvariantCultureIgnoreCase))
                            {
                                // This snippet file is not orphaned.
                                foundSnippetReference = true;

                                // Mark its directory as not orphaned.
                                if (projectPath is not null)
                                {
                                    if (!projectDirectories.TryGetValue(projectPath, out int value2))
                                        projectDirectories.Add(projectPath, 1);
                                    else
                                        projectDirectories[projectPath] = ++value2;
                                }

                                break;
                            }
                        }
                    }
                }

                if (foundSnippetReference)
                    break;
                // else check the next Markdown file.
            }

            if (!foundSnippetReference && !isPartOfProject)
            {
                // The snippet file is orphaned (not used anywhere).
                countOfOrphans++;
                output.AppendLine(Path.GetFullPath(snippetFile.Item1));

                if (deleteOrphanedSnippets)
                    File.Delete(snippetFile.Item1);
            }
        }

        // Output info for non-project snippets.
        Console.WriteLine($"\nFound {countOfOrphans} orphaned snippet files:\n");
        Console.WriteLine(output.ToString());

        // For any directories that still have 0 references, check if *any* files in 
        // the directory are referenced. If not, delete the project directory.
        foreach (string? projectPath in projectDirectories.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key))
        {
            bool foundDirectoryReference = false;

            var projectDirInfo = new DirectoryInfo(projectPath);

            string[] projectDirRegexes = [
                        @"\((([^\)\n]+?\/)?" + projectDirInfo.Name + @")\/[^\)\n]+?\)", // [!code-csharp[Vn#1](../code-quality/ca1010.cs)]
                        @"""(([^""\n]+?\/)?" + projectDirInfo.Name + @")\/[^""\n]+?"""  // :::code language="csharp" source="snippets/CounterSource.cs":::
                    ];

            foreach (FileInfo markdownFile in files)
            {
                string fileText = File.ReadAllText(markdownFile.FullName);

                foreach (string regex in projectDirRegexes)
                {
                    // Loop through all the matches in the file; ignores case.
                    MatchCollection matches = Regex.Matches(fileText, regex, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        if (match is not null && match.Length > 0)
                        {
                            string relativePath = match.Groups[1].Value.Trim();

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
                                    fullPath = Path.Combine(markdownFile.DirectoryName!, relativePath);
                                }

                                // Clean up the path.
                                fullPath = Path.GetFullPath(fullPath);

                                // Check if the full path for the link matches the project directory we're looking for.
                                if (string.Equals(projectPath, fullPath, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // This directory is not orphaned.
                                    foundDirectoryReference = true;

                                    // Increment the reference count.
                                    projectDirectories[projectPath]++;

                                    break;
                                }
                            }
                        }
                    }
                    if (foundDirectoryReference)
                        break;
                    // else check the next regex pattern.
                }

                if (foundDirectoryReference)
                    break;
                // else check the next Markdown file.
            }
        }

        StringBuilder dirSlnOutput = new("\nThe following project directories are orphaned:\n\n");

        // Delete orphaned directories.
        IEnumerable<string> directoriesToDelete = projectDirectories.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key);

        foreach (string directory in directoriesToDelete)
        {
            bool isPartOfSolution = false;

            // Check if the directory is referenced in a solution file.
            // If so, we'll (possibly) delete it in the next step.
            foreach ((string, List<string?>) slnFile in solutionFiles)
            {
                if (slnFile.Item2.Contains(directory, StringComparer.InvariantCultureIgnoreCase))
                {
                    isPartOfSolution = true;
                    break;
                }
            }

            if (!isPartOfSolution)
            {
                dirSlnOutput.AppendLine(directory);
                if (deleteOrphanedSnippets)
                {
                    try
                    {
                        Directory.Delete(directory, true);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        Console.WriteLine($"**Couldn't find directory '{directory}'. This is unusual.**");
                    }
                }
            }
        }

        dirSlnOutput.AppendLine("\nThe following solution directories are orphaned:\n");

        // Delete orphaned solutions.
        foreach ((string, List<string?>) solutionFile in solutionFiles)
        {
            // Check if any of its projects (snippets) are referenced anywhere.
            bool isReferenced = false;

            foreach (string? projectDir in solutionFile.Item2)
            {
                if (projectDir is null)
                    continue;

                if (projectDirectories.TryGetValue(projectDir, out int refCount))
                {
                    if (refCount > 0)
                    {
                        isReferenced = true;
                        break;
                    }
                }
            }

            if (!isReferenced)
            {
                string? path = Path.GetDirectoryName(solutionFile.Item1);
                if (path is not null)
                {
                    dirSlnOutput.AppendLine(path);
                    if (deleteOrphanedSnippets)
                    {
                        // Delete the solution and its directory.
                        try
                        {
                            Directory.Delete(path, true);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            Console.WriteLine($"**Couldn't find directory '{path}'. This is unusual.**");
                        }
                    }
                }
            }
        }

        // Output info for unreferenced projects and solutions.
        Console.WriteLine(dirSlnOutput.ToString());
    }
    #endregion

    #region Orphaned articles
    /// <summary>
    /// Lists the markdown files that aren't referenced from a TOC file.
    /// </summary>
    private static void ListOrphanedArticles(List<FileInfo> tocFiles, List<FileInfo> markdownFiles, bool deleteOrphanedTopics)
    {
        Console.WriteLine($"Checking {markdownFiles.Count} Markdown files in {tocFiles.Count} TOC files.");

        Dictionary<string, int> filesToKeep = [];

        static bool IsArticleFile(FileInfo file) =>
            !file.FullName.Contains($"{Path.DirectorySeparatorChar}includes{Path.DirectorySeparatorChar}") &&
            !file.FullName.Contains($"{Path.DirectorySeparatorChar}misc{Path.DirectorySeparatorChar}") &&
            string.Compare(file.Name, "TOC.md", StringComparison.InvariantCultureIgnoreCase) != 0 &&
            string.Compare(file.Name, "index.md", StringComparison.InvariantCultureIgnoreCase) != 0;

        List<FileInfo> orphanedFiles = [];

        StringBuilder sb = new("\n");

        Parallel.ForEach(markdownFiles.Where(IsArticleFile), markdownFile =>
        //foreach (var markdownFile in markdownFiles)
        {
            // Ignore TOC files.
            if (string.Compare(markdownFile.Name, "TOC.md", true) == 0)
                return; //continue;

            bool found = tocFiles.Any(tocFile => IsFileLinkedFromTocFile(markdownFile, tocFile));
            if (!found)
            {
                orphanedFiles.Add(markdownFile);
                sb.AppendLine(markdownFile.FullName);
            }
        });

        sb.AppendLine($"\nFound {orphanedFiles.Count} Markdown files that aren't referenced in a TOC.");
        Console.WriteLine(sb.ToString());

        // Delete files if the option is set.
        if (deleteOrphanedTopics)
        {
            Parallel.ForEach(markdownFiles, linkingFile =>
            {
                CheckFileLinks(orphanedFiles, linkingFile, ref filesToKeep);
            });

            // Delete files that aren't linked to.
            foreach (FileInfo orphanedFile in orphanedFiles)
            {
                if (!filesToKeep.ContainsKey(orphanedFile.FullName))
                    File.Delete(orphanedFile.FullName);
            }

            if (filesToKeep.Count > 0)
            {
                Console.Write($"\nThe following {filesToKeep.Count} files *weren't deleted* " +
                    $"because they're referenced in one or more files:\n\n");
                foreach (KeyValuePair<string, int> fileName in filesToKeep)
                {
                    Console.WriteLine(fileName);
                }
            }
            else
                Console.WriteLine($"\nDeleted {orphanedFiles.Count} files.");
        }
    }

    private static bool IsFileLinkedFromTocFile(FileInfo linkedFile, FileInfo tocFile)
    {
        string text = File.ReadAllText(tocFile.FullName);

        // Example links .yml/.md:
        // href: ide/managing-external-tools.md
        // href: "wfc-exe-tool.md"
        // # [Managing External Tools](ide/managing-external-tools.md)

        string regexSafeFilename = linkedFile.Name.Replace(".", "\\.");

        string linkRegEx = tocFile.Extension.Equals(".yml", StringComparison.CurrentCultureIgnoreCase) ?
            @"href:\s*""?(" + regexSafeFilename + @"|[^""\s]+?\/" + regexSafeFilename + ")" :
            @"\]\(\s*<?\s*(\/?" + regexSafeFilename + @"|[^\)]+\/" + regexSafeFilename + ")";

        // For each link that contains the file name...
        // Regex ignores case.
        foreach (Match match in Regex.Matches(text, linkRegEx, RegexOptions.IgnoreCase))
        {
            // For debugging only.
            //Console.WriteLine($"Matching text is '{match.Groups[1].Value.Trim()}'.");

            // Get the file-relative path to the linked file.
            string relativePath = match.Groups[1].Value.Trim();

            // Remove any quotation marks
            relativePath = relativePath.Replace("\"", "");

            if (relativePath.StartsWith('/') || relativePath.StartsWith("http:") || relativePath.StartsWith("https:"))
            {
                // The file is in a different repo, so ignore it.
                continue;

                // TODO - For links that start with "/", check if they are in the same repo.
            }

            if (relativePath != null)
            {
                // Construct the full path to the referenced file
                string fullPath = Path.Combine(tocFile.DirectoryName!, relativePath);

                // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                fullPath = Path.GetFullPath(fullPath);

                if (fullPath != null)
                {
                    // See if our constructed path matches the actual file we think it is (ignores case).
                    if (string.Compare(fullPath, linkedFile.FullName, true) == 0)
                        return true;
                    else
                    {
                        // If we get here, the file name matched but the full path did not.
                        //Console.WriteLine("\nNot a proper match:");
                        //Console.WriteLine($"Full path to referenced file is '{fullPath}'.");
                        //Console.WriteLine($"Path to orphaned file candidate is '{linkedFile.FullName}'.");
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
            return;

        string fileContents = File.ReadAllText(linkingFile.FullName);

        // Example links .yml/.md:
        // href: ide/managing-external-tools.md
        // [Managing External Tools](ide/managing-external-tools.md)

        foreach (FileInfo linkedFile in linkedFiles)
        {
            List<string> mdRegexes =
            [
                @"\]\(<?(([^\)])*?" + linkedFile.Name + @")",
                @"\]:\s" + linkedFile.Name
            ];

            string ymlRegex = @"href:(.*?" + linkedFile.Name + ")";

            if (linkingFile.Extension.Equals(".yml", StringComparison.CurrentCultureIgnoreCase))
                FindMatches(linkingFile, filesToKeep, fileContents, linkedFile, ymlRegex);
            else // Markdown file.
            {
                foreach (string mdRegex in mdRegexes)
                {
                    FindMatches(linkingFile, filesToKeep, fileContents, linkedFile, mdRegex);
                }
            }
        }
    }

    private static void FindMatches(FileInfo linkingFile, Dictionary<string, int> filesToKeep, string fileContents, FileInfo linkedFile, string linkRegEx)
    {
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
                    fullPath = Path.Combine(linkingFile.DirectoryName!, relativePath);
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
                    // See if our constructed path matches the actual file we think it is; ignores case.
                    if (string.Compare(fullPath, linkedFile.FullName, true) == 0)
                    {
                        // File is linked from another file.
                        if (filesToKeep.TryGetValue(linkedFile.FullName, out int value))
                        {
                            // Increment the count of links to this file.
                            filesToKeep[linkedFile.FullName] = ++value;
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
    #endregion

    #region Popular files
    /// <summary>
    /// Finds topics that appear more than once, either in one TOC file, or across multiple TOC files.
    /// </summary>
    private static void ListPopularFiles(List<FileInfo> tocFiles, List<FileInfo> markdownFiles)
    {
        bool found = false;
        StringBuilder output = new("The following files appear in more than one TOC file:\n\n");

        // Keep a hash table of each topic path with the number of times it's referenced
        Dictionary<string, int> topics = markdownFiles.ToDictionary<FileInfo, string, int>(mf => mf.FullName, mf => 0);

        foreach (FileInfo markdownFile in markdownFiles)
        {
            // If the file is in the Includes directory, ignore it
            if (markdownFile.FullName.Contains($"{Path.DirectorySeparatorChar}includes{Path.DirectorySeparatorChar}"))
                continue;

            foreach (FileInfo tocFile in tocFiles)
            {
                if (IsFileLinkedFromFile(markdownFile, tocFile))
                    topics[markdownFile.FullName]++;
            }
        }

        // Now spit out the topics that appear more than once.
        foreach (KeyValuePair<string, int> topic in topics)
        {
            if (topic.Value > 1)
            {
                found = true;
                output.AppendLine(topic.Key);
            }
        }

        // Only write the StringBuilder to the console if we found a topic referenced from more than one TOC file.
        if (found)
            Console.Write(output.ToString());
    }
    #endregion

    #region Generic helper methods
    /// <summary>
    /// Gets the actual (case-sensitive) file path on the file system for a specified case-insensitive file path.
    /// </summary>
    private static string GetActualCaseForFilePath(string pathAndFileName)
    {
        string? directory = Path.GetDirectoryName(pathAndFileName) ??
            throw new FileNotFoundException($"File not found: {pathAndFileName}.");
        string? directoryCaseSensitive = GetDirectoryCaseSensitive(directory);
        string pattern = $"{Path.GetFileName(pathAndFileName)}.*";
        string resultFileName;

        if (directoryCaseSensitive is null)
            throw new FileNotFoundException($"File not found: {pathAndFileName}.");

        // Enumerate all files in the directory, using the file name as a pattern.
        // This lists all case variants of the filename, even on file systems that are case sensitive.
        IEnumerable<string> foundFiles = Directory.EnumerateFiles(
            directoryCaseSensitive,
            pattern,
            new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

        if (foundFiles.Any())
            resultFileName = foundFiles.First();
        else
            throw new FileNotFoundException($"File not found: {pathAndFileName}.");

        return resultFileName;
    }

    /// <summary>
    /// Gets the actual (case-sensitive) directory path on the file system for a specified case-insensitive directory path.
    /// </summary>
    private static string? GetDirectoryCaseSensitive(string directory)
    {
        var directoryInfo = new DirectoryInfo(directory);
        if (directoryInfo.Exists)
            return directory;

        if (directoryInfo.Parent == null)
            return null;

        string? parent = GetDirectoryCaseSensitive(directoryInfo.Parent.FullName);
        if (parent == null)
            return null;

        return new DirectoryInfo(parent)
            .GetDirectories(directoryInfo.Name, new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive })
            .FirstOrDefault()?
            .FullName;
    }

    /// <summary>
    /// Looks for the specified file in the specified directory, and if not found,
    /// in all parent directories up to the disk root directory.
    /// </summary>
    public static FileInfo? GetFileHereOrInParent(string inputDirectory, string fileName)
    {
        DirectoryInfo dir = new(inputDirectory);

        try
        {
            while (dir.GetFiles(fileName, SearchOption.TopDirectoryOnly).Length == 0)
            {
                // Loop exit condition.
                if (dir.FullName == dir.Root.FullName)
                    return null;

                if (dir.Parent is null)
                    return null;

                dir = dir.Parent;
            }

            return dir.GetFiles(fileName, SearchOption.TopDirectoryOnly)[0];
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"Could not find directory {dir.FullName}");
            throw;
        }
    }

    /// <summary>
    /// Returns true if 'child' is a subdirectory (or the same directory) as 'other'.
    /// </summary>
    public static bool IsSubdirectoryOf(this string child, string other)
    {
        bool isChild = false;

        var childInfo = new DirectoryInfo(child);
        var otherInfo = new DirectoryInfo(other);

        if (childInfo.FullName == otherInfo.FullName) { return true; }

        while (childInfo.Parent != null)
        {
            if (childInfo.Parent.FullName == otherInfo.FullName)
            {
                isChild = true;
                break;
            }
            else childInfo = childInfo.Parent;
        }

        return isChild;
    }

    // The base path should be obtained through a user-provided option instead.
    //internal static string GetUrlBasePath(DirectoryInfo docFxDirectory)
    //{
    //    string docfxFilePath = Path.Combine(docFxDirectory.FullName, "docfx.json");
    //    string urlBasePath = null;

    //    // Deserialize the docfx.json file.
    //    DocFx docfx = LoadDocfxFile(docfxFilePath);
    //    if (docfx == null)
    //    {
    //        return null;
    //    }

    //    // Hack: Parse URL base path out of breadcrumbPath. Examples:
    //    // "breadcrumb_path": "/visualstudio/_breadcrumb/toc.json"
    //    // "breadcrumb_path": "/windows/uwp/breadcrumbs/toc.json"
    //    // "breadcrumb_path": "/dotnet/breadcrumb/toc.json"
    //    // "breadcrumb_path": "breadcrumb/toc.yml"  <--Need to handle this.

    //    string? breadcrumbPath = docfx.build.globalMetadata.breadcrumb_path;

    //    if (breadcrumbPath is not null)
    //    {
    //        // Remove everything after and including the second last / character.
    //        if (breadcrumbPath.Contains('/'))
    //        {
    //            breadcrumbPath = breadcrumbPath[0..breadcrumbPath.LastIndexOf('/')];
    //            if (breadcrumbPath.Contains('/'))
    //            {
    //                urlBasePath = breadcrumbPath[0..breadcrumbPath.LastIndexOf('/')];
    //            }
    //        }
    //    }

    //    if (!string.isNullOrEmpty(urlBasePath))
    //    {
    //        Console.WriteLine($"Is '{urlBasePath}' the correct URL base path for your docs (y or n)?");
    //        char key = Console.ReadKey().KeyChar;

    //        if (key == 'y' || key == 'Y')
    //        {
    //            Console.WriteLine("\nThanks!");
    //            return urlBasePath;
    //        }
    //    }

    //    Console.WriteLine($"\nWhat's the URL base path for articles in the `{docFxDirectory.FullName}` directory? (Example: /aspnet/core)");
    //    string basePath = Console.ReadLine();
    //    Console.WriteLine("\nThanks!");
    //    return basePath;
    //}

    /// <summary>
    /// Checks if the specified file path is referenced in the specified file.
    /// </summary>
    private static bool IsFileLinkedFromFile(FileInfo linkedFile, FileInfo linkingFile)
    {
        if (!File.Exists(linkingFile.FullName))
            return false;

        foreach (string line in File.ReadAllLines(linkingFile.FullName))
        {
            // Example links .yml/.md:
            // href: ide/managing-external-tools.md
            // [Managing External Tools](ide/managing-external-tools.md)

            string linkRegEx = linkingFile.Extension.Equals(".yml", StringComparison.CurrentCultureIgnoreCase) ?
                @"href:(.*?" + linkedFile.Name + ")" :
                @"\]\(<?(([^\)])*?" + linkedFile.Name + @")";

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
                        fullPath = Path.Combine(linkingFile.DirectoryName!, relativePath);
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
                        if (string.Compare(fullPath, linkedFile.FullName, true) == 0)
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
    private static List<FileInfo> GetMarkdownFiles(string directoryPath, params string[] dirsToIgnore)
    {
        DirectoryInfo dir = new(directoryPath);
        IEnumerable<FileInfo> files = dir.EnumerateFiles("*.md", SearchOption.AllDirectories).ToList();

        if (dirsToIgnore.Length > 0)
        {
            foreach (string ignoreDir in dirsToIgnore)
            {
                string dirWithSeparators = $"{Path.DirectorySeparatorChar}{ignoreDir}{Path.DirectorySeparatorChar}";
                files = files.Where(f => !f.DirectoryName!.Contains(dirWithSeparators, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        return files.ToList();
    }

    /// <summary>
    /// Gets all *.yml files recursively, starting in the specified directory.
    /// </summary>
    private static List<FileInfo> GetYAMLFiles(string directoryPath)
    {
        DirectoryInfo dir = new(directoryPath);
        return dir.EnumerateFiles("*.yml", SearchOption.AllDirectories).ToList();
    }

    /// <summary>
    /// Returns a dictionary of all .png/.jpg/.gif/.svg files in the directory.
    /// The search includes the specified directory and (optionally) all its subdirectories.
    /// </summary>
    private static Dictionary<string, List<string>> GetMediaFiles(string mediaDirectory, bool searchRecursively = true)
    {
        DirectoryInfo dir = new DirectoryInfo(mediaDirectory);

        SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        Dictionary<string, List<string>> mediaFiles = new(StringComparer.InvariantCultureIgnoreCase);

        string[] fileExtensions = { "*.png", "*.jpg", "*.gif", "*.svg" }; // Correctly initialize the array

        foreach (string extension in fileExtensions)
        {
            foreach (FileInfo file in dir.EnumerateFiles(extension, searchOption))
            {
                mediaFiles.Add(file.FullName, []);
            }
        }

        if (mediaFiles.Count == 0)
            Console.WriteLine("\nNo .png/.jpg/.gif/.svg files were found!");

        return mediaFiles;
    }
    /// <summary>
    /// Returns a dictionary of all .png/.jpg/.gif/.svg files in the directory.
    /// The search includes the text found in the files.
    /// </summary>
    private static Dictionary<string, string> ScanMediaFiles(List<string>? imageFilePaths, string ocrModelDirectory)
    {
        Dictionary<string, string> ocrDataForFiles = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        if (imageFilePaths.Count == 0)
            Console.WriteLine("\nNo .png/.jpg/.gif/.svg files to scan!");

        using (var engine = new TesseractEngine(ocrModelDirectory, "eng", EngineMode.Default))
        {

            foreach (string imageFilePath in imageFilePaths)
            {
                using (var img = Pix.LoadFromFile(imageFilePath))
                {
                    using (Page page = engine.Process(img))
                    {
                        string text = page.GetText();
                        ocrDataForFiles.Add(imageFilePath, text);
                    }
                }

            }

        }
        return ocrDataForFiles;

    }

    // Filter ocrDictionary by filterTerms
    private static Dictionary<string, List<KeyValuePair<string, string>>> FilterMediaFiles(Dictionary<string, string> ocrDictionary, List<string> filterTerms)
    {
        // Sort the filterTerms to ensure the result is sorted by filterTerm
        filterTerms.Sort();

        Dictionary<string, List<KeyValuePair<string, string>>> filterTermFilesDictionary = new Dictionary<string, List<KeyValuePair<string, string>>>();

        foreach (string filterTerm in filterTerms)
        {
            List<KeyValuePair<string, string>> matchedFiles = new List<KeyValuePair<string, string>>();

            foreach (var imageFile in ocrDictionary)
            {
                if (imageFile.Value.Contains(filterTerm, StringComparison.OrdinalIgnoreCase))
                {
                    // Add both the file path and the text for that file
                    matchedFiles.Add(new KeyValuePair<string, string>(imageFile.Key, imageFile.Value));
                }
            }

            if (matchedFiles.Count > 0)
            {
                filterTermFilesDictionary.Add(filterTerm, matchedFiles);
            }
        }

        return filterTermFilesDictionary;
    }

    /// <summary>
    /// Gets all *.yml files recursively, starting in the ancestor directory that contains docfx.json.
    /// </summary>
    internal static List<FileInfo>? GetAllYamlFiles(string directoryPath, out DirectoryInfo? rootDirectory)
    {
        // Look further up the path until we find docfx.json.
        rootDirectory = GetDirectory(new DirectoryInfo(directoryPath), "docfx.json");

        if (rootDirectory is null)
            return null;

        return rootDirectory.EnumerateFiles("*.yml", SearchOption.AllDirectories).ToList();
    }

    /// <summary>
    /// Gets all *.md files recursively, starting in the ancestor directory that contains docfx.json.
    /// </summary>
    internal static List<FileInfo>? GetAllMarkdownFiles(string directoryPath, out DirectoryInfo? rootDirectory)
    {
        // Look further up the path until we find docfx.json
        rootDirectory = GetDirectory(new DirectoryInfo(directoryPath), "docfx.json");

        if (rootDirectory is null)
            return null;

        return rootDirectory.EnumerateFiles("*.md", SearchOption.AllDirectories).ToList();
    }

    /// <summary>
    /// Gets all *.xml files recursively, starting in the ancestor directory that contains docfx.json.
    /// </summary>
    internal static List<FileInfo>? GetAllEcmaXmlFiles(string directoryPath, out DirectoryInfo? rootDirectory)
    {
        // Look further up the path until we find docfx.json
        rootDirectory = GetDirectory(new DirectoryInfo(directoryPath), "docfx.json");

        if (rootDirectory is null)
            return null;

        return rootDirectory.EnumerateFiles("*.xml", SearchOption.AllDirectories).ToList();
    }

    /// <summary>
    /// Returns the specified directory if it contains a file with the specified name.
    /// Otherwise returns the nearest parent directory that contains a file with the specified name.
    /// </summary>
    internal static DirectoryInfo? GetDirectory(DirectoryInfo dir, string fileName)
    {
        try
        {
            while (dir?.GetFiles(fileName, SearchOption.TopDirectoryOnly).Length == 0)
            {
                if (dir.Parent is null)
                    return null;

                dir = dir.Parent;

                if (string.Equals(dir.FullName, dir?.Root.FullName))
                    return null;
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
