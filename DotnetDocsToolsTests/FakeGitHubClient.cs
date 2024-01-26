using DotNetDocs.Tools.GitHubCommunications;
using System.Text.Json;

namespace DotNetDocs.Tools.Tests;

class FakeGitHubClient : IGitHubClient
{
    private readonly JsonDocument _document = JsonDocument.Parse("{}");
    private readonly JsonDocument[] _additional = Array.Empty<JsonDocument>();
    private readonly string[]? _lines;

    public FakeGitHubClient() { }

    public FakeGitHubClient(JsonDocument responseDocument) => _document = responseDocument;
    
    public FakeGitHubClient(JsonDocument responseDocument, params JsonDocument[] additionalpages) =>
        (_document, _additional) = (responseDocument, additionalpages);

    public FakeGitHubClient(string[] lines) => _lines = lines;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerable<string> GetContentAsync(string link)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        foreach (var line in _lines ?? Array.Empty<string>())
            yield return line;
    }

    public Task<JsonDocument> GetReposRESTRequestAsync(params string[] restPath) =>
        Task.FromResult(_document ?? throw new InvalidOperationException());

    // Might need to change for error based tests:
    private int _count = 0;

    public Task<JsonElement> PostGraphQLRequestAsync(GraphQLPacket queryText)
    {
        //ArgumentNullException.ThrowIfNull(_document);
        //ArgumentNullException.ThrowIfNull(_additional);
        
        var jsonElementTask = (_count is 0)
            ? Task.FromResult(_document.RootElement.GetProperty("data")!)
            : Task.FromResult(_additional[_count - 1].RootElement.GetProperty("data")!);
        
        _count++;

        return jsonElementTask;
    }
        

    public Task<string> PostMarkdownRESTRequestAsync(string markdownText)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {            
    }
}
