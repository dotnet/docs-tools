using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Checks;

internal sealed class Query : ICheck
{
    public string Value { get; }

    public Query(YamlMappingNode node, InstanceData data)
    {
        Value = node["value"].ToString();
        data.Logger.LogDebug("BUILD: Check-Query");
        data.Logger.LogTrace("BUILD: {value}", Value);
    }

    public async Task<bool> Run(InstanceData data)
    {
        bool result = Utilities.TestStateJMES(Value, data);

        if (result)
            data.Logger.LogInformation("PASS");
        else
            data.Logger.LogInformation("FAIL");

        return await Task.FromResult(result);
    }
}
