namespace DotNetDocs.RepoMan.GitHubCommands;

/// <summary>
/// Provides repository information.
/// </summary>
public interface IRepositoryProvider
{
    /// <summary>
    /// The repository ID.
    /// </summary>
    public long RepositoryId { get; }

    /// <summary>
    /// The name of the repository.
    /// </summary>
    public string RepositoryName { get; }

    /// <summary>
    /// The owner of the repository.
    /// </summary>
    public string RepositoryOwner { get; }
}
