using System;
using System.IO;
using System.Threading.Tasks;
using MarkdownLinksVerifier.Configuration;
using Xunit;

namespace MarkdownLinksVerifier.UnitTests.ConfigurationTests
{
    public class ConfigurationReaderTests
    {
        private const string ConfigurationFileName = ConfigurationReader.ConfigurationFileName;

        [Theory]
        [InlineData(",")]
        [InlineData("")]
        public async Task TestExcludeStartingWith(string trailingComma)
        {
            try
            {
                await File.WriteAllTextAsync(ConfigurationFileName,
@$"{{
  ""excludeStartingWith"": [
    ""xref:"",
    ""~/""{trailingComma}
  ]
}}");
                MarkdownLinksVerifierConfiguration configuration = await ConfigurationReader.GetConfigurationAsync();
                Assert.Equal(2, configuration.ExcludeStartingWith.Length);
                Assert.Contains(@"xref:", configuration.ExcludeStartingWith, StringComparer.Ordinal);
                Assert.Contains("~/", configuration.ExcludeStartingWith, StringComparer.Ordinal);
            }
            finally
            {
                File.Delete(ConfigurationFileName);
            }
        }

        [Fact]
        public async Task TestConfigurationFileDoesNotExist()
        {
            Assert.False(File.Exists(ConfigurationFileName), $"Expected '{ConfigurationFileName}' to not exist.");

            MarkdownLinksVerifierConfiguration configuration = await ConfigurationReader.GetConfigurationAsync();
            Assert.Equal(MarkdownLinksVerifierConfiguration.Empty, configuration);
        }
    }
}
