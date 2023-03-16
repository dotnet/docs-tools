using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CleanRepo;

/// <summary>
/// Represents a DocFx docset under a single docfx.json file.
/// </summary>
class DocFxRepo
{
    internal string UrlBasePath { get; set; }
    public DirectoryInfo DocFxDirectory { get; private set; }

    internal Dictionary<string, List<string>> ImageRefs = null;
    internal readonly List<string> ImageLinkRegExes = new List<string>
    {
        @"\]\((.*?(\.(png|jpg|gif|svg))+)", // ![hello](media/how-to/xamarin.png)
        "<img[^>]*?src[ ]*=[ ]*\"([^>]*?(\\.(png|gif|jpg|svg))+)[ ]*\"", // <img data-hoverimage="./images/start.svg" src="./images/start.png" alt="Start icon" />
        @"\[.*\]:(.*?(\.(png|gif|jpg|svg))+)", // [0]: ../../media/how-to/xamarin.png
        @"imageSrc:([^:]*?(\.(png|gif|jpg|svg))+)", // imageSrc: ./media/vs-mac.svg
        @"thumbnailUrl: (.*?(\.(png|gif|jpg|svg))+)", // thumbnailUrl: /thumbs/two-forest.png
        "lightbox=\"(.*?(\\.(png|gif|jpg|svg))+)\"", // lightbox="media/azure.png"
        ":::image [^:]*?source=\"(.*?(\\.(png|gif|jpg|svg))+)\"", // :::image type="content" source="media/publish.png" alt-text="Publish dialog.":::
        "<a href=\"([^\"]*?(\\.(png|gif|jpg|svg))+)\"" // <a href="./media/job-large.png" target="_blank"><img src="./media/job-small.png"></a>
    };
    private List<FileInfo> _allMdAndYmlFiles;
    private List<FileInfo> AllMdAndYmlFiles
    {
        get
        {
            if (_allMdAndYmlFiles == null)
            {
                _allMdAndYmlFiles = Program.GetAllMarkdownFiles(DocFxDirectory.FullName, out _);
                _allMdAndYmlFiles.AddRange(Program.GetAllYamlFiles(DocFxDirectory.FullName, out _));
            }
            return _allMdAndYmlFiles;
        }
    }
    private List<FileInfo> _allTocFiles;
    internal List<FileInfo> AllTocFiles
    {
        get
        {
            if (_allTocFiles == null)
            {
                _allTocFiles = DocFxDirectory.EnumerateFiles("toc.*", SearchOption.AllDirectories).ToList();

                // Add other TOC files for case-sensitive OSs.
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    _allTocFiles.AddRange(DocFxDirectory.EnumerateFiles("TOC.*", SearchOption.AllDirectories));
            }
            return _allTocFiles;
        }
    }
    private FileInfo _opsConfigFile;
    internal FileInfo OpsConfigFile
    {
        get
        {
            if (_opsConfigFile == null)
            {
                _opsConfigFile = Program.GetFileHereOrInParent(DocFxDirectory.FullName, ".openpublishing.publish.config.json");
            }
            if (_opsConfigFile == null)
            {
                throw new InvalidOperationException($"Could not find OPS config file for the {DocFxDirectory.FullName} directory.");
            }
            return _opsConfigFile;
        }
    }
    private List<string> _redirectionFiles;
    internal List<string> RedirectionFiles
    {
        get
        {
            if (_redirectionFiles == null)
            {
                _redirectionFiles = GetRedirectionFiles();
            }
            return _redirectionFiles;
        }
    }

    #region Constructors
    public DocFxRepo(string startDirectory)
    {
        DocFxDirectory = Program.GetDirectory(new DirectoryInfo(startDirectory), "docfx.json");

        // Check that this directory or one of its ancestors has a docfx.json file.
        // I.e. we don't want to be given a parent directory of a docfx.json directory; single docset only.
        if (DocFxDirectory is null)
        {
            throw new ArgumentException("Unable to find a docfx.json file in the provided directory or its ancestors.", "startDirectory");
        }
    }
    #endregion

    /// <summary>
    /// If any of the input image files are not
    /// referenced from a markdown (.md) or YAML (.yml) file anywhere in the docset, including up the directory 
    /// until the docfx.json file is found, the file path of those files is written to the console.
    /// </summary>
    /// TODO: Improve the perf of this method using the following pseudo code:
    /// For each image
    ///    For each markdown file
    ///       Do a RegEx search for the image
    ///          If found, BREAK to the next image
    internal void ListOrphanedImages(bool deleteOrphanedImages, params string[] dirsToIgnore)
    {
        // Find all image references.
        CatalogImages();

        // Determine if we need to check the docfx.json file for image references.
        string docfxText = File.ReadAllText(Path.Combine(DocFxDirectory.FullName, "docfx.json"));
        bool checkDocFxMetadata = docfxText.Contains("social_image_url", StringComparison.InvariantCultureIgnoreCase);

        int orphanedCount = 0;

        // Print out (and delete) the image files with zero references.
        StringBuilder output = new StringBuilder();
        foreach (var image in ImageRefs)
        {
            if (image.Value.Count == 0)
            {
                bool ignoreImageFile = false;

                // Check if the image is in an ignored directory.
                foreach (string dirToIgnore in dirsToIgnore)
                {
                    if (image.Key.Contains(dirToIgnore, StringComparison.InvariantCultureIgnoreCase))
                    {
                        ignoreImageFile = true;
                        break;
                    }
                }

                if (!ignoreImageFile && checkDocFxMetadata)
                {
                    // As a final get out of jail card, check if the
                    // image is referenced in any docfx.json metadata.
                    ignoreImageFile = IsImageInDocFxFile(image.Key);
                }

                if (!ignoreImageFile)
                {
                    orphanedCount++;
                    output.AppendLine(Path.GetFullPath(image.Key));

                    if (deleteOrphanedImages)
                    {
                        try
                        {
                            File.Delete(image.Key);
                        }
                        catch (PathTooLongException)
                        {
                            output.AppendLine($"Unable to delete {image.Key} because its path is too long.");
                        }
                    }
                }
            }
        }

        string deleted = deleteOrphanedImages ? "and deleted " : "";

        Console.WriteLine($"\nFound {deleted}{orphanedCount} orphaned .png/.jpg/.gif/.svg files:\n");
        Console.WriteLine(output.ToString());
        Console.WriteLine("DONE");
    }

    /// <summary>
    /// Returns true if the docfx.json file references the specified image, 
    /// for example, in metadata such as social_image_url.
    /// </summary>
    private bool IsImageInDocFxFile(string imageFilePath)
    {
        // Construct the site-relative path to the file.

        // First remove the directory path to the docfx.json file.
        imageFilePath = imageFilePath.Substring(DocFxDirectory.FullName.Length);

        imageFilePath = ConvertImagePathSrcToDest(imageFilePath.TrimStart(Path.DirectorySeparatorChar));

        // Replace backslashes with forward slashes.
        imageFilePath = imageFilePath.Replace('\\', '/');

        // Finally, add the base URL.
        imageFilePath = $"{UrlBasePath}{imageFilePath}";

        using (StreamReader sr = new StreamReader(Path.Combine(DocFxDirectory.FullName, "docfx.json")))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains($"\"{imageFilePath}\"", StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal void OutputImageReferences()
    {
        // Find all image references.
        CatalogImages();

        WriteImageRefsToFile();
    }

    /// <summary>
    /// Supports all of the following image reference variations:
    /// 
    /// [hello] (media/how-to-use-lightboxes/xamarin.png#lightbox)
    /// ![Auto hide] (../ide/media/vs2015_auto_hide.png)
    /// ![Unit Test Explorer showing Run All button] (../test/media/unittestexplorer-beta-.png "UnitTestExplorer(beta)")
    /// ![Architecture] (./media/ci-cd-flask/Architecture.PNG? raw = true)
    /// The Light Bulb icon ![Small Light Bulb Icon] (media/vs2015_lightbulbsmall.png "VS2017_LightBulbSmall")
    /// imageSrc: ./media/vs-mac-2019.svg
    /// <img src="/azure/mydocs/media/pic3.png" alt="Work Backlogs page shortcuts"/>
    /// [0]: ../../media/vs-acr-provisioning-dialog-2019.png
    /// :::image type = "complex" source="./media/seedwork-classes.png" alt-text="Screenshot of the SeedWork folder.":::
    /// :::image type = "content" source="../media/rpi.png" lightbox="../media/rpi-lightbox.png":::
    /// </summary>
    private void CatalogImages()
    {
        if (AllMdAndYmlFiles is null)
        {
            return;
        }

        // Find all image refs.
        Parallel.ForEach(AllMdAndYmlFiles, sourceFile =>
        //foreach (var sourceFile in MdAndYmlFiles)
        {
            foreach (string line in File.ReadAllLines(sourceFile.FullName))
            {
                foreach (var regEx in ImageLinkRegExes)
                {
                    // There could be more than one image reference on the line, hence the foreach loop.
                    foreach (Match match in Regex.Matches(line, regEx, RegexOptions.IgnoreCase))
                    {
                        string path = match.Groups[1].Value.Trim();
                        string absolutePath = GetAbsolutePath(path, sourceFile);

                        if (absolutePath != null)
                        {
                            TryAddLinkingFile(absolutePath, sourceFile.FullName);
                        }
                    }
                }
            }
        });
    }

    private void WriteImageRefsToFile()
    {
        // Serialize the image references to a JSON file.
        JsonSerializerOptions options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(ImageRefs, options);

        // Create a new file path.
        string fileName = $"ImageFiles-{UrlBasePath.TrimStart('/').Replace('/', '-')}-{DateTime.Now.Ticks.ToString()}.json";
        string outputPath = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"Image file catalog successfully written to {outputPath}.");
    }

    internal List<Redirect> GetAllRedirects()
    {
        // Gather all the redirects.
        List<Redirect> redirects = new List<Redirect>();
        foreach (string redirectionFile in RedirectionFiles)
        {
            FileInfo redirectsFile = new FileInfo(Path.Combine(OpsConfigFile.DirectoryName, redirectionFile));
            if (redirectsFile == null)
            {
                Console.WriteLine($"\nCould not find redirection file '{redirectionFile}'.");
                continue;
            }

            redirects.AddRange(GetAllRedirectedFiles(redirectsFile, OpsConfigFile.DirectoryName));
        }

        return redirects;
    }

    internal string GetDocsetAbsolutePath(string startDirectory)
    {
        // Deserialize the docfx.json file.
        DocFx docfx = LoadDocfxFile(Path.Combine(DocFxDirectory.FullName, "docfx.json"));
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
                docsetPath = DocFxDirectory.FullName;
            else
                docsetPath = Path.GetFullPath(entry.src, DocFxDirectory.FullName);

            // Check that it's a parent (or the same) directory as the input directory.
            if (startDirectory.StartsWith(docsetPath))
                break;
        }

        return docsetPath;
    }

    private string GetAbsolutePath(string path, FileInfo linkingFile)
    {
        if (path.StartsWith("http:") || path.StartsWith("https:"))
        {
            // This could be an absolute URL to a file in the repo, so check.
            string httpRegex = @"https?:\/\/docs.microsoft.com\/([A-z][A-z]-[A-z][A-z]\/)?" + UrlBasePath + @"\/";
            var httpMatch = Regex.Match(path, httpRegex, RegexOptions.IgnoreCase);

            if (!httpMatch.Success)
            {
                // The file is in a different repo, so ignore it.
                return null;
            }

            // Chop off the https://docs.microsoft.com/docset/ part of the path.
            path = path.Substring(httpMatch.Value.Length);
        }
        else if (path.StartsWith("/"))
        {
            if (!path.StartsWith($"{UrlBasePath}/"))
            {
                // The file is in a different repo, so ignore it.
                return null;
            }

            // Trim off the docset name, but leave the forward slash that follows it.
            path = path.Substring(UrlBasePath.Length);
        }

        if (path != null)
        {
            // Construct the full path to the referenced image file
            string absolutePath;
            try
            {
                // Path could start with a tilde e.g. ~/media/pic1.png
                if (path.StartsWith("~/"))
                {
                    absolutePath = Path.Combine(DocFxDirectory.FullName, path.TrimStart('~', '/'));
                }
                // This case includes site-relative links to files in the same repo where
                // we've already trimmed off the docset name.
                else if (path.StartsWith("/"))
                {
                    // Determine if any additional directory names must be added to the path.
                    path = ConvertImagePathDestToSrc(path.TrimStart('/'));

                    absolutePath = Path.Combine(DocFxDirectory.FullName, path);
                }
                else
                {
                    absolutePath = Path.Combine(linkingFile.DirectoryName, path);
                }
            }
            catch (ArgumentException)
            {
                return null;
            }

            // This cleans up the path by replacing forward slashes
            // with back slashes, removing extra dots, etc.
            try
            {
                absolutePath = Path.GetFullPath(absolutePath);
            }
            catch (ArgumentException)
            {
                return null;
            }

            return absolutePath;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Helper function to add a linking file to the catalog of image references.
    /// </summary>
    void TryAddLinkingFile(string key, string linkingFile)
    {
        if (ImageRefs.ContainsKey(key))
        {
            ImageRefs[key].Add(linkingFile);
        }
        else if (ImageRefs.ContainsKey(System.Web.HttpUtility.UrlDecode(key)))
        {
            ImageRefs[System.Web.HttpUtility.UrlDecode(key)].Add(linkingFile);
        }
    }

    /// <summary>
    /// Pulls docset information, including URL base path, from the OPS config and docfx.json files.
    /// </summary>
    internal Dictionary<string, string> GetDocsetInfo()
    {
        // Deserialize the OPS config file to get build source folders.
        OPSConfig config = LoadOPSJson();
        if (config == null)
        {
            Console.WriteLine("Could not deserialize OPS config file.");
            return null;
        }

        var mappingInfo = new Dictionary<string, string>();
        foreach (var sourceFolder in config.docsets_to_publish)
        {
            // Deserialize the corresponding docfx.json file.
            string docfxFilePath = Path.Combine(OpsConfigFile.DirectoryName, sourceFolder.build_source_folder, "docfx.json");
            DocFx docfx = LoadDocfxFile(docfxFilePath);
            if (docfx == null)
            {
                continue;
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
                        mappingInfo.Add(docsetFilePath, UrlBasePath);
                }
            }
        }

        return mappingInfo;
    }

    internal string ConvertImagePathSrcToDest(string currentImagePath)
    {
        // Deserialize the docfx.json file.
        DocFx docfx = LoadDocfxFile(Path.Combine(DocFxDirectory.FullName, "docfx.json"));
        if (docfx == null)
        {
            return null;
        }

        foreach (var entry in docfx.build.resource)
        {
            if (entry.src == null || entry.dest == null)
                continue;

            if (entry.src.TrimEnd('/') == ".")
                return Path.Combine(entry.dest, currentImagePath);

            if (!currentImagePath.StartsWith(entry.src))
                continue;

            // If we get here, the path starts with entry.src,
            // so replace that part of the path with whatever entry.dest is.
            return Path.Combine(entry.dest, currentImagePath.Substring(entry.src.Length));
        }

        // We didn't find any useful info in the docfx.json file, so just return the same string back.
        return currentImagePath;
    }

    internal string ConvertImagePathDestToSrc(string currentImagePath)
    {
        // Deserialize the docfx.json file.
        DocFx docfx = LoadDocfxFile(Path.Combine(DocFxDirectory.FullName, "docfx.json"));
        if (docfx == null)
        {
            return null;
        }

        foreach (var entry in docfx.build.resource)
        {
            if (entry.src == null || entry.dest == null)
                continue;

            // This one applies to the dotnet/docs repo.
            if (entry.dest.TrimEnd('/') == ".")
                return Path.Combine(entry.src, currentImagePath);

            if (!currentImagePath.StartsWith(entry.dest))
                continue;

            // If we get here, the path starts with entry.dest,
            // so replace that part of the path with whatever entry.src is.
            return Path.Combine(entry.src, currentImagePath.Substring(entry.dest.Length));
        }

        // We didn't find any useful info in the docfx.json file, so just return the same string back.
        return currentImagePath;
    }

    #region Redirected files

    public List<string> GetRedirectionFiles()
    {
        // Deserialize the OPS config file.
        OPSConfig config = LoadOPSJson();
        if (config == null || config.redirection_files == null)
        {
            return new List<string>() { ".openpublishing.redirection.json" };
        }
        else
            return config.redirection_files;
    }

    internal void RemoveAllRedirectHops()
    {
        // Get all docsets for the OPS config file.
        Dictionary<string, string> docsets = GetDocsetInfo();

        // Remove hops within each file.
        foreach (string redirectionFile in RedirectionFiles)
        {
            FileInfo redirectsFile = new FileInfo(Path.Combine(OpsConfigFile.DirectoryName, redirectionFile));
            if (redirectsFile == null)
            {
                Console.WriteLine($"\nCould not find redirection file '{redirectionFile}'.");
                continue;
            }

            Console.WriteLine($"\nRemoving hops from the '{redirectionFile}' redirection file.");
            RemoveRedirectHopsFromFile(redirectsFile, docsets, OpsConfigFile.DirectoryName);
        }
    }

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
    /// For each target URL, see if it's a source_path somewhere else.
    /// If so, replace the original target URL with the new target URL.
    /// </summary>
    private static void RemoveRedirectHopsFromFile(FileInfo redirectsFile, Dictionary<string, string> docsets, string rootPath)
    {
        RedirectionFile RedirectionFile = LoadRedirectJson(redirectsFile);
        if (RedirectionFile is null)
        {
            Console.WriteLine("Deserialization of redirection file failed.");
            return;
        }

        string fileText = File.ReadAllText(redirectsFile.FullName);

        // Load the sources and targets into a dictionary for easier look up.
        Dictionary<string, string> redirectsLookup = new Dictionary<string, string>(RedirectionFile.redirections.Count, StringComparer.InvariantCultureIgnoreCase);
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

            // Path.GetFullPath doesn't require the file or directory to exist,
            // so this works on case-sensitive file systems too.
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
                if (String.Equals(redirectsLookup[normalizedTargetPath], currentTarget, StringComparison.InvariantCultureIgnoreCase))
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

    internal void ReplaceRedirectedLinks(IList<Redirect> redirects, List<FileInfo> linkingFiles)
    {
        Dictionary<string, Redirect> redirectLookup = 
            Enumerable.ToDictionary(redirects, r => r.source_path_absolute, StringComparer.InvariantCultureIgnoreCase);

        // For each file...
        foreach (var linkingFile in linkingFiles)
        {
            bool foundOldLink = false;
            StringBuilder output = new StringBuilder($"FILE '{linkingFile.FullName}' contains the following link(s) to redirected files:\n\n");

            string text = File.ReadAllText(linkingFile.FullName);

            // Matches link with optional #bookmark on the end.
            string linkRegEx = linkingFile.Extension.ToLower() == ".yml" ?
                @"href:(.*\.md)(#[\w-]+)?" :
                @"\]\(<?([^\)]*\.md)(#[\w-]+)?>?\)";

            // For each link in the file...
            // Regex ignores case.
            foreach (Match match in Regex.Matches(text, linkRegEx, RegexOptions.IgnoreCase))
            {
                // Get the file-relative path to the linked file.
                string relativePath = match.Groups[1].Value.Trim();

                if (relativePath.StartsWith("http"))
                {
                    // This could be an absolute URL to a file in the repo, so check.
                    string httpRegex = @"https?:\/\/learn.microsoft.com\/([A-z][A-z]-[A-z][A-z]\/)?" + UrlBasePath + @"\/";
                    var httpMatch = Regex.Match(relativePath, httpRegex, RegexOptions.IgnoreCase);

                    if (!httpMatch.Success)
                    {
                        // The file is in a different repo, so ignore it.
                        continue;
                    }

                    // Chop off the https://learn.microsoft.com/BasePathUrl/ part of the path.
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

    /// <summary>
    /// Deserialize OPS config file.
    /// </summary>
    private OPSConfig LoadOPSJson()
    {
        using (StreamReader reader = new StreamReader(OpsConfigFile.FullName))
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
    /// Deserialize the docfx.json file for this docset.
    /// </summary>
    private DocFx LoadDocfxFile()
    {
        using (StreamReader reader = new StreamReader(Path.Combine(DocFxDirectory.FullName, "docfx.json")))
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
    /// Deserialize any docfx.json file.
    /// </summary>
    private static DocFx LoadDocfxFile(string docFxPath)
    {
        using (StreamReader reader = new StreamReader(docFxPath))
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

    #region Deserialization classes
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
        public List<Resource> resource { get; set; }
    }
    class Resource
    {
        public string src { get; set; }
        public string dest { get; set; }
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
    #endregion
}
