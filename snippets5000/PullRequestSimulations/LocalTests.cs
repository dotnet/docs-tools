using LocateProjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System.Reflection;
using System.Text;

namespace PullRequestSimulations;

[TestClass]
public partial class LocalTests
{
    PullRequest[] Requests;
    string CurrentFolder;

    public TestContext TestContext { get; set; }

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

        if (testItem.ExpectedResults != null)
        {
            Logger.LogMessage($"Expected result:");

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


            PullRequestProjectList.Test(CurrentFolder, item.Path, out DiscoveryResult? resultItem);

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