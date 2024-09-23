using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Checks;

internal sealed class DocMetadata : ICheck
{
    public string Name { get; }
    public string Value { get; }

    public DocMetadata(YamlMappingNode node, InstanceData data)
    {
        data.Logger.LogDebug("BUILD: Check-metadata-comment");

        Name = node["name"].ToString();
        Value = node["value"].ToString();

        data.Logger.LogTrace("BUILD: Name: {name} Value: {value}", Name, Value);
    }

    public async Task<bool> Run(InstanceData data)
    {
        bool result = false;

        data.Logger.LogInformation("RUN CHECK: Evaluating comment metadata: {name} for {value}", Name, Value);

        if (data.DocIssueMetadata.TryGetValue(Name, out string? value))
            result = Utilities.MatchRegex(Value, value, data);

        if (result)
            data.Logger.LogInformation($"PASS");
        else
            data.Logger.LogInformation($"FAIL");

        return await Task.FromResult<bool>(result);
    }
}
