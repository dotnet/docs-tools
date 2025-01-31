using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CleanRepo.Extensions;
using DotNet.DocsTools.GitHubObjects;
using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Tesseract;

namespace CleanRepo;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Annoying")]
class Program
{
    private static readonly List<string> s_functions = [
        "FindOrphanedArticles",
        "FindOrphanedImages",
        "FindOrphanedIncludes",
        "FindOrphanedSnippets",
        "CatalogImages",
        "CatalogImagesWithText",
        "FilterImagesForText",
        "ReplaceRedirectTargets",
        "ReplaceWithRelativeLinks",
        "RemoveRedirectHops",
        "AuditMSDate"
    ];

    static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.Sources.Clear();

        // Add appsettings.json.
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

        // Add command-line args (if any).
        if (args is { Length: > 0 })
        {
            builder.Configuration.AddCommandLine(args);
        }

        Options options = new();
        builder.Configuration.GetSection(nameof(Options))
            .Bind(options);

        await RunOptions(options);
    }

    static async Task RunOptions(Options options)
    {
        if (String.IsNullOrEmpty(options.Function))
        {
            Console.WriteLine($"\nYou didn't specify which function to perform, " +
                $"such as {s_functions[0]}, {s_functions[1]}, {s_functions[2]}, or {s_functions[3]}.");
            return;
        }

        if (String.IsNullOrEmpty(options.DocFxDirectory))
        {
            Console.WriteLine("\nYou didn't specify the directory that contains the docfx.json file.");
            return;
        }

        if (String.IsNullOrEmpty(options.TargetDirectory))
        {
            Console.WriteLine("\nYou didn't specify the directory to search/clean.");
            return;
        }

        if (String.IsNullOrEmpty(options.UrlBasePath))
        {
            Console.WriteLine("\nYou didn't specify the URL base path, such as /dotnet or /windows/uwp.");
            return;
        }

        if (!Directory.Exists(options.TargetDirectory))
        {
            Console.WriteLine($"\nThe '{options.TargetDirectory}' directory doesn't exist.");
            return;
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Initialize the DocFxRepo object for all options.
        var docFxRepo = new DocFxRepo(options.DocFxDirectory, options.UrlBasePath);
        if (docFxRepo.DocFxDirectory is null)
        {
            Console.WriteLine($"\nCouldn't find docfx.json file in '{options.DocFxDirectory}' or an ancestor directory...exiting.");
            return;
        }

        // Make sure the searchable directory is part of the same DocFx docset.
        if (!options.TargetDirectory.IsSubdirectoryOf(docFxRepo.DocFxDirectory!.FullName))
        {
            Console.WriteLine($"'{options.TargetDirectory}' is not a child of the " +
                $"docfx.json file's directory '{docFxRepo.DocFxDirectory}'.");
            return;
        }

        switch (options.Function)
        {
            case "FindOrphanedArticles":
                {
                    Console.WriteLine($"\nSearching the '{options.TargetDirectory}' directory and its subdirectories for orphaned articles...");

                    List<FileInfo> markdownFiles = HelperMethods.GetMarkdownFiles(options.TargetDirectory, "snippets");

                    if (docFxRepo.AllTocFiles is null || markdownFiles is null)
                        return;

                    ListOrphanedArticles(docFxRepo.AllTocFiles, markdownFiles, options.Delete);
                    break;
                }
            case "FindOrphanedImages":
                {
                    // Add regex to find image refs similar to 'social_image_url: "/dotnet/media/logo.png"'
                    // This is done here (dynamically) because it relies on knowing the base path URL.
                    docFxRepo._imageLinkRegExes.Add($"social_image_url: ?\"?(?<path>{docFxRepo.UrlBasePath}.*?(\\.(png|jpg|gif|svg))+)");

                    // Gather media file names.
                    if (docFxRepo._imageRefs is null)
                        docFxRepo._imageRefs = HelperMethods.GetMediaFiles(options.TargetDirectory);

                    Console.WriteLine($"\nSearching the '{options.TargetDirectory}' directory recursively " +
                        $"for orphaned .png/.jpg/.gif/.svg files...\n");

                    docFxRepo.ListOrphanedImages(options.Delete, "snippets");
                    break;
                }
            case "CatalogImages":
                {
                    // Add regex to find image refs similar to 'social_image_url: "/dotnet/media/logo.png"'
                    // This is done here (dynamically) because it relies on knowing the base path URL.
                    docFxRepo._imageLinkRegExes.Add($"social_image_url: ?\"?(?<path>{docFxRepo.UrlBasePath}.*?(\\.(png|jpg|gif|svg))+)");

                    // Gather media file names.
                    if (docFxRepo._imageRefs is null)
                        docFxRepo._imageRefs = HelperMethods.GetMediaFiles(options.TargetDirectory);

                    Console.WriteLine($"\nCataloging '{docFxRepo._imageRefs.Count}' images (recursively) " +
                        $"in the '{options.TargetDirectory}' directory...\n");

                    docFxRepo.OutputImageReferences();
                    break;
                }
            case "CatalogImagesWithText":
                {
                    if (string.IsNullOrEmpty(options.OcrModelDirectory))
                    {
                        Console.WriteLine($"'OcrModelDirectory' directory was not provided.");
                        return;
                    }

                    // Add regex to find image refs similar to 'social_image_url: "/dotnet/media/logo.png"'
                    // This is done here (dynamically) because it relies on knowing the base path URL.
                    docFxRepo._imageLinkRegExes.Add($"social_image_url: ?\"?(?<path>{docFxRepo.UrlBasePath}.*?(\\.(png|jpg|gif|svg))+)");

                    // Gather media file names.
                    if (docFxRepo._imageRefs is null)
                        docFxRepo._imageRefs = HelperMethods.GetMediaFiles(options.TargetDirectory);

                    Console.WriteLine($"\nCataloging '{docFxRepo._imageRefs.Count}' images (recursively) " +
                        $"in the '{options.TargetDirectory}' directory...\n");

                    // Extract hash keys from the dictionary.
                    List<string> mediaFilesList = docFxRepo._imageRefs.Keys.ToList();

                    // Pass hash keys to ScanMediaFiles.
                    docFxRepo._ocrRefs = HelperMethods.ScanMediaFiles(mediaFilesList, options.OcrModelDirectory);

                    docFxRepo.OutputImageReferences(true);
                    break;
                }
            case "FilterImagesForText":
                {
                    if (string.IsNullOrEmpty(options.OcrModelDirectory))
                    {
                        Console.WriteLine($"'OcrModelDirectory' directory was not provided.");
                        return;
                    }
                    if (string.IsNullOrEmpty(options.FilterTextJsonFile))
                    {
                        Console.WriteLine($"\nThe FilterTextJsonFile input can't be empty when requesting FilterImagesForText.");
                        return;
                    }
                    if (!File.Exists(options.FilterTextJsonFile))
                    {
                        Console.WriteLine($"\nThe filter text file '{options.FilterTextJsonFile}' doesn't exist.");
                        return;
                    }

                    List<string> searchTerms = [];
                    try
                    {
                        string jsonContent = File.ReadAllText(options.FilterTextJsonFile);
                        searchTerms = JsonSerializer.Deserialize<List<string>>(jsonContent) ?? [];
                    }
                    catch (IOException ioEx)
                    {
                        Console.WriteLine($"\nIO error reading '{options.FilterTextJsonFile}': {ioEx.Message}");
                    }
                    catch (UnauthorizedAccessException uaEx)
                    {
                        Console.WriteLine($"\nAccess error reading '{options.FilterTextJsonFile}': {uaEx.Message}");
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"\nError deserializing '{options.FilterTextJsonFile}': {jsonEx.Message}");
                    }
                    catch (Exception ex) // Fallback for any other unexpected exceptions.
                    {
                        Console.WriteLine($"\nUnexpected error: {ex.Message}");
                        return;
                    }
                    if (searchTerms.Count == 0)
                    {
                        Console.WriteLine($"\nNo search terms found in '{options.FilterTextJsonFile}'.");
                        return;
                    }

                    // Add regex to find image refs similar to 'social_image_url: "/dotnet/media/logo.png"'
                    // This is done here (dynamically) because it relies on knowing the base path URL.
                    docFxRepo._imageLinkRegExes.Add($"social_image_url: ?\"?(?<path>{docFxRepo.UrlBasePath}.*?(\\.(png|jpg|gif|svg))+)");

                    // Gather media file names.
                    if (docFxRepo._imageRefs is null)
                        docFxRepo._imageRefs = HelperMethods.GetMediaFiles(options.TargetDirectory);

                    Console.WriteLine($"\nCataloging '{docFxRepo._imageRefs.Count}' images (recursively) " +
                        $"in the '{options.TargetDirectory}' directory...\n");

                    // Extract hash keys from the dictionary.
                    List<string> mediaFilesList = docFxRepo._imageRefs.Keys.ToList();

                    if (mediaFilesList.Count == 0)
                    {
                        Console.WriteLine($"\nNo media files found.");
                    }

                    // Pass hash keys to ScanMediaFiles.
                    Dictionary<string, string> unfilteredResults = HelperMethods.ScanMediaFiles(mediaFilesList, options.OcrModelDirectory);

                    // Filter results.
                    docFxRepo._ocrFilteredRefs = HelperMethods.FilterMediaFiles(unfilteredResults, searchTerms);

                    docFxRepo.OutputImageReferences(true, true);
                    break;
                }
            // Find orphaned include-type files
            case "FindOrphanedIncludes":
                {
                    Console.WriteLine($"\nSearching the '{options.TargetDirectory}' directory recursively for orphaned .md files " +
                        $"in directories or subdirectories of a directory named 'includes'.");

                    Dictionary<string, int> includeFiles = GetIncludeFiles(options.TargetDirectory);

                    if (includeFiles.Count == 0)
                    {
                        Console.WriteLine("\nNo .md files were found in any directories or subdirectories of a directory named 'includes'.");
                        return;
                    }
                    else
                        Console.WriteLine($"\nChecking {includeFiles.Count} include files.");

                    ListOrphanedIncludes(options.TargetDirectory, includeFiles, options.Delete);
                    break;
                }
            case "FindOrphanedSnippets":
                {
                    Console.WriteLine($"\nSearching the '{options.TargetDirectory}' directory recursively for orphaned snippet files.");

                    // Get all snippet files.
                    List<(string, string?)> snippetFiles = GetSnippetFiles(options.TargetDirectory);
                    if (snippetFiles.Count == 0)
                    {
                        Console.WriteLine("\nNo files with matching extensions were found.");
                        return;
                    }

                    // Associate snippet files to a project (where applicable).
                    AddProjectInfo(ref snippetFiles);

                    // Catalog all the solution files and the project (directories) they reference.
                    List<(string, List<string?>)> solutionFiles = GetSolutionFiles(options.TargetDirectory);

                    ListOrphanedSnippets(options.TargetDirectory, snippetFiles, solutionFiles,
                        options.Delete, options.XmlSource, options.LimitReferencingDirectories);
                    break;
                }
            // Replace links to articles that are redirected in the master redirection files.
            case "ReplaceRedirectTargets":
                {
                    Console.WriteLine($"\nSearching the '{options.TargetDirectory}' directory for links to redirected topics...\n");

                    // Gather all the redirects.
                    List<Redirect> redirects = docFxRepo.GetAllRedirects();

                    // Get all the markdown and YAML files.
                    List<FileInfo> linkingFiles = HelperMethods.GetMarkdownFiles(options.TargetDirectory);
                    linkingFiles.AddRange(HelperMethods.GetYAMLFiles(options.TargetDirectory));

                    // Check all links, including in toc.yml, to files in the redirects list.
                    // Replace links to redirected files.
                    docFxRepo.ReplaceRedirectedLinks(redirects, linkingFiles);

                    Console.WriteLine("\nFinished replacing redirected links.");
                    break;
                }
            // Replace site-relative links to *this* repo with file-relative links.
            case "ReplaceWithRelativeLinks":
                {
                    // Check that this isn't the root directory of the repo. The code doesn't handle that case currently
                    // because it can't always determine the base path of the docset (e.g. for dotnet/docs repo).
                    if (string.Equals(docFxRepo.OpsConfigFile.DirectoryName, options.TargetDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"\nYou specified the repo root directory as the target directory. " +
                            $"Please enter a subdirectory in which to replace links.");
                        return;
                    }

                    // Get the absolute path to the base directory for this docset.
                    string? rootDirectory = docFxRepo.GetDocsetAbsolutePath(options.TargetDirectory);

                    if (rootDirectory is null)
                    {
                        Console.WriteLine($"\nThe docfx.json file for {options.TargetDirectory} is invalid.");
                        return;
                    }

                    Console.WriteLine($"\nReplacing site-relative links to '{docFxRepo.UrlBasePath}/' in " +
                        $"the '{options.TargetDirectory}' directory with file-relative links.\n");

                    // Get all the markdown and YAML files in the search directory.
                    List<FileInfo> linkingFiles = HelperMethods.GetMarkdownFiles(options.TargetDirectory);
                    linkingFiles.AddRange(HelperMethods.GetYAMLFiles(options.TargetDirectory));

                    // Check all links in these files.
                    ReplaceLinks(linkingFiles, docFxRepo.UrlBasePath, rootDirectory);

                    Console.WriteLine("\nFinished fixing relative links.");
                    break;
                }
            // Remove hops/daisy chains in a redirection file.        
            case "RemoveRedirectHops":
                {
                    docFxRepo.RemoveAllRedirectHops();

                    Console.WriteLine("\nFinished removing redirect hops.");
                    break;
                }
            // Audit the 'ms.date' property in all markdown files.
            case "AuditMSDate":
                {
                    Console.WriteLine($"\nAuditing the 'ms.date' property in all markdown files in '{options.TargetDirectory}'...");

                    if (docFxRepo.AllTocFiles is null)
                        return;

                    List<FileInfo> articleFiles = HelperMethods.GetMarkdownFiles(options.TargetDirectory, "snippets", "includes");

                    articleFiles.AddRange(HelperMethods.GetYAMLFiles(options.TargetDirectory));

                    Console.WriteLine($"Total number of files to process: {articleFiles.Count}");

                    await AuditMSDateAccuracy(options, docFxRepo, articleFiles);
                    break;
                }
            default:
                {
                    Console.WriteLine($"\nUnknown function '{options.Function}'. " +
                        $"Please specify one of the following functions: {string.Join(", ", s_functions)}.");
                    break;
                }
        }

        stopwatch.Stop();
        Console.WriteLine($"Elapsed time: {stopwatch.Elapsed.ToHumanReadableString()}");
    }

    private static async Task AuditMSDateAccuracy(Options options, DocFxRepo docFxRepo, List<FileInfo> articleFiles)
    {
        if (options.DocFxDirectory is null)
            return;
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        string key = config["GitHubKey"]!;
        IGitHubClient client = IGitHubClient.CreateGitHubClient(key);

        int totalArticles = 0;
        int freshArticles = 0;
        int trulyStateArticles = 0;
        int falseStaleArticles = 0;
        // This could be configurable in time (or now, even):
        DateOnly staleContentDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-1));

        string[] progressMarkers = ["| -", "/ \\", "- |", "\\ /"];
        const string removeProgressMarkers = "\b\b\b\b\b\b\b\b\b\b\b";

        Console.WriteLine($"PRs Changes Last Commit    ms.date Path");
        Console.Write($"{totalArticles,7} {progressMarkers[totalArticles % progressMarkers.Length]}");
        foreach (var article in articleFiles)
        {
            totalArticles++;
            Console.Write($"{removeProgressMarkers}{totalArticles,7} {progressMarkers[totalArticles % progressMarkers.Length]}");
            // First, don't do more work on fresh artricles. This is the
            // least expensive (in time) test to look for.
            DateOnly? msDate = await HelperMethods.GetmsDate(article.FullName);
            if (msDate is null)
            {
                continue;
            }
            if (msDate > staleContentDate)
            {
                freshArticles++;
                continue;
            }

            // Next, use git history to get the last commit. This starts a process,
            // so it's quite a bit more expensive than the msDate check.
            DateOnly? commitDate = await HelperMethods.GetCommitDate(options.DocFxDirectory, article.FullName);
            if (commitDate < staleContentDate)
            {
                trulyStateArticles++;
                continue;
            }

            // Give a week from msDate to allow for PR edits before merging.
            // Without this buffer of time, the checks below often include
            // the PR where the date was updated. That results in a lot of
            // false positives.
            DateOnly msDateMergeDate = DateOnly.FromDateTime(new DateTime(msDate.Value, default).AddDays(7));

            var query = new EnumerationQuery<FileHistory, FileHistoryVariables>(client);

            // Even on windows, the paths need to be unix-style for the GitHub API,
            // and the opening slash must be removed.
            var path = article.FullName.Replace(options.DocFxDirectory, "").Replace('\\', '/').Remove(0, 1);

            var variables = new FileHistoryVariables("dotnet", "docs", path);
            int numberChanges = 0;
            int numberPRs = 0;
            await foreach (var history in query.PerformQuery(variables))
            {
                if ((DateOnly.FromDateTime(history.CommittedDate) <= msDateMergeDate) ||
                    (DateOnly.FromDateTime(history.CommittedDate) <= staleContentDate))
                {
                    break;
                }
                if (history.ChangedFilesIfAvailable < 100) // not a bulk PR
                {
                    numberPRs++;
                    numberChanges += Math.Max(history.Deletions, history.Additions);
                }
            }
            if (numberChanges > 0)
            {
                Console.Write(removeProgressMarkers);
                falseStaleArticles++;
                Console.WriteLine($"{numberPRs,3} {numberChanges,7}  {commitDate:MM-dd-yyyy} {msDate:MM-dd-yyyy} {path}");
                Console.Write($"{totalArticles,7} {progressMarkers[totalArticles % progressMarkers.Length]}");
            }
        }
        Console.WriteLine($"{removeProgressMarkers} {totalArticles} checked. Fresh: {freshArticles}. Truly stale: {trulyStateArticles}. Updated but not fresh: {falseStaleArticles}");
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
            absolutePath = HelperMethods.GetActualCaseForFilePath(absolutePath);
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
        DirectoryInfo? rootDirectory = null;

        // Get all files that could possibly link to the include files
        List<FileInfo>? files = HelperMethods.GetAllReferencingFiles("*.md", inputDirectory, ref rootDirectory);

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

            DirectoryInfo? projectDir = HelperMethods.GetDirectory(new DirectoryInfo(fi.DirectoryName!), $"*{projExtension}");
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

    private static void ListOrphanedSnippets(string inputDirectory,
        List<(string, string?)> snippetFiles,
        List<(string, List<string?>)> solutionFiles,
        bool deleteOrphanedSnippets,
        bool searchEcmaXmlFiles,
        List<string>? limitReferencingDirectories)
    {
        // Get all files that could possibly link to the snippet files.
        List<FileInfo>? files;
        DirectoryInfo? rootDirectory = null;

        // XML or Markdown file repo?
        string searchPattern = searchEcmaXmlFiles ? "*.xml" : "*.md";

        if (limitReferencingDirectories is null)
            files = HelperMethods.GetAllReferencingFiles(searchPattern, inputDirectory, ref rootDirectory);
        else
        {
            files = [];
            foreach (string directory in limitReferencingDirectories)
            {
                List<FileInfo>? ecmaXmlFiles = HelperMethods.GetAllReferencingFiles(searchPattern, directory, ref rootDirectory, false);
                if (ecmaXmlFiles is not null)
                    files.AddRange(ecmaXmlFiles);
            }
        }

        if (files is null || files.Count == 0 || rootDirectory is null)
            return;

        Console.WriteLine($"Checking {snippetFiles.Count} snippet files " +
            $"against {files.Count} {(searchEcmaXmlFiles ? "XML" : "Markdown")} files.");

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
            //foreach (FileInfo mdOrXmlFile in files)
            Parallel.ForEach(files, mdOrXmlFile =>
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
                    //break;
                    return;
                // else check the next Markdown file.
            });

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

        // Exclude certain Markdown files.
        static bool IsArticleFile(FileInfo file) =>
            !file.FullName.Contains($"{Path.DirectorySeparatorChar}includes{Path.DirectorySeparatorChar}") &&
            !file.FullName.Contains($"{Path.DirectorySeparatorChar}misc{Path.DirectorySeparatorChar}") &&
            !file.FullName.Contains($"{Path.DirectorySeparatorChar}mermaidjs{Path.DirectorySeparatorChar}") &&
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
                if (HelperMethods.IsFileLinkedFromFile(markdownFile, tocFile))
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
}

#region Generic helper methods
static class HelperMethods
{
    /// <summary>
    /// Gets the actual (case-sensitive) file path on the file system for a specified case-insensitive file path.
    /// </summary>
    public static string GetActualCaseForFilePath(string pathAndFileName)
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
    public static string? GetDirectoryCaseSensitive(string directory)
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
    public static bool IsFileLinkedFromFile(FileInfo linkedFile, FileInfo linkingFile)
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
    public static List<FileInfo> GetMarkdownFiles(string directoryPath, params string[] dirsToIgnore)
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
    public static List<FileInfo> GetYAMLFiles(string directoryPath)
    {
        DirectoryInfo dir = new(directoryPath);
        return dir.EnumerateFiles("*.yml", SearchOption.AllDirectories).ToList();
    }

    /// <summary>
    /// Returns a dictionary of all .png/.jpg/.gif/.svg files in the directory.
    /// The search includes the specified directory and (optionally) all its subdirectories.
    /// </summary>
    public static Dictionary<string, List<string>> GetMediaFiles(string mediaDirectory, bool searchRecursively = true)
    {
        DirectoryInfo dir = new(mediaDirectory);

        SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        Dictionary<string, List<string>> mediaFiles = new(StringComparer.InvariantCultureIgnoreCase);

        string[] fileExtensions = ["*.png", "*.jpg", "*.gif", "*.svg"]; // Correctly initialize the array

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
    public static Dictionary<string, string> ScanMediaFiles(List<string>? imageFilePaths, string ocrModelDirectory)
    {

        Dictionary<string, string> ocrDataForFiles = new(StringComparer.InvariantCultureIgnoreCase);

        if (imageFilePaths is null or { Count: 0 })
        {
            Console.WriteLine("\nNo .png/.jpg/.gif/.svg files to scan!");
            return ocrDataForFiles;
        }

        using var engine = new TesseractEngine(ocrModelDirectory, "eng", EngineMode.Default);
        foreach (string imageFilePath in imageFilePaths)
        {
            using var img = Pix.LoadFromFile(imageFilePath);
            using Page page = engine.Process(img);

            string text = page.GetText();
            ocrDataForFiles.Add(imageFilePath, text);
        }
        return ocrDataForFiles;
    }

    // Filter ocrDictionary by filterTerms
    public static Dictionary<string, List<KeyValuePair<string, string>>> FilterMediaFiles(Dictionary<string, string> ocrDictionary, List<string> filterTerms)
    {
        // Sort the filterTerms to ensure the result is sorted by filterTerm
        filterTerms.Sort();

        Dictionary<string, List<KeyValuePair<string, string>>> filterTermFilesDictionary = [];

        foreach (string filterTerm in filterTerms)
        {
            List<KeyValuePair<string, string>> matchedFiles = [];

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
    /// Gets all files that match <paramref name="searchPattern"/>, starting in the ancestor directory that contains docfx.json.
    /// </summary>
    internal static List<FileInfo>? GetAllReferencingFiles(
        string searchPattern,
        string directoryPath,
        ref DirectoryInfo? rootDirectory,
        bool searchRootDirectory = true)
    {
        DirectoryInfo? currentRootDir = rootDirectory;

        // Look further up the path until we find docfx.json
        rootDirectory = GetDirectory(new DirectoryInfo(directoryPath), "docfx.json");

        if (rootDirectory is null)
            return null;
        else if (currentRootDir != null && string.Compare(rootDirectory.FullName, currentRootDir.FullName) != 0)
        {
            Console.WriteLine($"\n**WARNING** The provided referencing directories are from multiple docsets, which isn't allowed.\n");
        }

        if (searchRootDirectory)
            return rootDirectory.EnumerateFiles(searchPattern, SearchOption.AllDirectories).ToList();
        else
        {
            // Only search the specified directory and its subdirectories.
            DirectoryInfo searchDirInfo = new(directoryPath);
            return searchDirInfo.EnumerateFiles(searchPattern, SearchOption.AllDirectories).ToList();
        }
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

    internal static async Task<DateOnly?> GetmsDate(string filePath)
    {
        DateOnly? msDate = default;
        await foreach (var line in File.ReadLinesAsync(filePath))
        {
            if (line.Contains("ms.date"))
            {
                string[] parts = line.Split(":");
                if (parts.Length > 1)
                {
                    string date = parts[1].Trim().Replace("\"", ""); // yeah, remove quotes.
                    if (DateOnly.TryParse(date, out DateOnly parsedDate))
                    {
                        msDate = parsedDate;
                        break;
                    }
                    else
                    {
                        Console.Error.WriteLine($"Invalid date format in {filePath}: {date}");
                    }
                }
            }
        }
        return msDate;
    }

    internal static async Task<DateOnly> GetCommitDate(string folder, string path)
    {
        // Create a new process
        Process process = new Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = $"""log -1 --format="%cd" --date=short {path}""";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.WorkingDirectory = folder;

        // Start the process
        process.Start();

        // Read the output
        string output = await process.StandardOutput.ReadToEndAsync();

        // Wait for the process to exit
        await process.WaitForExitAsync();
        return DateOnly.Parse(output);
    }
}
#endregion
