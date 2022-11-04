namespace Quest2GitHub.Options;

public sealed class RawGitHubFileReader : IDisposable
{
    private readonly HttpClient _httpClient = new();

    public async Task<string?> TryInitializeOptionsAsync(
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
            if (options is not null)
            {
                return await WriteOptionsAsync(options, filename);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Attempted: {url}, {ex}");
        }

        return null;
    }

    static async Task<string?> WriteOptionsAsync(ImportOptions options, string filename)
    {
        try
        {
            var json = JsonSerializer.Serialize(options);
            await File.WriteAllTextAsync(filename, json);

            options.WriteValuesToConsole();

            return Path.GetFullPath(filename);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }

        return null;
    }

    void IDisposable.Dispose() => _httpClient.Dispose();
}
