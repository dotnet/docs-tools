using DotNet.DocsTools.GitHubObjects;
using DotNet.DocsTools.Utility;

namespace Quest2GitHub.Models;

public class QuestWorkItem
{
    // Keep track of failures to update the closing PR.
    // For any given run, if the REST call to add a closing PR
    // fails, stop sending invalid requests.
    private static bool? s_linkedGitHubRepo;

    /// <summary>
    /// The Work item ID
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// The work item title.
    /// </summary>
    /// <remarks>
    /// This is retrieved as /fields/System.Title
    /// </remarks>
    public required string Title { get; init; }

    // /fields/System.Description
    public required string Description { get; init; }

    /// <summary>
    /// The work item area path.
    /// </summary>
    /// <remarks>
    /// This is retrieved as /fields/System.AreaPath
    /// </remarks>
    public required string AreaPath { get; init; }

    /// <summary>
    /// The work item iteration path. Null if not assigned yet.
    /// </summary>
    /// <remarks>
    /// This is retrieved as /fields/System.IterationPath
    /// </remarks>
    public required string? IterationPath { get; init; }

    /// <summary>
    /// The assigned to email address.
    /// </summary>
    /// <remarks>
    /// This is stored as /fields/System.AssignedTo/id
    /// This is the unique ID of the assignee, or null if unassigned.
    /// </remarks>
    public required Guid? AssignedToId { get; init; }

    /// <summary>
    /// The state of the issue.
    /// </summary>
    /// <remarks>
    /// This is retrieved as /fields/System.State
    /// </remarks>
    public required string? State { get; init; }

    /// <summary>
    /// The story point value.
    /// </summary>
    /// <remarks>
    /// This is retrieved from /Microsoft.VSTS.Scheduling.StoryPoints
    /// </remarks>
    public required int? StoryPoints { get; init; }

    /// <summary>
    /// Create a work item object from the ID
    /// </summary>
    /// <param name="client">The client services object.</param>
    /// <param name="workItemID">The work item id.</param>
    /// <returns>The workitem retrieved from Quest.</returns>
    public static async Task<QuestWorkItem> QueryWorkItem(QuestClient client, int workItemID)
    {
        JsonElement root = await client.GetWorkItem(workItemID);
        return WorkItemFromJson(root);
    }

    /// <summary>
    /// Create a work item from a GitHub issue.
    /// </summary>
    /// <param name="issue">The GitHub issue.</param>
    /// <param name="questClient">The quest client.</param>
    /// <param name="ospoClient">the MS open source programs office client.</param>
    /// <param name="path">The path component for the area path.</param>
    /// <param name="currentIteration">The current AzDo iteration</param>
    /// <returns>The newly created linked Quest work item.</returns>
    /// <remarks>
    /// Fill in the Json patch document from the GitHub issue.
    /// Once that patch document is created, create the work item.
    /// Finally, create the work item object from the returned
    /// Json element.
    /// </remarks>
    public static async Task<QuestWorkItem> CreateWorkItemAsync(QuestIssueOrPullRequest issue,
        QuestClient questClient,
        OspoClient ospoClient,
        string path,
        string? requestLabelNodeId,
        QuestIteration currentIteration,
        IEnumerable<QuestIteration> allIterations)
    {
        string areaPath = $"""{questClient.QuestProject}\{path}""";

        var patchDocument = new List<JsonPatchDocument>()
        {
            new() {
                Operation = Op.Add,
                Path = "/fields/System.Title",
                From = default,
                Value = issue.Title
            },
            new() {
                Operation = Op.Add,
                Path = "/fields/System.Description",
                From = default,
                Value = BuildDescriptionFromIssue(issue, requestLabelNodeId)
            },
            new() {
                Operation = Op.Add,
                Path = "/fields/System.AreaPath",
                From = default,
                Value = areaPath,
            },

        };

        // Query if the GitHub ID maps to an FTE id, add that one:
        string? assigneeEmail = await issue.QueryAssignedMicrosoftEmailAddressAsync(ospoClient);
        AzDoIdentity? assigneeID = default;
        if (assigneeEmail?.EndsWith("@microsoft.com") == true)
        {
            assigneeID = await questClient.GetIDFromEmail(assigneeEmail);
        }
        var assignPatch = new JsonPatchDocument
        {
            Operation = Op.Add,
            Path = "/fields/System.AssignedTo",
            From = default,
            Value = assigneeID
        };
        patchDocument.Add(assignPatch);
        StoryPointSize? iterationSize = issue.LatestStoryPointSize();
        if (iterationSize != null)
        {
            Console.WriteLine($"Latest GitHub sprint project: {iterationSize?.Month}-{iterationSize?.CalendarYear}, size: {iterationSize?.Size}");
        }
        else
        {
            Console.WriteLine("No GitHub sprint project found - using current iteration");
        }
        QuestIteration? iteration = iterationSize?.ProjectIteration(allIterations);
        patchDocument.Add(new JsonPatchDocument
        {
            Operation = Op.Add,
            Path = "/fields/System.IterationPath",
            Value = iteration?.Path ?? currentIteration.Path,
        });
        if (iterationSize?.QuestStoryPoint() is not null)
        {
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                From = default,
                Path = "/fields/Microsoft.VSTS.Scheduling.StoryPoints",
                Value = iterationSize.QuestStoryPoint(),
            });
        }

        /* This is ignored by Azure DevOps. It uses the PAT of the 
         * account running the code.
        var creator = await issue.AuthorMicrosoftPreferredName(ospoClient);
        patchDocument.Add(
            new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.CreatedBy",
                From = default,
                Value = creator ?? "dotnet-bot"
            });
        */
        if (!issue.IsOpen)
        {
            // Created completed work item:
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.State",
                Value = "Closed",
            });
        }
        JsonElement result = default;
        QuestWorkItem? newItem;
        try
        {
            result = await questClient.CreateWorkItem(patchDocument);
            newItem = WorkItemFromJson(result);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine(result.ToString());
            // This will happen when the assignee IS a Microsoft FTE,
            // but that person isn't onboarded in Quest. (Likely a product team member, or CSE)

            // So, remove the node with the Assigned to and try again:
            patchDocument.Remove(assignPatch);

            // Yes, this could throw again. IF so, it's a new error.
            result = await questClient.CreateWorkItem(patchDocument);
            newItem = WorkItemFromJson(result);
        }
        // Add the closing PR in a separate request. 
        if (issue.ClosingPRUrl is not null)
        {
            newItem = await newItem.AddClosingPR(questClient, issue.ClosingPRUrl) ?? newItem;
        }
        return newItem;
    }

    public static string BuildDescriptionFromIssue(QuestIssueOrPullRequest issue, string? requestLabelNodeId)
    {
        var body = new StringBuilder($"<p>Imported from: {issue.LinkText}</p>");
        body.AppendLine($"<p>Author: {issue.FormattedAuthorLoginName}</p>");
        body.AppendLine(issue.BodyHtml.ScrubContent());
        if (issue.Labels.Length != 0)
        {
            body.AppendLine($"<p><b>Labels:</b></p>");
            body.AppendLine("<ul>");
            foreach (GitHubLabel? item in issue.Labels.Where(l => l.Id != requestLabelNodeId))
            {
                body.AppendLine($"<li>#{item.Name.Replace(' ', '-')}</li>");
            }
            body.AppendLine("</ul>");
        }
        if (issue.Comments.Length != 0)
        {
            body.AppendLine($"<p><b>Comments:</b></p>");
            body.AppendLine("<dl>");
            foreach ((string author, string bodyHTML) in issue.Comments)
            {
                body.AppendLine($"<dt>{author}</dt>");
                body.AppendLine($"<dd>{bodyHTML.ScrubContent()}</dd>");
            }
            body.AppendLine("</dl>");
        }
        return body.ToString();
    }

    internal async Task<QuestWorkItem?> AddClosingPR(QuestClient azdoClient, string closingPRUrl)
    {
        if (s_linkedGitHubRepo is false) return default;

        List<JsonPatchDocument> patchDocument =
        [
            new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/relations/-",
                Value = new Relation
                {
                    Url = closingPRUrl,
                    Attributes = { ["name"] = "GitHub Pull Request" }
                }
            }
        ];
        try
        {
            JsonElement jsonDocument = await azdoClient.PatchWorkItem(Id, patchDocument);
            var newItem = QuestWorkItem.WorkItemFromJson(jsonDocument);
            s_linkedGitHubRepo = true;
            return newItem;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"""
                Can't add closing PR. The GitHub repo is likely not configured as linked in Quest.
                Exception: {ex}
                """);

            s_linkedGitHubRepo = false;
            return null;
        }
    }

    /// <summary>
    /// Construct a work item from the JSON document.
    /// </summary>
    /// <param name="root">The root element.</param>
    /// <returns>The Quest work item.</returns>
    public static QuestWorkItem WorkItemFromJson(JsonElement root)
    {
        int id = root.GetProperty("id").GetInt32();
        JsonElement fields = root.GetProperty("fields");
        string title = fields.GetProperty("System.Title").GetString()!;
        string state = fields.GetProperty("System.State").GetString()!;
        string description = fields.GetProperty("System.Description").GetString()!;
        string areaPath = fields.GetProperty("System.AreaPath").GetString()!;
        string? iterationPath = fields.GetProperty("System.IterationPath").GetString();
        JsonElement assignedNode = fields.Descendent("System.AssignedTo", "id");
        int? storyPoints = fields.TryGetProperty("Microsoft.VSTS.Scheduling.StoryPoints", out JsonElement storyPointNode) ?
            (int)double.Truncate(storyPointNode.GetDouble()) : null;
        string? assignedID = (assignedNode.ValueKind is JsonValueKind.String) ?
            assignedNode.GetString() : null;
        return new QuestWorkItem
        {
            Id = id,
            Title = title,
            State = state,
            Description = description,
            AreaPath = areaPath,
            IterationPath = iterationPath,
            AssignedToId = (assignedID is not null) ? new Guid(assignedID) : null,
            StoryPoints = storyPoints
        };
    }

    /// <summary>
    /// Write this item as a string.
    /// </summary>
    /// <returns>A multi-line representation of this quest
    /// work item.</returns>
    public override string ToString() =>
        $$"""
          WorkItem: {{Id}} -- {{Title}}
          {{Description}}

          Assignee: {{AssignedToId}}
          AreaPath: {{AreaPath}}
          Iteration: {{IterationPath}}
          """;
}
