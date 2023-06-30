using DotNetDocs.Tools.Utility;
using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;

namespace Snippets5000;

/// <summary>
/// This class is used to process an item from the data.json file in the MSTest project, simulating a GitHub PR.
/// </summary>
internal class TestingProjectList
{
    private readonly string _testId;
    private readonly string _rootDir;
    private readonly string _testDataFile;

    internal TestingProjectList(string testId, string testDataFile, string rootDir)
    {
        _testId = testId;
        _rootDir = rootDir;
        _testDataFile = testDataFile;
    }

    internal IEnumerable<DiscoveryResult> GenerateBuildList()
    {
        var tests = PullRequestSimulations.PullRequest.LoadTests(_testDataFile);

        foreach (var item in tests.Where(t => t.Name.Equals(_testId, StringComparison.InvariantCultureIgnoreCase)).First().Items)
        {
            PullRequestProcessor.Test(_rootDir, item.Path, out DiscoveryResult? resultValue);

            if (resultValue != null) yield return resultValue.Value;
        }
    }
}
