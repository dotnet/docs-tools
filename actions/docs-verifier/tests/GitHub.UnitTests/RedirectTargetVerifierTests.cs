using System.Net;
using RedirectionVerifier;
using Xunit;

namespace GitHub.UnitTests;

public class RedirectTargetVerifierTests
{
    [Fact]
    public async Task WriteResultsAsyncReturnsTrueForValidLearnUrlPath()
    {
        string redirectionFilePath = await CreateRedirectionFileAsync("/dotnet");
        try
        {
            using var writer = new StringWriter();
            bool result = await RedirectTargetVerifier.WriteResultsAsync(
                writer,
                redirectionFilePath,
                _ => Task.FromResult<HttpStatusCode?>(HttpStatusCode.OK));

            Assert.True(result);
            Assert.Equal(string.Empty, writer.ToString());
        }
        finally
        {
            File.Delete(redirectionFilePath);
        }
    }

    [Fact]
    public async Task WriteResultsAsyncKeepsLineNumbersAlignedWhenRedirectUrlIsMissing()
    {
        string redirectionFilePath = await CreateRedirectionFileWithContentAsync("""
        {
          "redirections": [
            {
              "source_path": "docs/old.md"
            },
            {
              "source_path": "docs/new.md",
              "redirect_url": "/missing"
            }
          ]
        }
        """);

        try
        {
            using var writer = new StringWriter();
            bool result = await RedirectTargetVerifier.WriteResultsAsync(
                writer,
                redirectionFilePath,
                _ => Task.FromResult<HttpStatusCode?>(HttpStatusCode.NotFound));

            string output = writer.ToString();
            Assert.False(result);
            Assert.Contains("Redirection has an empty 'redirect_url'.", output, StringComparison.Ordinal);
            Assert.DoesNotContain("line=", output[..output.IndexOf("Redirection has an empty 'redirect_url'.", StringComparison.Ordinal)], StringComparison.Ordinal);
            Assert.Contains(",line=8::Redirect target returns 404: '/missing'.", output, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(redirectionFilePath);
        }
    }

    [Fact]
    public async Task WriteResultsAsyncSkipsNonLearnUrlTargets()
    {
        string redirectionFilePath = await CreateRedirectionFileAsync("not-a-valid-url");
        bool statusProviderCalled = false;

        try
        {
            using var writer = new StringWriter();
            bool result = await RedirectTargetVerifier.WriteResultsAsync(
                writer,
                redirectionFilePath,
                _ =>
                {
                    statusProviderCalled = true;
                    return Task.FromResult<HttpStatusCode?>(HttpStatusCode.OK);
                });

            Assert.True(result);
            Assert.Equal(string.Empty, writer.ToString());
            Assert.False(statusProviderCalled);
        }
        finally
        {
            File.Delete(redirectionFilePath);
        }
    }

    [Fact]
    public async Task WriteResultsAsyncReturnsFalseFor404Url()
    {
        string redirectionFilePath = await CreateRedirectionFileAsync("/missing");
        try
        {
            using var writer = new StringWriter();
            bool result = await RedirectTargetVerifier.WriteResultsAsync(
                writer,
                redirectionFilePath,
                _ => Task.FromResult<HttpStatusCode?>(HttpStatusCode.NotFound));

            Assert.False(result);
            Assert.Contains("returns 404", writer.ToString(), StringComparison.Ordinal);
            Assert.Contains(",line=5::Redirect target returns 404", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(redirectionFilePath);
        }
    }

    [Fact]
    public async Task WriteResultsAsyncReturnsFalseWhenLearnUrlCannotBeVerified()
    {
        string redirectionFilePath = await CreateRedirectionFileAsync("/dotnet");

        try
        {
            using var writer = new StringWriter();
            bool result = await RedirectTargetVerifier.WriteResultsAsync(
                writer,
                redirectionFilePath,
                _ => Task.FromResult<HttpStatusCode?>(null));

            Assert.False(result);
            Assert.Contains("Unable to verify 'redirect_url'", writer.ToString(), StringComparison.Ordinal);
            Assert.Contains(",line=5::Unable to verify 'redirect_url'", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(redirectionFilePath);
        }
    }

    private static async Task<string> CreateRedirectionFileAsync(string redirectUrl)
    {
        string content = $$"""
        {
          "redirections": [
            {
              "source_path": "docs/old.md",
              "redirect_url": "{{redirectUrl}}"
            }
          ]
        }
        """;

        return await CreateRedirectionFileWithContentAsync(content);
    }

    private static async Task<string> CreateRedirectionFileWithContentAsync(string content)
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"redirect-{Guid.NewGuid():N}.json");

        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

}
