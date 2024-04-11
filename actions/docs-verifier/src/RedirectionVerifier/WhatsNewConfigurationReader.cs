using System.Threading.Tasks;
using BuildVerifier.IO.Abstractions;

namespace RedirectionVerifier;

public class WhatsNewConfigurationReader
    : BaseMappedConfigurationReader<WhatsNewConfiguration, string?>
{
    public WhatsNewConfigurationReader()
    {
        ConfigurationFileName = ".whatsnew.json";
    }

    public override async ValueTask<string?> MapConfigurationAsync()
    {
        WhatsNewConfiguration? configuration = await ReadConfigurationAsync();
        if (configuration?.NavigationOptions is not null)
        {
            return configuration.NavigationOptions.WhatsNewPath;
        }

        return default;
    }
}
