using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace RedirectionVerifier
{
    public static class RedirectionHelpers
    {
        public static async Task<ImmutableArray<Docset>> GetDocsetsAsync()
        {
            OpenPublishingConfigReader configReader = new();
            return await configReader.MapConfigurationAsync();
        }

        public static ImmutableArray<string> GetRedirectionFileNames()
        {
            Task<ImmutableArray<Docset>> task = Task.Run(async () => await GetDocsetsAsync());
            ImmutableArray<Docset> docsets = task.Result;

            var redirectionFileNames = new List<string>();

            if (docsets.Length == 0)
                throw new InvalidOperationException("No docsets were found in the OPS config file.");

            // If there's more than one docset, combine all the redirection file names.
            foreach (Docset docset in docsets)
            {
                if (docset.RedirectionFiles != null)
                    redirectionFileNames.AddRange(docset.RedirectionFiles);
            }

            // If no redirection files are found in the OPS config, just use the default name.
            if (redirectionFileNames.Count == 0)
                redirectionFileNames.Add(".openpublishing.redirection.json");

            Console.WriteLine($"The following {redirectionFileNames.Count} redirection files are registered:");
            foreach (string filename in redirectionFileNames)
            {
                Console.WriteLine(filename);
            }

            return redirectionFileNames.ToImmutableArray<string>();
        }
    }
}
