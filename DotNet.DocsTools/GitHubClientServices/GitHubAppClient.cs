using DotNetDocs.Tools.GitHubCommunications;
using GitHubJwt;
using System.Net.Http.Headers;

namespace DotNet.DocsTools.GitHubClientServices;

internal class GitHubAppClient : GitHubClientBase, IGitHubClient, IDisposable
{
    private readonly int _appID;
    private readonly string _oauthPrivateKey;

    private Task regenerateTask = default!;
    private CancellationTokenSource? _tokenSource;

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
        _tokenSource = new CancellationTokenSource();
        CancellationToken ct = _tokenSource.Token;
        regenerateTask = RegenerateTokenAfter(timeout, ct);
    }

    private async Task RegenerateTokenAfter(TimeSpan duration, CancellationToken token)
    {
        await Task.Delay(duration);
        if (token.IsCancellationRequested)
        {
            return;
        }
        await GenerateTokenAsync();
    }

    public override void Dispose()
    {
        _tokenSource?.Cancel();
        base.Dispose();
    }
}

file sealed class PlainStringPrivateKeySource : IPrivateKeySource
{
    private readonly string _key;

    public PlainStringPrivateKeySource(string key) => _key = key;

    public TextReader GetPrivateKeyReader() => new StringReader(_key);
}


