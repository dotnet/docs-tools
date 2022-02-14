using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MarkdownLinksVerifier.Configuration;
using Xunit;

namespace MarkdownLinksVerifier.UnitTests.LinkValidatorTests
{
    public class LocalLinkValidatorTests
    {
        private static async Task<List<LinkError>> GetResultsAsync(MarkdownLinksVerifierConfiguration? config = null)
            => await MarkdownFilesAnalyzer.GetResultsAsync(config, $".{Path.DirectorySeparatorChar}WorkspaceTests");

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
            List<LinkError> result = await GetResultsAsync();
            var expected = new LinkError[]
            {
                new($".{separator}WorkspaceTests{separator}README.md", "README-2.md", $".{separator}WorkspaceTests")
            };

            Assert.Equal(expected, result);
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
            List<LinkError> result = await GetResultsAsync();
            Assert.Empty(result);
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
            List<LinkError> result = await GetResultsAsync();
            Assert.Empty(result);
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
            List<LinkError> result = await GetResultsAsync();
            Assert.Empty(result);
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
            List<LinkError> result = await GetResultsAsync();
            var expected = new LinkError[]
            {
                new($".{separator}WorkspaceTests{separator}README.md", "README2.md#Hello", $".{separator}WorkspaceTests")
            };

            Assert.Equal(expected, result);
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
            List<LinkError> result = await GetResultsAsync();
            var expected = new LinkError[]
            {
                new($".{separator}WorkspaceTests{separator}README.md", "#Heading1", $".{separator}WorkspaceTests")
            };
            Assert.Equal(expected, result);
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
            List<LinkError> result = await GetResultsAsync();
            Assert.Empty(result);
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
            List<LinkError> result = await GetResultsAsync();
            Assert.Empty(result);
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
            List<LinkError> result = await GetResultsAsync();
            Assert.Empty(result);
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
            List<LinkError> result = await GetResultsAsync();
            Assert.Empty(result);
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
            List<LinkError> result = await GetResultsAsync();
            var expected = new LinkError[]
            {
                new($".{separator}WorkspaceTests{separator}README.md", "#my_anchor", $".{separator}WorkspaceTests")
            };
            Assert.Equal(expected, result);
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
            List<LinkError> result = await GetResultsAsync();
            Assert.Empty(result);
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
            List<LinkError> result = await GetResultsAsync();
            Assert.Empty(result);
        }
        #endregion
    }
}
