using DocfxVerifier;
using Xunit;

namespace GitHub.UnitTests;

public class PathVerifierTests
{
    private static readonly SemaphoreSlim s_currentDirectoryLock = new(1, 1);

    [Fact]
    public async Task WriteResultsAsyncReturnsTrueForValidFileMetadataPaths()
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
                "fileMetadata": {
                  "ms.author": {
                    "docs/valid/**/**.{md,yml}": "someone"
                  }
                }
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
    public async Task WriteResultsAsyncIgnoresMissingFileMetadataPathWhenItMatchesExternalContentSrc()
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
                    "src": "_shared-content",
                    "files": ["**/*.md"]
                  }
                ],
                "fileMetadata": {
                  "ms.author": {
                    "_shared-content/**": "someone"
                  }
                }
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
    public async Task WriteResultsAsyncIgnoresWildcardParentPathWhenItMatchesExternalContentSrc()
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
                    "src": "_csharplang/proposals",
                    "files": ["**/*.md"]
                  }
                ],
                "fileMetadata": {
                  "ms.author": {
                    "_csharplang/**.*": "someone"
                  }
                }
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
    public async Task WriteResultsAsyncReturnsFalseForExactFileMetadataAncestorOfExternalContentSrc()
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
                    "src": "_csharplang/proposals",
                    "files": ["**/*.md"]
                  }
                ],
                "fileMetadata": {
                  "ms.author": {
                    "_csharplang": "someone"
                  }
                }
              }
            }
            """);

            using var writer = new StringWriter();
            bool result = await PathVerifier.WriteResultsAsync(writer);

            Assert.False(result);
            Assert.Contains("Invalid path '_csharplang'.", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(testRoot, recursive: true);
            s_currentDirectoryLock.Release();
        }
    }

    [Fact]
    public async Task WriteResultsAsyncReturnsFalseForInvalidFileMetadataPaths()
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
                "fileMetadata": {
                  "ms.author": {
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
            Assert.Contains("Invalid path 'missing/path/**/**.{md,yml}'.", output, StringComparison.Ordinal);
            Assert.Contains("::error file=docfx.json,line=5::Invalid path 'missing/path/**/**.{md,yml}'.", output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(testRoot, recursive: true);
            s_currentDirectoryLock.Release();
        }
    }

    [Fact]
    public async Task WriteResultsAsyncIgnoresNonFileMetadataPathEntries()
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
                ],
                "template": ["missing-template"],
                "fileMetadata": {
                  "ms.author": {
                    "**/*.md": "someone"
                  }
                }
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
    public async Task WriteResultsAsyncFindsDocfxInSubdirectory()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            Directory.CreateDirectory(Path.Combine("docs", "content", "guides"));
            await File.WriteAllTextAsync(Path.Combine("docs", "docfx.json"), """
            {
              "build": {
                "fileMetadata": {
                  "ms.topic": {
                    "content/**": "conceptual"
                  }
                }
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

            await File.WriteAllTextAsync("docfx.json", """
            {
              "build": {
                "fileMetadata": {
                  "ms.author": {
                    "missing-root-folder/**": "someone"
                  }
                }
              }
            }
            """);

            Directory.CreateDirectory(Path.Combine("valid-docs", "valid"));
            string modifiedDocfxPath = Path.Combine("valid-docs", "docfx.json");
            await File.WriteAllTextAsync(modifiedDocfxPath, """
            {
              "build": {
                "fileMetadata": {
                  "ms.author": {
                    "valid/**": "someone"
                  }
                }
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
    public async Task WriteResultsAsyncReturnsFalseForMissingSpecifiedDocfxPathWithFileAnnotation()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            string missingDocfxPath = Path.Combine("missing-docs", "docfx.json");
            using var writer = new StringWriter();

            bool result = await PathVerifier.WriteResultsAsync(writer, missingDocfxPath);

            string output = writer.ToString();
            Assert.False(result);
            Assert.Contains("docfx.json file", output, StringComparison.Ordinal);
            Assert.Contains("file=missing-docs/docfx.json", output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(testRoot, recursive: true);
            s_currentDirectoryLock.Release();
        }
    }

    [Fact]
    public async Task WriteResultsAsyncReturnsFalseWhenDocfxNotFoundWithFileAnnotation()
    {
        await s_currentDirectoryLock.WaitAsync();
        string testRoot = CreateTempDirectory();
        string originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(testRoot);
            using var writer = new StringWriter();

            bool result = await PathVerifier.WriteResultsAsync(writer);

            string output = writer.ToString();
            Assert.False(result);
            Assert.Contains("Unable to find docfx.json", output, StringComparison.Ordinal);
            Assert.Contains("file=docfx.json", output, StringComparison.Ordinal);
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
                "fileMetadata": {
                  "ms.author": {
                    "docs/**": "someone",
                  },
                },
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
