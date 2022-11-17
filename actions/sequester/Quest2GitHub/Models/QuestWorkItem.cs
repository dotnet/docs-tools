namespace Quest2GitHub.Models;

public class QuestWorkItem
{
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
    /// Create a work item object from the ID
    /// </summary>
    /// <param name="client">The client services object.</param>
    /// <param name="workItemID">The work item id.</param>
    /// <returns>The workitem retrieved from Quest.</returns>
    public static async Task<QuestWorkItem> QueryWorkItem(QuestClient client, int workItemID)
    {
        var root = await client.GetWorkItem(workItemID);
        return WorkItemFromJson(root);
    }

    /// <summary>
    /// Create a work item from a GitHub issue.
    /// </summary>
    /// <param name="issue">The Github issue.</param>
    /// <param name="questClient">The quest client.</param>
    /// <param name="ospoClient">the MS open source programs office client.</param>
    /// <param name="path">The path component for the area path.</param>
    /// <returns>The newly created linked Quest work item.</returns>
    /// <remarks>
    /// Fill in the Json patch document from the GitHub issue.
    /// Once that patch document is created, create the work item.
    /// Finally, create the work item object from the returned
    /// Json element.
    /// </remarks>
    public static async Task<QuestWorkItem> CreateWorkItemAsync(GithubIssue issue, 
        QuestClient questClient, 
        OspoClient ospoClient,
        string path,
        string? requestLabelNodeId)
    {
        StringBuilder body = new StringBuilder($"<p>Imported from: {issue.LinkText}</p>");
        body.AppendLine($"<p>Author: {issue.Author}</p>");
        body.AppendLine(issue.BodyHtml);
        var areaPath = $"""{questClient.QuestProject}\{path}""";
        if (issue.Labels.Any())
        {
            body.AppendLine($"<p><b>Labels:</b></p>");
            body.AppendLine("<ul>");
            foreach (var item in issue.Labels.Where(l => l.nodeID != requestLabelNodeId))
            {
                body.AppendLine($"<li>#{item.name.Replace(' ', '-')}</li>");
            }
            body.AppendLine("</ul>");
        }
        if (issue.Comments.Any())
        {
            body.AppendLine($"<p><b>Comments:</b></p>");
            body.AppendLine("<dl>");
            foreach (var item in issue.Comments)
            {
                body.AppendLine($"<dt>{item.author}</dt>");
                body.AppendLine($"<dd>{item.bodyHTML}</dd>");
            }
            body.AppendLine("</dl>");
        }

        var patchDocument = new List<JsonPatchDocument>()
        {
            new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.Title",
                From = default,
                Value = issue.Title
            },
            new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.Description",
                From = default,
                Value = body.ToString()
            },
            new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.AreaPath",
                From = default,
                Value = areaPath,
            },

        };

        // Query if the Github ID maps to an FTE id, add that one:
        var assigneeEmail = await issue.AssignedMicrosoftEmailAddress(ospoClient);
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
        try
        {
            result = await questClient.CreateWorkItem(patchDocument);
            var newItem = WorkItemFromJson(result);
            return newItem;
        } catch (InvalidOperationException)
        {
            Console.WriteLine(result.ToString());
            // This will happen when the assignee IS a Microsoft FTE,
            // but that person isn't onboarded in Quest. (Likely a product team member, or CSE)

            // So, remove the node with the Assigned to and try again:
            patchDocument.Remove(assignPatch);

            // Yes, this could throw again. IF so, it's a new error.
            result = await questClient.CreateWorkItem(patchDocument);
            return WorkItemFromJson(result);
        }
    }

    /// <summary>
    /// Construct a work item from the JSON document.
    /// </summary>
    /// <param name="root">The root element.</param>
    /// <returns>The Quest work item.</returns>
    public static QuestWorkItem WorkItemFromJson(JsonElement root)
    {
        var id = root.GetProperty("id").GetInt32();
        var fields = root.GetProperty("fields");
        var title = fields.GetProperty("System.Title").GetString()!;
        var state = fields.GetProperty("System.State").GetString()!;
        var description = fields.GetProperty("System.Description").GetString()!;
        var areaPath = fields.GetProperty("System.AreaPath").GetString()!;
        var iterationPath = fields.GetProperty("System.IterationPath").GetString();
        var assignedNode = fields.Descendent("System.AssignedTo", "id");
        var assignedID = (assignedNode.ValueKind is JsonValueKind.String) ?
            assignedNode.GetString() : null;
        return new QuestWorkItem
        {
            Id = id,
            Title = title,
            State = state,
            Description = description,
            AreaPath = areaPath,
            IterationPath = iterationPath,
            AssignedToId = (assignedID is not null) ? new Guid(assignedID) : null
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
