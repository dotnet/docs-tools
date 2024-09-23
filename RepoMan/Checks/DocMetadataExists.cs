using Microsoft.Extensions.Logging;

namespace DotNetDocs.RepoMan.Checks;

internal sealed class DocMetadataExists : ICheck
{
    public DocMetadataExists(InstanceData data)
    {
        data.Logger.LogDebug("BUILD: Check-metadata-exists");
    }

    public async Task<bool> Run(InstanceData data)
    {
        data.Logger.LogInformation("RUN CHECK: Evaluating if doc v2 metadata exists");

        if (data.HasDocMetadata())
        {
            data.Logger.LogInformation("PASS");
            return await Task.FromResult(true);
        }
        else
        {
            data.Logger.LogInformation("FAIL");
            return await Task.FromResult(false);
        }
    }
}
