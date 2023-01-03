using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CleanRepo
{
    class DocFxRepo
    {
        public string BasePathUrl { get; private set; }
        public DirectoryInfo DocFxDirectory { get; private set; }
        public Dictionary<string, List<string>> ImageRefs = new Dictionary<string, List<string>>();
        List<FileInfo> MdAndYmlFiles { get; set; }

        private readonly string[] RegExes = new string[]
        {
            @"\]\(([^\)]*?\.(png|jpg|gif|svg))", // ![hello](media/how-to/xamarin.png)
            "<img[^>]*?src[ ]*=[ ]*\"([^>]*?.(png|gif|jpg|svg))[ ]*\"", // <img data-hoverimage="./images/start.svg" src="./images/start.png" alt="Start icon" />
            @"\[.*\]:(.*\.(png|gif|jpg|svg))", // [0]: ../../media/how-to/xamarin.png
            @"imageSrc:([^:]*\.(png|gif|jpg|svg))", // imageSrc: ./media/vs-mac.svg
            @"thumbnailUrl: (.*\.(png|gif|jpg|svg))", // thumbnailUrl: /thumbs/two-forest.png
            "lightbox=\"(.*?.(png|gif|jpg|svg))\"", // lightbox="media/azure.png"
            ":::image [^:]*?source=\"(.*?.(png|gif|jpg|svg))\"" // :::image type="content" source="media/publish.png" alt-text="Publish dialog.":::
        };

        // Constructor.
        public DocFxRepo(string inputDirectory)
        {
            DocFxDirectory = Program.GetDirectory(new DirectoryInfo(inputDirectory), "docfx.json");

            if (DocFxDirectory is null)
            {
                throw new ArgumentException("Unable to find a docfx.json file in the input directory or one of its ancestors.");
            }

            BasePathUrl = Program.GetUrlBasePath(DocFxDirectory);

            // Gather media file names.
            ImageRefs = GetMediaFiles(inputDirectory);

            // Gather Markdown files.
            MdAndYmlFiles = Program.GetAllMarkdownFiles(inputDirectory, out _);

            // Add YAML files.
            MdAndYmlFiles.AddRange(Program.GetAllYamlFiles(inputDirectory, out _));
        }

        /// <summary>
        /// Returns a dictionary of all .png/.jpg/.gif/.svg files in the directory.
        /// The search includes the specified directory and (optionally) all its subdirectories.
        /// </summary>
        private static Dictionary<string, List<string>> GetMediaFiles(string mediaDirectory, bool searchRecursively = true)
        {
            DirectoryInfo dir = new DirectoryInfo(mediaDirectory);

            SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            Dictionary<string, List<string>> mediaFiles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            string[] fileExtensions = new string[] { "*.png", "*.jpg", "*.gif", "*.svg" };

            foreach (var extension in fileExtensions)
            {
                foreach (var file in dir.EnumerateFiles(extension, searchOption))
                {
                    mediaFiles.Add(file.FullName.ToLower(), new List<string>());
                }
            }

            if (mediaFiles.Count == 0)
            {
                Console.WriteLine("\nNo .png/.jpg/.gif/.svg files were found!");
            }

            return mediaFiles;
        }

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
        internal void ListOrphanedImages(bool deleteOrphanedImages)
        {
            // Find all image references.
            CatalogImages();

            int orphanedCount = 0;

            // Print out the image files with zero references.
            StringBuilder output = new StringBuilder();
            foreach (var image in ImageRefs)
            {
                if (image.Value.Count == 0)
                {
                    orphanedCount++;
                    output.AppendLine(Path.GetFullPath(image.Key));
                }
            }

            if (deleteOrphanedImages)
            {
                // Delete orphaned image files
                foreach (var image in ImageRefs)
                {
                    if (image.Value.Count == 0)
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

            string deleted = deleteOrphanedImages ? "and deleted " : "";

            Console.WriteLine($"\nFound {deleted}{orphanedCount} orphaned .png/.jpg/.gif/.svg files:\n");
            Console.WriteLine(output.ToString());
            Console.WriteLine("DONE");
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
        /// 
        /// Does not currently support file names that contain parentheses: [VS image] (../media/pic(azure)_1.png)
        /// </summary>
        private void CatalogImages()
        {
            if (MdAndYmlFiles is null)
            {
                return;
            }

            // Find all image refs.
            Parallel.ForEach(MdAndYmlFiles, sourceFile =>
            //foreach (var sourceFile in MdAndYmlFiles)
            {
                foreach (string line in File.ReadAllLines(sourceFile.FullName))
                {
                    foreach (var regEx in RegExes)
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
            string fileName = $"ImageFiles-{BasePathUrl.TrimStart('/').Replace('/', '-')}-{DateTime.Now.Ticks.ToString()}.json";
            string outputPath = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(outputPath, json);

            Console.WriteLine($"Image file catalog successfully written to {outputPath}.");
        }

        private string GetAbsolutePath(string path, FileInfo linkingFile)
        {
            if (path.StartsWith("http:") || path.StartsWith("https:"))
            {
                // This could be an absolute URL to a file in the repo, so check.
                string httpRegex = @"https?:\/\/docs.microsoft.com\/([A-z][A-z]-[A-z][A-z]\/)?" + BasePathUrl + @"\/";
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
                if (!path.StartsWith("/" + BasePathUrl + "/"))
                {
                    // The file is in a different repo, so ignore it.
                    return null;
                }

                // Trim off the docset name, but leave the forward slash that follows it.
                path = path.Substring(BasePathUrl.Length + 1);
            }

            if (path != null)
            {
                // Construct the full path to the referenced image file
                string absolutePath;
                try
                {
                    // Path could start with a tilde e.g. ~/media/pic1.png
                    // This case also includes site-relative links to files in the same repo where
                    // we've already trimmed off the docset name.
                    if (path.StartsWith("~/") || path.StartsWith("/"))
                    {
                        absolutePath = Path.Combine(DocFxDirectory.FullName, path.TrimStart('~', '/'));
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

                // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
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

        void TryAddLinkingFile(string key, string linkingFile)
        {
            if (ImageRefs.ContainsKey(key))
            {
                ImageRefs[key].Add(linkingFile);
            }
        }
    }
}
