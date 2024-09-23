using GitHubJwt;
using Microsoft.Extensions.Logging;
using Octokit;

namespace DotNetDocs.RepoMan;

internal static class GitHubAccess
{
    private static string? _gitHubAppToken;
    private static DateTime _gitHubTokenExpiresAt;

    public static TimeSpan JwtTokenLifetime { get; set; } = TimeSpan.FromMinutes(8);

    public static bool IsGitHubAppTokenValid =>
        DateTime.Now < _gitHubTokenExpiresAt;

    public static async Task<string> GetAppToken(string secretAppKey, int appID, long installationID, ILogger logger)
    {
        if (string.IsNullOrEmpty(_gitHubAppToken) || DateTime.Now.ToUniversalTime() >= _gitHubTokenExpiresAt)
        {
            logger.LogInformation("Requesting GitHub installation token");

            GitHubClient githubClient = new(new Octokit.ProductHeaderValue(FunctionRepoMain.AppProductName, FunctionRepoMain.AppProductVersion))
            {
                Credentials = new Credentials(GenerateJwtToken(secretAppKey, appID), AuthenticationType.Bearer)
            };

            AccessToken response = await githubClient.GitHubApps.CreateInstallationToken(installationID);

            _gitHubAppToken = response.Token;
            _gitHubTokenExpiresAt = response.ExpiresAt.ToUniversalTime().DateTime - TimeSpan.FromMinutes(2);
            logger.LogInformation("Token expires at {time}", _gitHubTokenExpiresAt.ToLocalTime().ToString());
        }
        else
            logger.LogTrace("Using cached GitHub installation token");

        return _gitHubAppToken;
    }

    private static string GenerateJwtToken(string privateKey, int appId)
    {
        var privateKeySource = new PlainStringPrivateKeySource(privateKey);

        var generator = new GitHubJwtFactory(
            privateKeySource,
            new GitHubJwtFactoryOptions
            {
                AppIntegrationId = appId,
                ExpirationSeconds = (int)JwtTokenLifetime.TotalSeconds
            });

        return generator.CreateEncodedJwtToken();
    }

    private sealed class PlainStringPrivateKeySource(string key) : IPrivateKeySource
    {
        public TextReader GetPrivateKeyReader() =>
            new StringReader(key);
    }
}
