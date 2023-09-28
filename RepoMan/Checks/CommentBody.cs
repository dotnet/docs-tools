using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Checks;

internal sealed class CommentBody : ICheck
{
    public string RegEx { get; }

    public CommentBody(YamlMappingNode node, State state)
    {
        RegEx = node["value"].ToString();
        state.Logger.LogDebugger($"BUILD: comment-body {RegEx}");
    }

    public async Task<bool> Run(State state)
    {
        state.Logger.LogDebugger($"RUN: CommentBody regex check '{RegEx}'");
        if (Utilities.MatchRegex(RegEx, state.IssuePrBody ?? "", state))
        {
            state.Logger.LogDebugger($"RUN: CommentBody pass");
            return await Task.FromResult<bool>(true);
        }

        state.Logger.LogDebugger($"RUN: CommentBody fail");
        return await Task.FromResult<bool>(false);
    }
}
