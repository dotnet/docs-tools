using System.Collections.Generic;
using BuildVerifier.IO.Abstractions;

namespace MarkdownLinksVerifier.Configuration
{
    public class ConfigurationReader : BaseConfigurationReader<MarkdownLinksVerifierConfiguration>
    {
        public override List<string> ConfigurationFileNames => new List<string>(new string[] { "markdown-links-verifier-config.json" });
    }
}
