using CommandLine;

namespace CleanRepo;

// Define a class to receive parsed values.
class Options
{
    [Option("docfx-directory", HelpText = "Directory that contains the docfx.json file of interest.")]
    public string DocFxDirectory { get; set; }

    [Option("snippets-directory", HelpText = "Top-level directory in which to perform snippet cleanup.")]
    public string SnippetsDirectory { get; set; }

    [Option("media-directory", HelpText = "Top-level directory in which to perform image/media cleanup.")]
    public string MediaDirectory { get; set; }

    [Option("includes-directory", HelpText = "Top-level directory in which to perform include-file cleanup.")]
    public string IncludesDirectory { get; set; }

    [Option("articles-directory", HelpText = "Top-level directory in which to perform article cleanup (i.e. find orphans or fix links).")]
    public string ArticlesDirectory { get; set; }

    [Option("url-base-path", Required = false, HelpText = "The URL base path for the docset, e.g. '/windows/uwp' or '/dotnet'.")]
    public string UrlBasePath { get; set; }

    [Option("delete", Required = false, HelpText = "True to delete orphaned files.")]
    public bool? Delete { get; set; }

    [Option("orphaned-articles", HelpText = "Use this option to find orphaned articles.")]
    public bool FindOrphanedArticles { get; set; }

    [Option("orphaned-images", HelpText = "Find orphaned .png, .gif, .jpg, or .svg files.")]
    public bool FindOrphanedImages { get; set; }

    [Option("catalog-images", Default = false, HelpText = "Map images to the markdown/YAML files that reference them.")]
    public bool CatalogImages { get; set; }

    [Option("orphaned-snippets", HelpText = "Find orphaned .cs, .vb, .fs, and .cpp files.")]
    public bool FindOrphanedSnippets { get; set; }

    [Option("orphaned-includes", HelpText = "Find orphaned INCLUDE files.")]
    public bool FindOrphanedIncludes { get; set; }

    [Option("replace-redirects", HelpText = "Find backlinks to redirected files and replace with new target.")]
    public bool ReplaceRedirectTargets { get; set; }

    [Option("relative-links", HelpText = "Replace site-relative links with file-relative links.")]
    public bool ReplaceWithRelativeLinks { get; set; }

    [Option("remove-hops", HelpText = "Clean redirection JSON file by replacing targets that are themselves redirected (daisy chains).")]
    public bool RemoveRedirectHops { get; set; }

    //[Option("format-redirects", Required = false, HelpText = "Format the redirection JSON file by deserializing and then serializing with pretty printing.")]
    //public bool FormatRedirectsFile { get; set; }

    //[Option("trim-redirects", Required = false, HelpText = "Remove redirect entries for links that haven't been clicked in the specified number of days.")]
    //public bool TrimRedirectsFile { get; set; }

    //[Option("lookback-days", Default = 180, HelpText = "The number of days to check for link-click activity.")]
    //public int LinkActivityDays { get; set; }

    //[Option("output-file", HelpText = "The file to write the redirect page view output to.")]
    //public string OutputFilePath { get; set; }
}
