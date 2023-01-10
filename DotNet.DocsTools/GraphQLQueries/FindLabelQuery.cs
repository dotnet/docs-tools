using DotnetDocsTools.GitHubCommunications;

namespace DotnetDocsTools.GraphQLQueries;

/// <summary>
/// This query returns a single label nodeID
/// </summary>
/// <remarks>
/// This query object is constructed, then the query is performed. If the label
/// is found, the label can be retrieved from the LabelID property.
/// </remarks>
public class FindLabelQuery
{
    private static readonly string findLabel =
@"query FindLabel($labelName: String!, $organization: String!, $repository: String!) {
  repository(owner:$organization, name:$repository) {
    label(name: $labelName) {
      id
    }
  }
}";
    private readonly IGitHubClient client;
    private readonly string organization;
    private readonly string repository;
    private readonly string labelText;

    private string? labelNodeId = default;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="client">The GitHub client.</param>
    /// <param name="organization">The organization name.</param>
    /// <param name="repository">The repository name.</param>
    /// <param name="labelText">The label text.</param>
    /// <remarks>
    /// When the label contains emojis, the label text should use the `:` text indication
    /// of the emoji label.
    /// </remarks>
    public FindLabelQuery(IGitHubClient client, string organization, string repository, string labelText)
    {
        this.client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Cannot be null");
        this.organization = !string.IsNullOrWhiteSpace(organization)
            ? organization
            : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(organization));
        this.repository = !string.IsNullOrWhiteSpace(repository)
            ? repository
            : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(repository));
        this.labelText = !string.IsNullOrWhiteSpace(labelText)
                        ? labelText
                        : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(labelText));
    }

    /// <summary>
    /// Perform the query.
    /// </summary>
    /// <returns>true if the label was found. False otherwise.</returns>
    public async Task<bool> PerformQuery()
    {
        var findLabelPacket = new GraphQLPacket
        {
            query = findLabel,
            variables =
            {
                ["organization"] = organization,
                ["repository"] = repository,
                ["labelName"] = labelText
            }
        };

        var jsonElement = await client.PostGraphQLRequestAsync(findLabelPacket);
        var labelNode = jsonElement.Descendent("repository", "label", "id");
        var labelID = (labelNode.ValueKind == System.Text.Json.JsonValueKind.String) ? labelNode.GetString() : default;
        labelNodeId = labelID;
        return !string.IsNullOrWhiteSpace(labelNodeId);
    }

    /// <summary>
    /// Access the label after the query has been performed.
    /// </summary>
    public string Id => labelNodeId ?? throw new InvalidOperationException("The label ID was not retrieved");
}
