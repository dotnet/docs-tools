using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan;

internal sealed class Runner: IRunnerItem
{
    public List<IRunnerItem> Actions { get; } = new List<IRunnerItem>();

    public static Runner Build(YamlSequenceNode actionNode, State state)
    {
        var runner = new Runner();
        state.Logger.LogDebug("BUILD: Create runner");

        foreach (var item in actionNode)
        {
            try
            {
                if (item.NodeType == YamlNodeType.Mapping)
                {
                    var mappingItem = item.AsMappingNode();
                    var firstProperty = mappingItem.FirstProperty();
                    var itemType = GetRunnerType(firstProperty.Name, state);

                    switch (itemType)
                    {
                        case RunnerItemTypes.Check:
                            state.Logger.LogDebug("BUILD: Create check");
                            runner.Actions.Add(Checks.Group.Build(mappingItem, state));
                            break;

                        case RunnerItemTypes.Label:
                            state.Logger.LogDebug("BUILD: Create label");
                            runner.Actions.Add(new Actions.Labels(firstProperty.Node, GetRunnerItemSubType(firstProperty.Name, state), state));
                            break;

                        case RunnerItemTypes.Milestone:
                            state.Logger.LogDebug("BUILD: Create milestone");
                            runner.Actions.Add(new Actions.Milestone(firstProperty.Node, GetRunnerItemSubType(firstProperty.Name, state), state));
                            break;

                        case RunnerItemTypes.Project:
                            state.Logger.LogDebug("BUILD: Create project");
                            runner.Actions.Add(new Actions.Projects(firstProperty.Node, GetRunnerItemSubType(firstProperty.Name, state), state));
                            break;

                        case RunnerItemTypes.Files:
                            // This is different from a typical action, it scans the existing files for regex matches and then runs more actions
                            runner.Actions.Add(new Actions.File(firstProperty.Node.AsSequenceNode(), firstProperty.Name.Split('-')[1], state));
                            break;

                        case RunnerItemTypes.Variable:
                            state.Logger.LogDebug("BUILD: Create Variable set/remove");
                            runner.Actions.Add(new Actions.Variable(mappingItem, GetRunnerItemSubType(firstProperty.Name, state), state));
                            break;

                        case RunnerItemTypes.Predefined:
                            var predefinedReference = firstProperty.Node.ToString();
                            state.Logger.LogDebug($"BUILD: Predefined reference: {predefinedReference}");

                            // Do we have a predefined section?
                            if (state.RepoRulesYaml.Exists("predefined"))
                            {
                                YamlMappingNode predefinedSection = state.RepoRulesYaml["predefined"].AsMappingNode();

                                if (predefinedSection.Exists(predefinedReference))
                                {
                                    runner.Actions.Add(Runner.Build(predefinedSection[predefinedReference].AsSequenceNode(), state));
                                    state.Logger.LogDebug($"BUILD: End predefined reference: {predefinedReference}");
                                }
                                else
                                    state.Logger.LogError($"BUILD: Predefined logic missing.");
                            }
                            else
                                state.Logger.LogError("BUILD: Predefined section is missing, it was referenced.");

                            break;

                        case RunnerItemTypes.Comment:
                            state.Logger.LogDebug($"BUILD: Create Comment");
                            runner.Actions.Add(new Actions.Comment(firstProperty.Node, state));
                            break;

                        case RunnerItemTypes.Assignee:
                            state.Logger.LogDebug($"BUILD: Assignees");
                            runner.Actions.Add(new Actions.Assignees(firstProperty.Node, GetRunnerItemSubType(firstProperty.Name, state), state));
                            break;

                        case RunnerItemTypes.Reviewer:
                            state.Logger.LogDebug($"BUILD: Reviewers");
                            runner.Actions.Add(new Actions.Reviewers(firstProperty.Node, GetRunnerItemSubType(firstProperty.Name, state), state));
                            break;

                        case RunnerItemTypes.Issue:
                            // Future
                            // Should have child property specifying the action, open/close/move/whatever
                            break;
                        case RunnerItemTypes.PullRquest:
                            // Future
                            // Should have child property specifying the action, open/close/move/whatever
                            break;
                        default:
                            break;
                    }

                }
                else if (item.NodeType == YamlNodeType.Sequence)
                    runner.Actions.Add(Runner.Build(item.AsSequenceNode(), state));
            }
            catch (Exception e)
            {
                state.Logger.LogError($"BUILD: Error building an action\n{e.Message}\n{e.StackTrace}");
            }
        }

        state.Logger.LogDebug("BUILD: runner done");

        return runner;
    }

    public async Task Run(State state)
    {
        state.Logger.LogInformation($"Running items; count: {Actions.Count}");
        foreach (var item in Actions)
            await item.Run(state);
    }

    public static RunnerItemTypes GetRunnerType(string nodeName, State state)
    {
        if (nodeName.Equals("check", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Check;

        else if (nodeName.StartsWith("labels-", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Label;

        else if (nodeName.StartsWith("milestone-", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Milestone;

        else if (nodeName.StartsWith("projects-", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Project;

        else if (nodeName.StartsWith("comment-", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Comment;

        else if (nodeName.StartsWith("files-", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Files;

        else if (nodeName.StartsWith("issue", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Issue;

        else if (nodeName.StartsWith("pullrequest", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Files;

        else if (nodeName.StartsWith("variable-", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Variable;

        else if (nodeName.StartsWith("assignee-", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Assignee;

        else if (nodeName.StartsWith("reviewer-", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Reviewer;

        else if (nodeName.Equals("predefined", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Predefined;

        else if (nodeName.Equals("comment", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Comment;

        state.Logger.LogTrace($"BUILD: Invalid item type: {nodeName}");
        throw new Exception($"Invalid item type: {nodeName}");
    }

    public static RunnerItemSubTypes GetRunnerItemSubType(string itemName, State state)
    {
        if (itemName.EndsWith("-add"))
            return RunnerItemSubTypes.Add;
        else if (itemName.EndsWith("-remove"))
            return RunnerItemSubTypes.Remove;
        else if (itemName.EndsWith("-set"))
            return RunnerItemSubTypes.Set;

        state.Logger.LogTrace($"BUILDING: Invalid sub item type: {itemName}");
        throw new Exception($"Invalid sub item type: {itemName}");
    }
}
