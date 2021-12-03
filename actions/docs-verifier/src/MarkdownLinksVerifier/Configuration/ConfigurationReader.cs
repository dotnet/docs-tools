using BuildVerifier.IO.Abstractions;

namespace MarkdownLinksVerifier.Configuration
{
    public class ConfigurationReader : BaseConfigurationReader<MarkdownLinksVerifierConfiguration>
    {
        public override string ConfigurationFileName => "markdown-links-verifier-config.json";
    }
}
