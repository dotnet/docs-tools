using DotNet.DocsTools.GraphQLQueries;
using Xunit;

namespace DotnetDocsTools.Tests.GraphQLProcessingTests
{
    public class DefaultBranchQueryTests
    {
        [Fact]
        public void GitHubClient_Must_Not_Be_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new DefaultBranchQuery(
                default!,
                "dotnet",
                "docs"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(null)]
        public void Organization_Must_Be_Non_Whitespace(string organization)
        {
            Assert.Throws<ArgumentException>(() => new DefaultBranchQuery(
                new FakeGitHubClient(),
                organization,
                "docs"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(null)]
        public void Repository_Must_Be_Non_Whitespace(string repository)
        {
            Assert.Throws<ArgumentException>(() => new DefaultBranchQuery(
                new FakeGitHubClient(),
                "dotnet",
                repository));
        }
    }
}
