import { Octokit } from "@octokit/rest";
import { getGitHubSessionToken } from "./github-auth-service";

export interface Issue {
    title: string | null;
    body: string | null | undefined;
}

export async function getIssue(url: string): Promise<Issue | undefined> {
    const accessToken = await getGitHubSessionToken();

    if (!accessToken) {
        return undefined;
    }

    const octokit = new Octokit({
        auth: accessToken
    });

    const issueUrl = new URL(url);
    const [owner, repo, _, issueNumber] = issueUrl.pathname.split("/").filter(Boolean);

    const issue = await octokit.issues.get({
        owner,
        repo,
        issue_number: parseInt(issueNumber)
    });

    return {
        title: issue.data.title.replace("[Breaking change]: ", ""),
        body: issue.data.body
    };
}
