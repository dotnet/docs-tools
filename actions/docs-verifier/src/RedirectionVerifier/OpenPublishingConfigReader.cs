using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using BuildVerifier.IO.Abstractions;

namespace RedirectionVerifier
{
    public class OpenPublishingConfigReader
        : BaseMappedConfigurationReader<OpenPublishingDocsets, ImmutableArray<Docset>>
    {
        public override List<string> ConfigurationFileNames => new List<string>(new string[] { ".openpublishing.publish.config.json" });

        public override async ValueTask<ImmutableArray<Docset>> MapConfigurationAsync()
        {
            OpenPublishingDocsets? configuration = await ReadConfigurationAsync();
            if (configuration is { Docsets: { Length: > 0 } })
            {
                return configuration.Docsets;
            }

            return default;
        }
    }
}
