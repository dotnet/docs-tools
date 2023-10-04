using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Checks;

internal sealed class ForceFail : ICheck
{
    public ForceFail()
    {
        
    }

    public async Task<bool> Run(State state)
    {
        state.Logger.LogInformation($"Check ForceFail");

        return await Task.FromResult(false);
    }
}
