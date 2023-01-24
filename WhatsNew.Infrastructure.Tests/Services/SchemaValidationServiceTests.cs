using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using WhatsNew.Infrastructure.Services;
using Xunit;
using STJ = System.Text.Json;

namespace WhatsNew.Infrastructure.Tests.Services;

public class SchemaValidationServiceTests
{
    private readonly string _configDirectory;
    private readonly SchemaValidationService _service;

    public SchemaValidationServiceTests()
    {
        _configDirectory = Path.Combine(Directory.GetCurrentDirectory(), "assets");
        _service = new();
    }

    [Fact]
    public async Task Empty_DocSet_Product_Name_Throws_JsonException()
    {
        var configFilePath = Path.Combine(
            _configDirectory, "MicrosoftDocs", "azure-devops-docs-pr.json");
        JToken configFile = await GetConfigFileAsync(configFilePath);

        var exception = Assert.Throws<STJ.JsonException>(
            () => _service.ValidateConfiguration(configFile));
        Assert.StartsWith(
            $"JSON schema validation failed for config file. String '' is less than minimum length of 1.",
            exception.Message);
    }
    
    [Fact]
    public async Task Empty_Root_Directory_Throws_JsonException()
    {
        var configFilePath = Path.Combine(
            _configDirectory, "dotnet", "AspNetCore.Docs.json");
        JToken configFile = await GetConfigFileAsync(configFilePath);

        var exception = Assert.Throws<STJ.JsonException>(
            () => _service.ValidateConfiguration(configFile));
        Assert.StartsWith(
            "JSON schema validation failed for config file. String '' is less than minimum length of 1.",
            exception.Message);
    }

    [Fact]
    public async Task Missing_Relative_Link_Prefix_Property_Throws_JsonException()
    {
        var configFilePath = Path.Combine(
            _configDirectory, "dotnet", "docs.json");
        JToken configFile = await GetConfigFileAsync(configFilePath);

        var exception = Assert.Throws<STJ.JsonException>(
            () => _service.ValidateConfiguration(configFile));
        Assert.StartsWith(
            "JSON schema validation failed for config file. JSON is valid against no schemas from 'oneOf'. Path 'docLinkSettings',",
            exception.Message);
    }

    [Fact]
    public async Task Empty_Areas_Array_Throws_JsonException()
    {
        var configFilePath = Path.Combine(
            _configDirectory, "MicrosoftDocs", "cpp-docs-pr.json");
        JToken configFile = await GetConfigFileAsync(configFilePath);

        var exception = Assert.Throws<STJ.JsonException>(
            () => _service.ValidateConfiguration(configFile));
        Assert.StartsWith(
            "JSON schema validation failed for config file. Array item count 0 is less than minimum count of 1. Path 'areas',",
            exception.Message);
    }

    private async Task<JToken> GetConfigFileAsync(string filePath)
    {
        using StreamReader file = File.OpenText(filePath);
        using var reader = new JsonTextReader(file);
        var configFile = await JToken.ReadFromAsync(reader);

        return configFile;
    }
}
