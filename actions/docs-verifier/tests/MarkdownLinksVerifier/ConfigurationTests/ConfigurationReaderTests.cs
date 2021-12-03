using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MarkdownLinksVerifier.Configuration;
using Xunit;

namespace MarkdownLinksVerifier.UnitTests.ConfigurationTests
{
    public class ConfigurationReaderTests
    {
        private static readonly List<string> ConfigurationFileNames =
            new ConfigurationReader().ConfigurationFileNames;

        [Theory]
        [InlineData(",")]
        [InlineData("")]
        public async Task TestExcludeStartingWith(string trailingComma)
        {
            try
            {
                await File.WriteAllTextAsync(ConfigurationFileNames[0],
@$"{{
  ""excludeStartingWith"": [
    ""xref:"",
    ""~/""{trailingComma}
  ]
}}");
                MarkdownLinksVerifierConfiguration? configuration =
                    await new ConfigurationReader().ReadConfigurationAsync();
                Assert.Equal(2, configuration?.ExcludeStartingWith.Length);
                Assert.Contains(@"xref:", configuration?.ExcludeStartingWith, StringComparer.Ordinal);
                Assert.Contains("~/", configuration?.ExcludeStartingWith, StringComparer.Ordinal);
            }
            finally
            {
                File.Delete(ConfigurationFileNames[0]);
            }
        }

        [Fact]
        public async Task TestConfigurationFileDoesNotExist()
        {
            Assert.False(File.Exists(ConfigurationFileNames[0]), $"Expected '{ConfigurationFileNames[0]}' to not exist.");

            MarkdownLinksVerifierConfiguration? configuration =
                await new ConfigurationReader().ReadConfigurationAsync();
            Assert.Null(configuration);
        }
    }
}
