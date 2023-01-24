using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Checks;

public class DocMetadataExists : ICheck
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
