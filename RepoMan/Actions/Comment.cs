using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class Comment : IRunnerItem
{
    private readonly string _comment;

    public Comment(YamlNode node, InstanceData data)
    {
        _comment = node.ToString();
    }

    public async Task Run(InstanceData data)
    {
        data.Logger.LogInformation("RUN [COMMENT]:Adding");
        data.Logger.LogDebug("comment is: {comment}", _comment);
        await GitHubCommands.Comments.AddComment(_comment, data);
    }
}
