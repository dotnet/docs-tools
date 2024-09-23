using System.Diagnostics.CodeAnalysis;
using System.Text;
using DotNetDocs.RepoMan.GitHubCommands;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Octokit;
using Octokit.GraphQL;
using YamlDotNet.RepresentationModel;
using Octokit.GraphQL.Core;

namespace DotNetDocs.RepoMan;

internal partial class InstanceData: IRepositoryProvider
{
    private JObject _cachedStateBody;
    private bool? _isDryRun;

    public bool IsDryRun
    {
        get
        {
            if (!_isDryRun.HasValue)
            {
                _isDryRun ??= false.OverrideWithEnvironmentVariable("IsDryRun");

                if (_isDryRun.Value)
                    Logger.LogWarning("Is dry run; GitHub disabled");
            }

            return _isDryRun.Value;
        }
    }

    public EventType EventType { get; }
    public GitHubClient GitHubRESTClient { get; }
    public Octokit.GraphQL.Connection GitHubGraphQLClient { get; set; }
    public ILogger<FunctionRepoMain> Logger { get; }

    [MemberNotNullWhen(true, nameof(Issue))]
    public bool HasIssueData => Issue != null;
    [MemberNotNullWhen(true, nameof(PullRequest))]
    public bool HasPullRequestData => PullRequest != null;
    [MemberNotNullWhen(true, nameof(Comment))]
    public bool HasCommentData => Comment != null;

    public bool HasProjectItemData => false; //TODO when we support editing project items

    // HAD UNLOADED ISSUE DATA?


    public long RepositoryId { get; set; }
    public string RepositoryName { get; set; }
    public string RepositoryOwner { get; set; }

    public string IssuePrBody { get; set; }

    public Issue? Issue { get; set; }
    public PullRequest? PullRequest { get; set; }
    public IssueComment? Comment { get; set; }

    public PullRequestFile[] PullRequestFiles { get; set; } = [];
    public PullRequestReview[] PullRequestReviews { get; set; } = [];

    public JObject EventPayload { get; }

    public YamlMappingNode? RepoRulesYaml { get; set; }

    public string EventAction { get; set; } = string.Empty;

    public IReadOnlyList<Milestone>? Milestones { get; set; }

    public Dictionary<string, string> DocIssueMetadata { get; set; } = [];

    public Dictionary<string, string> Variables { get; } = [];

    public OperationPool Operations { get; } = new();

    public bool HasFailure { get; set; }

    public string FailureMessage { get; set; }

    public InstanceData(ILogger<FunctionRepoMain> logger, GitHubClient client, EventType eventType, JObject payloadData)
    {
        Logger = logger;
        GitHubRESTClient = client;
        EventType = eventType;
        EventPayload = payloadData;
    }

    public async Task SetupFromIssueEventPayload(IssueEventPayload payload)
    {
        Logger.LogInformation("Creating data object from 'issue' event payload");

        RepositoryId = payload.Repository.Id;
        RepositoryName = payload.Repository.Name;
        RepositoryOwner = payload.Repository.Owner.Login;
        Issue = payload.Issue;
        IssuePrBody = payload.Issue.Body ?? string.Empty;
        EventAction = payload.Action;
        Issue.ClosedBy.

        Logger.LogInformation("RepoID: {repoid} RepoOrg/Name: '{repoowner}/{reponame}' Issue #: {issueid}", RepositoryId, RepositoryOwner, RepositoryName, Issue.Number);

        //TODO: Change this for dynamic loading. When the actions and conditions look for PR info
        //      then they should load it from this data object.
        if (payload.Issue.PullRequest != null)
            PullRequest = await GitHubRESTClient.PullRequest.Get(RepositoryId, payload.Issue.Number);
    }


    public async Task SetupFromPullRequestEventPayload(PullRequestEventPayload payload)
    {
        Logger.LogInformation("Creating data object from 'PR' event payload");

        RepositoryId = payload.Repository.Id;
        RepositoryName = payload.Repository.Name;
        RepositoryOwner = payload.Repository.Owner.Login;
        PullRequest = payload.PullRequest;
        IssuePrBody = payload.PullRequest.Body ?? string.Empty;
        EventAction = payload.Action;

        Logger.LogInformation("RepoID: {repoid} RepoOrg/Name: '{repoowner}/{reponame}' PullRequest #: {issueid}", RepositoryId, RepositoryOwner, RepositoryName, PullRequest.Number);

        //TODO: Change this for dynamic loading. When the actions and conditions look for PR info
        //      then they should load it from this data object.
        Issue = await GitHubRESTClient.Issue.Get(RepositoryId, PullRequest.Number);

        Logger.LogInformation("Retrieving PR files and reviews");
        await LoadPRFilesReviews();
    }

    public async Task SetupFromCommentEventPayload(IssueCommentPayload payload)
    {
        Logger.LogInformation("Creating data object from 'comment' event payload");

        RepositoryId = payload.Repository.Id;
        RepositoryName = payload.Repository.Name;
        RepositoryOwner = payload.Repository.Owner.Login;
        Issue = payload.Issue;
        Comment = payload.Comment;
        IssuePrBody = payload.Comment.Body ?? string.Empty;
        EventAction = payload.Action;

        Logger.LogInformation("RepoID: {repoid} RepoOrg/Name: '{repoowner}/{reponame}' Issue #: {issueid}", RepositoryId, RepositoryOwner, RepositoryName, Issue.Number);

        //TODO: Change this for dynamic loading. When the actions and conditions look for PR info
        //      then they should load it from this data object.
        if (payload.Issue.PullRequest != null)
            PullRequest = await GitHubRESTClient.PullRequest.Get(RepositoryId, payload.Issue.Number);
    }

    public async Task SetupFromProjectItem(EventType contentType, string nodeID, string eventAction)
    {
        Logger.LogInformation("Creating data object from 'project v2 item' event payload");

        GitHubGraphQLClient = new Octokit.GraphQL.Connection(new Octokit.GraphQL.ProductHeaderValue(FunctionRepoMain.AppProductName, FunctionRepoMain.AppProductVersion), GitHubRESTClient.Credentials.GetToken());

        if (contentType == EventType.Issue)
        {
            var issueQuery = new Query()
                .Node(new Arg<ID>(new(nodeID)))
                .Cast<Octokit.GraphQL.Model.Issue>()
                .Select(
                    issue => new
                    {
                        issue.Number,
                        RepoName = issue.Repository.Name,
                        RepoID = issue.Repository.DatabaseId,
                        RepoOwner = issue.Repository.Owner.Login
                    })
                .Compile();

            var result = await GitHubGraphQLClient.Run(issueQuery);

            RepositoryId = result.RepoID!.Value;
            RepositoryName = result.RepoName;
            RepositoryOwner = result.RepoOwner;
            Issue = await GitHubRESTClient.Issue.Get(RepositoryId, result.Number);
            Logger.LogInformation("RepoID: {repoid} RepoOrg/Name: '{repoowner}/{reponame}' Issue #: {issueid}", RepositoryId, RepositoryOwner, RepositoryName, Issue.Number);
        }
        else if (contentType == EventType.PullRequest)
        {
            var pullRequestQuery = new Query()
                .Node(new Arg<ID>(new(nodeID)))
                .Cast<Octokit.GraphQL.Model.PullRequest>()
                .Select(
                    pullRequest => new
                    {
                        pullRequest.Number,
                        RepoName = pullRequest.Repository.Name,
                        RepoID = pullRequest.Repository.DatabaseId,
                        RepoOwner = pullRequest.Repository.Owner.Login
                    })
                .Compile();

            var result = await GitHubGraphQLClient.Run(pullRequestQuery);

            RepositoryId = result.RepoID!.Value;
            RepositoryName = result.RepoName;
            RepositoryOwner = result.RepoOwner;
            Issue = await GitHubRESTClient.Issue.Get(RepositoryId, result.Number);
            PullRequest = await GitHubRESTClient.PullRequest.Get(RepositoryId, result.Number);
            Logger.LogInformation("RepoID: {repoid} RepoOrg/Name: '{repoowner}/{reponame}' PullRequest #: {issueid}", RepositoryId, RepositoryOwner, RepositoryName, PullRequest.Number);
        }
        else
            throw new Exception("Tried to get non issue/pullrequest info when working with a project item");

        EventAction = eventAction;
        IssuePrBody = Issue.Body ?? string.Empty;
    }

    public bool HasDocMetadata() =>
        DocIssueMetadata.Count != 0;

    public async Task LoadPRFilesReviews()
    {
        if (!HasPullRequestData) return;

        PullRequestFiles = (await GitHubRESTClient.PullRequest.Files(RepositoryId, PullRequest.Number)).ToArray();
        PullRequestReviews = (await GitHubRESTClient.PullRequest.Review.GetAll(RepositoryId, PullRequest.Number)).ToArray();
    }

    public JObject GetJSONObject()
    {
        if (_cachedStateBody != null)
        {
            // Update things that could possibly change due to actions
            _cachedStateBody["Variables"] = JArray.FromObject(Variables.Select(kv => new { kv.Key, kv.Value }).ToArray());
            _cachedStateBody["InstanceData"]!["IssuePrBody"] = IssuePrBody;
            return _cachedStateBody;
        }
        
        // Hide Issue.PullRequest -- The data is always invalid during serialization
        //Newtonsoft.Json.JsonIgnoreAttribute


        _cachedStateBody = new JObject(
            new JProperty("InstanceData", new JObject(
                new JProperty("RepositoryId", RepositoryId),
                new JProperty("RepositoryName", RepositoryName),
                new JProperty("RepositoryOwner", RepositoryOwner),
                new JProperty("RequestType", EventType.ToString()),
                new JProperty("EventAction", EventAction),
                new JProperty("IsPullRequest", HasPullRequestData),
                new JProperty("IsIssue", HasIssueData),
                new JProperty("IsComment", HasCommentData),
                new JProperty("IssuePrBody", IssuePrBody)
                )),
            new JProperty("Issue", Issue == null ? null : JObject.FromObject(new IssueSerializedFixed(Issue))),
            new JProperty("PullRequest", PullRequest == null ? null : JObject.FromObject(PullRequest)),
            new JProperty("PullRequestFiles", PullRequestFiles == null ? null : JArray.FromObject(PullRequestFiles)),
            new JProperty("PullRequestReviews", PullRequestReviews == null ? null : JArray.FromObject(PullRequestReviews)),
            new JProperty("Comment", Comment == null ? null : JObject.FromObject(Comment)),
            new JProperty("EventPayload", EventPayload),
            new JProperty("Variables", JArray.FromObject(Variables.Select(kv => new { kv.Key, kv.Value }).ToArray()))
            );

        if (false.OverrideWithEnvironmentVariable("UseLocalRepoFile"))
            System.IO.File.WriteAllText("state.json", _cachedStateBody.ToString(Newtonsoft.Json.Formatting.Indented));

        return _cachedStateBody;
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

    public async Task LoadIssueCommentMetadata()
    {
        // Data was already loaded.
        if (DocIssueMetadata.Count != 0) return;
        if (Settings is null) throw new Exception("Settings wasn't loaded from repo rules");

        string transformedComment = IssuePrBody.Replace("\r", "");
        
        Logger.LogInformation("Look for article URL");

        if (Settings.DocMetadata.ContentUrlRegex != null)
        {
            foreach (string regexSearch in Settings.DocMetadata.ContentUrlRegex)
            {
                Logger.LogInformation("Processing regex: {regex}", regexSearch.Replace("\n", "\\n"));
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(transformedComment, regexSearch, System.Text.RegularExpressions.RegexOptions.Multiline);

                if (match.Success)
                {

                    Dictionary<string, string> newMetadata = Utilities.ScrapeArticleMetadata(new Uri(match.Groups[1].Value.ToLower()), this).Result;

                    DocIssueMetadata = new(DocIssueMetadata.Union(newMetadata));

                    break;
                }
            }
        }
        else
            Logger.LogInformation("ContentUrlRegex is missing from repo rules metadata settings");

        // Load each metadata item into a variable
        foreach (string key in DocIssueMetadata.Keys)
            Variables[key] = DocIssueMetadata[key];
    }

    /// <summary>
    /// Reads the repoman config file from the github repository stored in the data object.
    /// </summary>
    /// <param name="data">The data object.</param>
    /// <returns>True when the file is loaded and parsed; otherwise, false.</returns>
    [MemberNotNullWhen(true, nameof(RepoRulesYaml))]
    [MemberNotNullWhen(true, nameof(Settings))]
    public async Task<(bool hasRulesFile, bool schemaOutOfDate, bool failure)> ReadRepoManConfig()
    {
        // Read the .repoman.yml file from the repository
        try
        {
            bool UseLocalFile = false.OverrideWithEnvironmentVariable("UseLocalRepoFile");
            string fileContent;

            // Load from github unless using a local file for testing
            if (!UseLocalFile)
            {
                IReadOnlyList<RepositoryContent> rulesResponse = await GitHubRESTClient.Repository.Content.GetAllContents(RepositoryId, FunctionRepoMain.RulesFileName);
                fileContent = rulesResponse[0].Content;

                if (fileContent == null)
                {
                    Logger.LogError("Read file from repo, but content is blank!");
                    return (false, false, true);
                }

                Logger.LogInformation("Reading repo rules file: {file}", FunctionRepoMain.RulesFileName);

                ///* HACK This is broken... github sometimes adds byte 63 to the start of the file which breaks the parser. Trim it off
                byte[] bytes = Encoding.ASCII.GetBytes(fileContent);
                if (bytes[0] == 63)
                    fileContent = Encoding.UTF8.GetString(bytes.AsSpan(1));
            }
            else
            {
                Logger.LogWarning("Reading local repo rules file");
                fileContent = File.ReadAllText(FunctionRepoMain.RulesFileName);
            }

            // Parse the file content into the Yaml object
            using StringReader reader = new(fileContent);
            YamlStream parser = new();
            parser.Load(reader);

            // Convert string content into YAML object
            RepoRulesYaml = (YamlMappingNode)parser.Documents[0].RootNode;

            // Check schema version
            int revision = RepoRulesYaml["revision"].ToInt();
            int schemaVersion = RepoRulesYaml["schema-version"].ToInt();
            string contact = RepoRulesYaml["owner-ms-alias"].ToString();

            Logger.LogInformation("Repo rules file [version]: {revision} [schema-version]: {schemaVersion} [contact]: {contact}", revision, schemaVersion, contact);

            if (schemaVersion < FunctionRepoMain.SchemaVersionMinimum)
            {
                Logger.LogError("schema-version is out-of-date: {schemaVersion}, must be: {SchemaVersionMinimum}", schemaVersion, FunctionRepoMain.SchemaVersionMinimum);
                return (true, true, false);
            }

            // Read settings
            LoadSettings(RepoRulesYaml["config"]);

            return (true, false, false);
        }
        catch (NotFoundException)
        {
            Logger.LogError("Rules file doesn't exist in repo");
            return (false, false, false);
        }
        catch (YamlDotNet.Core.SyntaxErrorException e)
        {
            Logger.LogError("Unable to parse repo rules:\nDescription: {message}\nLine info:{yaml_start}", e.Message, e.Start);
            return (true, false, true);
        }
        catch (Exception e)
        {
            Logger.LogError("Unknown error retrieving or loading the yaml file\n{error}", e.Message);
            return (false, false, true);
        }

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
            await GitHubCommands.Labels.AddLabels(uniqueLabels, this);
        }

        if (Operations.LabelsRemove.Count != 0)
        {
            string[] uniqueLabels = Operations.LabelsRemove.Distinct().ToArray();

            Logger.LogInformation($"Removing {uniqueLabels.Length} labels");
            await GitHubCommands.Labels.RemoveLabels(uniqueLabels, this.Issue!.Labels, this);
        }

        if (Operations.Assignees.Count != 0)
        {
            string[] uniqueNames = Operations.Assignees.Distinct().ToArray();

            Logger.LogInformation($"Adding {uniqueNames.Length} assignees");
            await GitHubCommands.Assignees.AddAssignees(uniqueNames, this);
        }

        if (Operations.Reviewers.Count != 0)
        {
            string[] uniqueNames = Operations.Reviewers.Distinct().ToArray();

            Logger.LogInformation($"Adding {uniqueNames.Length} reviewers");
            await GitHubCommands.Reviewers.AddReviewers(uniqueNames, this);
        }
    }

    // This class simply mirrors the Octokit.Issue type but omits the PullRequest property as that can contain unserializable data.
    private class IssueSerializedFixed
    {
        public long Id { get; private set; }
        public string NodeId { get; private set; }
        public string Url { get; private set; }
        public string HtmlUrl { get; private set; }
        public string CommentsUrl { get; private set; }
        public string EventsUrl { get; private set; }
        public int Number { get; private set; }
        public StringEnum<ItemState> State { get; private set; }
        public string Title { get; private set; }
        public string Body { get; private set; }
        public User ClosedBy { get; private set; }
        public User User { get; private set; }
        public User Assignee { get; private set; }
        public int Comments { get; private set; }
        public DateTimeOffset? ClosedAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }
        public bool Locked { get; private set; }
        public Repository Repository { get; private set; }
        public ReactionSummary Reactions { get; private set; }
        public StringEnum<LockReason>? ActiveLockReason { get; private set; }
        public StringEnum<ItemStateReason>? StateReason { get; private set; }
        public IReadOnlyList<Label> Labels { get; private set; }
        public IReadOnlyList<User> Assignees { get; private set; }
        public Octokit.Milestone Milestone { get; private set; }
        public IssueSerializedFixed(Issue issue)
        {
            Id = issue.Id;
            NodeId = issue.NodeId;
            Url = issue.Url;
            HtmlUrl = issue.HtmlUrl;
            CommentsUrl = issue.CommentsUrl;
            EventsUrl = issue.EventsUrl;
            Number = issue.Number;
            State = issue.State;
            Title = issue.Title;
            Body = issue.Body;
            ClosedBy = issue.ClosedBy;
            User = issue.User;
            Labels = issue.Labels;
            Assignee = issue.Assignee;
            Assignees = issue.Assignees;
            Milestone = issue.Milestone;
            Comments = issue.Comments;
            ClosedAt = issue.ClosedAt;
            CreatedAt = issue.CreatedAt;
            UpdatedAt = issue.UpdatedAt;
            Locked = issue.Locked;
            Repository = issue.Repository;
            Reactions = issue.Reactions;
            ActiveLockReason = issue.ActiveLockReason;
            StateReason = issue.StateReason;
        }
    }
}
