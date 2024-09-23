

using Microsoft.Extensions.Logging;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class CloseObject: IRunnerItem
{
    public CloseObject()
    {
    }

    public async Task Run(InstanceData data)
    {
        data.Logger.LogInformation("RUN [CLOSE]: Running");
        await GitHubCommands.IssuePullRequest.Close(data);
    }
}
