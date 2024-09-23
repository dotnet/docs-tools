using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace DotNetDocs.RepoMan.Actions;

internal sealed class LinkRelatedIssues : IRunnerItem
{
    public LinkRelatedIssues()
    {
    }

    public async Task Run(InstanceData data)
    {
        data.Logger.LogInformation("RUN [LINK-RELATED-ISSUES]");

        if (data.IssuePrBody is not null)
        {
            if (data.IssuePrBody.Contains("[Related Issues](") is false)
            {
                string relatedIssuesQuery = string.Format("https://github.com/{0}/{1}/issues?q=is%3Aissue+is%3Aopen+{2}", data.RepositoryOwner, data.RepositoryName, data.DocIssueMetadata["document_version_independent_id"]);
                data.Logger.LogInformation("Adding link to related issues: {link}", relatedIssuesQuery);
                await GitHubCommands.IssuePullRequest.UpdateBody(data, $"{data.IssuePrBody}\n\n[Related Issues]({relatedIssuesQuery})");
            }
            else
                data.Logger.LogInformation("Related issues already linked");
        }
        else
        {
            data.Logger.LogInformation("Body text not available to modify");
            data.HasFailure = true;
            data.FailureMessage = "Tried to link related issues, but body text isn't aviailable";
        }
    }
}
