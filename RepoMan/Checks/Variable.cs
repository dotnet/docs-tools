using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Checks;

public sealed class Variable : ICheck
{
    public string Name { get; }
    public string Value { get; }

    public Variable(YamlMappingNode node, State state)
    {
        state.Logger.LogDebugger($"BUILD: Variable");
        Name = node["name"].ToString();
        Value = node["value"].ToString();
        state.Logger.LogTrace($"BUILD: - {Name}={Value}");
    }

    public async Task<bool> Run(State state)
    {
        state.Logger.LogInformation($"Check variable: {Name}={Value}");

        bool result = state.Variables.ContainsKey(Name) && state.Variables[Name] == Value;

        if (result)
            state.Logger.LogInformation($"PASS");
        else
            state.Logger.LogInformation($"FAIL");

        return await Task.FromResult(result);
    }
}
