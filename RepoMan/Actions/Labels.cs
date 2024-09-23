using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class Labels: IRunnerItem
{
    private readonly RunnerItemSubTypes _type;
    private readonly string[] _labels;

    public Labels(YamlNode node, RunnerItemSubTypes subType, InstanceData data)
    {
        if (subType == RunnerItemSubTypes.Set)
            throw new Exception("BUILD: Label actions don't support set");

        string mode = subType == RunnerItemSubTypes.Add ? "Create" : "Remove";
        _type = subType;


        List<string> labels = [];

        // Check for direct value or array
        if (node.NodeType == YamlNodeType.Scalar)
        {
            data.Logger.LogDebug("BUILD: {mode} label: {node}", mode, node);
            labels.Add(node.ToString());
        }

        else
        {
            foreach (YamlNode item in node.AsSequenceNode())
            {
                data.Logger.LogDebug("BUILD: {mode} label: {item}", mode, item);
                labels.Add(item.ToString());
            }
        }

        _labels = [.. labels];
    }


    public async Task Run(InstanceData data)
    {
        if (_type == RunnerItemSubTypes.Add)
        {
            data.Logger.LogInformation("RUN [LABELS-ADD]: Adding add-labels to pool");

            // Add to state pooled labels for add
            foreach (string item in _labels)
                data.Operations.LabelsAdd.Add(item);
        }
        else
        {
            data.Logger.LogInformation("RUN [LABELS-REMOVE]: Adding remove-labels to pool");

            // Add to state pooled labels for remove
            foreach (string item in _labels)
                data.Operations.LabelsRemove.Add(item);
        }

        await Task.CompletedTask;
    }
}
