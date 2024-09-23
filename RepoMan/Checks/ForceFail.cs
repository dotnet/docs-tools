using Microsoft.Extensions.Logging;

namespace DotNetDocs.RepoMan.Checks;

internal sealed class ForceFail : ICheck
{
    public ForceFail()
    {
        
    }

    public async Task<bool> Run(InstanceData data)
    {
        data.Logger.LogInformation("RUN CHECK: ForceFail");

        return await Task.FromResult(false);
    }
}
