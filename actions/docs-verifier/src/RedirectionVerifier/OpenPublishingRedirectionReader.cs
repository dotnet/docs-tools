using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using BuildVerifier.IO.Abstractions;

namespace RedirectionVerifier
{
    public class OpenPublishingRedirectionReader
        : BaseMappedConfigurationReader<OpenPublishingRedirections, ImmutableArray<Redirection>>
    {
        public override List<string> ConfigurationFileNames
        {
            get
            {
                var result = new List<string>();

                Task<ImmutableArray<Docset>> task = Task.Run(async () => await GetDocsetsAsync());
                ImmutableArray<Docset> docsets = task.Result;

                // If there's more than one docset, just combine all the redirection file names.
                foreach (Docset docset in docsets)
                {
                    if (docset.RedirectionFiles != null)
                        result.AddRange(docset.RedirectionFiles);
                }

                // If no redirection files are found in the OPS config, just use the default name.
                if (result.Count == 0)
                    result.Add(".openpublishing.redirection.json");

                return result;
            }
        }

        private static async Task<ImmutableArray<Docset>> GetDocsetsAsync()
        {
            OpenPublishingConfigReader configReader = new();
            return await configReader.MapConfigurationAsync();
        }

        public override async ValueTask<ImmutableArray<Redirection>> MapConfigurationAsync()
        {
            OpenPublishingRedirections? configuration = await ReadConfigurationAsync();
            if (configuration is { Redirections: { Length: > 0 } })
            {
                return configuration.Redirections;
            }

            return default;
        }
    }
}
