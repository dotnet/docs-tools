using System.Net;
using RedirectionVerifier;
using Xunit;

namespace GitHub.UnitTests;

public class RedirectTargetVerifierTests
{
    [Fact]
    public async Task WriteResultsAsyncReturnsTrueForValidUrl()
    {
        string redirectionFilePath = await CreateRedirectionFileAsync("https://learn.microsoft.com/dotnet");
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
    public async Task WriteResultsAsyncReturnsFalseForInvalidUrl()
    {
        string redirectionFilePath = await CreateRedirectionFileAsync("not-a-valid-url");
        try
        {
            using var writer = new StringWriter();
            bool result = await RedirectTargetVerifier.WriteResultsAsync(writer, redirectionFilePath);

            Assert.False(result);
            Assert.Contains("Invalid 'redirect_url'", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(redirectionFilePath);
        }
    }

    [Fact]
    public async Task WriteResultsAsyncReturnsFalseFor404Url()
    {
        string redirectionFilePath = await CreateRedirectionFileAsync("https://learn.microsoft.com/missing");
        try
        {
            using var writer = new StringWriter();
            bool result = await RedirectTargetVerifier.WriteResultsAsync(
                writer,
                redirectionFilePath,
                _ => Task.FromResult<HttpStatusCode?>(HttpStatusCode.NotFound));

            Assert.False(result);
            Assert.Contains("returns 404", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(redirectionFilePath);
        }
    }

    [Fact]
    public async Task WriteResultsAsyncReturnsFalseForLocalAddressTarget()
    {
        string redirectionFilePath = await CreateRedirectionFileAsync("http://127.0.0.1/internal");
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

            Assert.False(result);
            Assert.Contains("Disallowed 'redirect_url' target", writer.ToString(), StringComparison.Ordinal);
            Assert.False(statusProviderCalled);
        }
        finally
        {
            File.Delete(redirectionFilePath);
        }
    }

    private static async Task<string> CreateRedirectionFileAsync(string redirectUrl)
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"redirect-{Guid.NewGuid():N}.json");
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

        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

}
