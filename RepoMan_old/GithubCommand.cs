using Microsoft.Extensions.Logging;
using Octokit;

namespace RepoMan;

internal static class GithubCommand
{
    /// <summary>
    /// Caches and returns a list of all milestones associated with the repository.
    /// </summary>
    /// <param name="state">The state object of the Azure Function.</param>
    /// <returns>A read-only collection of milestones.</returns>
    public static async Task<IReadOnlyList<Milestone>> GetMilestones(State state)
    {
        if (state.Milestones != null) return state.Milestones;

        state.Milestones = await state.Client.Issue.Milestone.GetAllForRepository(state.RepositoryId);

        return state.Milestones;
    }

    /// <summary>
    /// Caches and returns a list of all projects associated with the repository.
    /// </summary>
    /// <param name="state">The state object of the Azure Function.</param>
    /// <returns>A read-only collection of projects.</returns>
    public static async Task<IReadOnlyList<Project>> GetProjects(State state)
    {
        if (state.Projects != null) return state.Projects;

        state.ProjectsClient = new ProjectsClient(new ApiConnection(state.Client.Connection));
        state.Projects = await state.ProjectsClient.GetAllForRepository(state.RepositoryId);

        return state.Projects;
    }

    public static async Task<ProjectColumn[]?> GetProjectColumns(State state, int projectId)
    {
        // If columns already cached, return those.
        if (state.ProjectColumns.ContainsKey(projectId)) return state.ProjectColumns[projectId];

        // Get projects (which will be cached)
        IReadOnlyList<Project> projects = await GetProjects(state);

        // Search for the project we want
        Project? project = projects.FirstOrDefault(e => e.Number == projectId);

        // Exit if project not found
        if (project == null) return null;

        // Get the columns
        ProjectColumn[] columns = (await state.ProjectsClient.Column.GetAll(project.Id)).ToArray();
        state.ProjectColumns[projectId] = columns;

        return columns;
    }

    /// <summary>
    /// Adds the specified labels to the provided <see cref="State.Issue"/>.
    /// </summary>
    /// <param name="labels">A list of labels to add.</param>
    /// <param name="state">The state object of the Azure Function.</param>
    /// <returns>An empty task.</returns>
    public static async Task AddLabels(string[] labels, State state)
    {
        if (labels.Length != 0)
        {
            state.Logger.LogInformation($"GitHub: Labels added: {string.Join(",", labels)}");
            await state.Client.Issue.Labels.AddToIssue(state.RepositoryId, state.Issue.Number, labels.Select(state.ExpandVariables).ToArray());
        }
        else
            state.Logger.LogTrace("No labels to add");
    }

    /// <summary>
    /// Removes labels from the provided <see cref="State.Issue"/>.
    /// </summary>
    /// <param name="labels">An array of labels to remove from the issue.</param>
    /// <param name="existingLabels">Labels from the issue.</param>
    /// <param name="state">The state object of the Azure Function.</param>
    /// <returns>An empty task.</returns>
    public static async Task RemoveLabels(string[] labels, IReadOnlyList<Label> existingLabels, State state)
    {
        IEnumerable<string> existingLabelsTransformed = existingLabels.Select(l => l.Name.ToLower());

        if (labels.Length != 0 && existingLabels != null && existingLabels.Count != 0)
        {
            List<string> removedLabels = new();

            foreach (string label in labels)
                if (existingLabelsTransformed.Contains(state.ExpandVariables(label).ToLower()))
                    removedLabels.Add(label);

            state.Logger.LogInformation($"GitHub: Labels removed: {string.Join(",", labels)}");

            foreach (string label in removedLabels)
                await state.Client.Issue.Labels.RemoveFromIssue(state.RepositoryId, state.Issue.Number, label);
        }
        else
            state.Logger.LogTrace("No labels to remove");
    }

    /// <summary>
    /// Adds assignees to the provided <see cref="State.Issue"/>.
    /// </summary>
    /// <param name="names">The login names of the users.</param>
    /// <param name="state">The state object of the Azure Function.</param>
    /// <returns>An empty task</returns>
    public static async Task AddAssignees(string[] names, State state)
    {
        if (names.Length != 0)
        {
            state.Logger.LogInformation($"GitHub: Adding assignees: {string.Join(",", names)}");

            IssueUpdate updateIssue = state.Issue.ToUpdate();
            
            foreach (string item in names)
                updateIssue.AddAssignee(state.ExpandVariables(item));

            await state.Client.Issue.Update(state.RepositoryId, state.Issue.Number, updateIssue);
        }
    }

    /// <summary>
    /// Adds reviewers to the provided <see cref="State.PullRequest"/>.
    /// </summary>
    /// <param name="names">The names of the reviewers. Teams must start with 'team:'.</param>
    /// <param name="state">The state object of the Azure Function.</param>
    /// <returns>An empty task</returns>
    public static async Task AddReviewers(string[] names, State state)
    {
        if (names.Length != 0)
        {
            List<string>? logins = new();
            List<string>? teams = new();

            foreach (string name in names)
            {
                if (name.StartsWith("team:", StringComparison.OrdinalIgnoreCase))
                    teams.Add(state.ExpandVariables(name.Substring(5)));
                else
                    logins.Add(state.ExpandVariables(name));
            }

            if (logins.Count != 0)
                state.Logger.LogInformation($"GitHub: Adding reviewers: {string.Join(",", logins)}");
            else
                logins = null;

            if (teams.Count != 0)
                state.Logger.LogInformation($"GitHub: Adding reviewer teams: {string.Join(",", teams)}");
            else
                teams = null;

            await state.Client.PullRequest.ReviewRequest.Create(state.RepositoryId, state.Issue.Number, new PullRequestReviewRequest(logins, teams));
        }
    }

    /// <summary>
    /// Creates a comment on an issue or pull request.
    /// </summary>
    /// <param name="comment">The comment body string.</param>
    /// <param name="state">The state object of the Azure Function.</param>
    public static async Task AddComment(string comment, State state)
    {
        state.Logger.LogInformation($"GitHub: Create comment");
        await state.Client.Issue.Comment.Create(state.RepositoryId, state.Issue.Number, state.ExpandVariables(comment));
    }

    /// <summary>
    /// Assigns a milestone to the provided <see cref="State.Issue"/>.
    /// </summary>
    /// <param name="milestone">The milestone id.</param>
    /// <param name="state">The state object of the Azure Function.</param>
    /// <returns>An empty task.</returns>
    public static async Task SetMilestone(int milestone, State state)
    {
        IssueUpdate updateIssue = state.Issue.ToUpdate();
        updateIssue.Milestone = milestone;
        state.Logger.LogInformation($"GitHub: Set milestone to {milestone}");
        await state.Client.Issue.Update(state.RepositoryId, state.Issue.Number, updateIssue);
    }

    /// <summary>
    /// Removes the milestone.
    /// </summary>
    /// <param name="milestone">The milestone id.</param>
    /// <param name="state">The state object of the Azure Function.</param>
    /// <returns>An empty task.</returns>
    public static async Task RemoveMilestone(State state)
    {
        IssueUpdate updateIssue = state.Issue.ToUpdate();
        updateIssue.Milestone = null;
        state.Logger.LogInformation($"GitHub: Clearing milestone");
        await state.Client.Issue.Update(state.RepositoryId, state.Issue.Number, updateIssue);
    }

    /// <summary>
    /// Assigns projects (classic) to the provided <see cref="State.Issue"/>.
    /// </summary>
    /// <param name="projects">The project numbers to set.</param>
    /// <param name="state">The state object of the Azure Function.</param>
    /// <returns>An empty task.</returns>
    public static async Task AddProjects(string[] projects, State state)
    {
        // TODO: Move this logic of checking for existing projects into the loader.
        // No wait.. maybe this is wrong having this in the loader. If its in the loader, it's loaded every time, even if 
        // the logic path doens't work with projects.. Ah!! Move milestone logic back here.
        if (projects.Length != 0)
        {
            foreach (string projectString in projects)
            {
                if (!int.TryParse(projectString, out int id))
                {
                    state.Logger.LogError($"Trying to add project with an invalid project id: {projectString}");
                    continue;
                }

                bool foundProject = false;

                foreach (Project projectObject in state.Projects)
                {
                    if (projectObject.Number == id)
                    {
                        ProjectColumn[]? columns = await GetProjectColumns(state, id);

                        if (columns != null)
                        {
                            NewProjectCard projectCard;

                            if (state.RequestType == RequestType.Issue)
                                projectCard = new NewProjectCard(state.Issue.Id, ProjectCardContentType.Issue);
                            else
                                projectCard = new NewProjectCard(state.PullRequest!.Id, ProjectCardContentType.PullRequest);

                            state.Logger.LogInformation($"GitHub: Project added: {projectObject.Number}:{projectObject.Name}");
                            await state.ProjectsClient.Card.Create(columns[0].Id, projectCard);

                            foundProject = true;
                            break;
                        }

                        state.Logger.LogError($"Some how project {projectString} doesn't have any columns.");
                    }
                }

                if (!foundProject)
                    state.Logger.LogError($"Project ID not found in repository: {projectString}");
            }
        }
        else
            state.Logger.LogTrace("No projects to add");
    }
}
