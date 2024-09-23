using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Checks;

internal sealed class CommentBody : ICheck
{
    public string RegEx { get; }

    public CommentBody(YamlMappingNode node, InstanceData data)
    {
        RegEx = node["value"].ToString();
        data.Logger.LogDebug("BUILD: comment-body {regex}", RegEx.Replace("\n", "\\n"));
    }

    public async Task<bool> Run(InstanceData data)
    {
        data.Logger.LogDebug("RUN CHECK: CommentBody regex check '{regex}'", RegEx.Replace("\n", "\\n"));
        if (Utilities.MatchRegex(RegEx, data.IssuePrBody ?? "", data))
        {
            data.Logger.LogDebug("RUN CHECK: CommentBody pass");
            return await Task.FromResult(true);
        }

        data.Logger.LogDebug("RUN CHECK: CommentBody fail");
        return await Task.FromResult(false);
    }
}
