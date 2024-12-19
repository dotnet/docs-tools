using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CleanRepo;

/// <summary>
/// Represents a DocFx docset under a single docfx.json file.
/// </summary>
class DocFxRepo(string startDirectory, string urlBasePath)
{
    internal string UrlBasePath { get; set; } = urlBasePath;
    public DirectoryInfo? DocFxDirectory { get; private set; } = HelperMethods.GetDirectory(new DirectoryInfo(startDirectory), "docfx.json");

    internal Dictionary<string, List<string>>? _imageRefs = null;
    internal Dictionary<string, string>? _ocrRefs = null;
    internal Dictionary<string, List<KeyValuePair<string, string>>>? _ocrFilteredRefs = null;
    internal readonly List<string> _imageLinkRegExes =
    [
        @"!\[.*?\]\((?<path>.*?(\.(png|jpg|gif|svg))+)", // ![hello](media/how-to/xamarin.png)
        "<img[^>]*?src[ ]*=[ ]*[\"'](?<path>[^>]*?(\\.(png|gif|jpg|svg))+)[ ]*[\"']", // <img data-hoverimage="./images/start.svg" src="./images/start.png" alt="Start icon" />
        @"\[.*\]:(?<path>.*?(\.(png|gif|jpg|svg))+)", // [0]: ../../media/how-to/xamarin.png
        @"imageSrc:(?<path>[^:]*?(\.(png|gif|jpg|svg))+)", // imageSrc: ./media/vs-mac.svg
        @"thumbnailUrl: (?<path>.*?(\.(png|gif|jpg|svg))+)", // thumbnailUrl: /thumbs/two-forest.png
        "lightbox\\s*=\\s*\"(?<path>.*?(\\.(png|gif|jpg|svg))+)\"", // lightbox="media/azure.png"
        ":::image [^:]*?source\\s*=\\s*\"(?<path>.*?(\\.(png|gif|jpg|svg))+)(\\?[\\w\\s=\\.]+)?\\s*\"", // :::image type="content" source="media/publish.png?text=Publish dialog." alt-text="Publish dialog.":::
        "<a href=\"(?<path>[^\"]*?(\\.(png|gif|jpg|svg))+)\"", // <a href="./media/job-large.png" target="_blank"><img src="./media/job-small.png"></a>
        "\\]\\((?<path>[^\\)]*?(\\.(png|jpg|gif|svg)))+(#lightbox)[\\s|\\)]" //](../images/alignment-expansion-large.png#lightbox)
    ];
    private List<FileInfo>? _allMdAndYmlFiles;
    private List<FileInfo>? AllMdAndYmlFiles
    {
        get
        {
            DirectoryInfo? rootDirectory = null;
            if (_allMdAndYmlFiles == null)
            {
                _allMdAndYmlFiles = HelperMethods.GetAllReferencingFiles("*.md", DocFxDirectory!.FullName, ref rootDirectory);
                List<FileInfo>? yamlFiles = HelperMethods.GetAllReferencingFiles("*.yml", DocFxDirectory!.FullName, ref rootDirectory);
                if (yamlFiles is not null)
                    _allMdAndYmlFiles?.AddRange(yamlFiles);
            }
            return _allMdAndYmlFiles;
        }
    }
    private List<FileInfo>? _allTocFiles;
    internal List<FileInfo> AllTocFiles
    {
        get
        {
            if (_allTocFiles == null)
            {
                _allTocFiles = DocFxDirectory!.EnumerateFiles("toc.*", SearchOption.AllDirectories).ToList();

                // Add other TOC files for case-sensitive OSs.
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    _allTocFiles.AddRange(DocFxDirectory!.EnumerateFiles("TOC.*", SearchOption.AllDirectories));
            }
            return _allTocFiles;
        }
    }
    private FileInfo? _opsConfigFile;
    internal FileInfo OpsConfigFile
    {
        get
        {
            if (_opsConfigFile == null)
                _opsConfigFile = HelperMethods.GetFileHereOrInParent(DocFxDirectory!.FullName, ".openpublishing.publish.config.json");

            if (_opsConfigFile == null)
                throw new InvalidOperationException($"Could not find OPS config file for the {DocFxDirectory!.FullName} directory.");

            return _opsConfigFile;
        }
    }
    private List<string>? _redirectionFiles;
    internal List<string> RedirectionFiles
    {
        get
        {
            if (_redirectionFiles == null)
                _redirectionFiles = GetRedirectionFiles();

            return _redirectionFiles;
        }
    }

    #region Constructors
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
        if (_imageRefs is null)
            throw new ApplicationException("Never obtained media files.");

        // Find all image references.
        CatalogImages();

        // Determine if we need to check the docfx.json file for image references.
        string docfxText = File.ReadAllText(Path.Combine(DocFxDirectory!.FullName, "docfx.json"));
        bool checkDocFxMetadata = docfxText.Contains("social_image_url", StringComparison.InvariantCultureIgnoreCase);

        int orphanedCount = 0;

        // Print out (and delete) the image files with zero references.
        StringBuilder output = new();
        foreach (KeyValuePair<string, List<string>> image in _imageRefs)
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
        imageFilePath = imageFilePath.Substring(DocFxDirectory!.FullName.Length);

        imageFilePath = ConvertImagePathSrcToDest(imageFilePath.TrimStart(Path.DirectorySeparatorChar)) ?? imageFilePath;

        // Replace backslashes with forward slashes.
        imageFilePath = imageFilePath.Replace('\\', '/');

        // Finally, add the base URL.
        imageFilePath = $"{UrlBasePath}{imageFilePath}";

        using (StreamReader sr = new(Path.Combine(DocFxDirectory!.FullName, "docfx.json")))
        {
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains($"\"{imageFilePath}\"", StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    internal void OutputImageReferences(bool ocrImages = false, bool filteredOcrImage = false)
    {
        // Find all image references.
        CatalogImages();

        if (ocrImages)
        {
            WriteOcrImageRefsToFile();
        } else if (ocrImages && filteredOcrImage)
        {
            WriteFilteredOcrImageRefsToFile();
        } else
        {
            WriteImageRefsToFile();
        }
            
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
            return;

        // Find all image refs.
        Parallel.ForEach(AllMdAndYmlFiles, sourceFile =>
        //foreach (var sourceFile in MdAndYmlFiles)
        {
            foreach (string line in File.ReadAllLines(sourceFile.FullName))
            {
                foreach (string regEx in _imageLinkRegExes)
                {
                    // There could be more than one image reference on the line, hence the foreach loop.
                    foreach (Match match in Regex.Matches(line, regEx, RegexOptions.IgnoreCase))
                    {
                        string path = match.Groups["path"].Value.Trim();
                        string? absolutePath = GetAbsolutePath(path, sourceFile);

                        if (absolutePath != null)
                            TryAddLinkingFile(absolutePath, sourceFile.FullName);
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

        string json = JsonSerializer.Serialize(_imageRefs, options);

        // Create a new file path.
        string fileName = $"ImageFiles-{UrlBasePath.TrimStart('/').Replace('/', '-')}-{DateTime.Now.Ticks}.json";
        string outputPath = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"Image file catalog successfully written to {outputPath}.");

    }
    private void WriteOcrImageRefsToFile()
    {
        // Serialize the image references to a JSON file.
        JsonSerializerOptions options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(_ocrRefs, options);

        // Create a new file path.
        string fileName = $"OcrImageFiles-{UrlBasePath.TrimStart('/').Replace('/', '-')}-{DateTime.Now.Ticks}.json";
        string outputPath = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"OCR Image file catalog successfully written to {outputPath}.");

    }
    private void WriteFilteredOcrImageRefsToFile()
    {
        // Serialize the image references to a JSON file.
        JsonSerializerOptions options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(_ocrFilteredRefs, options);

        // Create a new file path.
        string fileName = $"FilteredOcrImageFiles-{UrlBasePath.TrimStart('/').Replace('/', '-')}-{DateTime.Now.Ticks}.json";
        string outputPath = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"Filtered Image file catalog successfully written to {outputPath}.");

    }
    internal List<Redirect> GetAllRedirects()
    {
        // Gather all the redirects.
        List<Redirect> allRedirects = [];
        foreach (string redirectionFile in RedirectionFiles)
        {
            FileInfo redirectsFile = new(Path.Combine(OpsConfigFile.DirectoryName!, redirectionFile));
            if (redirectsFile == null)
            {
                Console.WriteLine($"\nCould not find redirection file '{redirectionFile}'.");
                continue;
            }

            IList<Redirect>? redirectsFromOneFile = GetAllRedirectedFiles(redirectsFile, OpsConfigFile.DirectoryName!);
            if (redirectsFromOneFile is not null)
                allRedirects.AddRange(redirectsFromOneFile);
        }

        return allRedirects;
    }

    internal string? GetDocsetAbsolutePath(string startDirectory)
    {
        // Deserialize the docfx.json file.
        DocFx? docfx = LoadDocfxFile(Path.Combine(DocFxDirectory!.FullName, "docfx.json"));
        if (docfx == null)
            return null;

        string? docsetPath = null;

        // If there's more than one docset, choose the one that includes the input directory.
        if (docfx.build?.content is not null)
        {
            foreach (Content entry in docfx.build.content)
            {
                if (entry.src == null)
                    continue;

                if (entry.src.TrimEnd('/') == ".")
                    docsetPath = DocFxDirectory!.FullName;
                else
                    docsetPath = Path.GetFullPath(entry.src, DocFxDirectory!.FullName);

                // Check that it's a parent (or the same) directory as the input directory.
                if (startDirectory.StartsWith(docsetPath))
                    break;
            }
        }

        return docsetPath;
    }

    private string? GetAbsolutePath(string path, FileInfo linkingFile)
    {
        if (path.StartsWith("http:") || path.StartsWith("https:"))
        {
            // This could be an absolute URL to a file in the repo, so check.
            string httpRegex = @"https?:\/\/docs.microsoft.com\/([A-z][A-z]-[A-z][A-z]\/)?" + UrlBasePath + @"\/";
            Match httpMatch = Regex.Match(path, httpRegex, RegexOptions.IgnoreCase);

            if (!httpMatch.Success)
            {
                // The file is in a different repo, so ignore it.
                return null;
            }

            // Chop off the https://docs.microsoft.com/docset/ part of the path.
            path = path.Substring(httpMatch.Value.Length);
        }
        else if (path.StartsWith('/'))
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
                    absolutePath = Path.Combine(DocFxDirectory!.FullName, path.TrimStart('~', '/'));

                // This case includes site-relative links to files in the same repo where
                // we've already trimmed off the docset name.
                else if (path.StartsWith('/'))
                {
                    // Determine if any additional directory names must be added to the path.
                    path = ConvertImagePathDestToSrc(path.TrimStart('/'));

                    absolutePath = Path.Combine(DocFxDirectory!.FullName, path);
                }
                else
                {
                    absolutePath = Path.Combine(linkingFile.DirectoryName!, path);
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
        if (_imageRefs is null)
            throw new ApplicationException("Never obtained media files.");

        if (_imageRefs.TryGetValue(key, out List<string>? value))
            value.Add(linkingFile);
        else if (_imageRefs.TryGetValue(System.Web.HttpUtility.UrlDecode(key), out List<string>? value2))
            value2.Add(linkingFile);
    }

    /// <summary>
    /// Pulls docset information, including URL base path, from the OPS config and docfx.json files.
    /// </summary>
    internal Dictionary<string, string>? GetDocsetInfo()
    {
        // Deserialize the OPS config file to get build source folders.
        OPSConfig? config = LoadOPSJson();
        if (config == null)
        {
            Console.WriteLine("Could not deserialize OPS config file.");
            return null;
        }

        var mappingInfo = new Dictionary<string, string>();
        if (config.docsets_to_publish is not null)
        {
            foreach (Docset sourceFolder in config.docsets_to_publish)
            {
                // Deserialize the corresponding docfx.json file.
                if (sourceFolder.build_source_folder is null)
                    continue;

                string docfxFilePath = Path.Combine(OpsConfigFile.DirectoryName!, sourceFolder.build_source_folder, "docfx.json");
                DocFx? docfx = LoadDocfxFile(docfxFilePath);
                if (docfx == null)
                    continue;

                // There can be more than one "src" path.
                if (docfx.build?.content is not null)
                {
                    foreach (Content item in docfx.build.content)
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
                                        docsetFilePath = string.Concat(docsetFilePath, "/", item.src);
                                }
                            }

                            if (!mappingInfo.ContainsKey(docsetFilePath))
                                mappingInfo.Add(docsetFilePath, UrlBasePath);
                        }
                    }
                }
            }
        }

        return mappingInfo;
    }

    internal string? ConvertImagePathSrcToDest(string currentImagePath)
    {
        // Deserialize the docfx.json file.
        DocFx? docfx = LoadDocfxFile(Path.Combine(DocFxDirectory!.FullName, "docfx.json"));
        if (docfx == null)
            return null;

        if (docfx.build?.resource is not null)
        {
            foreach (Resource entry in docfx.build.resource)
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
        }

        // We didn't find any useful info in the docfx.json file, so just return the same string back.
        return currentImagePath;
    }

    internal string ConvertImagePathDestToSrc(string currentImagePath)
    {
        // Deserialize the docfx.json file.
        DocFx? docfx = LoadDocfxFile(Path.Combine(DocFxDirectory!.FullName, "docfx.json"));
        if (docfx == null)
            return currentImagePath;

        if (docfx.build?.resource is not null)
        {
            foreach (Resource entry in docfx.build.resource)
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
        }

        // We didn't find any useful info in the docfx.json file, so just return the same string back.
        return currentImagePath;
    }

    #region Redirected files

    public List<string> GetRedirectionFiles()
    {
        // Deserialize the OPS config file.
        OPSConfig? config = LoadOPSJson();
        if (config == null || config.redirection_files == null)
            return [".openpublishing.redirection.json"];
        else
            return config.redirection_files;
    }

    internal void RemoveAllRedirectHops()
    {
        // Get all docsets for the OPS config file.
        Dictionary<string, string>? docsets = GetDocsetInfo();

        // Remove hops within each file.
        foreach (string redirectionFile in RedirectionFiles)
        {
            FileInfo redirectsFile = new(Path.Combine(OpsConfigFile.DirectoryName!, redirectionFile));
            if (redirectsFile == null)
            {
                Console.WriteLine($"\nCould not find redirection file '{redirectionFile}'.");
                continue;
            }

            Console.WriteLine($"\nRemoving hops from the '{redirectionFile}' redirection file.");
            RemoveRedirectHopsFromFile(redirectsFile, docsets, OpsConfigFile.DirectoryName!);
        }
    }

    private static RedirectionFile? LoadRedirectJson(FileInfo redirectsFile)
    {
        using (StreamReader reader = new(redirectsFile.FullName))
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
    private static void RemoveRedirectHopsFromFile(FileInfo redirectsFile, Dictionary<string, string>? docsets, string rootPath)
    {
        RedirectionFile? redirectionFile = LoadRedirectJson(redirectsFile);
        if (redirectionFile is null)
        {
            Console.WriteLine("Deserialization of redirection file failed.");
            return;
        }

        string fileText = File.ReadAllText(redirectsFile.FullName);

        if (redirectionFile.redirections is null)
            return;

        // Load the sources and targets into a dictionary for easier look up.
        Dictionary<string, string> redirectsLookup = new(redirectionFile.redirections.Count, StringComparer.InvariantCultureIgnoreCase);
        foreach (Redirect redirect in redirectionFile.redirections)
        {
            string? fullPath = null;
            if (redirect.source_path != null)
            {
                // Construct the full path to the redirected file
                fullPath = Path.Combine(redirectsFile.DirectoryName!, redirect.source_path);
            }
            else if (redirect.source_path_from_root != null)
            {
                // Construct the full path to the redirected file
                fullPath = Path.Combine(rootPath, redirect.source_path_from_root.Substring(1));
            }

            // Path.GetFullPath doesn't require the file or directory to exist,
            // so this works on case-sensitive file systems too.
            if (redirect.redirect_url is not null)
                redirectsLookup.Add(Path.GetFullPath(fullPath!), redirect.redirect_url);
        }

        foreach (KeyValuePair<string, string> redirectPair in redirectsLookup)
        {
            string currentTarget = redirectPair.Value;

            string? docsetRootFolderName = null;
            string? basePathUrl = null;

            if (docsets is not null)
            {
                foreach (KeyValuePair<string, string> docset in docsets)
                {
                    if (currentTarget.StartsWith(docset.Value + "/"))
                    {
                        docsetRootFolderName = docset.Key;
                        basePathUrl = docset.Value;
                        break;
                    }
                }
            }

            if (docsetRootFolderName is null || basePathUrl is null)
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
                if (string.Equals(redirectsLookup[normalizedTargetPath], currentTarget, StringComparison.InvariantCultureIgnoreCase))
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

    private static IList<Redirect>? GetAllRedirectedFiles(FileInfo redirectsFile, string rootPath)
    {
        RedirectionFile? RedirectionFile = LoadRedirectJson(redirectsFile);

        if (RedirectionFile is null)
        {
            Console.WriteLine("Deserialization failed.");
            return null;
        }

        if (RedirectionFile.redirections is not null)
        {
            // Set the absolute redirect path for each redirect.
            foreach (Redirect redirect in RedirectionFile.redirections)
            {
                if (redirect.source_path != null)
                {
                    // Construct the full path to the redirected file
                    string fullPath = Path.Combine(redirectsFile.DirectoryName!, redirect.source_path);

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
        }

        return RedirectionFile.redirections;
    }

    internal void ReplaceRedirectedLinks(IList<Redirect> redirects, List<FileInfo> linkingFiles)
    {
        Dictionary<string, Redirect> redirectLookup =
            Enumerable.ToDictionary(
                redirects,
                r => r.source_path_absolute ?? "empty",
                StringComparer.InvariantCultureIgnoreCase
                );

        List<string> mdRegexes =
            [
                @"\]\(<?([^\)]*\.md)(#[\w-]+)?>?\)",
                @"\]:\s(.*\.md)(#[\w-]+)?"
            ];

        // Matches link with optional #bookmark on the end.
        string ymlRegex = @"href:(.*\.md)(#[\w-]+)?";

        // For each file...
        foreach (FileInfo linkingFile in linkingFiles)
        {
            bool foundOldLink = false;
            StringBuilder output = new($"FILE '{linkingFile.FullName}' contains the following link(s) to redirected files:\n\n");

            string text = File.ReadAllText(linkingFile.FullName);

            if (linkingFile.Extension.Equals(".yml", StringComparison.CurrentCultureIgnoreCase))
                FindAndReplaceLinks(ymlRegex, redirectLookup, linkingFile, ref foundOldLink, output, ref text);
            else // Markdown file.
            {
                foreach (string mdRegex in mdRegexes)
                {
                    FindAndReplaceLinks(mdRegex, redirectLookup, linkingFile, ref foundOldLink, output, ref text);

                }
            }

            if (foundOldLink)
                Console.WriteLine(output.ToString());
        }
    }

    private void FindAndReplaceLinks(string regexPattern, Dictionary<string, Redirect> redirectLookup, FileInfo linkingFile, ref bool foundOldLink, StringBuilder output, ref string text)
    {
        // For each link in the file...
        // Regex ignores case.
        foreach (Match match in Regex.Matches(text, regexPattern, RegexOptions.IgnoreCase))
        {
            // Get the file-relative path to the linked file.
            string relativePath = match.Groups[1].Value.Trim();

            if (relativePath.StartsWith("http"))
            {
                // This could be an absolute URL to a file in the repo, so check.
                string httpRegex = @"https?:\/\/learn.microsoft.com\/([A-z][A-z]-[A-z][A-z]\/)?" + UrlBasePath + @"\/";
                Match httpMatch = Regex.Match(relativePath, httpRegex, RegexOptions.IgnoreCase);

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

            string? fullPath = null;
            try
            {
                if (linkingFile.DirectoryName is null)
                    continue;

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
                if (redirectLookup.TryGetValue(fullPath, out Redirect? value))
                {
                    foundOldLink = true;
                    output.AppendLine($"'{relativePath}'");

                    string? redirectURL = value.redirect_url;

                    // Add the bookmark back on, in case it applies to the new target.
                    if (!string.IsNullOrEmpty(match.Groups[2].Value))
                        redirectURL += match.Groups[2].Value;

                    output.AppendLine($"REPLACING '{relativePath}' with '{redirectURL}'.");

                    // Replace the link.
                    if (linkingFile.Extension.Equals(".md", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (regexPattern.StartsWith(@"\]:"))
                            text = text.Replace(match.Groups[0].Value, $"]: {redirectURL}");
                        else
                            text = text.Replace(match.Groups[0].Value, $"]({redirectURL})");
                    }
                    else // .yml file
                        text = text.Replace(match.Groups[0].Value, $"href: {redirectURL}");

                    File.WriteAllText(linkingFile.FullName, text);
                }
            }
        }
    }
    #endregion

    /// <summary>
    /// Deserialize OPS config file.
    /// </summary>
    private OPSConfig? LoadOPSJson()
    {
        using (StreamReader reader = new(OpsConfigFile.FullName))
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
    /// Deserialize any docfx.json file.
    /// </summary>
    private static DocFx? LoadDocfxFile(string docFxPath)
    {
        using (StreamReader reader = new(docFxPath))
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
        public List<Docset>? docsets_to_publish { get; set; }
        public List<string>? redirection_files { get; set; }
    }
    class Docset
    {
        public string? build_source_folder { get; set; }
    }
    class DocFx
    {
        public Build? build { get; set; }
    }
    class Build
    {
        public GlobalMetadata? globalMetadata { get; set; }
        public List<Content>? content { get; set; }
        public List<Resource>? resource { get; set; }
    }
    class Resource
    {
        public string? src { get; set; }
        public string? dest { get; set; }
    }
    class Content
    {
        public string? src { get; set; }
        public string? dest { get; set; }
    }
    class GlobalMetadata
    {
        public string? breadcrumb_path { get; set; }
    }
    #endregion
}
