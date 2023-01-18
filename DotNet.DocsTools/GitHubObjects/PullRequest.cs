using DotNetDocs.Tools.GraphQLQueries;
using System.Text.Json;

namespace DotNetDocs.Tools.GitHubObjects;

/// <summary>
/// This encapsulates the PullRequest node from a 
/// GraphQL query.
/// </summary>
/// <remarks>
/// This readonly struct provides easy access
/// to the properties of the PR.
/// </remarks>
public readonly struct PullRequest
{
    private readonly JsonElement node;

    /// <summary>
    /// Construct the PullRequest from the Json node.
    /// </summary>
    /// <param name="pullRequestNode"></param>
    public PullRequest(JsonElement pullRequestNode) => this.node = pullRequestNode;

    /// <summary>
    /// The node ID for the issue.
    /// </summary>
    public string Id => node.GetProperty("id").GetString() ?? throw new InvalidOperationException("Id property not found");

    /// <summary>
    /// Access the PR number.
    /// </summary>
    public int Number => node.GetProperty("number").GetInt32();

    /// <summary>
    /// Access the title.
    /// </summary>
    public string Title => node.GetProperty("title").GetString() ?? throw new InvalidOperationException("title Property not found");

    /// <summary>
    /// Retrieve the Url property.
    /// </summary>
    public string Url => node.GetProperty("url").GetString() ?? throw new InvalidOperationException("Url property not found");

    /// <summary>
    /// Access the number of changed files.
    /// </summary>
    public int ChangedFiles => node.GetProperty("changedFiles").GetInt32();

    /// <summary>
    /// Access the author object for this PR.
    /// </summary>
    public Actor Author => new Actor(node.GetProperty("author"));

    /// <summary>
    /// Retrun the list of labels on this issue.
    /// </summary>
    public IEnumerable<string> Labels => from label in node
                                         .Descendent("labels", "nodes")
                                             .EnumerateArray()
                                         select label.GetProperty("name").GetString();
}
