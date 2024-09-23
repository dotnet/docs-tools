using Microsoft.Extensions.Logging;
using System.Diagnostics;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class Runner: IRunnerItem
{
    public static YamlNode? DebugNode;

    public List<IRunnerItem> Actions { get; } = [];

    public static Runner Build(YamlSequenceNode actionNode, InstanceData data)
    {
        Runner runner = new();
        data.Logger.LogDebug("BUILD: Create runner");

        foreach (YamlNode item in actionNode)
        {
            try
            {
                DebugNode = item;

                // If this is a mapping node, some sort of 'prop: value' pair or a 'prop: -value' object.
                if (item.NodeType == YamlNodeType.Mapping)
                {
                    YamlMappingNode mappingItem = item.AsMappingNode();
                    (string Name, YamlNode Node) = mappingItem.FirstProperty();
                    RunnerItemTypes itemType = GetRunnerType(Name, data);

                    switch (itemType)
                    {
                        case RunnerItemTypes.Check:
                            data.Logger.LogDebug("BUILD: Create check");
                            runner.Actions.Add(Group.Build(mappingItem, data));
                            break;

                        case RunnerItemTypes.Label:
                            data.Logger.LogDebug("BUILD: Create label");
                            runner.Actions.Add(new Labels(Node, GetRunnerItemSubType(Name, data), data));
                            break;

                        case RunnerItemTypes.Milestone:
                            data.Logger.LogDebug("BUILD: Create milestone");
                            runner.Actions.Add(new Milestone(Node, GetRunnerItemSubType(Name, data), data));
                            break;

                        //case RunnerItemTypes.Project:
                        //    data.Logger.LogDebug("BUILD: Create project");
                        //    runner.Actions.Add(new Projects(Node, GetRunnerItemSubType(Name, data), data));
                        //    break;

                        case RunnerItemTypes.Files:
                            // This is different from a typical action, it scans the existing files for regex matches and then runs more actions
                            runner.Actions.Add(new File(Node.AsSequenceNode(), Name.Split('-')[1], data));
                            break;

                        case RunnerItemTypes.Variable:
                            data.Logger.LogDebug("BUILD: Create Variable set/remove");
                            runner.Actions.Add(new Variable(mappingItem, GetRunnerItemSubType(Name, data), data));
                            break;

                        case RunnerItemTypes.Predefined:
                            string predefinedReference = Node.ToString();
                            data.Logger.LogDebug("BUILD: Predefined reference: {ref}", predefinedReference);

                            // Do we have a predefined section?
                            if (data.RepoRulesYaml!.Exists("predefined"))
                            {
                                YamlMappingNode predefinedSection = data.RepoRulesYaml!["predefined"].AsMappingNode();

                                if (predefinedSection.Exists(predefinedReference))
                                {
                                    runner.Actions.Add(Runner.Build(predefinedSection[predefinedReference].AsSequenceNode(), data));
                                    data.Logger.LogDebug("BUILD: End predefined reference: {ref}", predefinedReference);
                                }
                                else
                                    data.Logger.LogError($"BUILD: Predefined logic missing.");
                            }
                            else
                                data.Logger.LogError("BUILD: Predefined section is missing, it was referenced.");

                            break;

                        case RunnerItemTypes.Comment:
                            data.Logger.LogDebug($"BUILD: Create Comment");
                            runner.Actions.Add(new Comment(Node, data));
                            break;

                        case RunnerItemTypes.Assignee:
                            data.Logger.LogDebug($"BUILD: Assignees");
                            runner.Actions.Add(new Assignees(Node, GetRunnerItemSubType(Name, data), data));
                            break;

                        case RunnerItemTypes.Reviewer:
                            data.Logger.LogDebug($"BUILD: Reviewers");
                            runner.Actions.Add(new Reviewers(Node, GetRunnerItemSubType(Name, data), data));
                            break;

                        case RunnerItemTypes.Issue:
                            // Future
                            // Should have child property specifying the action, open/close/move/whatever
                            break;
                        case RunnerItemTypes.PullRquest:
                            // Future
                            // Should have child property specifying the action, open/close/move/whatever
                            break;
                        case RunnerItemTypes.SvcSubSvcLabels:
                            data.Logger.LogDebug($"BUILD: Service/SubService labels");
                            runner.Actions.Add(new SetSvcSubSvcLabels());
                            break;
                        
                        default:
                            break;
                    }

                }

                // List of actions
                else if (item.NodeType == YamlNodeType.Sequence)
                    runner.Actions.Add(Runner.Build(item.AsSequenceNode(), data));

                // Single named item
                else if (item.NodeType == YamlNodeType.Scalar)
                {
                    RunnerItemTypes itemType = GetRunnerType(((YamlScalarNode)item).Value!, data);
                    switch (itemType)
                    {
                        case RunnerItemTypes.Close:
                            data.Logger.LogDebug($"BUILD: Close issue/pr");
                            runner.Actions.Add(new CloseObject());
                            break;

                        case RunnerItemTypes.Reopen:
                            data.Logger.LogDebug($"BUILD: Reopen issue/pr");
                            runner.Actions.Add(new OpenObject());
                            break;

                        case RunnerItemTypes.LinkRelatedIssues:
                            data.Logger.LogDebug($"BUILD: Link related issues");
                            runner.Actions.Add(new LinkRelatedIssues());
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                if (DebugNode != null)
                    data.Logger.LogError("BUILD: Error building an action Line: {line} Col: {col}\n{message}\n{stack}", DebugNode.Start.Line, DebugNode.Start.Column, e.Message, e.StackTrace);
                else
                    data.Logger.LogError("BUILD: Error building an action");

                if (Debugger.IsAttached)
                    Debugger.Break();
            }
        }

        data.Logger.LogDebug("BUILD: runner done");
        DebugNode = null;

        return runner;
    }

    public async Task Run(InstanceData data)
    {
        data.Logger.LogInformation("RUN [RUNNER]: Running items; count: {count}", Actions.Count);
        foreach (IRunnerItem item in Actions)
            await item.Run(data);
    }

    public static RunnerItemTypes GetRunnerType(string nodeName, InstanceData data)
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

        else if (nodeName.Equals("close", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Close;

        else if (nodeName.Equals("reopen", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Reopen;

        else if (nodeName.Equals("open", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.Reopen;

        else if (nodeName.Equals("link-related-issues", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.LinkRelatedIssues;

        else if (nodeName.Equals("svc_subsvc_labels", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.SvcSubSvcLabels;

        // Deprecated... remove in next update
        else if (nodeName.Equals("prod_tech_labels", StringComparison.OrdinalIgnoreCase))
            return RunnerItemTypes.SvcSubSvcLabels;

        throw new Exception($"Invalid item type: {nodeName}");
    }

    public static RunnerItemSubTypes GetRunnerItemSubType(string itemName, InstanceData data)
    {
        if (itemName.EndsWith("-add"))
            return RunnerItemSubTypes.Add;
        else if (itemName.EndsWith("-remove"))
            return RunnerItemSubTypes.Remove;
        else if (itemName.EndsWith("-set"))
            return RunnerItemSubTypes.Set;

        data.Logger.LogTrace("BUILDING: Invalid sub item type: {name}", itemName);
        throw new Exception($"Invalid sub item type: {itemName}");
    }
}
