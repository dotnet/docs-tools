using DotNetDocs.Tools.Utility;
using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;
using System.Text.RegularExpressions;
using DotNet.DocsTools.GitHubObjects;

namespace Snippets5000;

internal class PullRequestProcessor
{
    private readonly string _repo;
    private readonly string _owner;
    private readonly int _prNumber;
    private readonly string _rootDir;

    /// <summary>
    /// Project extensions (including the solution) that are valid targets to discover.
    /// </summary>
    public static string[] EnvExtensionsProjects;

    /// <summary>
    /// File name extensions that trigger the scanner, such as .cs or .vb.
    /// </summary>
    public static string[] EnvExtensionsCodeTriggers;

    /// <summary>
    /// Additional files that can trigger the scanner, such as snippets.5000.json or global.json
    /// </summary>
    public static string[] EnvFileTriggers;

    static PullRequestProcessor()
    {
        // Pull global environment variables
        EnvExtensionsProjects = CommandLineUtility.GetEnvVariable(Program.ENV_EXTENSIONS_PROJECTS_NAME, "", Program.ENV_EXTENSIONS_PROJECTS_DEFAULT).Split(";");
        EnvExtensionsCodeTriggers = CommandLineUtility.GetEnvVariable(Program.ENV_EXTENSIONS_CODE_TRIGGERS_NAME, "", Program.ENV_EXTENSIONS_CODE_TRIGGERS_DEFAULT).Split(";");
        EnvFileTriggers = CommandLineUtility.GetEnvVariable(Program.ENV_FILE_TRIGGERS_NAME, "", Program.ENV_FILE_TRIGGERS_DEFAULT).Split(";");
    }

    internal PullRequestProcessor(string owner, string repo, int prNumber, string rootDir)
    {
        _owner = owner;
        _repo = repo;
        _prNumber = prNumber;
        _rootDir = rootDir;
    }

    internal async IAsyncEnumerable<DiscoveryResult> GenerateBuildList(string key)
    {
        var projectList = new List<DiscoveryResult>();
        await foreach (var project in FindAllSolutionsAndProjects(key))
            projectList.Add(project);

        foreach (var proj in projectList.Distinct())
            yield return proj;
    }

    private async IAsyncEnumerable<DiscoveryResult> FindAllSolutionsAndProjects(string key)
    {
        var client = IGitHubClient.CreateGitHubClient(key);

        var filesQuery = new EnumerationQuery<PullRequestFiles, FilesModifiedVariables>(client);

        await foreach (var item in filesQuery.PerformQuery(new FilesModifiedVariables(_owner, _repo, _prNumber)))
        {
            DiscoveryResult? resultValue = GenerateItemResult(_rootDir, item.Path);

            if (resultValue != null)
                yield return resultValue.Value;
        }
    }

    static internal DiscoveryResult? GenerateItemResult(string rootDir, string item)
    {
        // Get components of the file path
        string fullPath = Path.Combine(rootDir, item);
        string itemFileName = Path.GetFileName(fullPath);
        string itemPath = Path.GetDirectoryName(fullPath)!;

        // The file must be in the list of file name triggers or its extension must be one we care about
        if (!EnvFileTriggers.Contains(itemFileName, StringComparer.OrdinalIgnoreCase) &&
            !EnvExtensionsCodeTriggers.Contains(Path.GetExtension(itemFileName), StringComparer.OrdinalIgnoreCase) &&
            !EnvExtensionsProjects.Contains(Path.GetExtension(itemFileName), StringComparer.OrdinalIgnoreCase))
            return null;

        bool itemWasDeleted = !File.Exists(fullPath);
        bool allProjectsFoundInSln = false;
        List<string> projectsFound = new List<string>();

        // Check for the project/solution to test with was found
        FindProjectOrSolution(rootDir, itemPath, out string? project, out int countOfSln, out int countOfProjs, out int countOfCode, out int countOfSpecial, ref projectsFound);

        // If it's a solution file, check that all the projects are referenced in it:
        if (countOfSln == 1)
        {
            string solutionFolder = Path.GetFullPath(Path.GetDirectoryName(project)!);
            string solutionFileContents = File.ReadAllText(project!);
            int matchCounter = 0;

            foreach (string proj in projectsFound)
            {
                foreach (Match match in Regex.Matches(solutionFileContents!, "Project\\(\"{[A-Fa-f0-9\\-]+}\"\\) = \"[^\"]+\", \"([^\"]+)\""))
                {
                    if (proj.Equals(Path.GetFullPath(Path.Combine(solutionFolder, match.Groups[1].Value)), StringComparison.OrdinalIgnoreCase))
                        matchCounter++;
                }
            }

            allProjectsFoundInSln = matchCounter == projectsFound.Count;
        }

        // Fix file path back to github style for results
        if (project != null)
            project = TransformPathToUnix(rootDir, project);

        // Process the condition checks to see if this item is valid or not
        return (project, countOfSln, countOfProjs, countOfCode, countOfSpecial, itemWasDeleted, allProjectsFoundInSln) switch
        {
            //                            Proj File, Sln#, Proj#, Code#, Spec#,   Del, SlnHasPrj
            /* File del, no code/proj  */ (null,        0,     0,     0,     0,  true, _)     => null,
            /* Too many solutions      */ (not null,  > 1,     _,     _,     _,     _, _)     => new DiscoveryResult(DiscoveryResult.RETURN_TOOMANY, item, project),
            /* Too many projs          */ (not null,    0,   > 1,     _,     _,     _, _)     => new DiscoveryResult(DiscoveryResult.RETURN_TOOMANY, item, project),
            /* SLN found               */ (not null,    1,   > 0,     _,     _,     _, true)  => new DiscoveryResult(DiscoveryResult.RETURN_GOOD, item, project),
            /* SLN found, missing proj */ (not null,    1,   > 0,     _,     _,     _, false) => new DiscoveryResult(DiscoveryResult.RETURN_SLN_PROJ_MISSING, item, project),
            /* SLN found no projs      */ (not null,    1,     0,     _,     _, false, _)     => new DiscoveryResult(DiscoveryResult.RETURN_SLN_NOPROJ, item, project),
            /* SLN found no projs, del */ (not null,    1,     0,     _,     _,  true, _)     => new DiscoveryResult(DiscoveryResult.RETURN_GOOD, item, project),
            /* Project found           */ (not null,    0,     1,     _,     _,     _, _)     => new DiscoveryResult(DiscoveryResult.RETURN_GOOD, item, project),
            /* Code no proj            */ (null,        0,     0,   > 0,     _,     _, _)     => new DiscoveryResult(DiscoveryResult.RETURN_NOPROJ, item, ""),
            /* Code no proj            */ (null,        0,     0,     _,   > 0,     _, _)     => new DiscoveryResult(DiscoveryResult.RETURN_NOPROJ, item, ""),
            /* catch all               */ _                                                  => new DiscoveryResult(DiscoveryResult.RETURN_NOPROJ, item, "CONDITION NOT FOUND"),
        };
    }

    static void FindProjectOrSolution(string rootDir, string itemDirectory, out string? project, out int countOfSln, out int countOfProjs, out int countOfCode, out int countOfSpecial, ref List<string> projectsFound)
    {
        project = null;
        countOfSln = 0;
        countOfProjs = 0;
        countOfCode = 0;
        countOfSpecial = 0;

        // First, process the current folder and all child folders for content
        // If a file is deleted, and no other content is there, the directory won't exist
        if (Directory.Exists(itemDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(itemDirectory, $"*.*", SearchOption.AllDirectories))
                ScanFile(file, ref project, ref countOfSln, ref countOfProjs, ref countOfCode, ref countOfSpecial, ref projectsFound);
        }

        // Navigate back one folder and start scanning all of the parents
        itemDirectory = Path.GetFullPath(Path.Combine(itemDirectory, ".."));

        // Traverse back until the root folder.
        while (!itemDirectory.Equals(rootDir, StringComparison.OrdinalIgnoreCase))
        {
            if (Directory.Exists(itemDirectory))
            {
                foreach (var file in Directory.EnumerateFiles(itemDirectory, $"*.*", SearchOption.TopDirectoryOnly))
                    ScanFile(file, ref project, ref countOfSln, ref countOfProjs, ref countOfCode, ref countOfSpecial, ref projectsFound);
            }

            // Navigate back a folder
            itemDirectory = Path.GetFullPath(Path.Combine(itemDirectory, ".."));
        }

        if (project != null)
        {
            // If project is solution, we need to discover any other projects under it, since this is file
            // overrides a project file.
            if (project.EndsWith(".sln"))
            {
                itemDirectory = Path.GetDirectoryName(project)!;
                string? discardString = "";
                int discardProjs = 0;
                int discardCode = 0;
                int discardSpecial = 0;

                foreach (var file in Directory.EnumerateFiles(itemDirectory, $"*.*", SearchOption.AllDirectories))
                    ScanFile(file, ref discardString, ref countOfSln, ref discardProjs, ref discardCode, ref discardSpecial, ref projectsFound);

                // Trim 1 solution count out, since we counted it twice, once the first time it was discovered
                // and again in the scan that just happened
                countOfSln--;

                // Get rid of the duplicates found via the previous scan
                // Remove any .proj file, these are special and don't count
                projectsFound = projectsFound.Distinct().Where(s => !s.EndsWith(".proj")).ToList();
            }
        }

        return;

        // Processes a single file, keeping a running total of discovered items.
        // 1. The file is a solution
        // 2. The file is a project
        // 3. The file is a code file
        // 4. The file is a special file
        static void ScanFile(string file, ref string? project, ref int countOfSln, ref int countOfProjs, ref int countOfCode, ref int countOfSpecial, ref List<string> projectsFound)
        {
            string ext = Path.GetExtension(file);

            // If solution file
            if (ext.Equals(".sln", StringComparison.OrdinalIgnoreCase))
            {
                countOfSln++;

                // If this is the first solution file, capture it
                if (countOfSln == 1)
                    project = file;
            }

            // If a project file
            else if (EnvExtensionsProjects.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                countOfProjs++;

                // If this is the first project and a solution hasn't been found, capture it
                if (countOfSln == 0)
                    project = file;

                projectsFound.Add(file);
            }

            // If a code file
            else if (EnvExtensionsCodeTriggers.Contains(ext, StringComparer.OrdinalIgnoreCase))
                countOfCode++;

            // If a special trigger file
            else if (EnvFileTriggers.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase))
                countOfSpecial++;
        }
    }

    static string TransformPathToUnix(string repoRoot, string filePath) =>
        filePath.Substring(repoRoot.Length + 1).Replace("\\", "/");
}
