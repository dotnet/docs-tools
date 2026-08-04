import { info, warning, startGroup, endGroup } from "@actions/core";
import { context, getOctokit } from "@actions/github";
import * as https from "https";
import { PullRequestDetails } from "./types/PullRequestDetails";
import { workflowInput } from "./types/WorkflowInput";

const PREVIEW_TABLE_START = "<!-- PREVIEW-TABLE-START -->";
const PREVIEW_TABLE_END = "<!-- PREVIEW-TABLE-END -->";
const OPS_CHECK_NAME = "OpenPublishing.Build";
const OPS_POLL_DELAY_MS = 30_000;

type StatusCheck = {
    name: string;
    status: string;
    detailsUrl: string | null;
};

export async function tryUpdatePullRequestBody(token: string) {
    try {
        const prNumber: number = context.payload.number;
        info(`Update pull ${prNumber} request body.`);

        const details = await getPullRequest(token);
        if (!details) {
            info("Unable to get the pull request from GitHub GraphQL");
            return;
        }

        const pullRequest = details.repository?.pullRequest;
        if (!pullRequest) {
            info("Unable to pull request details from object-graph.");
            return;
        }

        if (pullRequest.changedFiles === 0) {
            info("No files changed at all...");
            return;
        }

        try {
            startGroup("Pull request JSON body");
            info(JSON.stringify(pullRequest, undefined, 2));
            endGroup();
        } catch {
            endGroup();
        }

        const commitOid = context.payload.pull_request?.head.sha;
        if (!commitOid) {
            info("Unable to resolve PR head commit SHA.");
            return;
        }

        const opsCheck = await waitForStatusCheck(
            token,
            commitOid,
            OPS_CHECK_NAME,
            calculateMaxPollAttempts(
                workflowInput.maxWaitTimeMinutes,
                OPS_POLL_DELAY_MS
            ),
            OPS_POLL_DELAY_MS
        );

        if (!opsCheck || !opsCheck.detailsUrl) {
            info(
                `Unable to find a completed ${OPS_CHECK_NAME} status check with a build report URL.`
            );
            return;
        }

        if (opsCheck.status !== "success") {
            info(
                `${OPS_CHECK_NAME} completed with status '${opsCheck.status}'. Skipping preview table update.`
            );
            return;
        }

        const buildReportHtml = await downloadUrl(opsCheck.detailsUrl);
        if (!buildReportHtml) {
            info("Unable to download OPS build report HTML.");
            return;
        }

        const previewLinks = extractPreviewLinksFromBuildReport(buildReportHtml);
        if (previewLinks.size === 0) {
            info("No preview links found in OPS build report.");
            return;
        }

        const markdownTable = buildMarkdownPreviewTableFromExtractedLinks(
            previewLinks,
            commitOid,
            pullRequest.checksUrl
        );

        const updatedBody =
            pullRequest.body.includes(PREVIEW_TABLE_START) &&
            pullRequest.body.includes(PREVIEW_TABLE_END)
                ? replaceExistingTable(pullRequest.body, markdownTable)
                : appendTable(pullRequest.body, markdownTable);

        startGroup("Proposed PR body");
        info(updatedBody);
        endGroup();

        const octokit = getOctokit(token);
        const response = await octokit.rest.pulls.update({
            owner: context.repo.owner,
            repo: context.repo.repo,
            pull_number: prNumber,
            body: updatedBody,
        });

        if (response && response.status === 200) {
            info("Pull request updated...");
        } else {
            info("Unable to update pull request...");
        }
    } catch (error) {
        warning(`Unable to process markdown preview: ${error}`);
    } finally {
        info("Finished attempting to generate preview.");
    }
}

async function waitForStatusCheck(
    token: string,
    commitSha: string,
    checkName: string,
    maxAttempts: number,
    pollDelayMs: number
): Promise<StatusCheck | null> {
    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
        const check = await getSpecificStatusCheck(token, commitSha, checkName);
        if (check) {
            info(
                `Found ${checkName} status check (${check.status}) on attempt ${attempt}/${maxAttempts}.`
            );

            if (check.status === "success") {
                return check;
            }

            if (check.status === "failure" || check.status === "error") {
                return check;
            }
        } else {
            info(
                `${checkName} status check not found on attempt ${attempt}/${maxAttempts}.`
            );
        }

        if (attempt < maxAttempts) {
            await delay(pollDelayMs);
        }
    }

    return null;
}

async function getSpecificStatusCheck(
    token: string,
    commitSha: string,
    checkName: string
): Promise<StatusCheck | null> {
    const octokit = getOctokit(token);
    const response = await octokit.rest.repos.getCombinedStatusForRef({
        owner: context.repo.owner,
        repo: context.repo.repo,
        ref: commitSha,
    });

    const exactMatch = response.data.statuses.find(
        (status) => status.context === checkName
    );

    if (!exactMatch) {
        return null;
    }

    return {
        name: exactMatch.context,
        status: exactMatch.state,
        detailsUrl: exactMatch.target_url,
    };
}

function delay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

function calculateMaxPollAttempts(
    maxWaitTimeMinutes: number,
    pollDelayMs: number
): number {
     return pollDelayMs > 0
         ? Math.max(1, Math.floor((maxWaitTimeMinutes * 60_000) / pollDelayMs) + 1)
         : 1;
}

async function downloadUrl(url: string): Promise<string> {
    return await new Promise((resolve) => {
        const request = https.get(url, (response) => {
            const statusCode = response.statusCode ?? 0;

            if (
                statusCode >= 300 &&
                statusCode < 400 &&
                response.headers.location
            ) {
                response.resume();
                downloadUrl(response.headers.location)
                    .then(resolve)
                    .catch(() => resolve(""));
                return;
            }

            if (statusCode < 200 || statusCode >= 300) {
                response.resume();
                resolve("");
                return;
            }

            let data = "";
            response.setEncoding("utf8");
            response.on("data", (chunk) => {
                data += chunk;
            });
            response.on("end", () => resolve(data));
        });

        request.on("error", () => resolve(""));
    });
}

function extractPreviewLinksFromBuildReport(html: string): Map<string, string> {
    const previewLinks = new Map<string, string>();
    if (!html) {
        return previewLinks;
    }

    const tables = html.match(/<table[\s\S]*?<\/table>/gi) ?? [];
    for (const table of tables) {
        if (!tableHasPreviewHeaders(table)) {
            continue;
        }

        const rows = table.match(/<tr[\s\S]*?<\/tr>/gi) ?? [];
        for (let i = 1; i < rows.length; i++) {
            const cells = rows[i].match(/<td[\s\S]*?<\/td>/gi) ?? [];
            if (cells.length < 3) {
                continue;
            }

            const fileCell = cells[0] ?? "";
            const previewCell = cells[2] ?? "";
            const file = decodeHtmlEntities(stripTags(fileCell)).trim();
            const previewHref = extractAnchorHref(previewCell);

            if (file && previewHref) {
                previewLinks.set(file, previewHref);
            }
        }

        if (previewLinks.size > 0) {
            return previewLinks;
        }
    }

    return previewLinks;
}

function tableHasPreviewHeaders(tableHtml: string): boolean {
    const firstRow = tableHtml.match(/<tr[\s\S]*?<\/tr>/i)?.[0] ?? "";
    const headerCells = firstRow.match(/<t[dh][\s\S]*?<\/t[dh]>/gi) ?? [];
    const headerValues = headerCells.map((cell) =>
        decodeHtmlEntities(stripTags(cell)).trim().toLowerCase()
    );

    const hasFileHeader = headerValues.some((_) => _ === "file");
    const hasPreviewHeader = headerValues.some((_) => _.includes("preview url"));

    return hasFileHeader && hasPreviewHeader;
}

function extractAnchorHref(cellHtml: string): string {
    const hrefMatch = cellHtml.match(/<a[^>]+href=["']([^"']+)["']/i);
    return hrefMatch?.[1]?.trim() ?? "";
}

function stripTags(input: string): string {
    return input.replace(/<[^>]*>/g, " ");
}

function decodeHtmlEntities(input: string): string {
    return input
        .replace(/&nbsp;/g, " ")
        .replace(/&lt;/g, "<")
        .replace(/&gt;/g, ">")
        .replace(/&quot;/g, '"')
        .replace(/&#39;/g, "'")
        .replace(/\s+/g, " ");
}

function buildMarkdownPreviewTableFromExtractedLinks(
    previewLinks: Map<string, string>,
    commitOid: string,
    checksUrl: string
): string {
    let markdownTable = "#### Internal previews\n\n";
    const isCollapsible =
        (workflowInput.collapsibleAfter ?? 10) < previewLinks.size;
    if (isCollapsible) {
        markdownTable +=
            "<details><summary><strong>Toggle expand/collapse</strong></summary><br/>\n\n";
    }

    markdownTable += "| File | Preview link |\n";
    markdownTable += "|:--|:--|\n";

    const sortedLinks = [...previewLinks.entries()].sort(([a], [b]) =>
        a.localeCompare(b)
    );
    const exceedsMax = sortedLinks.length > workflowInput.maxRowCount;
    const displayedLinks = sortedLinks.slice(0, workflowInput.maxRowCount);

    for (const [file, previewUrl] of displayedLinks) {
        const previewTitle = "Preview published page";
        markdownTable += `| [${file}](${toGitHubLink(
            file,
            commitOid
        )}) | [${previewTitle}](${previewUrl}) |\n`;
    }

    if (isCollapsible) {
        markdownTable += "\n</details>\n";
    }

    if (exceedsMax) {
        markdownTable += `\n> [!NOTE]\n> This table shows the first ${workflowInput.maxRowCount} preview links (sorted alphabetically by file path) found in the OPS build report. For the full list, select <strong>OpenPublishing.Build Details</strong> within [checks](${checksUrl}).\n`;
    }

    return markdownTable;
}

/**
 * Returns the {PullRequestDetails} that correspond to
 * the contextual GitHub Action workflow run.
 * @param token The GITHUB_TOKEN value to obtain an instance of octokit with.
 * @returns A {Promise} of {PullRequestDetails}.
 */
async function getPullRequest(token: string): Promise<PullRequestDetails> {
    const octokit = getOctokit(token);
    return await octokit.graphql<PullRequestDetails>({
        query: `query getPullRequest($name: String!, $owner: String!, $number: Int!) {
      repository(name: $name, owner: $owner) {
        pullRequest(number: $number) {
          body
          checksUrl
          changedFiles
          state
        }
      }
    }`,
        name: context.repo.repo,
        owner: context.repo.owner,
        number: context.payload.number,
    });
}

function toGitHubLink(
    file: string,
    commitOid: string | undefined | null
): string {
    const owner = context.repo.owner;
    const repo = context.repo.repo;

    return commitOid
        ? `https://github.com/${owner}/${repo}/blob/${commitOid}/${file}`
        : `_${file}_`;
}

function replaceExistingTable(body: string, table: string) {
    const startIndex = body.indexOf(PREVIEW_TABLE_START);
    if (startIndex === -1) {
        return "Unable to parse starting index of existing markdown table.";
    }
    const endIndex = body.lastIndexOf(PREVIEW_TABLE_END);
    if (endIndex === -1) {
        return "Unable to parse ending index of existing markdown table.";
    }
    const start = body.substring(0, startIndex + PREVIEW_TABLE_START.length);
    const tail = body.substring(endIndex);

    return `${start}\n\n---\n\n${table}\n\n${tail}`;
}

function appendTable(body: string, table: string) {
    return `${body}\n\n${PREVIEW_TABLE_START}\n\n---\n\n${table}\n${PREVIEW_TABLE_END}`;
}

export const exportedForTesting = {
    appendTable,
    buildMarkdownPreviewTableFromExtractedLinks,
    calculateMaxPollAttempts,
    extractPreviewLinksFromBuildReport,
    PREVIEW_TABLE_END,
    PREVIEW_TABLE_START,
    replaceExistingTable,
};
