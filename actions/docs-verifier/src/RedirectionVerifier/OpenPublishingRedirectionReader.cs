using System.Collections.Immutable;
using System.Threading.Tasks;
using BuildVerifier.IO.Abstractions;

namespace RedirectionVerifier;

public class OpenPublishingRedirectionReader
    : BaseMappedConfigurationReader<OpenPublishingRedirections, ImmutableArray<Redirection>>
{
    public OpenPublishingRedirectionReader(string configFileName)
    {
        ConfigurationFileName = configFileName;
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
