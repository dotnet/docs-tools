using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Octokit;
using StarodubOleg.GPPG.Runtime;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace RepoMan;

internal sealed class State
{
    private JObject _cachedStateBody;

    [MemberNotNullWhen(true, "PullRequest")]
    public bool IsPullRequest { get; set; }
    public GitHubClient Client;
    public Issue Issue;
    public PullRequest? PullRequest;
    public string IssuePrBody;
    public PullRequestFile[] PullRequestFiles = Array.Empty<PullRequestFile>();
    public PullRequestReview[] PullRequestReviews = Array.Empty<PullRequestReview>();
    public IssueComment? Comment;
    public long RepositoryId;
    public string RepositoryName;
    public string RepositoryOwner;
    public RequestType RequestType;
    public string EventAction;
    public ILogger Logger;
    public JObject EventPayload;
    public IReadOnlyList<Milestone> Milestones;
    public IReadOnlyList<Project> Projects;
    public Dictionary<int, ProjectColumn[]> ProjectColumns = new Dictionary<int, ProjectColumn[]>();
    public ProjectsClient ProjectsClient;
    public Dictionary<string, string> DocIssueMetadata = new Dictionary<string, string>();
    public bool IsV2Metadata = false;
    public SettingsConfig Settings;
    public OperationPool Operations = new OperationPool();
    public YamlMappingNode RepoRulesYaml;
    public Dictionary<string, string> Variables = new Dictionary<string, string>();

    public State() { }

    public JObject RequestBody()
    {
        if (_cachedStateBody != null) return _cachedStateBody;

        //var test = Client.Connection.Get<PullRequestReview[]>(ApiUrls.PullRequestReviews(RepositoryId, PullRequest.Number), TimeSpan.FromSeconds(5)).Result.HttpResponse.Body.ToString();

        _cachedStateBody = new JObject(
            new JProperty("Issue", JObject.Parse(Client.Connection.Get<Issue>(ApiUrls.Issue(RepositoryId, Issue.Number), TimeSpan.FromSeconds(5)).Result.HttpResponse.Body.ToString())),
            new JProperty("PullRequest", PullRequest == null ? null :
                                         JObject.Parse(Client.Connection.Get<PullRequest>(ApiUrls.PullRequest(RepositoryId, PullRequest.Number), TimeSpan.FromSeconds(5)).Result.HttpResponse.Body.ToString())),
            new JProperty("PullRequestFiles", PullRequestFiles == null ? null : JArray.FromObject(PullRequestFiles)),
            new JProperty("PullRequestReviews", PullRequestReviews == null ? null : JArray.FromObject(PullRequestReviews)),
                                                //JArray.Parse(Client.Connection.Get<PullRequestReview[]>(ApiUrls.PullRequestReviews(RepositoryId, PullRequest.Number), TimeSpan.FromSeconds(5)).Result.HttpResponse.Body.ToString())),
            new JProperty("Comment", Comment == null ? null : JObject.Parse(Client.Connection.Get<IssueComment>(ApiUrls.IssueComment(RepositoryId, Comment.Id), TimeSpan.FromSeconds(5)).Result.HttpResponse.Body.ToString())),
            new JProperty("EventPayload", EventPayload),
            new JProperty("Variables", JArray.FromObject(Variables.Select(kv => new { kv.Key, kv.Value }).ToArray()))
            );

        return _cachedStateBody;
    }

    /// <summary>
    /// Writes the state json object to disk, for testing.
    /// </summary>
    public void SaveStateJson()
    {
        File.WriteAllText("state.json", RequestBody().ToString());
    }

    /// <summary>
    /// Loads the metadata from an issue comment.
    /// </summary>
    /// <param name="comment"></param>
    public void LoadCommentMetadata(string comment)
    {
        // We only read the issue metadata once.
        if (DocIssueMetadata.Count != 0) return;

        comment = comment.Replace("\r", "");

        string[] content = comment.Split('\n');

        // Log debug information about the headers loaded
        Logger.LogDebugger("Header check metadata settings: ");

        int counter = 0;
        foreach (string[] item in Settings.DocMetadata.Headers)
        {
            counter++;
            Logger.LogDebugger($"- Set {counter}");

            foreach (string setItem in item)
                Logger.LogDebugger($"  - {setItem}");
        }

        Logger.LogInformation("Checking for comment metadata");

        // Use the headers defined in the yaml config. You can define different sets of headers
        foreach (string[] item in Settings.DocMetadata.Headers)
        {
            for (int i = 0; i < content.Length; i++)
            {
                // If the first item in the set of headers matches, start
                if (content[i].StartsWith(item[0]))
                {
                    Logger.LogDebugger($"Found header match: '{item[0]}' in '{content[i]}'");

                    // No other items in set, so we matched.
                    if (item.Length == 1)
                        ScanLines(i + 1);

                    else
                    {
                        bool passed = false;

                        // Process the rest of the items in the set
                        for (int headerIndex = 1; headerIndex < item.Length; headerIndex++)
                        {
                            // a "" skips this line, otherwise check for a match
                            if (item[headerIndex] == string.Empty || content[i + headerIndex].StartsWith(item[headerIndex]))
                            {
                                Logger.LogDebugger($"Found header match: '{item[headerIndex]}' in '{content[i + headerIndex]}'");
                                passed = true;
                            }
                            else
                            {
                                passed = false;
                                break;
                            }
                        }
                        
                        // everything matched, read the lines for metadata fields
                        if (passed)
                            ScanLines(i + item.Length);
                        else
                            Logger.LogDebugger($"Additional headers not matched, skipping this line");
                    }
                }
            }
        }

        // After comment has been scanned, if the URL was found in the metaata, load the
        // article page and scrape the metadata from the HTML
        if (DocIssueMetadata.Count != 0)
        {
            // This same code is copied below in the other logic
            Logger.LogInformation("Look for article URL");
            if (DocIssueMetadata.ContainsKey("content source"))
            {
                Dictionary<string, string> newMetadata = Utilities.ScrapeArticleMetadata(new Uri(DocIssueMetadata["content source"]), this).Result;

                DocIssueMetadata = new(DocIssueMetadata.Union(newMetadata));
            }
        }

        // If no comment metadata was found, need to see if the new template is being used
        // and if so, load the article page and scrape the metadata from the HTML
        else
        {
            Logger.LogInformation("Look for article URL");

            foreach (var regexSearch in Settings.DocMetadata.ContentUrlRegex)
            {
               Logger.LogInformation($"Processing regex: {regexSearch}");
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(comment, regexSearch, System.Text.RegularExpressions.RegexOptions.Multiline);

                if (match.Success)
                {
                    IsV2Metadata = true;

                    Dictionary<string, string> newMetadata = Utilities.ScrapeArticleMetadata(new Uri(match.Groups[1].Value.ToLower()), this).Result;

                    DocIssueMetadata = new(DocIssueMetadata.Union(newMetadata));

                    break;
                }
            }
        }

        // Load each metadata item into a variable
        foreach (string key in DocIssueMetadata.Keys)
            Variables[key] = DocIssueMetadata[key];

        // Reads each line from the index of the content to the end, checking for a metadata field pattern
        void ScanLines(int index)
        {
            Logger.LogInformation("Found comment metadata");

            for (int i = index; i < content.Length; i++)
            {
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(content[i], Settings.DocMetadata.ParserRegex);

                if (match.Success)
                {
                    string key = match.Groups[1].Value.ToLower();
                    DocIssueMetadata[key] = Utilities.StripMarkdown(match.Groups[2].Value).Trim();
                    Logger.LogDebugger($"Added metadata: Key: '{key}' Value: '{DocIssueMetadata[key]}'");
                }
            }
        }
    }

    public string ExpandVariables(string input)
    {
        if (input.StartsWith("jmes:"))
        {
            input = Utilities.GetJMESResult(input.Substring("jmes:".Length), this).Trim('"');
            if (input.Equals("null", StringComparison.InvariantCultureIgnoreCase))
                input = string.Empty;
        }

        foreach (string key in Variables.Keys)
        {
            string magicKey = $"${key.ToLower().Trim()}$";

            if (input.Contains(magicKey))
                input = input.Replace(magicKey, Variables[key]);
        }

        return input;
    }

    public async Task RunPooledActions()
    {
        // Remove labels in the remove category from the add cateogry
        // We still run via remove, they may be already on the issue
        foreach (string item in Operations.LabelsRemove)
        {
            if (Operations.LabelsAdd.Contains(item))
                Operations.LabelsAdd.Remove(item);
        }


        if (Operations.LabelsAdd.Count != 0)
        {
            string[] uniqueLabels = Operations.LabelsAdd.Distinct().ToArray();

            Logger.LogInformation($"Adding {uniqueLabels.Length} labels");
            await GithubCommand.AddLabels(uniqueLabels, this);
        }

        if (Operations.LabelsRemove.Count != 0)
        {
            string[] uniqueLabels = Operations.LabelsRemove.Distinct().ToArray();

            Logger.LogInformation($"Removing {uniqueLabels.Length} labels");
            await GithubCommand.RemoveLabels(uniqueLabels, this.Issue.Labels, this);
        }

        if (Operations.Assignees.Count != 0)
        {
            string[] uniqueNames = Operations.Assignees.Distinct().ToArray();

            Logger.LogInformation($"Adding {uniqueNames.Length} assignees");
            await GithubCommand.AddAssignees(uniqueNames, this);
        }

        if (Operations.Reviewers.Count != 0)
        {
            string[] uniqueNames = Operations.Reviewers.Distinct().ToArray();

            Logger.LogInformation($"Adding {uniqueNames.Length} reviewers");
            await GithubCommand.AddReviewers(uniqueNames, this);
        }
    }

    public class OperationPool
    {
        public List<string> LabelsAdd { get; } = new List<string>();
        public List<string> LabelsRemove { get; } = new List<string>();
        public List<string> Assignees { get; } = new List<string>();
        public List<string> Reviewers { get; } = new List<string>();
    }


    public void LoadSettings(YamlNode settingsNode)
    {
        YamlDotNet.Serialization.Deserializer deserializer = new YamlDotNet.Serialization.Deserializer();
        YamlStream resultStream = new YamlStream(new YamlDocument(settingsNode));
        StringBuilder builder = new StringBuilder();
        using StringWriter writer = new System.IO.StringWriter(builder);
        resultStream.Save(writer);
        Settings = deserializer.Deserialize<SettingsConfig>(builder.ToString());
    }



    public class SettingsConfig
    {
        public ConfigDocMetadata? DocMetadata;

        public class ConfigDocMetadata
        {
            public List<string[]>? Headers;
            public List<string>? ContentUrlRegex;
            public string? ParserRegex { get; set; }
        }
    }
}
