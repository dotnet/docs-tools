using DotNetDocs.Tools.Utility;
using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;

namespace LocateProjects;

internal class PullRequestProjectList
{
    private readonly string _repo;
    private readonly string _owner;
    private readonly int _prNumber;
    private readonly string _rootDir;

    const int RETURN_GOOD = 0;
    const int RETURN_NOPROJ = 1;
    const int RETURN_TOOMANY = 2;
    const int RETURN_SLN = 3;

    internal PullRequestProjectList(string owner, string repo, int prNumber, string rootDir)
    {
        _owner = owner;
        _repo = repo;
        _prNumber = prNumber;
        _rootDir = rootDir;
    }

    internal async IAsyncEnumerable<string> GenerateBuildList(string key)
    {
        var projectList = new List<string>();
        await foreach (var project in FindAllSolutionsAndProjects(key))
            projectList.Add(project);

        foreach (var proj in projectList.Distinct())
            yield return proj;
    }

    private async IAsyncEnumerable<string> FindAllSolutionsAndProjects(string key)
    {
        // Only search for projects to match a file of this extension, generally a code file
        var includeExtensions = CommandLineUtility.GetEnvVariable("LocateExts", "LocateExts variable does not exist, skipping", "!").Split(';');

        // If there is only one element in the array and it's ! 
        if (includeExtensions.Length == 1 && includeExtensions[0] == "!")
            includeExtensions = System.Array.Empty<string>();

        var client = IGitHubClient.CreateGitHubClient(key);
        var files = new FilesInPullRequest(client, _owner, _repo, _prNumber);


        await foreach (var item in files.PerformQuery())
        {
            Directory.SetCurrentDirectory(_rootDir);
            // Extensions variable does not include two special files we do care about.
            // Check them here:
            var file = Path.GetFileName(item);
            bool specialJsonFile = (file == "snippets.5000.json") || (file == "global.json");
            if (!specialJsonFile &&
                ((includeExtensions.Length != 0 && !includeExtensions.Contains(Path.GetExtension(item), System.StringComparer.OrdinalIgnoreCase))))
                continue;

            var folders = item.Split("/");

            // Deleted files require special checking
            // - When a code file is deleted and 1 project/sln file is found in the folder or parent folder: PROCESS
            // - When a code file is deleted and no project/sln file is found in the folder or parent folder: SKIP
            // - If a project/sln is deleted and code files remain at or below the current folder and no project/sln file is found above or below: ERROR
            // - If a project/sln is deleted and no code files remain at or below and no project/sln file is found above or below: SKIP
            bool deletedFile = !File.Exists(item);
            int returnCode = RETURN_NOPROJ;
            string returnFile = item;
            string returnProj = "";
            bool checkingSln = Path.GetExtension(item) == ".sln";
            bool checkingProj = Path.GetExtension(item).Contains("proj");
            // Well, this is ugly:
            bool moreCodeExists = Directory.Exists(Path.GetDirectoryName(item)) && 
                Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.vb", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.fs", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.cpp", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.h", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.xaml", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.razor", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.cshtml", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(item)!, "*.vbhtml", SearchOption.AllDirectories))
                .Any();

            var subPath = ".";
            // The important part of this logic is that for any source file that
            // is part of this PR, it must be part of exactly one project. That one
            // project may be part of a multi-project solution.
            foreach (var folder in folders[0..^1]) // Don't include the file name component.
            {
                // We already have too many, no need to keep checking
                if (returnCode == RETURN_TOOMANY)
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
                    if (returnCode == RETURN_TOOMANY)
                        return;

                    foreach (var file in fileCollection)
                    {
                        // Never found a proj/sln until now
                        if (returnCode == RETURN_NOPROJ)
                        {
                            returnCode = isFindSLN ? RETURN_SLN : RETURN_GOOD;
                            returnProj = $"{subPath}/{Path.GetFileName(file)}";
                        }
                        // Found a solution earlier, we can find 1 project, no more
                        else if (!isFindSLN && returnCode == RETURN_SLN)
                        {
                            returnCode = RETURN_GOOD;
                        }
                        // We already found something, but we found another
                        else if (returnCode == RETURN_GOOD)
                        {
                            returnCode = RETURN_TOOMANY;
                            break;
                        }
                    }
                }

                LoopFiles(Directory.EnumerateFiles(".", "*.sln", SearchOption.TopDirectoryOnly), true);
                LoopFiles(Directory.EnumerateFiles(".", "*.csproj", SearchOption.TopDirectoryOnly), false);
                LoopFiles(Directory.EnumerateFiles(".", "*.vbproj", SearchOption.TopDirectoryOnly), false);
                LoopFiles(Directory.EnumerateFiles(".", "*.fsproj", SearchOption.TopDirectoryOnly), false);
                LoopFiles(Directory.EnumerateFiles(".", "*.vcxproj", SearchOption.TopDirectoryOnly), false);
            }

            // If we're actually checking a sln (it was modified/added) we don't want to error
            if ((checkingSln || specialJsonFile) && returnCode == RETURN_SLN)
                returnCode = RETURN_GOOD;

            if (deletedFile && (returnCode == RETURN_NOPROJ) && !moreCodeExists)
            {
                // all code gone. It's OK
                continue;
            }

            // This works for code files that are deleted.
            if ((deletedFile)&& !(checkingProj || checkingSln) && (returnCode == RETURN_NOPROJ))
                continue;
            
            yield return $"{returnCode}|{returnFile}|{returnProj}";

            Directory.SetCurrentDirectory(_rootDir);
        }
    }
}
