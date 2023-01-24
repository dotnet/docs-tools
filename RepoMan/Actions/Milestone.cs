using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Actions;

public class Milestone: IRunnerItem
{
    private const int InvalidMilestone = -1;

    private RunnerItemSubTypes _type = RunnerItemSubTypes.Add;
    private string _milestone;

    public Milestone(YamlNode node, RunnerItemSubTypes subType, State state)
    {
        if (subType == RunnerItemSubTypes.Add)
            subType = RunnerItemSubTypes.Set;

        string mode = subType == RunnerItemSubTypes.Set ? "Create" : "Remove";
        _type = subType;


        _milestone = node.ToString();
        state.Logger.LogInformation($"BUILD: {mode} milestone '{_milestone}'");
    }

    public async Task Run(State state)
    {
        if (string.IsNullOrEmpty(_milestone))
        {
            state.Logger.LogError("Milestone string is blank");
            return;
        }

        var milestones = await GithubCommand.GetMilestones(state);
        int milestoneId = InvalidMilestone;

        // Special milestone
        if (_milestone == "![month]")
        {
            string monthYear = DateTime.UtcNow.ToString("MMMM yyyy").ToLower();

            state.Logger.LogInformation($"Setting [Month] milestone: {monthYear}");

            foreach (var item in milestones)
            {
                if (item.Title.Equals(monthYear, StringComparison.OrdinalIgnoreCase))
                {
                    milestoneId = item.Number;
                    state.Logger.LogDebug($"Found milestone: {item.Title}:{item.Number}");
                    break;
                }
            }
        }
        else if (_milestone == "![sprint]")
        {
            try
            {
                var sprint = DotNetDocs.Tools.Utility.SprintDateRange.GetSprintFor(DateTime.Now).SprintName;
                state.Logger.LogInformation($"Setting [sprint] milestone: {sprint}");

                foreach (var item in milestones)
                {
                    if (item.Title.Equals(sprint, StringComparison.OrdinalIgnoreCase))
                    {
                        milestoneId = item.Number;
                        state.Logger.LogDebug($"Found milestone: {item.Title}:{item.Number}");
                    break;
                    }
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                state.Logger.LogError($"Can't set sprint milestone, sprint not found for datetime: {DateTime.Now}, error message is {e.Message}");
            }
        }
        else
        {
            if (int.TryParse(_milestone, out int id))
            {
                state.Logger.LogInformation($"Searching for milestone {_milestone}");

                foreach (var item in milestones)
                {
                    if (id == item.Id)
                    {
                        state.Logger.LogInformation($"Found");
                        milestoneId = item.Number;
                        break;
                    }
                }
            }
            else
                state.Logger.LogError($"Milestone isn't a number {_milestone}");
        }

        if (milestoneId == InvalidMilestone)
        {
            state.Logger.LogError("Milestone is invalid, can't run this action");
            state.Logger.LogDebug($"Milestones available { string.Concat(milestones.Select(m => $"\n{m.Title}")) }");
            return;
        }

        // Run the github action
        if (_type == RunnerItemSubTypes.Set)
            await GithubCommand.SetMilestone(milestoneId, state);
        else
            await GithubCommand.RemoveMilestone(state);
    }
}
