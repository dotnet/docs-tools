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
    public async Task WriteResultsAsyncValidatesFileMetadataPaths()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            Directory.CreateDirectory(Path.Combine("docs", "valid"));

            await File.WriteAllTextAsync("docfx.json", """
            {
              "build": {
                "content": [
                  {
                    "files": ["**/*.md"],
                    "src": "docs"
                  }
                ],
                "fileMetadata": {
                  "ms.author": {
                    "docs/valid/**/**.{md,yml}": "someone",
                    "missing/path/**/**.{md,yml}": "someone"
                  }
                }
              }
            }
            """);

            using var writer = new StringWriter();
            bool result = await PathVerifier.WriteResultsAsync(writer);
            string output = writer.ToString();

            Assert.False(result);
            Assert.Contains("Path 'missing/path/**/**.{md,yml}' is invalid", output, StringComparison.Ordinal);
            Assert.DoesNotContain("Path 'docs/valid/**/**.{md,yml}' is invalid", output, StringComparison.Ordinal);
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

    [Fact]
    public async Task WriteResultsAsyncUsesSpecifiedDocfxPath()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);

            Directory.CreateDirectory("valid-docs");

            await File.WriteAllTextAsync("docfx.json", """
            {
              "build": {
                "content": [
                  {
                    "files": ["**/*.md"],
                    "src": "missing-root-folder"
                  }
                ]
              }
            }
            """);

            string modifiedDocfxPath = Path.Combine("valid-docs", "docfx.json");
            await File.WriteAllTextAsync(modifiedDocfxPath, """
            {
              "build": {
                "content": [
                  {
                    "files": ["**/*.md"],
                    "src": "."
                  }
                ]
              }
            }
            """);

            using var writer = new StringWriter();
            bool result = await PathVerifier.WriteResultsAsync(writer, modifiedDocfxPath);

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
    public async Task WriteResultsAsyncValidatesFilesRelativeToMappingSrc()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            Directory.CreateDirectory(Path.Combine("docs", "content", "guides"));
            await File.WriteAllTextAsync(Path.Combine("docs", "content", "guides", "a.md"), "# title");

            string configurationPath = Path.Combine("docs", "docfx.json");
            await File.WriteAllTextAsync(configurationPath, """
            {
              "build": {
                "content": [
                  {
                    "src": "content",
                    "files": ["guides/a.md"]
                  }
                ]
              }
            }
            """);

            using var writer = new StringWriter();
            bool result = await PathVerifier.WriteResultsAsync(writer, configurationPath);

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
    public async Task WriteResultsAsyncAllowsParentRelativePathWhenInsideRepository()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            Directory.CreateDirectory(Path.Combine("shared", "templates"));
            Directory.CreateDirectory("docs");

            string configurationPath = Path.Combine("docs", "docfx.json");
            await File.WriteAllTextAsync(configurationPath, """
            {
              "build": {
                "template": ["../shared/templates"]
              }
            }
            """);

            using var writer = new StringWriter();
            bool result = await PathVerifier.WriteResultsAsync(writer, configurationPath);

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
    public async Task WriteResultsAsyncSupportsFileMappingShorthandAndObjectForms()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            Directory.CreateDirectory(Path.Combine("docs", "articles"));
            Directory.CreateDirectory(Path.Combine("docs", "assets", "images"));
            Directory.CreateDirectory(Path.Combine("docs", "content", "overwrite"));
            await File.WriteAllTextAsync(Path.Combine("docs", "assets", "images", "logo.png"), "binary");

            string configurationPath = Path.Combine("docs", "docfx.json");
            await File.WriteAllTextAsync(configurationPath, """
            {
              "build": {
                "content": ["articles/**/*.md"],
                "resource": [
                  {
                    "src": "assets",
                    "files": ["images/logo.png"]
                  }
                ],
                "overwrite": [
                  {
                    "src": "content",
                    "files": ["overwrite/**/*.md"]
                  }
                ]
              }
            }
            """);

            using var writer = new StringWriter();
            bool result = await PathVerifier.WriteResultsAsync(writer, configurationPath);

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
    public async Task WriteResultsAsyncIgnoresArbitraryMetadataPropertyNames()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            Directory.CreateDirectory("docs");

            await File.WriteAllTextAsync("docfx.json", """
            {
              "build": {
                "content": [
                  {
                    "src": "docs",
                    "files": ["**/*.md"]
                  }
                ]
              },
              "globalMetadata": {
                "src": "not-a-docfx-path"
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
    public async Task WriteResultsAsyncAllowsTrailingCommas()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            Directory.CreateDirectory("docs");

            await File.WriteAllTextAsync("docfx.json", """
            {
              "build": {
                "content": [
                  {
                    "src": "docs",
                    "files": ["**/*.md",],
                  },
                ],
              },
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
