using DotNetDocs.Tools.GitHubCommunications;
using System.Text.Json;

namespace DotNetDocs.Tools.Tests
{
    class FakeGitHubClient : IGitHubClient
    {
        private readonly JsonDocument? document;
        private readonly JsonDocument[]? additional;
        private readonly string[]? lines;

        public FakeGitHubClient() { }

        public FakeGitHubClient(JsonDocument response) => this.document = response;
        public FakeGitHubClient(JsonDocument response, params JsonDocument[] additionalpages) =>
            (this.document, this.additional) = (response, additionalpages);

        public FakeGitHubClient(string[] lines) => this.lines = lines;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<string> GetContentAsync(string link)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            _ = lines ?? throw new ArgumentNullException(nameof(lines));
            foreach (var line in lines)
                yield return line;
        }

        public Task<JsonDocument> GetReposRESTRequestAsync(params string[] restPath) =>
            Task.FromResult(document ?? throw new InvalidOperationException());

        // Might need to change for error based tests:
        private int count = 0;
        public Task<JsonElement> PostGraphQLRequestAsync(GraphQLPacket queryText)
        {
            _ = document ?? throw new ArgumentNullException(nameof(document));
            _ = additional ?? throw new ArgumentNullException(nameof(additional));
            var rVal = (count == 0)
                ? Task.FromResult(document.RootElement.GetProperty("data"))
                : Task.FromResult(additional[count - 1].RootElement.GetProperty("data"));
            count++;
            return rVal;
        }
            

        public Task<string> PostMarkdownRESTRequestAsync(string markdownText)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }
    }
}
