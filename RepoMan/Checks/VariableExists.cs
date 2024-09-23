using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Checks;

internal sealed class VariableExists : ICheck
{
    public string Name { get; }

    public VariableExists(YamlMappingNode node, InstanceData data)
    {
        data.Logger.LogDebug("BUILD: Variable exists check");
        Name = node["name"].ToString();
        data.Logger.LogTrace("BUILD: - {name}", Name);
    }

    public async Task<bool> Run(InstanceData data)
    {
        data.Logger.LogInformation("Check variable exists: {name}", Name);

        bool result = data.Variables.ContainsKey(Name);

        if (result)
            data.Logger.LogInformation($"PASS");
        else
            data.Logger.LogInformation($"FAIL");

        return await Task.FromResult(result);
    }
}
