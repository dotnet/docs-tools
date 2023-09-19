using Microsoft.Extensions.Logging;

namespace RepoMan.Checks;

internal sealed class DocNewMetadataExists : ICheck
{
    public DocNewMetadataExists(State state)
    {
        state.Logger.LogDebug($"BUILD: Check-newmetadata-exists");
    }

    public async Task<bool> Run(State state)
    {
        state.Logger.LogInformation($"Evaluating if doc v2 metadata exists");

        if (state.IsV2Metadata)
            state.Logger.LogInformation($"PASS");
        else
            state.Logger.LogInformation($"FAIL");

        return await Task.FromResult<bool>(state.IsV2Metadata);
    }
}
