using Microsoft.Extensions.Logging;
using Octokit;
using YamlDotNet.RepresentationModel;

namespace RepoMan.Actions;

public sealed class Projects: IRunnerItem
{
    private readonly RunnerItemSubTypes _type;
    private readonly int[] _projects;

    public Projects(YamlNode node, RunnerItemSubTypes subType, State state)
    {
        if (subType == RunnerItemSubTypes.Set)
            throw new Exception("Project actions don't support set");
        if (subType == RunnerItemSubTypes.Remove)
            throw new Exception("Project actions don't support remove");

        _type = subType;

        List<int> projects = new List<int>();

        // Check for direct value or array
        if (node.NodeType == YamlNodeType.Scalar)
            projects.Add(node.ToInt());
        else
        {
            foreach (YamlNode item in node.AsSequenceNode())
                projects.Add(item.ToInt());
        }

        _projects = projects.ToArray();
    }

    public async Task Run(State state)
    {
        IReadOnlyList<Project> existingProjects = await GithubCommand.GetProjects(state);
        List<(int Column, NewProjectCard Card, int ProjectId)> projectCards = new List<(int, NewProjectCard, int)>();

        foreach (int id in _projects)
        {
            bool foundProject = false;

            foreach (Project projectObject in state.Projects)
            {
                if (projectObject.Number == id)
                {
                    ProjectColumn[]? columns = await GithubCommand.GetProjectColumns(state, id);

                    if (columns != null)
                    {
                        // TODO: If remove action, check for if the issue exists in the column then add to list.
                        NewProjectCard projectCard;

                        if (state.RequestType == RequestType.Issue)
                            projectCard = new NewProjectCard(state.Issue.Id, ProjectCardContentType.Issue);
                        else
                        {
                            if (state.PullRequest != null)
                                projectCard = new NewProjectCard(state.PullRequest.Id, ProjectCardContentType.PullRequest);
                            else
                            {
                                state.Logger.LogError($"Can't set project on PR. Event was PR but the state.pullrequest isn't set. contact adegeo@ms to debug");
                                continue;
                            }
                        }

                        state.Logger.LogInformation($"Project added: {projectObject.Number}:{projectObject.Name}");
                        projectCards.Add((columns[0].Id, projectCard, id));

                        foundProject = true;
                        break;
                    }
                    else
                        state.Logger.LogError($"Project {id} doesn't have any columns.");
                }
            }

            if (!foundProject)
                state.Logger.LogError($"Project ID not found in repository: {id}");
        }

        // Add issue/pr to projects
        if (_type == RunnerItemSubTypes.Add)
        {
            foreach ((int Column, NewProjectCard Card, int ProjectId) item in projectCards)
            {
                state.Logger.LogInformation($"Adding project: {item.ProjectId}");

                try
                {
                    await state.ProjectsClient.Card.Create(item.Column, item.Card);
                }
                catch
                {
                    // Failure at this point is most likely related to the issue already existing
                    // in the project. We'll leave this catch in here. The alternative is to
                    // query every column in the project for the existence of the issue/pr id, but
                    // that is a waste of time/data for now.
                }
                
            }
        }
        else
        {
            // Future
            // var tempResults = await state.ProjectsClient.Card.GetAll(columns[0].Id);
        }
    }
}
