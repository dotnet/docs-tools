using System;
using System.IO;
using System.Threading.Tasks;
using MarkdownLinksVerifier.Configuration;
using Xunit;

namespace MarkdownLinksVerifier.UnitTests.LinkValidatorTests
{
    public class LocalLinkValidatorTests
    {
        private static async Task<int> WriteResultsAndGetExitCodeAsync(StringWriter writer, MarkdownLinksVerifierConfiguration? config = null)
            => await MarkdownFilesAnalyzer.WriteResultsAsync(writer, config ?? MarkdownLinksVerifierConfiguration.Empty, $".{Path.DirectorySeparatorChar}WorkspaceTests");

        private static void Verify(string[] actual, (string File, string Link, string RelativeTo)[] expected)
        {
            Assert.True(actual.Length > 2, $"The actual output is expected to have at least two lines. Found {actual.Length} lines:\r\n" + string.Join(Environment.NewLine, actual));

            char separator = Path.DirectorySeparatorChar;
            // The first line is always expected to be that.
            Assert.Equal($"Starting Markdown Links Verifier in '.{separator}WorkspaceTests'.", actual[0]);

            // The last line is always an empty line.
            Assert.Equal("", actual[^1]);

            for (var expectedIndex = 0; expectedIndex < expected.Length; expectedIndex++)
            {
                int actualIndex = expectedIndex + 1;
                Assert.Equal(
                    $"::error::In file '{expected[expectedIndex].File}': Invalid link: '{expected[expectedIndex].Link}' relative to '{expected[expectedIndex].RelativeTo}'.",
                    actual[actualIndex]);
            }

            Assert.True(actual.Length == expected.Length + 2, $"Expected length doesn't match actual. Expected: {expected.Length + 2}, Actual: {actual.Length}.");
        }

        private static void VerifyNoErrors(string[] actual)
        {
            Assert.True(actual.Length == 2, $"The actual output is expected to have exactly two lines.  Found {actual.Length} lines:\r\n" + string.Join(Environment.NewLine, actual));

            char separator = Path.DirectorySeparatorChar;
            // The first line is always expected to be that.
            Assert.Equal($"Starting Markdown Links Verifier in '.{separator}WorkspaceTests'.", actual[0]);

            // The last line is always an empty line.
            Assert.Equal("", actual[1]);

        }

        [Fact]
        public async Task TestSimpleCase_FileDoesNotExist()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", "[text](README-2.md)" }
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            (string File, string Link, string RelativeTo)[] expected = new[]
            {
                ($".{separator}WorkspaceTests{separator}README.md", "README-2.md", $".{separator}WorkspaceTests")
            };

            Verify(writer.ToString().Split(writer.NewLine), expected);
            Assert.Equal(expected: 1, actual: returnCode);
        }

        [Fact]
        public async Task TestValidHeadingIdInAnotherFile()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", "[text](README2.md#hello)" },
                    { "/README2.md", "### HeLLo" }
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            VerifyNoErrors(writer.ToString().Split(writer.NewLine));
            Assert.Equal(expected: 0, actual: returnCode);
        }

        [Fact]
        public async Task TestValidHeadingIdInAnotherFile_ComplexHeading()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", "[text](README2.md#1-scope)" },
                    { "/README2.md", "### 1 Scope" }
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            VerifyNoErrors(writer.ToString().Split(writer.NewLine));
            Assert.Equal(expected: 0, actual: returnCode);
        }

        [Fact]
        public async Task TestValidHeadingIdInSameFile()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", @"## Heading1

Hello world.

## Heading 2
[text](#heading1)" },
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            VerifyNoErrors(writer.ToString().Split(writer.NewLine));
            Assert.Equal(expected: 0, actual: returnCode);
        }

        [Fact]
        public async Task TestInvalidHeadingIdInAnotherFile()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", "[text](README2.md#Hello)" },
                    { "/README2.md", "### HeLLo" }
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            (string File, string Link, string RelativeTo)[] expected = new[]
            {
                ($".{separator}WorkspaceTests{separator}README.md", "README2.md#Hello", $".{separator}WorkspaceTests")
            };

            Verify(writer.ToString().Split(writer.NewLine), expected);
            Assert.Equal(expected: 1, actual: returnCode);
        }

        [Fact]
        public async Task TestInvalidHeadingIdInSameFile()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", @"## Heading1

Hello world.

## Heading 2
[text](#Heading1)" },
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            (string File, string Link, string RelativeTo)[] expected = new[]
            {
                ($".{separator}WorkspaceTests{separator}README.md", "#Heading1", $".{separator}WorkspaceTests")
            };
            Verify(writer.ToString().Split(writer.NewLine), expected);
            Assert.Equal(expected: 1, actual: returnCode);
        }

        [Fact]
        public async Task TestHeadingReferenceToNonMarkdownFile()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", "[text](Program.cs#Snippet1)" },
                    { "/Program.cs", "class Program {}" },
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);

            VerifyNoErrors(writer.ToString().Split(writer.NewLine));
            Assert.Equal(expected: 0, actual: returnCode);
        }

        [Fact]
        public async Task TestHeadingReferenceUsingAnchorTag_Id()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", @"<a id=""my_anchor""/> ## Heading1

Hello world.

## Heading 2
[text](#my_anchor)" },
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            VerifyNoErrors(writer.ToString().Split(writer.NewLine));
            Assert.Equal(expected: 0, actual: returnCode);
        }

        [Fact]
        public async Task TestHeadingReferenceUsingAnchorTag_Name()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", @"<a name=""my_anchor""/> ## Heading1

Hello world.

## Heading 2
[text](#my_anchor)" },
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            VerifyNoErrors(writer.ToString().Split(writer.NewLine));
            Assert.Equal(expected: 0, actual: returnCode);
        }

        [Fact]
        public async Task TestHeadingReferenceUsingAnchorTag_Block()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", @"
.NET Framework 4.7.1 includes new features in the following areas:

- [Networking](#net471)

#### Common language runtime (CLR)

**Garbage collection performance improvements**

<a name=""net471""/>

#### Networking

**SHA-2 support for Message.HashAlgorithm **
" },
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            VerifyNoErrors(writer.ToString().Split(writer.NewLine));
            Assert.Equal(expected: 0, actual: returnCode);
        }

        [Fact]
        public async Task TestHeadingReferenceUsingAnchorTag_Invalid()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", @"<a name=""my_anchor2""/> ## Heading1

Hello world.

## Heading 2
[text](#my_anchor)" },
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            (string File, string Link, string RelativeTo)[] expected = new[]
{
                ($".{separator}WorkspaceTests{separator}README.md", "#my_anchor", $".{separator}WorkspaceTests")
            };
            Verify(writer.ToString().Split(writer.NewLine), expected);
            Assert.Equal(expected: 1, actual: returnCode);
        }

        #region "MSDocs-specific"
        [Fact]
        public async Task TestHeadingReference_MSDocsTab()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/README.md", @"# [.NET 5.0](#tab/net50)

Hello world.
"
                    },
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            VerifyNoErrors(writer.ToString().Split(writer.NewLine));
            Assert.Equal(expected: 0, actual: returnCode);
        }

        [Fact(Skip = "https://github.com/Youssef1313/markdown-links-verifier/issues/93")]
        public async Task TestHeadingReference_Includes()
        {
            using var workspace = new Workspace
            {
                Files =
                {
                    { "/aspnetcore.md", @"The following breaking changes in ASP.NET Core 3.0 and 3.1 are documented on this page:
- [Obsolete Antiforgery, CORS, Diagnostics, MVC, and Routing APIs removed](#obsolete-antiforgery-cors-diagnostics-mvc-and-routing-apis-removed)


[!INCLUDE[Obsolete Antiforgery, CORS, Diagnostics, MVC, and Routing APIs removed](~/include.md)]
"
                    },
                    { "/include.md", @"### Obsolete Antiforgery, CORS, Diagnostics, MVC, and Routing APIs removed

Obsolete members and compatibility switches in ASP.NET Core 2.2 were removed.
"
                    },
                }
            };

            char separator = Path.DirectorySeparatorChar;

            string workspacePath = await workspace.InitializeAsync();
            using var writer = new StringWriter();
            int returnCode = await WriteResultsAndGetExitCodeAsync(writer);
            VerifyNoErrors(writer.ToString().Split(writer.NewLine));
            Assert.Equal(expected: 0, actual: returnCode);
        }
        #endregion
    }
}
