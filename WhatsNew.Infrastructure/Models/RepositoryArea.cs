namespace WhatsNew.Infrastructure.Models;

/// <summary>
/// Represents a name-value pair, where <see cref="Name"/> represents the
/// directory name(s) within the repo and <see cref="Heading"/> represents the
/// heading text corresponding to that directory.
/// </summary>
public class RepositoryArea
{
    /// <summary>
    /// A collection of directory names in the GitHub repository.
    /// </summary>
    public IEnumerable<string> Names { get; init; } = null!;

    /// <summary>
    /// The heading text for the area/directory.
    /// </summary>
    public string Heading { get; init; } = null!;
}
