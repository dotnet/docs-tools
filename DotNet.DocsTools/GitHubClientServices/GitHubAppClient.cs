using DotNetDocs.Tools.GitHubCommunications;
using GitHubJwt;
using System.Net.Http.Headers;

namespace DotNet.DocsTools.GitHubClientServices;

internal class GitHubAppClient : GitHubClientBase, IGitHubClient, IDisposable
{
    private readonly int _appID;
    private readonly string _oauthPrivateKey;

    private Task regenerateTask = default!;

    public sealed class PlainStringPrivateKeySource : IPrivateKeySource
    {
        private readonly string _key;

        public PlainStringPrivateKeySource(string key) => _key = key;

        public TextReader GetPrivateKeyReader() => new StringReader(_key);
    }

    internal GitHubAppClient(int appID, string oauthPrivateKey) =>
        (_appID, _oauthPrivateKey) = (appID, oauthPrivateKey);

    internal async Task GenerateTokenAsync()
    {
        var privateKeySource = new PlainStringPrivateKeySource(_oauthPrivateKey);
        var generator = new GitHubJwtFactory(
            privateKeySource,
            new GitHubJwtFactoryOptions
            {
                AppIntegrationId = _appID,
                ExpirationSeconds = 8 * 60 // 600 is apparently too high
            });

        var token = generator.CreateEncodedJwtToken();
        SetAuthorizationHeader(new AuthenticationHeaderValue("Bearer", token));

        var installationId = await RetrieveInstallationIDAsync();

        var (oauthToken, timeout) = await RetrieveAuthTokenAsync(installationId);

        SetAuthorizationHeader(new AuthenticationHeaderValue("Token", oauthToken));

        timeout = timeout - TimeSpan.FromMinutes(5);
        // Don't await. Fire and store:
        regenerateTask = RegenerateTokenAfter(timeout);
    }

    private async Task RegenerateTokenAfter(TimeSpan duration)
    {
        await Task.Delay(duration);
        await GenerateTokenAsync();
    }
}
