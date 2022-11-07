namespace Quest2GitHub.Options;

public sealed class RawGitHubFileReader : IDisposable
{
    private readonly HttpClient _httpClient = new();

    public async Task<ImportOptions?> ReadOptionsAsync(
        string org,
        string repo,
        string branch = "main",
        string filename = "quest-config.json")
    {
        // Example:
        //  https://raw.githubusercontent.com/dotnet/docs/main/quest-config.json
        var url = $"https://raw.githubusercontent.com/{org}/{repo}/{branch}/{filename}";

        try
        {
            var json = await _httpClient.GetStringAsync(url);
            var options = JsonSerializer.Deserialize<ImportOptions>(json);
            
            options.WriteValuesToConsole();
            if (options is not null)
            {
                options = options with
                {
                    ApiKeys = EnvironmentVariableReader.GetApiKeys()
                };
            }

            return options;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Attempted: {url}, {ex}");
        }

        return null;
    }

    void IDisposable.Dispose() => _httpClient.Dispose();
}
