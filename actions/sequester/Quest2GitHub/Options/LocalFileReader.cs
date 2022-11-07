namespace Quest2GitHub.Options;

public sealed class LocalFileReader
{
    public async Task<ImportOptions?> ReadOptionsAsync(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
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
            Console.Error.WriteLine($"Attempted reading: {path}, {ex}");
        }

        return null;
    }
}
