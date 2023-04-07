using DotNetDocs.Tools.Utility;
using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;
using System.Text.RegularExpressions;

namespace LocateProjects;

/*
 *
 * On each file edit/add by the PR:
 * - Determine the folder of the file
 * - Remove duplicates
 * - Associate one file in the PR with the folder (so we can report directly in github the error location)
 * 
 * On each file deleted by the PR:
 * - Check if there is already a found project/solution for that hierarchy, if so, PASS
 * - If not found, check for other code fragments at that folder or below, if so, FAIL
 * 
 * On each folder found
 * - Hunt for project file above
 * - If none found, 
 * 
*/

internal class PullRequestProjectList
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

    static PullRequestProjectList()
    {
        // Pull global environment variables
        EnvExtensionsProjects = CommandLineUtility.GetEnvVariable(Program.ENV_EXTENSIONS_PROJECTS_NAME, "", Program.ENV_EXTENSIONS_PROJECTS_DEFAULT).Split(";");
        EnvExtensionsCodeTriggers = CommandLineUtility.GetEnvVariable(Program.ENV_EXTENSIONS_CODE_TRIGGERS_NAME, "", Program.ENV_EXTENSIONS_CODE_TRIGGERS_DEFAULT).Split(";");
        EnvFileTriggers = CommandLineUtility.GetEnvVariable(Program.ENV_FILE_TRIGGERS_NAME, "", Program.ENV_FILE_TRIGGERS_DEFAULT).Split(";");
    }

    internal PullRequestProjectList(string owner, string repo, int prNumber, string rootDir)
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
        var files = new FilesInPullRequest(client, _owner, _repo, _prNumber);


        await foreach (var item in files.PerformQuery())
        {
            Test(_rootDir, item, out DiscoveryResult? resultValue);

            if (resultValue != null) yield return resultValue.Value;
        }
    }

    internal static void Test(string _rootDir, string item, out DiscoveryResult? resultValue)
    {
        resultValue = null;
        Directory.SetCurrentDirectory(_rootDir);
        bool specialFileTrigger = EnvFileTriggers.Contains(Path.GetFileName(item), StringComparer.OrdinalIgnoreCase);

        // The file must be in the list of file name triggers or its extension must be one we care about
        if (!specialFileTrigger &&
            !EnvExtensionsCodeTriggers.Contains(Path.GetExtension(item), StringComparer.OrdinalIgnoreCase))
            return;

        var folders = item.Split("/");

        // Deleted files require special checking
        // - When a code file is deleted and 1 project/sln file is found in the folder or parent folder: PROCESS
        // - When a code file is deleted and no project/sln file is found in the folder or parent folder: SKIP
        // - If a project/sln is deleted and code files remain at or below the current folder and no project/sln file is found above or below: ERROR
        // - If a project/sln is deleted and no code files remain at or below and no project/sln file is found above or below: SKIP
        bool deletedFile = !File.Exists(item);
        int returnCode = DiscoveryResult.RETURN_NOPROJ;
        string returnFile = item;
        string returnProj = "";
        bool checkingSln = Path.GetExtension(item) == ".sln";
        bool checkingProj = EnvExtensionsProjects.Contains(Path.GetExtension(item), StringComparer.OrdinalIgnoreCase);

        // If the directory of the target file exists, it's not a full delete and we should hunt for any code fragment files
        // in that directory or a parent.
        bool moreCodeExists =
            Directory.Exists(Path.GetDirectoryName(item)) &&
            EnvExtensionsCodeTriggers.Any(ext => Directory.EnumerateFiles(Path.GetDirectoryName(item)!, $"*{ext}", SearchOption.AllDirectories).Any());

        string? solutionFileContents = null;

        var subPath = ".";
        // The important part of this logic is that for any source file that
        // is part of this PR, it must be part of exactly one project. That one
        // project may be part of a multi-project solution.
        foreach (var folder in folders[0..^1]) // Don't include the file name component.
        {
            // We already have too many, no need to keep checking
            if (returnCode == DiscoveryResult.RETURN_TOOMANY)
                break;

            subPath = $"{subPath}/{folder}";
            if (!Directory.Exists(folder))
            {
                break;
            }

            Directory.SetCurrentDirectory(folder);

            // Local function to set return values
            void LoopFiles(IEnumerable<string> fileCollection, bool isFindSLN)
            {
                // We already have too many, no need to keep checking
                if (returnCode == DiscoveryResult.RETURN_TOOMANY)
                    return;

                foreach (var file in fileCollection)
                {
                    // Never found a proj/sln until now
                    if (returnCode == DiscoveryResult.RETURN_NOPROJ)
                    {
                        returnCode = isFindSLN ? DiscoveryResult.RETURN_TEMP_SLNFOUND : DiscoveryResult.RETURN_GOOD;
                        returnProj = $"{subPath}/{Path.GetFileName(file)}";

                        if (returnCode == DiscoveryResult.RETURN_TEMP_SLNFOUND)
                            solutionFileContents = File.ReadAllText(Path.Combine(_rootDir, returnProj));
                    }

                    // Found a solution earlier, we can find 1 project, no more
                    else if (!isFindSLN && returnCode == DiscoveryResult.RETURN_TEMP_SLNFOUND)
                    {
                        //returnCode = DiscoveryResult.RETURN_GOOD;
                        // Check if file is in solution; default to solution found but project not in it
                        returnCode = DiscoveryResult.RETURN_SLN_NOPROJ;
                        string solutionFolder = Path.GetFullPath(Path.GetDirectoryName(Path.Combine(_rootDir, returnProj))!);
                        string projectPath = Path.GetFullPath(Path.Combine(Path.Combine(_rootDir, subPath), file));

                        foreach (Match match in Regex.Matches(solutionFileContents!, "Project\\(\"{[A-Fa-f0-9\\-]+}\"\\) = \"[^\"]+\", \"([^\"]+)\""))
                        {
                            if (projectPath.Equals(Path.GetFullPath(Path.Combine(solutionFolder, match.Groups[1].Value)), StringComparison.OrdinalIgnoreCase))
                            {
                                returnCode = DiscoveryResult.RETURN_TEMP_SLNFOUND;
                                break;
                            }
                        }
                    }

                    // We already found something, but we found another
                    else if (returnCode == DiscoveryResult.RETURN_GOOD)
                    {
                        returnCode = DiscoveryResult.RETURN_TOOMANY;
                        break;
                    }
                }
            }

            foreach (string proj in EnvExtensionsProjects)
                LoopFiles(Directory.EnumerateFiles(".", $"*{proj}", SearchOption.TopDirectoryOnly), proj.Equals(".sln", StringComparison.OrdinalIgnoreCase));
        }

        // If a solution was found, we're good.
        if (returnCode == DiscoveryResult.RETURN_TEMP_SLNFOUND)
            returnCode = DiscoveryResult.RETURN_GOOD;

        if (deletedFile && (returnCode == DiscoveryResult.RETURN_NOPROJ) && !moreCodeExists)
        {
            // all code gone. It's OK
            return;
        }

        // This works for code files that are deleted.
        if ((deletedFile) && !(checkingProj || checkingSln) && (returnCode == DiscoveryResult.RETURN_NOPROJ))
            return;

        resultValue = new DiscoveryResult(returnCode, returnFile, returnProj);
    }
}
