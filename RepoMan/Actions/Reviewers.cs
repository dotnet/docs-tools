using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class Reviewers: IRunnerItem
{
    private readonly RunnerItemSubTypes _type;
    private readonly string[] _names;

    public Reviewers(YamlNode node, RunnerItemSubTypes subType, InstanceData data)
    {
        if (subType != RunnerItemSubTypes.Add)
            throw new Exception("BUILD: Reviewer actions only support add");

        _type = subType;

        List<string> names = new List<string>();

        // Check for direct value or array
        if (node.NodeType == YamlNodeType.Scalar)
        {
            data.Logger.LogDebug("BUILD: Reviewer: {node}", node);
            names.Add(node.ToString());
        }

        else
        {
            foreach (YamlNode item in node.AsSequenceNode())
            {
                data.Logger.LogDebug("BUILD: Reviewer: {item}", item);
                names.Add(item.ToString());
            }
        }

        _names = names.ToArray();
    }


    public async Task Run(InstanceData data)
    {
        if (_type == RunnerItemSubTypes.Add)
        {
            data.Logger.LogInformation("RUN [MILESTONE]: Adding reviewers to pool (skipping self)");

            if (data.HasPullRequestData == false)
            {
                data.Logger.LogError("Running reviewers action on non-pull request type");
                return;
            }

            // Add to state pooled labels for add
            foreach (string item in _names)
                if (!item.Equals(data.PullRequest.User.Login))
                    data.Operations.Reviewers.Add(item);
        }

        await Task.CompletedTask;
    }
}
