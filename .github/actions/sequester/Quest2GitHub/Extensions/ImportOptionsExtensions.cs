namespace Quest2GitHub.Extensions;

public static class ImportOptionsExtensions
{
    public static ImportOptions ValidateOptions(this ImportOptions? options)
    {
        ArgumentNullException.ThrowIfNull(options);

        static void AttemptFallbackOrThrowIfNullOrWhiteSpace(
            string? value, string envVarKey, Action<string> updateWithFallback, string paramName, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                string? envVariableValue = Environment.GetEnvironmentVariable(envVarKey);
                if (string.IsNullOrWhiteSpace(envVariableValue))
                {
                    throw new ArgumentNullException(paramName, message);
                }
                else
                {
                    updateWithFallback(envVariableValue);
                }
            }
        }

        AttemptFallbackOrThrowIfNullOrWhiteSpace(
            options.ApiKeys?.GitHubToken,
            "GitHubKey",
            fallbackValue =>
            {
                options = EnsureApiKeys(options);
                options = options with
                {
                    ApiKeys = options.ApiKeys! with
                    {
                        GitHubToken = fallbackValue
                    }
                };
            },
            nameof(options.ApiKeys.GitHubToken),
            """
            A GitHub Token is required. In a consuming GitHub Action workflow assign it as follows:
              ImportOptions__ApiKeys__GitHubToken: ${{ secrets.GITHUB_TOKEN }}
            """);

        AttemptFallbackOrThrowIfNullOrWhiteSpace(
            options.ApiKeys?.QuestKey,
            "QuestKey",
            fallbackValue =>
            {
                options = EnsureApiKeys(options);
                options = options with
                {
                    ApiKeys = options.ApiKeys! with
                    {
                        QuestKey = fallbackValue
                    }
                };
            },
            nameof(options.ApiKeys.QuestKey),
            """
            A Quest API key is required. In a consuming GitHub Action workflow assign it as follows:
              ImportOptions__ApiKeys__QuestKey: ${{ secrets.QUEST_API_KEY }}
            """);

        return options.ApiKeys is null
            ? throw new ArgumentException(
                $"The API keys configuration options are required.",
                nameof(options))
            : options;
    }

    /// <summary>
    /// Omits the <see cref="ImportOptions.ApiKeys"/> values, as those are secrets.
    /// </summary>
    public static void WriteValuesToConsole(this ImportOptions? options)
    {
        Console.WriteLine($"Values configured in the ImportOptions (API Keys omitted):");
        Console.WriteLine($"  options.ImportTriggerLabel = \"{options?.ImportTriggerLabel}\"");
        Console.WriteLine($"  options.ImportedLabel = \"{options?.ImportedLabel}\"");
        Console.WriteLine($"  options.AzureDevOps.Org = \"{options?.AzureDevOps?.Org}\"");
        Console.WriteLine($"  options.AzureDevOps.Project = \"{options?.AzureDevOps?.Project}\"");
        Console.WriteLine($"  options.AzureDevOps.AreaPath = \"{options?.AzureDevOps?.AreaPath}\"");
        Console.WriteLine($"  options.ParentNodes:");
        foreach (var node in options?.ParentNodes ?? Enumerable.Empty<ParentForLabel>())
        {
            Console.WriteLine($"    {node.Label, -20}  ==> {node.ParentNodeId}");
        }
    }

    static ImportOptions EnsureApiKeys(ImportOptions options)
    {
        if (options is { ApiKeys: null })
        {
            options = options with
            {
                ApiKeys = new()
                {
                    SequesterPrivateKey = "",
                    GitHubToken = "",
                    QuestKey = "",
                    SequesterAppID = 0
                }
            };
        }

        return options;
    }
}
