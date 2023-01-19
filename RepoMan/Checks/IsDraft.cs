using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Checks;

public class IsDraft : ICheck
{
    public bool Condition { get; }

    public IsDraft(YamlMappingNode node, State state)
    {
        state.Logger.LogDebug($"BUILD: IsDraft");
        Condition = Convert.ToBoolean(node["value"].ToString());
        state.Logger.LogTrace($"BUILD: - {Condition}");
    }

    public async Task<bool> Run(State state)
    {
        state.Logger.LogInformation($"Check IsDraft: {Condition}");

        if (!state.IsPullRequest)
        {
            state.Logger.LogError("Tried to check IsDraft on non-PR");
            return await Task.FromResult(false);
        }

        var result = state.PullRequest?.Draft == Condition;

        if (result)
            state.Logger.LogInformation($"PASS");
        else
            state.Logger.LogInformation($"FAIL");

        return await Task.FromResult(result);
    }
}
