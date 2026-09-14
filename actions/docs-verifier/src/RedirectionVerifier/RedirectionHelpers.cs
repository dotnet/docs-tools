using System.Collections.Immutable;

namespace RedirectionVerifier;

public static class RedirectionHelpers
{
    public static async Task<ImmutableArray<string>?> GetRedirectionFilesAsync()
    {
        OpenPublishingConfigReader configReader = new();
        return await configReader.MapConfigurationAsync();
    }

    public static async Task<ImmutableArray<string>> GetRedirectionFileNames()
    {
        // If no redirection files are found in the OPS config, just use the default name.
        ImmutableArray<string> redirectionFileNames = await GetRedirectionFilesAsync() ?? 
            [".openpublishing.redirection.json"];
        Console.WriteLine($"Found {redirectionFileNames.Length} registered redirection files.");

        return redirectionFileNames;
    }
}
