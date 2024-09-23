using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;
using DotNetDocs.RepoMan.Checks;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class Group: IRunnerItem
{
    public List<ICheck> Checks { get; } = new List<ICheck>();

    public Runner? PassActions { get; set; }
    public Runner? FailActions { get; set; }

    public async Task Run(InstanceData data)
    {
        data.Logger.LogInformation("RUN [GROUP]: Running check group; count: {count}", Checks.Count);

        bool result = true;

        foreach (ICheck check in Checks)
        {
            if (!await check.Run(data))
            {
                result = false;
                break;
            }
        }

        data.Logger.LogInformation("Checks passed? {result}", result);

        if (result && PassActions != null)
        {
            data.Logger.LogInformation("Running Pass actions");
            await PassActions.Run(data);
        }

        if (!result && FailActions != null)
        {
            data.Logger.LogInformation("Running Fail actions");
            await FailActions.Run(data);
        }
    }

    public static Group Build(YamlMappingNode node, InstanceData data)
    {
        data.Logger.LogDebug("BUILD: Check group start");

        Group checkGroup = new Group();

        IList<YamlNode> checkItems = node["check"].AsSequenceNode().Children;

        foreach (YamlNode item in checkItems)
        {
            string typeProperty = item["type"].ToString();

            data.Logger.LogDebug("BUILD: Finding check type {prop}", typeProperty);

            if (typeProperty.Equals("query", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new Query(item.AsMappingNode(), data));

            else if (typeProperty.Equals("metadata-comment", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new DocMetadata(item.AsMappingNode(), data));

            else if (typeProperty.Equals("metadata-exists", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new DocMetadataExists(data));

            else if (typeProperty.Equals("metadata-new-exists", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new DocMetadataExists(data));

            else if (typeProperty.Equals("isdraft", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new IsDraft(item.AsMappingNode(), data));

            else if (typeProperty.Equals("variable", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new Checks.VariableValue(item.AsMappingNode(), data));

            else if (typeProperty.Equals("variable-exists", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new Checks.VariableExists(item.AsMappingNode(), data));

            else if (typeProperty.Equals("metadata-file", StringComparison.OrdinalIgnoreCase))
            {
                // Future
            }
            else if (typeProperty.Equals("comment-body", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new CommentBody(item.AsMappingNode(), data));
            else
            {
                data.Logger.LogError("Check type not found: {prop}", typeProperty);
                checkGroup.Checks.Add(new ForceFail());
            }
        }

        if (node.Exists("pass", out YamlSequenceNode? values))
            checkGroup.PassActions = Runner.Build(values, data);

        if (node.Exists("fail", out YamlSequenceNode? valuesFailed))
            checkGroup.FailActions = Runner.Build(valuesFailed, data);

        data.Logger.LogTrace("BUILD: Check group end");

        return checkGroup;
    }
}
