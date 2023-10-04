using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Actions;

internal sealed class Milestone: IRunnerItem
{
    private const int InvalidMilestone = -1;

    private readonly RunnerItemSubTypes _type = RunnerItemSubTypes.Add;
    private readonly string _milestone;

    public Milestone(YamlNode node, RunnerItemSubTypes subType, State state)
    {
        if (subType == RunnerItemSubTypes.Add)
            subType = RunnerItemSubTypes.Set;

        string mode = subType == RunnerItemSubTypes.Set ? "Create" : "Remove";
        _type = subType;


        _milestone = node.ToString();
        state.Logger.LogDebugger($"BUILD: {mode} milestone '{_milestone}'");
    }

    public async Task Run(State state)
    {
        if (string.IsNullOrEmpty(_milestone))
        {
            state.Logger.LogError("Milestone string is blank");
            return;
        }

        IReadOnlyList<Octokit.Milestone> milestones = await GithubCommand.GetMilestones(state);
        int milestoneId = InvalidMilestone;

        // Special milestone
        if (_milestone == "![month]")
        {
            string monthYear = DateTime.UtcNow.ToString("MMMM yyyy").ToLower();

            state.Logger.LogInformation($"Setting [Month] milestone: {monthYear}");

            foreach (Octokit.Milestone item in milestones)
            {
                if (item.Title.Equals(monthYear, StringComparison.OrdinalIgnoreCase))
                {
                    milestoneId = item.Number;
                    state.Logger.LogDebugger($"Found milestone: {item.Title}:{item.Number}");
                    break;
                }
            }
        }
        else if (_milestone == "![sprint]")
        {
            try
            {
                string sprint = DotNetDocs.Tools.Utility.SprintDateRange.GetSprintFor(DateTime.Now).SprintName;
                state.Logger.LogInformation($"Setting [sprint] milestone: {sprint}");

                foreach (Octokit.Milestone item in milestones)
                {
                    if (item.Title.Equals(sprint, StringComparison.OrdinalIgnoreCase))
                    {
                        milestoneId = item.Number;
                        state.Logger.LogDebugger($"Found milestone: {item.Title}:{item.Number}");
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

                foreach (Octokit.Milestone item in milestones)
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
            state.Logger.LogDebugger($"Milestones available { string.Concat(milestones.Select(m => $"\n{m.Title}")) }");
            return;
        }

        // Run the github action
        if (_type == RunnerItemSubTypes.Set)
            await GithubCommand.SetMilestone(milestoneId, state);
        else
            await GithubCommand.RemoveMilestone(state);
    }
}
