using System;
using System.IO;
using System.Threading.Tasks;
using MarkdownLinksVerifier.Configuration;
using MarkdownLinksVerifier.UnitTests.LinkValidatorTests;
using Xunit;

namespace MarkdownLinksVerifier.UnitTests.ConfigurationTests;

public class ConfigurationReaderTests
{
    private static readonly string? ConfigurationFileName =
        new ConfigurationReader().ConfigurationFileName;

    [Theory]
    [InlineData(",")]
    [InlineData("")]
    public async Task TestExcludeStartingWith(string trailingComma)
    {
        // Without this, we get a race when running tests in parallel. The race happens as follows:
        // 1. We first write the json contents into ConfigurationFileName in the current directory.
        // 2. A test using Workspace is run and changes the current directory to WorkspaceTests
        // 3. ReadConfigurationAsync will fail because the file doesn't appear to exist.
        using var workspace = new Workspace();
        await workspace.InitializeAsync();

        try
        {
            await File.WriteAllTextAsync(ConfigurationFileName!,
@$"{{
  ""excludeStartingWith"": [
    ""xref:"",
    ""~/""{trailingComma}
  ]
}}");
            MarkdownLinksVerifierConfiguration? configuration =
                await new ConfigurationReader().ReadConfigurationAsync();
            Assert.NotNull(configuration);
            Assert.Equal(2, configuration.ExcludeStartingWith.Length);
            Assert.Contains(@"xref:", configuration.ExcludeStartingWith, StringComparer.Ordinal);
            Assert.Contains("~/", configuration.ExcludeStartingWith, StringComparer.Ordinal);
        }
        finally
        {
            if (ConfigurationFileName != null)
            {
                File.Delete(ConfigurationFileName);
            }
        }
    }

    [Fact]
    public async Task TestConfigurationFileDoesNotExist()
    {
        Assert.False(File.Exists(ConfigurationFileName), $"Expected '{ConfigurationFileName}' to not exist.");

        MarkdownLinksVerifierConfiguration? configuration =
            await new ConfigurationReader().ReadConfigurationAsync();
        Assert.Null(configuration);
    }
}
