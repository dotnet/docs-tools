namespace WhatsNew.Infrastructure;

/// <summary>
/// Constants used throughout the what's new solution's projects.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The directory name in the WhatsNew.Infrastructure project which stores the
    /// JSON configuration files for each docset.
    /// </summary>
    public const string ConfigurationDirectory = "Configuration";

    /// <summary>
    /// The name of the default directory to which the generated Markdown file is 
    /// written.
    /// </summary>
    public const string MarkdownFileDirectoryName = "whatsnew";

    /// <summary>
    /// The suffix used to indicate a private GitHub repository.
    /// </summary>
    public const string PrivateRepoNameSuffix = "-pr";
}
