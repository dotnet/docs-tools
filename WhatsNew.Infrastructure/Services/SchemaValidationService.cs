using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Reflection;
using static WhatsNew.Infrastructure.Constants;
using STJ = System.Text.Json;

namespace WhatsNew.Infrastructure.Services;

/// <summary>
/// The class responsible for validating JSON configuration files against
/// the associated JSON schema.
/// </summary>
public class SchemaValidationService
{
    public void ValidateConfiguration(JToken configToken)
    {
        var schema = GetSchemaFileJSchema();
        bool isValid = configToken.IsValid(schema, out IList<string> errors);

        if (!isValid)
        {
            var delimitedErrorDetails = string.Join(", ", errors);
            throw new STJ.JsonException(
                $"JSON schema validation failed for config file. {delimitedErrorDetails}");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Config file passed schema validation");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine();
    }

    public async Task<string> GetSchemaFileContents()
    {
        var fileInfo = GetSchemaFileInfo();

        using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream);
        var fileContents = await reader.ReadToEndAsync();

        return fileContents;
    }

    private JSchema GetSchemaFileJSchema()
    {
        var fileInfo = GetSchemaFileInfo();

        using var fileStream = fileInfo.CreateReadStream();
        using var streamReader = new StreamReader(fileStream);
        using var textReader = new JsonTextReader(streamReader);
        var schema = JSchema.Load(textReader);

        return schema;
    }

    private IFileInfo GetSchemaFileInfo()
    {
        var filePath = Path.Combine(ConfigurationDirectory, "reposettings.schema.json");
        var fileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly());
        var fileInfo = fileProvider.GetFileInfo(filePath);

        if (!fileInfo.Exists)
            throw new FileNotFoundException($"The schema file {filePath} doesn't exist.");

        return fileInfo;
    }
}
