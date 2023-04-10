using System.Text.Json.Serialization;
using System.Text.Json;

namespace PullRequestSimulations;

/// <summary>
/// A data type for the data.json file. Represents a pull request from GitHub
/// </summary>
public struct PullRequest
{
    /// <summary>
    /// The name.
    /// </summary>
    [JsonRequired]
    public string Name { get; init; } = "";

    /// <summary>
    /// The change request items, such as adding a file.
    /// </summary>
    [JsonRequired]
    public ChangeItem[] Items { get; init; } = Array.Empty<ChangeItem>();

    /// <summary>
    /// The scanner results this PR should produce.
    /// </summary>
    public ExpectedResult[]? ExpectedResults { get; init; } = Array.Empty<ExpectedResult>();

    /// <summary>
    /// How many items in this PR won't produce a scanner result.
    /// </summary>
    public int CountOfEmptyResults { get; init; } = default;

    /// <summary>
    /// Creates the default implementation of this object.
    /// </summary>
    public PullRequest() { }

    /// <summary>
    /// Loads an array of pull requests from the specified JSON file.
    /// </summary>
    /// <param name="file">The file to load.</param>
    /// <returns>An array of pull requests.</returns>
    public static PullRequest[] LoadTests(string file)
    {
        JsonSerializerOptions options = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() }, ReadCommentHandling = JsonCommentHandling.Skip };

        return JsonSerializer.Deserialize<PullRequest[]>(File.ReadAllText(file), options)!;
    }
}
