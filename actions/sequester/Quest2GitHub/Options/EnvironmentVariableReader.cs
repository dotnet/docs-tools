namespace Quest2GitHub.Options;

internal sealed class EnvironmentVariableReader
{
    internal static ApiKeys GetApiKeys()
    {
        var githubToken = CoalesceEnvVar(("ImportOptions__ApiKeys__GitHubToken", "GitHubKey"));
        // This is optional so that developers can run the app locally without setting up the devOps token.
        // In GitHub Actions, this is preferred.
        var questToken = CoalesceEnvVar(("ImportOptions__ApiKeys__QuestAccessToken", "QuestAccessToken"), false);

        // These keys are used when the app is run as an org enabled action. They are optional. 
        // If missing, the action runs using repo-only rights.
        var oauthPrivateKey = CoalesceEnvVar(("ImportOptions__ApiKeys__SequesterPrivateKey", "SequesterPrivateKey"), false);
        var appIDString = CoalesceEnvVar(("ImportOptions__ApiKeys__SequesterAppID", "SequesterAppID"), false);

        var azureAccessToken = CoalesceEnvVar(("ImportOptions__ApiKeys__AzureAccessToken", "AZURE_ACCESS_TOKEN"), false);

        // This key is the PAT for Quest access. It's now a legacy key. Secretless should be better.
        var questKey = CoalesceEnvVar(("ImportOptions__ApiKeys__QuestKey", "QuestKey"), false);

        if (!int.TryParse(appIDString, out int appID)) appID = 0;

        return new ApiKeys()
        {
            GitHubToken = githubToken,
            QuestAccessToken =  questToken,
            AzureAccessToken = azureAccessToken,
            QuestKey = questKey,
            SequesterPrivateKey = oauthPrivateKey,
            SequesterAppID = appID
        };
    }

    static string CoalesceEnvVar((string preferredKey, string fallbackKey) keys, bool required = true)
    {
        var (preferredKey, fallbackKey) = keys;

        // Attempt the preferred key first.
        var value = Environment.GetEnvironmentVariable(preferredKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            // If the preferred key is not set, try the fallback key.
            value = Environment.GetEnvironmentVariable(fallbackKey);
            Console.WriteLine($"{(string.IsNullOrWhiteSpace(value) ? $"Neither {preferredKey} or {fallbackKey} found" : $"Found {fallbackKey}")}");
        }
        else
        {
            Console.WriteLine($"Found value for {preferredKey}");
        }

        // If neither key is set, throw an exception if required.
        if (string.IsNullOrWhiteSpace(value) && required)
        {
            throw new Exception(
                $"Missing env var, checked for both: {preferredKey} and {fallbackKey}.");
        }

        return value!;
    }
}
