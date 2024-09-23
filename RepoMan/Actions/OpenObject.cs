using Microsoft.Extensions.Logging;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class OpenObject : IRunnerItem
{
    public OpenObject()
    {
    }

    public async Task Run(InstanceData data)
    {
        data.Logger.LogInformation("RUN [OPEN]: Running");
        await GitHubCommands.IssuePullRequest.Open(data);
    }
}
