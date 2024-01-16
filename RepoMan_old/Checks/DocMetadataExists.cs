using Microsoft.Extensions.Logging;

namespace RepoMan.Checks;

internal sealed class DocMetadataExists : ICheck
{
    public DocMetadataExists(State state)
    {
        state.Logger.LogDebug($"BUILD: Check-metadata-exists");
    }

    public async Task<bool> Run(State state)
    {
        state.Logger.LogInformation($"Evaluating if doc metadata exists");

        bool result = state.DocIssueMetadata.Count != 0;

        if (result)
            state.Logger.LogInformation($"PASS");
        else
            state.Logger.LogInformation($"FAIL");

        return await Task.FromResult<bool>(result);
    }
}
