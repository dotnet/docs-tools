using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Checks;

internal sealed class Query : ICheck
{
    public string Value { get; }

    public Query(YamlMappingNode node, State state)
    {
        Value = node["value"].ToString();
        state.Logger.LogDebug($"BUILD: Check-Query");
        state.Logger.LogTrace($"BUILD: {Value}");
    }

    public async Task<bool> Run(State state)
    {
        state.Logger.LogInformation($"Evaluating: {Value}");

        var result = Utilities.TestStateJMES(Value, state);

        if (result)
            state.Logger.LogInformation($"PASS");
        else
            state.Logger.LogInformation($"FAIL");

        return await Task.FromResult<bool>(result);
    }
}
