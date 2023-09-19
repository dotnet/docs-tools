using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Checks;

internal sealed class CommentBody : ICheck
{
    public string RegEx { get; }

    public CommentBody(YamlMappingNode node, State state)
    {
        RegEx = node["value"].ToString();
        state.Logger.LogDebug($"BUILD: comment-body");
        state.Logger.LogTrace($"BUILD: {RegEx}");
    }

    public async Task<bool> Run(State state)
    {
        if (Utilities.MatchRegex(RegEx, state.IssuePrBody ?? "", state))
            return await Task.FromResult<bool>(true);

        return await Task.FromResult<bool>(false);
    }
}
