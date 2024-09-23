using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Checks;

internal sealed class VariableValue : ICheck
{
    public string Name { get; }
    public string Value { get; }

    public VariableValue(YamlMappingNode node, InstanceData data)
    {
        data.Logger.LogDebug("BUILD: Variable value check");
        Name = node["name"].ToString();
        Value = node["value"].ToString();
        data.Logger.LogTrace("BUILD: - {name}={value}", Name, Value);
    }

    public async Task<bool> Run(InstanceData data)
    {
        data.Logger.LogInformation("Check variable: {name}={value}", Name, Value);

        bool result = data.Variables.TryGetValue(Name, out string? variableValue) && variableValue == Value;

        if (result)
            data.Logger.LogInformation($"PASS");
        else
            data.Logger.LogInformation($"FAIL");

        return await Task.FromResult(result);
    }
}
