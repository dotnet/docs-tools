using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using BuildVerifier.IO.Abstractions;

namespace RedirectionVerifier
{
    public class OpenPublishingConfigReader
        : BaseMappedConfigurationReader<OpenPublishingConfig, ImmutableArray<Docset>>
    {
        public override string ConfigurationFileName => ".openpublishing.publish.config.json";

        public override async ValueTask<ImmutableArray<Docset>> MapConfigurationAsync()
        {
            OpenPublishingConfig? configuration = await ReadConfigurationAsync();
            if (configuration is { Docsets: { Length: > 0 } })
            {
                return configuration.Docsets;
            }

            return default;
        }
    }
}
