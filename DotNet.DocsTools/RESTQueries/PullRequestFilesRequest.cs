using DotNetDocs.Tools.GitHubCommunications;
using System.Text.Json;

namespace DotNetDocs.Tools.RESTQueries;

/// <summary>
/// Manage the interaction for the PR Files GitHub request
/// </summary>
/// <remarks>
/// To use this class, create an instance with the arguments
/// needed. Then, perform the query. The return indicates success
/// or failure.
/// In the case of success, you can enumerate the results. In the case
/// of failure, you can access the message indicating the reason for
/// the failure.
/// </remarks>
public class PullRequestFilesRequest
{
    /// <summary>
    /// This enum describes the status of each file
    /// in a Pull Request.
    /// </summary>
    public enum FileStatus
    {
        /// <summary>
        /// This file was added.
        /// </summary>
        Added,
        /// <summary>
        /// This file was modified.
        /// </summary>
        Modified,
        /// <summary>
        /// This file was removed.
        /// </summary>
        Removed
    }

    /// <summary>
    /// This nested struct defines the contents of a node in the REST response for pull request files.
    /// </summary>
    /// <remarks>
    /// The storage is the JsonElement, but the public API processes
    /// those elements to provide the C# types as properties.
    /// </remarks>
    public readonly struct PullRequestFile
    {
        private readonly JsonElement node;

        /// <summary>
        /// Construct from a JsonElement
        /// </summary>
        /// <param name="node">The node for this file.</param>
        /// <remarks>
        /// The node is checked to ensure it is an object. No
        /// other validation is done.
        /// </remarks>
        public PullRequestFile(JsonElement node) => 
            this.node = (node.ValueKind == JsonValueKind.Object) ?
            node : throw new ArgumentException(message: "Node is not an object", paramName: nameof(node));
        
        /// <summary>
        /// The public sha of this pull request file.
        /// </summary>
        public string Sha => node.GetProperty("sha").GetString() ?? throw new InvalidOperationException("Sha property not found");
        /// <summary>
        /// The path (relative to the repo root) of this pull request file.
        /// </summary>
        public string Filename=> node.GetProperty("filename").GetString() ?? throw new InvalidOperationException("Filename property not found");
        /// <summary>
        /// The status of the changes for this filename.
        /// </summary>
        public FileStatus Status => node.GetProperty("status").GetString() switch
        {
            "added" => FileStatus.Added,
            "modified" => FileStatus.Modified,
            "renamed" => FileStatus.Modified,
            "removed" => FileStatus.Removed,
            _ => (FileStatus)(-1),
        };
        /// <summary>
        /// The number of additions for this file.
        /// </summary>
        public int Additions => node.GetProperty("additions").GetInt32();
        /// <summary>
        /// The number of deletions for this file.
        /// </summary>
        public int Deletions => node.GetProperty("deletions").GetInt32();
        /// <summary>
        /// The number of modifications in this file. Note that this is the sum of additions and deletions.
        /// </summary>
        public int Changes => node.GetProperty("changes").GetInt32();
        /// <summary>
        /// The full URL of the blob for this commit.
        /// </summary>
        public string BlobUrl => node.GetProperty("blob_url").GetString() ?? throw new InvalidOperationException("BlobUrl not found");
        /// <summary>
        /// The URL of the raw file for this commit.
        /// </summary>
        public string RawUrl => node.GetProperty("raw_url").GetString() ?? throw new InvalidOperationException("RawUrl not found");
        /// <summary>
        /// The URL for the REST API for this file at this commit.
        /// </summary>
        public string ContentsUrl => node.GetProperty("contents_url").GetString() ?? throw new InvalidOperationException("contents URL not found");
        /// <summary>
        /// The patch contents for this file.
        /// </summary>
        public string Patch => node.GetProperty("patch").GetString() ?? throw new InvalidOperationException("Patch node not found");
    }

    private readonly IGitHubClient client;
    private readonly string owner;
    private readonly string repo;
    private readonly int number;
    private JsonElement root;

    /// <summary>
    /// Construct a pull request files query request
    /// </summary>
    /// <param name="client">The request client.</param>
    /// <param name="owner">The owner segment of the path</param>
    /// <param name="repo">The repository segment of the path</param>
    /// <param name="number">The pull request number</param>
    public PullRequestFilesRequest(IGitHubClient client, string owner, string repo, int number)
    {
        this.client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Client cannot be null"); ;
        this.owner = (string.IsNullOrWhiteSpace(owner)
            ? throw new ArgumentException(message: "Not a valid URL component", paramName: nameof(owner))
            : owner);
        this.repo = (string.IsNullOrWhiteSpace(repo)
            ? throw new ArgumentException(message: "Not a valid URL component", paramName: nameof(repo))
            : repo);
        this.number = (number > 0) 
            ? number 
            : throw new ArgumentOutOfRangeException(paramName: nameof(number), message: "The PR number must be postive");
    }

    /// <summary>
    /// Perform the query. 
    /// </summary>
    /// <returns>The task value is true if the request succeeded, false if it failed.</returns>
    /// <remarks>
    /// This API should only be called once for each object. If you make the request again,
    /// the object will request updates from the GitHub API.
    /// </remarks>
    public async Task<bool> PerformQueryAsync()
    {
        var jsonDocument = await client.GetReposRESTRequestAsync
            (owner, repo, "pulls", number.ToString(), "files");
        root = jsonDocument.RootElement;
        return root.ValueKind switch
        {
            JsonValueKind.Array => true,
            JsonValueKind.Object => false,
            _ => throw new InvalidOperationException($"Unexpected result: {root.ToString()}"),
        };
    }

    /// <summary>
    /// The error message, in case of failure.
    /// </summary>
    /// <remarks>
    /// This throws an InvalidOperationException if the request has not
    /// been made, or if the request has succeeded.
    /// </remarks>
    public string ErrorMessage => root.GetProperty("message").GetString() ?? throw new InvalidOperationException("message not found");

    /// <summary>
    /// Access the file nodes from this query.
    /// </summary>
    /// <remarks>
    /// This throws an InvalidOperationException if the request has not
    /// been made, or if the request has failed.
    /// </remarks>
    public IEnumerable<PullRequestFile> Files => 
        from element in root.EnumerateArray()
        select new PullRequestFile(element);
    
    /// <summary>
    /// The string representation of this object
    /// </summary>
    /// <returns>A string of the format "owner/repo/pull/number"</returns>
    public override string ToString() => $"{owner}/{repo}/pull/{number}";
}
