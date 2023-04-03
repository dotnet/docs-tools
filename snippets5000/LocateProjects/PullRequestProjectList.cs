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
            Logic.Test(_rootDir, item, includeExtensions, out string? resultValue);

            if (resultValue != null) yield return resultValue;
        }
    }
}
