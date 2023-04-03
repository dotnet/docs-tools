using System.Text.Json.Serialization;
using System.Text.Json;

namespace PullRequestSimulations;

public class PullRequest
{
    public string Name { get; set; }

    public ChangeItem[] Items { get; set; }

    public DiscoveryResult[] ExpectedResults { get; set; }

    public PullRequest() { }

    public static PullRequest[] LoadTests(string file)
    {
        JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true, Converters = { new JsonStringEnumConverter() }, ReadCommentHandling = JsonCommentHandling.Skip };

        return JsonSerializer.Deserialize<PullRequest[]>(System.IO.File.ReadAllText(file), options)!;
    }
}

public struct ChangeItem
{
    public ChangeItemType ItemType { get; init; }

    public string Path { get; init; }

    public ChangeItem(ChangeItemType itemType, string path)
    {
        ItemType = itemType;
        Path = path;
    }
}

public struct DiscoveryResult
{
    public int ResultCode { get; init; }
    public string DiscoveredProject { get; init; }

    public DiscoveryResult(int resultCode, string discoveredProject)
    {
        ResultCode = resultCode;
        DiscoveredProject = discoveredProject;
    }
}

public enum ChangeItemType
{
    Create,
    Edit,
    Delete
}