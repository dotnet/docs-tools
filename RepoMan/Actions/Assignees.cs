using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class Assignees: IRunnerItem
{
    private readonly RunnerItemSubTypes _type;
    private readonly string[] _names;

    public Assignees(YamlNode node, RunnerItemSubTypes subType, InstanceData data)
    {
        if (subType != RunnerItemSubTypes.Add)
            throw new Exception("BUILD: Assignee actions only support add");

        _type = subType;

        List<string> names = [];

        // Check for direct value or array
        if (node.NodeType == YamlNodeType.Scalar)
        {
            data.Logger.LogDebug("BUILD: Assignee: {node}", node);
            names.Add(node.ToString());
        }

        else
        {
            foreach (YamlNode item in node.AsSequenceNode())
            {
                data.Logger.LogDebug("BUILD: Assignee: {item}", item);
                names.Add(item.ToString());
            }
        }

        _names = [.. names];
    }


    public async Task Run(InstanceData data)
    {
        if (_type == RunnerItemSubTypes.Add)
        {
            data.Logger.LogInformation($"RUN [ASSIGNEE]: Adding assignees to pool");

            // Add to state pooled labels for add
            foreach (string item in _names)
                data.Operations.Assignees.Add(item);
        }

        await Task.CompletedTask;
    }
}
