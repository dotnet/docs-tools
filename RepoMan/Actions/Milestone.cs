using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class Milestone: IRunnerItem
{
    private const int InvalidMilestone = -1;

    private readonly RunnerItemSubTypes _type = RunnerItemSubTypes.Add;
    private readonly string _milestone;

    public Milestone(YamlNode node, RunnerItemSubTypes subType, InstanceData data)
    {
        if (subType == RunnerItemSubTypes.Add)
            subType = RunnerItemSubTypes.Set;

        string mode = subType == RunnerItemSubTypes.Set ? "Create" : "Remove";
        _type = subType;


        _milestone = node.ToString();
        data.Logger.LogDebug("BUILD: {mode} milestone '{milestone}'", mode, _milestone);
    }

    public async Task Run(InstanceData data)
    {
        if (string.IsNullOrEmpty(_milestone))
        {
            data.Logger.LogError("Milestone string is blank");
            return;
        }

        data.Logger.LogInformation("RUN [MILESTONE]: Setting or clearing");

        IReadOnlyList<Octokit.Milestone> milestones = await GitHubCommands.Milestones.GetMilestones(data);
        int milestoneId = InvalidMilestone;

        // Special milestone
        if (_milestone == "![month]")
        {
            string monthYear = DateTime.UtcNow.ToString("MMMM yyyy").ToLower();

            data.Logger.LogInformation("Setting [Month] milestone: {monthyear}", monthYear);

            foreach (Octokit.Milestone item in milestones)
            {
                if (item.Title.Equals(monthYear, StringComparison.OrdinalIgnoreCase))
                {
                    milestoneId = item.Number;
                    data.Logger.LogDebug("Found milestone: {title}:{number}", item.Title, item.Number);
                    break;
                }
            }
        }
        else if (_milestone == "![sprint]")
        {
            try
            {
                string sprint = Tools.Utility.SprintDateRange.GetSprintFor(DateTime.Now).SprintName;
                data.Logger.LogInformation("Setting [sprint] milestone: {sprint}", sprint);

                foreach (Octokit.Milestone item in milestones)
                {
                    if (item.Title.Equals(sprint, StringComparison.OrdinalIgnoreCase))
                    {
                        milestoneId = item.Number;
                        data.Logger.LogDebug("Found milestone: {title}:{number}", item.Title, item.Number);
                    break;
                    }
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                data.Logger.LogError("Can't set sprint milestone, sprint not found for datetime: {datetime}, error message is {message}", DateTime.Now, e.Message);
            }
        }
        else
        {
            if (int.TryParse(_milestone, out int id))
            {
                data.Logger.LogInformation("Searching for milestone {milestone}", _milestone);

                foreach (Octokit.Milestone item in milestones)
                {
                    if (id == item.Id)
                    {
                        data.Logger.LogInformation($"Found");
                        milestoneId = item.Number;
                        break;
                    }
                }
            }
            else
                data.Logger.LogError("Milestone isn't a number {milestone}", _milestone);
        }

        if (milestoneId == InvalidMilestone)
        {
            data.Logger.LogError("Milestone is invalid, can't run this action");
            data.Logger.LogDebug("Milestones available {milestones}", string.Concat(milestones.Select(m => $"\n{m.Title}")));
            return;
        }

        // Run the github action
        if (_type == RunnerItemSubTypes.Set)
            await GitHubCommands.Milestones.SetMilestone(milestoneId, data);
        else
            await GitHubCommands.Milestones.RemoveMilestone(data);

        await Task.CompletedTask;
    }
}
