using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Actions;

internal sealed class Labels: IRunnerItem
{
    private readonly RunnerItemSubTypes _type;
    private readonly string[] _labels;

    public Labels(YamlNode node, RunnerItemSubTypes subType, State state)
    {
        if (subType == RunnerItemSubTypes.Set)
            throw new Exception("BUILD: Label actions don't support set");

        string mode = subType == RunnerItemSubTypes.Add ? "Create" : "Remove";
        _type = subType;


        List<string> labels = new List<string>();

        // Check for direct value or array
        if (node.NodeType == YamlNodeType.Scalar)
        {
            state.Logger.LogDebugger($"BUILD: {mode} label: {node}");
            labels.Add(node.ToString());
        }

        else
        {
            foreach (YamlNode item in node.AsSequenceNode())
            {
                state.Logger.LogDebugger($"BUILD: {mode} label: {item}");
                labels.Add(item.ToString());
            }
        }

        _labels = labels.ToArray();
    }


    public async Task Run(State state)
    {
        if (_type == RunnerItemSubTypes.Add)
        {
            state.Logger.LogInformation($"Adding remove-labels to pool");

            // Add to state pooled labels for add
            foreach (string item in _labels)
                state.Operations.LabelsAdd.Add(item);
        }
        else
        {
            state.Logger.LogInformation($"Adding add-labels to pool");

            // Add to state pooled labels for remove
            foreach (string item in _labels)
                state.Operations.LabelsRemove.Add(item);
        }
    }
}
