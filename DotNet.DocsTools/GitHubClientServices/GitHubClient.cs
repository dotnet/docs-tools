using System.Net.Http.Headers;

namespace DotNetDocs.Tools.GitHubCommunications;
public sealed class GitHubClient : GitHubClientBase, IGitHubClient, IDisposable
{
    internal GitHubClient(string token)
    {
        Console.WriteLine("Using default GitHub PAT token.");
        SetAuthorizationHeader(new AuthenticationHeaderValue("Token", token));
    }
}
