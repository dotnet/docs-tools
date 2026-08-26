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

type BuildDiagnostic = {
    path: string;
    line: number;
    severity: "Error" | "Warning" | "Suggestion";
    message: string;
};

const ANNOTATION_BATCH_SIZE = 50;

export async function tryUpdatePullRequestBody(token: string) {
    try {
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

        const buildReportHtml = await downloadUrl(opsCheck.detailsUrl);
        if (!buildReportHtml) {
            info("Unable to download OPS build report HTML.");
            return;
        }

        const previewLinks =
            extractPreviewLinksFromBuildReport(buildReportHtml);
        if (previewLinks.size === 0) {
            info("No preview links found in OPS build report.");
            return;
        }

        const graphQLResponse = await getPullRequest(token);
        if (!graphQLResponse) {
            info("Unable to get the pull request from GitHub GraphQL");
            return;
        }

        const prDetails = graphQLResponse.repository?.pullRequest;
        if (!prDetails) {
            info("Unable to pull request details from object graph.");
            return;
        }

        const prNumber = context.payload.pull_request?.number;
        if (prNumber === undefined) {
            info("Unable to resolve the pull request number.");
            return;
        }
        info(`Update pull ${prNumber} request body.`);

        try {
            startGroup("Pull request JSON body");
            info(JSON.stringify(prDetails, undefined, 2));
            endGroup();
        } catch {
            endGroup();
        }

        const markdownTable = buildMarkdownPreviewTableFromExtractedLinks(
            previewLinks,
            commitOid,
            prDetails.checksUrl
        );

        // Generate the updated body text.
        let updatedBody =
            prDetails.body.includes(PREVIEW_TABLE_START) &&
            prDetails.body.includes(PREVIEW_TABLE_END) ?
                replaceExistingTable(prDetails.body, markdownTable) :
                appendTable(prDetails.body, markdownTable);

        // Add or update the build report link.
        const buildReportLinkPattern = /\[Build report\]\([^)]+\)/;
        if (buildReportLinkPattern.test(updatedBody)) {
            updatedBody = updatedBody.replace(
                buildReportLinkPattern,
                `[Build report](${opsCheck.detailsUrl})`
            );
        } else {
            updatedBody += `\n\n[Build report](${opsCheck.detailsUrl})`;
        }

        startGroup("Proposed PR body");
        info(updatedBody);
        endGroup();

        // Update the pull request body with the new content.
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

        // Add build warning annotations to changed lines in the PR.
        if (workflowInput.annotateFiles) {
            try {
                await annotateChangedLines(
                    token,
                    prNumber,
                    commitOid,
                    buildReportHtml,
                    opsCheck.detailsUrl
                );
            } catch (error) {
                warning(`Unable to annotate changed files: ${error}`);
            }
        }
    } catch (error) {
        warning(`Encountered error: ${error}`);
    } finally {
        info("Finished work.");
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
        ? Math.max(
            1,
            Math.floor((maxWaitTimeMinutes * 60_000) / pollDelayMs) + 1
        )
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
    const hasPreviewHeader = headerValues.some((_) =>
        _.includes("preview url")
    );

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
        .replace(/&amp;/g, "&")
        .replace(/&lt;/g, "<")
        .replace(/&gt;/g, ">")
        .replace(/&quot;/g, '"')
        .replace(/&#39;/g, "'")
        .replace(/\s+/g, " ");
}

function extractDiagnosticsFromBuildReport(html: string): BuildDiagnostic[] {
    const diagnostics: BuildDiagnostic[] = [];
    const tables = html.match(/<table[\s\S]*?<\/table>/gi) ?? [];

    for (const table of tables) {
        const rows = table.match(/<tr[\s\S]*?<\/tr>/gi) ?? [];
        if (rows.length === 0) {
            continue;
        }

        const headers = extractCellText(rows[0] ?? "").map((header) =>
            header.toLowerCase()
        );
        const fileIndex = headers.indexOf("file");
        const statusIndex = headers.indexOf("status");
        const detailsIndex = headers.indexOf("details");
        if (fileIndex < 0 || statusIndex < 0 || detailsIndex < 0) {
            continue;
        }

        for (const row of rows.slice(1)) {
            const cells = extractCellText(row);
            const path = cells[fileIndex]?.trim();
            const details = cells[detailsIndex] ?? "";
            if (!path || !cells[statusIndex]) {
                continue;
            }

            const detailPattern =
                /Line\s+(\d+)\s*:\s*\[(Error|Warning|Suggestion)\]\s*([\s\S]*?)(?=\s+Line\s+\d+\s*:\s*\[(?:Error|Warning|Suggestion)\]|$)/gi;
            for (const match of details.matchAll(detailPattern)) {
                const line = Number(match[1]);
                if (line > 0) {
                    const severity = match[2].toLowerCase();
                    diagnostics.push({
                        path,
                        line,
                        severity:
                            severity === "error"
                                ? "Error"
                                : severity === "warning"
                                    ? "Warning"
                                    : "Suggestion",
                        message: match[3].trim(),
                    });
                }
            }
        }

        if (diagnostics.length > 0) {
            return diagnostics;
        }
    }

    return diagnostics;
}

function extractCellText(rowHtml: string): string[] {
    const cells = rowHtml.match(/<t[dh][\s\S]*?<\/t[dh]>/gi) ?? [];
    return cells.map((cell) => decodeHtmlEntities(stripTags(cell)).trim());
}

function extractChangedLinesFromPatch(patch: string): Set<number> {
    const changedLines = new Set<number>();
    let newLine = 0;

    for (const patchLine of patch.split("\n")) {
        const hunk = patchLine.match(/^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@/);
        if (hunk) {
            newLine = Number(hunk[1]);
            continue;
        }

        if (patchLine.startsWith("+") && !patchLine.startsWith("+++")) {
            changedLines.add(newLine);
            newLine++;
        } else if (
            !patchLine.startsWith("-") &&
            !patchLine.startsWith("\\ No newline") &&
            newLine > 0
        ) {
            newLine++;
        }
    }

    return changedLines;
}

function filterDiagnosticsToChangedLines(
    diagnostics: BuildDiagnostic[],
    changedLinesByPath: Map<string, Set<number>>
): BuildDiagnostic[] {
    return diagnostics.filter((diagnostic) =>
        changedLinesByPath.get(diagnostic.path)?.has(diagnostic.line)
    );
}

async function annotateChangedLines(
    token: string,
    pullNumber: number,
    commitOid: string,
    buildReportHtml: string,
    buildReportUrl: string
): Promise<void> {
    const diagnostics = extractDiagnosticsFromBuildReport(buildReportHtml);
    if (diagnostics.length === 0) {
        info(
            "No suggestions, warnings, or errors with line numbers found in validated files."
        );
        return;
    }

    const octokit = getOctokit(token);
    const files = await octokit.paginate(octokit.rest.pulls.listFiles, {
        owner: context.repo.owner,
        repo: context.repo.repo,
        pull_number: pullNumber,
        per_page: 100,
    });
    const changedLinesByPath = new Map<string, Set<number>>();
    for (const file of files) {
        if (file.patch) {
            changedLinesByPath.set(
                file.filename,
                extractChangedLinesFromPatch(file.patch)
            );
        }
    }

    const filteredDiagnostics = filterDiagnosticsToChangedLines(
        diagnostics,
        changedLinesByPath
    );
    if (filteredDiagnostics.length === 0) {
        info("No build errors, warnings, or suggestions occur on changed PR lines.");
        return;
    }

    const annotations = filteredDiagnostics.map((diagnostic) => ({
        path: diagnostic.path,
        start_line: diagnostic.line,
        end_line: diagnostic.line,
        annotation_level: (diagnostic.severity === "Error"
            ? "failure"
            : diagnostic.severity === "Warning"
                ? "warning"
                : "notice") as "failure" | "warning" | "notice",
        title: `OPS ${diagnostic.severity}`,
        message: diagnostic.message,
    }));
    const firstBatch = annotations.slice(0, ANNOTATION_BATCH_SIZE);
    const response = await octokit.rest.checks.create({
        owner: context.repo.owner,
        repo: context.repo.repo,
        name: "OpenPublishing.Build annotations",
        head_sha: commitOid,
        status: "completed",
        conclusion: "neutral",
        details_url: buildReportUrl,
        output: {
            title: "OpenPublishing.Build diagnostics",
            summary: `${annotations.length} error(s), warning(s), or suggestion(s) found on changed lines.`,
            annotations: firstBatch,
        },
    });

    for (
        let index = ANNOTATION_BATCH_SIZE;
        index < annotations.length;
        index += ANNOTATION_BATCH_SIZE
    ) {
        await octokit.rest.checks.update({
            owner: context.repo.owner,
            repo: context.repo.repo,
            check_run_id: response.data.id,
            output: {
                title: "OpenPublishing.Build diagnostics",
                summary: `${annotations.length} error(s), warning(s), or suggestion(s) found on changed lines.`,
                annotations: annotations.slice(
                    index,
                    index + ANNOTATION_BATCH_SIZE
                ),
            },
        });
    }

    info(`Annotated ${annotations.length} changed line(s).`);
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
        const previewTitle = "Learn preview";
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
    extractChangedLinesFromPatch,extractDiagnosticsFromBuildReport,
    extractPreviewLinksFromBuildReport,
    filterDiagnosticsToChangedLines,
    PREVIEW_TABLE_END,
    PREVIEW_TABLE_START,
    replaceExistingTable,
};
