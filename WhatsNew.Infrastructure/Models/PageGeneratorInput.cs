namespace WhatsNew.Infrastructure.Models;

/// <summary>
/// An encapsulation of the arguments and options accepted by the command-line tool.
/// </summary>
public class PageGeneratorInput
{
    private string _dateEnd = null!;
    private string _dateStart = null!;
    private string _owner = null!;
    private string _repository = null!;

    /// <summary>
    /// The owner/organization of the GitHub repository to be processed.
    /// </summary>
    /// <example>dotnet</example>
    public string Owner
    {
        get => _owner;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"The parameter '{nameof(Owner)}' cannot be null, empty, or whitespace.");

            _owner = value.Trim();
        }
    }
    /// <summary>
    /// The name of the GitHub repository within the organization specified
    /// in <see cref="Owner"/>.
    /// </summary>
    /// <example>AspNetCore.Docs</example>
    public string Repository
    {
        get => _repository;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"The parameter '{nameof(Repository)}' cannot be null, empty, or whitespace.");

            _repository = value.Trim();
        }
    }

    /// <summary>
    /// The name of the branch within the repository specified in
    /// <see cref="Repository"/>.
    /// </summary>
    /// <example>main</example>
    public string? Branch { get; set; }

    /// <summary>
    /// The docset directory name. This setting is used when the <see cref="Owner"/>
    /// and <see cref="Repository"/> aren't enough to select the appropriate repo config
    /// file. This setting is useful for the MicrosoftDocs/azure-docs-pr repo.
    /// </summary>
    /// <example>cognitive-services</example>
    public string? DocSet { get; init; }

    /// <summary>
    /// The directory path to which the generated Markdown file should be written.
    /// </summary>
    /// <example>C:\whatsnew</example>
    public string? SaveDir { get; init; }

    /// <summary>
    /// The directory path to the root of the repository.
    /// </summary>
    /// <example>../dotnet/docs/</example>
    public string RepoRoot { get; init; } = "./";

    /// <summary>
    /// An absolute path to a local JSON configuration file.
    /// </summary>
    /// <example>C:\config\.whatsnew.json</example>
    public string? LocalConfig { get; init; }

    /// <summary>
    /// The range end date in a valid format.
    /// </summary>
    /// <remarks>
    /// Examples of valid formats include yyyy-MM-dd and MM/dd/yyyy.
    /// </remarks>
    public string DateEnd
    {
        get => _dateEnd;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    $"The parameter cannot be null, empty, or whitespace.", nameof(DateEnd));

            _dateEnd = value.Trim();
        }
    }

    /// <summary>
    /// The range start date in a valid format.
    /// </summary>
    /// <remarks>
    /// Examples of valid formats include yyyy-MM-dd and MM/dd/yyyy.
    /// </remarks> 
    public string DateStart
    {
        get => _dateStart;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    $"The parameter cannot be null, empty, or whitespace.", nameof(DateStart));

            _dateStart = value.Trim();
        }
    }

    /// <summary>
    /// Default date range when none is provided on the command line.
    /// </summary>
    /// <remarks>
    /// If the custom command line doesn't add any date information, this
    /// string contains "Month, YYYY". Otherwise it is null. In the case
    /// where it is null, the dates are formatted from the start and end dates.
    /// </remarks>
    public string? MonthYear { get; init; }
}
