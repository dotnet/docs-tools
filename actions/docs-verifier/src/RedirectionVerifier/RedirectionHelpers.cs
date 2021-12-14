using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace RedirectionVerifier
{
    public static class RedirectionHelpers
    {
        public static async Task<ImmutableArray<string>?> GetRedirectionFilesAsync()
        {
            OpenPublishingConfigReader configReader = new();
            return await configReader.MapConfigurationAsync();
        }

        public static async Task<ImmutableArray<string>> GetRedirectionFileNames()
        {
            ImmutableArray<string>? redirectionFileNames = await GetRedirectionFilesAsync();

            // If no redirection files are found in the OPS config, just use the default name.
            if (redirectionFileNames == null)
                redirectionFileNames = ImmutableArray.Create(".openpublishing.redirection.json");

            Console.WriteLine($"The following {redirectionFileNames.Value.Length} redirection files are registered:");
            foreach (string filename in redirectionFileNames)
            {
                Console.WriteLine(filename);
            }

            return redirectionFileNames.Value;
        }
    }
}
