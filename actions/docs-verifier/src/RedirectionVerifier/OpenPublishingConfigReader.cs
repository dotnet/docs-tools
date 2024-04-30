using System.Collections.Immutable;
using BuildVerifier.IO.Abstractions;

namespace RedirectionVerifier;

public class OpenPublishingConfigReader
    : BaseMappedConfigurationReader<OpenPublishingConfig, ImmutableArray<string>?>
{
    public OpenPublishingConfigReader()
    {
        ConfigurationFileName = ".openpublishing.publish.config.json";
    }

    public override async ValueTask<ImmutableArray<string>?> MapConfigurationAsync()
    {
        OpenPublishingConfig? configuration = await ReadConfigurationAsync();
        if (configuration is { RedirectionFiles: { Length: > 0 } })
        {
            return configuration.RedirectionFiles;
        }

        return default;
    }
}
