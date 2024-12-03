using DotNet.DocsTools.GitHubObjects;
using DotNet.DocsTools.Utility;
using Microsoft.DotnetOrg.Ospo;

namespace Quest2GitHub.Models;

public class QuestWorkItem
{
    // Keep track of failures to update the closing PR.
    // For any given run, if the REST call to add a closing PR
    // fails, stop sending invalid requests.
    // 7/9/2024: Set this to false. GitHub integration links
    // are currently disabled. Linking to the closing PR always fails.
    // So, don't try for now.
    private static bool? s_linkedGitHubRepo = false;

    private string _title = "";

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
    public required string Title
    {
        get => _title;
        init => _title = TruncateTitleWhenLongerThanMax(value);
    }

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
    /// The priority
    /// </summary>
    /// <remarks>
    /// This is retrieved from Microsoft.VSTS.Common.Priority
    /// </remarks>
    public required int Priority { get; init; }

    /// <summary>
    /// The ID of the parent work item.
    /// </summary>
    /// <remarks>
    /// Starting with the next semester, our work items
    /// must have a parent Epic or Feature.
    /// Note that existing work items may not have
    /// a current parent, which would make a 0 parent ID.
    /// </remarks>
    public required int ParentWorkItemId { get; init; }

    /// <summary>
    /// The index of the parent relation in the relations array.
    /// </summary>
    /// <remarks>
    /// The relations array items can't be updated. Therefore,
    /// to edit the parent, we need to know the index of the
    /// existing relationship so we can remove it first.
    /// </remarks>
    public required int? ParentRelationIndex { get; init; }

    public required IEnumerable<string> Tags { get; init; }

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
    /// <param name="requestLabelNodeId">The ID of the request label</param>
    /// <returns>The newly created linked Quest work item.</returns>
    /// <remarks>
    /// Fill in the Json patch document from the GitHub issue.
    /// Once that patch document is created, create the work item.
    /// Finally, create the work item object from the returned
    /// Json element.
    /// </remarks>
    public static async Task<QuestWorkItem> CreateWorkItemAsync(QuestIssueOrPullRequest issue,
        QuestClient questClient,
        OspoClient? ospoClient,
        string path,
        string? requestLabelNodeId,
        WorkItemProperties issueProperties)
    {
        string areaPath = $"""{questClient.QuestProject}\{path}""";

        List<JsonPatchDocument> patchDocument =
        [
            new() {
                Operation = Op.Add,
                Path = "/fields/System.Title",
                From = default,
                Value = TruncateTitleWhenLongerThanMax(issue.Title)
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
            }
        ];
        if (issueProperties.ParentNodeId != 0)
        {
            var parentRelation = new Relation
            {
                RelationName = "System.LinkTypes.Hierarchy-Reverse",
                Url = $"https://dev.azure.com/{questClient.QuestOrg}/{questClient.QuestProject}/_apis/wit/workItems/{issueProperties.ParentNodeId}",
                Attributes =
                {
                    ["name"] = "Parent",
                    ["isLocked"] = false
                }
            };
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/relations/-",
                From = default,
                Value = parentRelation
            });
        }

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
        patchDocument.Add(new JsonPatchDocument
        {
            Operation = Op.Add,
            Path = "/fields/System.IterationPath",
            Value = issueProperties.IterationPath,
        });
        if (issueProperties.StoryPoints != 0)
        {
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                From = default,
                Path = "/fields/Microsoft.VSTS.Scheduling.StoryPoints",
                Value = issueProperties.StoryPoints,
            });
        }
        if (issueProperties.Priority != -1)
        {
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                From = default,
                Path = "/fields/Microsoft.VSTS.Common.Priority",
                Value = issueProperties.Priority
            });
        }

        if (issueProperties.Tags.Any())
        {
            string azDoTags = string.Join(";", issueProperties.Tags);
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.Tags",
                Value = azDoTags
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
        return newItem;
    }

    private static string TruncateTitleWhenLongerThanMax(string title)
    {
        // https://learn.microsoft.com/azure/devops/boards/work-items/about-work-items?view=azure-devops&tabs=agile-process#common-work-tracking-fields
        const int MaxTitleLength = 255;

        if (title is { Length: > MaxTitleLength })
        {
            return title[0..MaxTitleLength];
        }

        return title;
    }

    public static string BuildDescriptionFromIssue(QuestIssueOrPullRequest issue, string? requestLabelNodeId)
    {
        var body = new StringBuilder($"<p>Imported from: {issue.LinkText}</p>");
        body.AppendLine($"<p>Author: {issue.FormattedAuthorLoginName}</p>");
        var assigneeStrings = issue.Assignees.Select(a => $"{a.Login}-({a.Name})").ToArray();
        body.AppendLine($"<p>Assignees: {string.Join(", ", assigneeStrings)}</p>");  
        body.AppendLine(issue.BodyHtml?.ScrubContent());
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
            var newItem = WorkItemFromJson(jsonDocument);
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

    static internal async Task<QuestWorkItem?> UpdateWorkItemAsync(QuestWorkItem questItem,
        QuestIssueOrPullRequest ghIssue,
        QuestClient questClient,
        OspoClient? ospoClient,
        WorkItemProperties issueProperties)
    {
        string? ghAssigneeEmailAddress = await ghIssue.QueryAssignedMicrosoftEmailAddressAsync(ospoClient);
        AzDoIdentity? questAssigneeID = default;

        if (ghAssigneeEmailAddress?.EndsWith("@microsoft.com") == true)
        {
            questAssigneeID = await questClient.GetIDFromEmail(ghAssigneeEmailAddress);
        }
        List<JsonPatchDocument> patchDocument = [];
        if (issueProperties.ParentNodeId != questItem.ParentWorkItemId)
        {
            if (questItem.ParentWorkItemId != 0)
            {
                // Remove the existing parent relation.
                patchDocument.Add(new JsonPatchDocument
                {
                    Operation = Op.Remove,
                    Path = "/relations/" + questItem.ParentRelationIndex,
                });
            };
            if (issueProperties.ParentNodeId != 0)
            {
                var parentRelation = new Relation
                {
                    RelationName = "System.LinkTypes.Hierarchy-Reverse",
                    Url = $"https://dev.azure.com/{questClient.QuestOrg}/{questClient.QuestProject}/_apis/wit/workItems/{issueProperties.ParentNodeId}",
                    Attributes =
                    {
                        ["name"] = "Parent",
                        ["isLocked"] = false
                    }
                };

                patchDocument.Add(new JsonPatchDocument
                {
                    Operation = Op.Add,
                    Path = "/relations/-",
                    From = default,
                    Value = parentRelation
                });
            }
        }
        if ((questAssigneeID is not null) && (questAssigneeID?.Id != questItem.AssignedToId))
        {
            // build patch document for assignment.
            JsonPatchDocument assignPatch = new()
            {
                Operation = Op.Add,
                Path = "/fields/System.AssignedTo",
                Value = questAssigneeID,
            };
            patchDocument.Add(assignPatch);
        }
        Console.WriteLine(issueProperties.IssueLogString);
        if (issueProperties.WorkItemState != questItem.State)
        {
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.State",
                Value = issueProperties.WorkItemState,
            });
        }
        if (issueProperties.IterationPath != questItem.IterationPath)
        {
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.IterationPath",
                Value = issueProperties.IterationPath,
            });
        }
        if (issueProperties.StoryPoints != (questItem.StoryPoints ?? 0))
        {
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                From = default,
                Path = "/fields/Microsoft.VSTS.Scheduling.StoryPoints",
                Value = issueProperties.StoryPoints,
            });
        }
        if (issueProperties.Priority != questItem.Priority)
        {
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/Microsoft.VSTS.Common.Priority",
                Value = (issueProperties.Priority == -1) ? 4 : issueProperties.Priority
            });
        }
        var tags = from t in issueProperties.Tags
                   where !questItem.Tags.Contains(t)
                   select t;
        if (tags.Any())
        {
            string azDoTags = string.Join(";", tags);
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.Tags",
                Value = azDoTags
            });
        }

        QuestWorkItem? newItem = default;
        if (patchDocument.Count != 0)
        {
            // If any updates are needed, add the description.
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.Description",
                From = default,
                Value = BuildDescriptionFromIssue(ghIssue, null)
            });
            JsonElement jsonDocument = await questClient.PatchWorkItem(questItem.Id, patchDocument);
            newItem = WorkItemFromJson(jsonDocument);
        }
        if (!ghIssue.IsOpen && (ghIssue.ClosingPRUrl is not null))
        {
            newItem = await questItem.AddClosingPR(questClient, ghIssue.ClosingPRUrl) ?? newItem;
        }
        return newItem;
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
        int parentID = fields.TryGetProperty("System.Parent", out JsonElement parentNode) ?
            parentNode.GetInt32() : 0;
        int? parentRelationIndex = null;
        if (parentID != 0)
        {
            string relType = "System.LinkTypes.Hierarchy-Reverse";
            (JsonElement r, int Index) parentRelation = root.GetProperty("relations")
                .EnumerateArray().Select((r,Index) => (r,Index))
                .FirstOrDefault(t => t.r.GetProperty("rel").GetString() == relType);
            parentRelationIndex = parentRelation.Index;
        }

        string title = fields.GetProperty("System.Title").GetString()!;
        string state = fields.GetProperty("System.State").GetString()!;
        string description = fields.GetProperty("System.Description").GetString()!;
        string areaPath = fields.GetProperty("System.AreaPath").GetString()!;
        string? iterationPath = fields.GetProperty("System.IterationPath").GetString();
        JsonElement assignedNode = fields.Descendent("System.AssignedTo", "id");
        int? storyPoints = fields.TryGetProperty("Microsoft.VSTS.Scheduling.StoryPoints", out JsonElement storyPointNode) ?
            (int)double.Truncate(storyPointNode.GetDouble()) : null;
        int priority = fields.TryGetProperty("Microsoft.VSTS.Common.Priority", out JsonElement priorityNode) ?
            (int)priorityNode.GetInt32() : 2;
        string ? assignedID = (assignedNode.ValueKind is JsonValueKind.String) ?
            assignedNode.GetString() : null;
        string tagElement = fields.TryGetProperty("System.Tags", out JsonElement tagsNode) ?
            tagsNode.GetString()! : string.Empty;
        IEnumerable<string> tags = [..tagElement.Split(';').Select(s => s.Trim())];
        return new QuestWorkItem
        {
            Id = id,
            ParentWorkItemId = parentID,
            ParentRelationIndex = parentRelationIndex,
            Title = title,
            State = state,
            Description = description,
            AreaPath = areaPath,
            IterationPath = iterationPath,
            AssignedToId = (assignedID is not null) ? new Guid(assignedID) : null,
            StoryPoints = storyPoints,
            Priority = priority,
            Tags = tags
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
