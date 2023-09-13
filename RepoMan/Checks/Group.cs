using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Checks;

internal sealed class Group: IRunnerItem
{
    public List<ICheck> Checks { get; } = new List<ICheck>();

    public Runner? PassActions { get; set; }
    public Runner? FailActions { get; set; }

    public async Task Run(State state)
    {
        state.Logger.LogInformation($"Running check group; count: {Checks.Count}");

        bool result = true;

        foreach (ICheck check in Checks)
        {
            if (!await check.Run(state))
            {
                result = false;
                break;
            }
        }

        state.Logger.LogInformation($"Checks? {result}");

        if (result && PassActions != null)
        {
            state.Logger.LogInformation("Running Pass actions");
            await PassActions.Run(state);
        }

        if (!result && FailActions != null)
        {
            state.Logger.LogInformation("Running Fail actions");
            await FailActions.Run(state);
        }
    }

    public static Group Build(YamlMappingNode node, State state)
    {
        state.Logger.LogDebug("BUILD: Check group start");

        Group checkGroup = new Group();

        IList<YamlNode> checkItems = node["check"].AsSequenceNode().Children;

        foreach (YamlNode item in checkItems)
        {
            string typeProperty = item["type"].ToString();

            state.Logger.LogDebug($"BUILD: Finding check type {typeProperty}");

            if (typeProperty.Equals("query", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new Query(item.AsMappingNode(), state));

            else if (typeProperty.Equals("metadata-comment", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new DocMetadata(item.AsMappingNode(), state));

            else if (typeProperty.Equals("metadata-exists", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new DocMetadataExists(state));

            else if (typeProperty.Equals("isdraft", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new IsDraft(item.AsMappingNode(), state));

            else if (typeProperty.Equals("variable", StringComparison.OrdinalIgnoreCase))
                checkGroup.Checks.Add(new Variable(item.AsMappingNode(), state));

            else if (typeProperty.Equals("metadata-file", StringComparison.OrdinalIgnoreCase))
            {
                // Future
            }
            else if (typeProperty.Equals("comment", StringComparison.OrdinalIgnoreCase))
            {
                // Future
            }
            
        }

        if (node.Exists("pass", out YamlSequenceNode? values))
            checkGroup.PassActions = Runner.Build(values, state);

        if (node.Exists("fail", out YamlSequenceNode? valuesFailed))
            checkGroup.FailActions = Runner.Build(valuesFailed, state);

        state.Logger.LogTrace("BUILD: Check group end");

        return checkGroup;
    }
}
