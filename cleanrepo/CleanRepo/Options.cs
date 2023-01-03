using CommandLine;
using CommandLine.Text;

namespace CleanRepo
{
    // Define a class to receive parsed values
    class Options
    {
        [Option("start-directory", HelpText = "Top-level directory in which to perform clean up (for example, find orphaned markdown files).")]
        public string InputDirectory { get; set; }

        [Option("docset-root", Required = false, HelpText = "The full path to the root directory for the docset, e.g. 'c:\\users\\gewarren\\dotnet-docs\\docs'.")]
        public string DocsetRoot { get; set; }

        [Option("repo-root", Required = false, HelpText = "The full path to the local root directory for the repository, e.g. 'c:\\users\\gewarren\\dotnet-docs'.")]
        public string RepoRoot { get; set; }

        [Option("delete", Required = false, HelpText = "True to delete orphaned files.")]
        public bool? Delete { get; set; }

        [Option("orphaned-topics", HelpText = "Use this option to find orphaned topics.")]
        public bool FindOrphanedTopics { get; set; }

        [Option("orphaned-images", HelpText = "Find orphaned .png, .gif, .jpg, or .svg files.")]
        public bool FindOrphanedImages { get; set; }

        [Option("catalog-images", HelpText = "Map images to the markdown/YAML files that reference them.")]
        public bool CatalogImages { get; set; }

        [Option("orphaned-snippets", HelpText = "Find orphaned .cs and .vb files.")]
        public bool FindOrphanedSnippets { get; set; }

        [Option("orphaned-includes", HelpText = "Find orphaned INCLUDE files.")]
        public bool FindOrphanedIncludes { get; set; }

        //[Option("format-redirects", Required = false, HelpText = "Format the redirection JSON file by deserializing and then serializing with pretty printing.")]
        //public bool FormatRedirectsFile { get; set; }

        //[Option("trim-redirects", Required = false, HelpText = "Remove redirect entries for links that haven't been clicked in the specified number of days.")]
        //public bool TrimRedirectsFile { get; set; }

        [Option("lookback-days", Default = 180, HelpText = "The number of days to check for link-click activity.")]
        public int LinkActivityDays { get; set; }

        [Option("output-file", HelpText = "The file to write the redirect page view output to.")]
        public string OutputFilePath { get; set; }

        [Option("replace-redirects", Required = false, HelpText = "Find backlinks to redirected files and replace with new target.")]
        public bool ReplaceRedirectTargets { get; set; }

        [Option("relative-links", HelpText = "Replace site-relative links with file-relative links.")]
        public bool ReplaceWithRelativeLinks { get; set; }

        [Option("remove-hops", Required = false, HelpText = "Clean redirection JSON file by replacing targets that are themselves redirected (daisy chains).")]
        public bool RemoveRedirectHops { get; set; }
    }
}
