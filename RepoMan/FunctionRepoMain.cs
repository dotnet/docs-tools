using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Octokit;
using Octokit.GraphQL.Core.Deserializers;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan;

public class FunctionRepoMain
{
    public const string RulesFileName = ".repoman.yml";
    public const int SchemaVersionMinimum = 5;
    public static string AppProductName = "DotNetDocs.RepoMan";
    public static readonly string AppProductVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!.ToString();

    private readonly ILogger<FunctionRepoMain> _logger;

    public FunctionRepoMain(ILogger<FunctionRepoMain> logger) =>
        _logger = logger;

    [Function("FunctionRepoMain")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("RepoMan v{AppVersion}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            // Grab the GitHub App key from the environment
            string gitHubAppKey = Environment.GetEnvironmentVariable("AppKeySecret", EnvironmentVariableTarget.Process) ?? "";
            if (string.IsNullOrEmpty(gitHubAppKey))
                return _logger.LogFailure("AppKeySecret not set");

            // Validate payload is from GitHub
            _logger.LogTrace($"Reading request body");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (req.Headers.TryGetSingleString("X-Hub-Signature-256", out string signature))
            {
                if (Environment.GetEnvironmentVariable("SecretToken", EnvironmentVariableTarget.Process) is string hashSecret)
                {
                    if (Tools.GitHubCommunications.GitHubEventPayloadValidator.IsSecure(requestBody, signature, hashSecret) is false)
                        return _logger.LogFailure("Payload failed hash check");

                    // Log the GitHub GUID of the request
                    _logger.LogInformation("GitHub GUID: {guid}", req.Headers["X-GitHub-Delivery"].ToString());
                }
                else
                    return _logger.LogFailure("Hash secret env variable not set");
            }
            else
                return _logger.LogFailure("Signature header is missing or has an invalid value");

            // Get the event type from the payload
            if (req.Headers.TryGetSingleString("X-GitHub-Event", out string eventType) is false)
                return _logger.LogFailure("Event type not found in payload header");
            _logger.LogInformation("GitHub event: {event}", eventType);

            // Get the app ID from the payload header
            if (req.Headers.TryGetSingleInt("X-GitHub-Hook-Installation-Target-ID", out int appID) is false)
                return _logger.LogFailure("App ID not found in payload header");

            // TODO: Look into sharing this with InvokationData.EventPayload which is a LINQ JObject
            // Read the installation ID from request
            _logger.LogTrace("Convert request body to a JsonDocument and read installation/id property");
            JsonDocument doc = JsonDocument.Parse(requestBody);
            long installationID = doc.RootElement.GetProperty("installation").GetProperty("id").GetInt64();
            doc.Dispose();

            // Create github client in the context of the github app
            _logger.LogInformation("Get app install token from GitHub");
            string appToken = await GitHubAccess.GetAppToken(gitHubAppKey, appID, installationID, _logger);
            AppProductName = $"{AppProductName}-Installation{appID}";
            GitHubClient installationClient = new(new ProductHeaderValue(AppProductName, AppProductVersion))
            {
                Credentials = new Credentials(appToken)
            };

            // Create the data object passed around to actions and checks
            EventType castedEvent = ParseEventTypeName(eventType);

            if (castedEvent == EventType.Unknown)
                return _logger.LogFailure("Unknown event sent to service");

            InstanceData data = new(_logger, installationClient, castedEvent, JObject.Parse(requestBody));

            // Based on the type of GitHub event, load the payload and configure the data object
            if (data.EventType == EventType.Issue)
                await data.SetupFromIssueEventPayload(new Octokit.Internal.SimpleJsonSerializer().Deserialize<IssueEventPayload>(requestBody));

            else if (data.EventType == EventType.PullRequest)
                await data.SetupFromPullRequestEventPayload(new Octokit.Internal.SimpleJsonSerializer().Deserialize<PullRequestEventPayload>(requestBody));

            else if (data.EventType == EventType.Comment)
                await data.SetupFromCommentEventPayload(new Octokit.Internal.SimpleJsonSerializer().Deserialize<IssueCommentPayload>(requestBody));

            else if (data.EventType == EventType.ProjectItem)
            {
                JObject obj = JObject.Parse(requestBody);
                string contentType = obj.SelectToken("projects_v2_item.content_type")!.Value<string>()!;
                string nodeId = obj.SelectToken("projects_v2_item.content_node_id")!.Value<string>()!;
                string eventAction = obj.SelectToken("action")!.Value<string>()!;

                try
                {
                    if (contentType == "Issue")
                    {
                        await data.SetupFromProjectItem(EventType.Issue, nodeId, eventAction);
                    }
                    else if (contentType == "PullRequest")
                    {
                        await data.SetupFromProjectItem(EventType.PullRequest, nodeId, eventAction);
                    }
                    else // DraftIssue which is just a card without issue/pr
                    {
                        _logger.LogInformation("Project event doesn't have a related issue or pullrequest");
                        return new OkResult();
                    }
                }
                catch (ResponseDeserializerException ex)
                {
                    if (ex.Message.Contains("Could not resolve to a node with the global id"))
                    {
                        _logger.LogInformation("Unable to get item details from payload. Possibly private project or repo.");
                        return new OkResult();
                    }
                    else
                    {
                        if (false.OverrideWithEnvironmentVariable("LogUnhandledEx"))
                            _logger.LogInformation("Unhandled exception\r\n{ex}", ex);
                        else
                            _logger.LogError("Unhandled exception");

                        return new BadRequestObjectResult("Unhandled exception");
                    }
                }
            }

            // Try to load the rules file from GitHub, validate the schema version, and load the config
            (bool hasRulesFile, bool isSchemaOutOfDate, bool loadFailure) = await data.ReadRepoManConfig();

            // Check if rules failed to load
            if (loadFailure)
            {
                string error = $"Unable to load rules file ({RulesFileName})";
                return data.Logger.LogFailure(error);
            }
            else if (isSchemaOutOfDate)
            {
                string error = $"Rules schema is out-of-date";
                return data.Logger.LogFailure(error);
            }
            else if (!hasRulesFile)
            {
                return new OkObjectResult($"Repo rules not in repo {data.RepositoryOwner}/{data.RepositoryName}");
            }

            // Check if a magic label was sent, modify the state accordingly
            await RerunLabelCheck(data);

            // Load the issue body and see if it points to an article. If so, load the metadata
            await data.LoadIssueCommentMetadata();

            // Check rules for event+action combination
            if (data.RepoRulesYaml!.Exists(eventType))
            {
                YamlMappingNode eventNode = data.RepoRulesYaml![eventType].AsMappingNode();

                if (eventNode.Children.ContainsKey(data.EventAction))
                {
                    bool remappedEvent = false;

                restart_node_check:
                    YamlNode actionNode = eventNode[data.EventAction];

                    // Remapping
                    if (actionNode.NodeType == YamlNodeType.Scalar)
                    {
                        // We've remapped once, don't allow it again.
                        if (remappedEvent)
                        {
                            data.Logger.LogError($"Remapping already happened once. Can't remap an event into another remap.");
                            return new BadRequestObjectResult("Remapped twice.");
                        }

                        data.Logger.LogInformation("Remap found in rules. From: {from_action} To: {to_action}", data.EventAction, actionNode);

                        // Prevent circular reference
                        if (data.EventAction == actionNode.ToString())
                        {
                            data.Logger.LogError($"Remapped to self.");
                            return new BadRequestObjectResult("Remapped to self.");
                        }

                        data.EventAction = actionNode.ToString();
                        goto restart_node_check;
                    }
                    else if (actionNode.NodeType != YamlNodeType.Sequence)
                    {
                        data.Logger.LogError($"Event should use a sequence.");
                        return new BadRequestObjectResult("Event should use a sequence.");
                    }

                    data.Logger.LogInformation("Processing action: {action}", data.EventAction);

                    await Actions.Runner.Build(actionNode.AsSequenceNode(), data).Run(data);

                    await data.RunPooledActions();
                }
                else
                    data.Logger.LogInformation("Action {action} not defined in rules. Nothing to do.", data.EventAction);
            }
            else
                data.Logger.LogInformation("Event {event} not defined in rules. Nothing to do.", eventType);

            return data.HasFailure
                ? _logger.LogFailure(data.FailureMessage)
                : new OkResult();
        }
        catch (ForbiddenException ex)
        {
            return _logger.LogFailure("Forbidden exception captured. Perhaps this repo isn't approved?");
        }
        catch (Exception ex)
        {
            if (false.OverrideWithEnvironmentVariable("LogUnhandledEx"))
                _logger.LogInformation("Unhandled exception\r\n{ex}", ex);
            else
                _logger.LogError("Unhandled exception");

            return new BadRequestObjectResult("Unhandled exception");
        }
    }

    /// <summary>
    /// Converts the payload event name to an enumeration.
    /// </summary>
    /// <param name="eventType">The payload event name.</param>
    /// <returns>The event type.</returns>
    /// <exception cref="Exception">Unknown event was provided.</exception>
    private static EventType ParseEventTypeName(string eventType)
    {
        const string EventTypeIssue = "issues";
        const string EventTypePullRequest = "pull_request";
        const string EventTypeComment = "issue_comment";
        const string EventProjectV2Item = "projects_v2_item";

        return eventType switch
        {
            EventTypeIssue          => EventType.Issue,
            EventTypePullRequest    => EventType.PullRequest,
            EventTypeComment        => EventType.Comment,
            EventProjectV2Item      => EventType.ProjectItem,
            _ => EventType.Unknown
        };

    }

    /// <summary>
    /// Determines if the issue has a magic label that triggers a re-run of the action as a different type.
    /// </summary>
    /// <param name="data">The data associated with the trigger.</param>
    /// <returns>A task.</returns>
    private static async Task RerunLabelCheck(InstanceData data)
    {
        if (data.EventAction == "labeled")
        {
            Label? magicLabel = data.Issue!.Labels.FirstOrDefault(l => l.Name.StartsWith("rerun-action-", StringComparison.OrdinalIgnoreCase));

            if (magicLabel != null)
            {
                data.EventAction = magicLabel.Name.ToLower().Substring("rerun-action-".Length);
                data.Logger.LogInformation("Magic label found: {magic_label}; Reprocessing issue {issue} as {event_action}", magicLabel.Name, data.Issue.Number, data.EventAction);

                // Remove the trigger labels
                data.Operations.LabelsRemove.Add(magicLabel.Name);

                // Refresh the issue.
                data.Issue = await data.GitHubRESTClient.Issue.Get(data.RepositoryId, data.Issue.Number);

                // Refresh the PR
                if (data.EventType == EventType.PullRequest)
                {
                    data.PullRequest = await data.GitHubRESTClient.PullRequest.Get(data.RepositoryId, data.Issue.Number);
                    data.IssuePrBody = data.PullRequest.Body ?? string.Empty;
                }
                else if (data.EventType == EventType.Issue)
                {
                    data.IssuePrBody = data.Issue?.Body ?? string.Empty;
                }
                else // Comment
                {
                    data.IssuePrBody = data.Comment?.Body ?? string.Empty;
                }
            }
        }
    }
}
