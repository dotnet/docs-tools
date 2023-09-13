using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Actions;

internal sealed class Assignees: IRunnerItem
{
    private readonly RunnerItemSubTypes _type;
    private readonly string[] _names;

    public Assignees(YamlNode node, RunnerItemSubTypes subType, State state)
    {
        if (subType != RunnerItemSubTypes.Add)
            throw new Exception("BUILD: Assignee actions only support add");

        _type = subType;

        List<string> names = new List<string>();

        // Check for direct value or array
        if (node.NodeType == YamlNodeType.Scalar)
        {
            state.Logger.LogDebug($"BUILD: Assignee: {node}");
            names.Add(node.ToString());
        }

        else
        {
            foreach (var item in node.AsSequenceNode())
            {
                state.Logger.LogDebug($"BUILD: Assignee: {item}");
                names.Add(item.ToString());
            }
        }

        _names = names.ToArray();
    }


    public async Task Run(State state)
    {
        if (_type == RunnerItemSubTypes.Add)
        {
            state.Logger.LogInformation($"Adding assignees to pool");

            // Add to state pooled labels for add
            foreach (var item in _names)
                state.Operations.Assignees.Add(item);
        }
    }
}
