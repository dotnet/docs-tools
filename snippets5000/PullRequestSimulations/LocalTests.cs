using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Snippets5000;

namespace PullRequestSimulations;

[TestClass]
public partial class LocalTests
{
    //Disable these warnings. These are set via the Init method when the test framework initializes itself.
    #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    PullRequest[] Requests;
    string CurrentFolder;
    #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void Init()
    {
        CurrentFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        Directory.SetCurrentDirectory(CurrentFolder);
        Requests = PullRequest.LoadTests("data.json");
        Logger.LogMessage($"Current Folder is: {CurrentFolder}");

    }

    public void RunTest(string name)
    {
        Logger.LogMessage("Running " + name);
        PullRequest testItem = Requests.Where(p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).First();

        if (testItem.ExpectedResults != null && testItem.ExpectedResults.Length != 0)
        {
            Logger.LogMessage($"Expected result(s):");

            foreach (var result in testItem.ExpectedResults)
                Logger.LogMessage($"  {result.ResultCode}|{result.DiscoveredProject}");
        }
        else
            Logger.LogMessage($"Expected result items? No");

        Logger.LogMessage($"Empty results expected: {testItem.CountOfEmptyResults}");

        int emptyErrors = 0;

        foreach (var item in testItem.Items)
        {
            Logger.LogMessage($"\nProcessing item: {item.ItemType}\n  {item.Path}");

            if (item.ItemType != ChangeItemType.Delete)
                Assert.IsTrue(File.Exists(Path.GetFullPath(Path.Combine(CurrentFolder, item.Path))));


            PullRequestProcessor.Test(CurrentFolder, item.Path, out DiscoveryResult? resultItem);

            if (!resultItem.HasValue)
                emptyErrors++;

            else
            {
                Logger.LogMessage($"  {resultItem.Value.Code}|{resultItem.Value.DiscoveredFile}");
                Assert.IsNotNull(testItem.ExpectedResults);
                Assert.IsTrue(testItem.ExpectedResults.Where(r => r.ResultCode == resultItem.Value.Code && resultItem.Value.DiscoveredFile.EndsWith(r.DiscoveredProject, StringComparison.InvariantCultureIgnoreCase)).Count() == 1);
            }
        }

        Assert.AreEqual(emptyErrors, testItem.CountOfEmptyResults, $"  Expected {testItem.CountOfEmptyResults} empty results, but got {emptyErrors}");
    }
}