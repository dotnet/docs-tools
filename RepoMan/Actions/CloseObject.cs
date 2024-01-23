using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Actions;

public sealed class CloseObject: IRunnerItem
{
    public CloseObject()
    {
    }

    public async Task Run(State state)
    {
        await GithubCommand.Close(state);
    }
}
