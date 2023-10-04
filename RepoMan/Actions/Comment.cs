using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Actions;

internal sealed class Comment : IRunnerItem
{
    private readonly string _comment;
    private readonly string _value;

    public Comment(YamlNode node, State state)
    {
        _comment = node.ToString();
    }

    public async Task Run(State state)
    {
        state.Logger.LogInformation($"Adding comment");
        state.Logger.LogDebugger(_comment);
        await GithubCommand.AddComment(_comment, state);
    }
}
