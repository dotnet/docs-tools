using BuildVerifier.IO.Abstractions;

namespace MarkdownLinksVerifier.Configuration;

public class ConfigurationReader : BaseConfigurationReader<MarkdownLinksVerifierConfiguration>
{
    public ConfigurationReader()
    {
        ConfigurationFileName = "markdown-links-verifier-config.json";
    }
}
