namespace Quest2GitHub.Options;

internal sealed class EnvironmentVariableReader
{
    internal static ApiKeys GetApiKeys()
    {
        var githubToken = CoalesceEnvVar(("ImportOptions__ApiKeys__GitHubToken", "GitHubKey"));
        var ospoKey = CoalesceEnvVar(("ImportOptions__ApiKeys__OSPOKey", "OSPOKey"));
        var questKey = CoalesceEnvVar(("ImportOptions__ApiKeys__QuestKey", "QuestKey"));
        var oauthPrivateKey = CoalesceEnvVar(("ImportOptions__ApiKeys__SequesterPrivateKey", "SequesterPrivateKey"));

        var appIDString = CoalesceEnvVar(("ImportOptions__ApiKeys__SequesterAppID", "SequesterAppID"));
        if (!int.TryParse(appIDString, out int appID)) appID = 0;

        return new ApiKeys()
        {
            GitHubToken = githubToken,
            OSPOKey = ospoKey,
            QuestKey = questKey,
            SequesterPrivateKey = oauthPrivateKey,
            SequesterAppID = appID
        };
    }

    static string CoalesceEnvVar((string preferredKey, string fallbackKey) keys)
    {
        var (preferredKey, fallbackKey) = keys;

        var value = Environment.GetEnvironmentVariable(preferredKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Environment.GetEnvironmentVariable(fallbackKey);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception(
                    $"Missing env var, checked for both: {preferredKey} and {fallbackKey}.");
            }
        }

        return value;
    }
}
