using System.Collections.Immutable;
using System.Threading.Tasks;
using BuildVerifier.IO.Abstractions;

namespace RedirectionVerifier
{
    public class OpenPublishingRedirectionReader
        : BaseMappedConfigurationReader<OpenPublishingRedirections, ImmutableArray<Redirection>>
    {
        private readonly string _configFileName;

        public OpenPublishingRedirectionReader(string configFileName)
        {
            _configFileName = configFileName;
        }

        public override string ConfigurationFileName => _configFileName;

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
