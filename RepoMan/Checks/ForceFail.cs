using Microsoft.Extensions.Logging;

namespace RepoMan.Checks;

public sealed class ForceFail : ICheck
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
