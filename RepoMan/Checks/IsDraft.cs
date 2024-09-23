using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Checks;

internal sealed class IsDraft : ICheck
{
    public bool Condition { get; }

    public IsDraft(YamlMappingNode node, InstanceData data)
    {
        data.Logger.LogDebug("BUILD: IsDraft");
        Condition = Convert.ToBoolean(node["value"].ToString());
        data.Logger.LogTrace("BUILD: - {condition}", Condition);
    }

    public async Task<bool> Run(InstanceData data)
    {
        data.Logger.LogInformation("RUN CHECK: IsDraft {condition}", Condition);

        if (!data.HasPullRequestData)
        {
            data.Logger.LogError("Tried to check IsDraft on non-PR");
            return await Task.FromResult(false);
        }

        bool result = data.PullRequest?.Draft == Condition;

        if (result)
            data.Logger.LogInformation("PASS");
        else
            data.Logger.LogInformation("FAIL");

        return await Task.FromResult(result);
    }
}
