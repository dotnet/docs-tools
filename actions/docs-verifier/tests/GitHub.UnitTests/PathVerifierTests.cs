using DocfxVerifier;
using Xunit;

namespace GitHub.UnitTests;

public class PathVerifierTests
{
    private static readonly SemaphoreSlim s_currentDirectoryLock = new(1, 1);

    [Fact]
    public async Task WriteResultsAsyncReturnsTrueForValidPaths()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            Directory.CreateDirectory("docs");
            Directory.CreateDirectory(Path.Combine("templates", "default"));
            await File.WriteAllTextAsync("docfx.json", """
            {
              "build": {
                "content": [
                  {
                    "files": ["**/*.md", "**/*.yml"],
                    "src": "docs",
                    "exclude": ["**/includes/**"]
                  }
                ],
                "template": ["templates/default"],
                "xref": ["https://learn.microsoft.com/dotnet/.xrefmap.json"]
              }
            }
            """);

            using var writer = new StringWriter();
            bool result = await PathVerifier.WriteResultsAsync(writer);

            Assert.True(result);
            Assert.Equal(string.Empty, writer.ToString());
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(testRoot, recursive: true);
            s_currentDirectoryLock.Release();
        }
    }

    [Fact]
    public async Task WriteResultsAsyncReturnsFalseForInvalidPaths()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            await File.WriteAllTextAsync("docfx.json", """
            {
              "build": {
                "content": [
                  {
                    "files": ["**/*.md"],
                    "src": "missing-folder"
                  }
                ]
              }
            }
            """);

            using var writer = new StringWriter();
            bool result = await PathVerifier.WriteResultsAsync(writer);

            Assert.False(result);
            Assert.Contains("Path 'missing-folder' is invalid", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(testRoot, recursive: true);
            s_currentDirectoryLock.Release();
        }
    }

    [Fact]
    public async Task WriteResultsAsyncFindsDocfxInSubdirectory()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            Directory.CreateDirectory("docs");
            Directory.CreateDirectory("docs/content");
            Directory.CreateDirectory(Path.Combine("docs", "templates", "default"));
            await File.WriteAllTextAsync(Path.Combine("docs", "docfx.json"), """
            {
              "build": {
                "content": [
                  {
                    "files": ["**/*.md"],
                    "src": "content"
                  }
                ],
                "template": ["templates/default"]
              }
            }
            """);

            using var writer = new StringWriter();
            bool result = await PathVerifier.WriteResultsAsync(writer);

            Assert.True(result);
            Assert.Equal(string.Empty, writer.ToString());
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(testRoot, recursive: true);
            s_currentDirectoryLock.Release();
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"path-verifier-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
