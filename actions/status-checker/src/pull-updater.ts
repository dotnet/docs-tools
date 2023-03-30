import { context, getOctokit } from "@actions/github";
import { FileChange } from "./types/FileChange";
import { Pull } from "./types/Pull";
import { PullRequestDetails } from "./types/PullRequestDetails";
import { NodeOf } from "./types/NodeOf";
import { workflowInput } from "./types/WorkflowInput";

const PREVIEW_TABLE_START = "<!-- PREVIEW-TABLE-START -->";
const PREVIEW_TABLE_END = "<!-- PREVIEW-TABLE-END -->";

export async function tryUpdatePullRequestBody(token: string) {
  try {
    const prNumber: number = context.payload.number;
    console.log(`Update pull ${prNumber} request body.`);

    const details = await getPullRequest(token);
    if (!details) {
      console.log("Unable to get the pull request from GitHub GraphQL");
    }

    const pr = details.repository?.pullRequest;
    if (!pr) {
      console.log("Unable to pull request details from object-graph.");
    }

    if (pr.changedFiles == 0) {
      console.log("No files changed at all...");
      return;
    } else {
      try {
        console.log(JSON.stringify(pr, undefined, 2));
      } catch {}
    }

    if (isPullRequestModifyingMarkdownFiles(pr) == false) {
      console.log("No updated markdown files...");
      return;
    }

    const { files, exceedsMax } = getModifiedMarkdownFiles(pr);
    const commitOid = context.payload.pull_request?.head.sha;
    const markdownTable = buildMarkdownPreviewTable(
      prNumber,
      files,
      pr.checksUrl,
      commitOid,
      exceedsMax
    );

    let updatedBody = "";
    if (
      pr.body.includes(PREVIEW_TABLE_START) &&
      pr.body.includes(PREVIEW_TABLE_END)
    ) {
      // Replace existing preview table.
      updatedBody = replaceExistingTable(pr.body, markdownTable);
    } else {
      // Append preview table to bottom.
      updatedBody = appendTable(pr.body, markdownTable);
    }

    console.log("Proposed PR body:");
    console.log(updatedBody);

    const octokit = getOctokit(token);
    const response = await octokit.rest.pulls.update({
      owner: context.repo.owner,
      repo: context.repo.repo,
      pull_number: prNumber,
      body: updatedBody,
    });

    if (response && response.status === 200) {
      console.log("Pull request updated...");
    } else {
      console.log("Unable to update pull request...");
    }
  } catch (error) {
    console.log(`Unable to process markdown preview: ${error}`);
  } finally {
    console.log("Finished attempting to generate preview.");
  }
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
          files(first: 100) {
            edges {
              node {
                additions
                changeType
                deletions
                path
              }
            }
          }
        }
      }
    }`,
    name: context.repo.repo,
    owner: context.repo.owner,
    number: context.payload.number,
  });
}

function isFilePreviewable(_: NodeOf<FileChange>) {
  return (
    _.node.changeType == "ADDED" ||
    _.node.changeType == "CHANGED" ||
    _.node.changeType == "MODIFIED" ||
    _.node.changeType == "RENAMED"
  );
}

function isPullRequestModifyingMarkdownFiles(pr: Pull): boolean {
  return (
    pr &&
    pr.changedFiles > 0 &&
    pr.files &&
    pr.files.edges &&
    pr.files.edges.some(
      (_) => isFilePreviewable(_) && _.node.path.endsWith(".md")
    )
  );
}

/**
 * Gets the modified markdown files using the following filtering rules:
 * -  It's a markdown file, that isn't an "include", and is considered previewable.
 * -  Files are sorted by most changes in descending order, a max number of files are returned.
 * -  The remaining files are then sorted alphabetically.
 */
function getModifiedMarkdownFiles(pr: Pull): {
  files: FileChange[];
  exceedsMax: boolean;
} {
  const modifiedFiles = pr.files.edges
    .filter(
      (_) =>
        _.node.path.endsWith(".md") &&
        _.node.path.includes("includes/") === false &&
        isFilePreviewable(_)
    )
    .map((_) => _.node);

  const exceedsMax = modifiedFiles.length > workflowInput.maxRowCount;
  const mostChanged = sortByMostChanged(modifiedFiles, true);
  const sorted = sortAlphabetically(
    mostChanged.slice(0, workflowInput.maxRowCount)
  );

  return { files: sorted, exceedsMax };
}

function sortByMostChanged(
  files: FileChange[],
  descending?: boolean
): FileChange[] {
  return files.sort((a, b) => {
    const aChanges = a.additions + a.deletions;
    const bChanges = b.additions + b.deletions;

    return descending ? bChanges - aChanges : aChanges - bChanges;
  });
}

function sortAlphabetically(files: FileChange[]): FileChange[] {
  return files.sort((a, b) => a.path.localeCompare(b.path));
}

function toGitHubLink(
  file: string,
  commitOid: string | undefined | null
): string {
  const owner = context.repo.owner;
  const repo = context.repo.repo;

  return !!commitOid
    ? `https://github.com/${owner}/${repo}/blob/${commitOid}/${file}`
    : `_${file}_`;
}

function toPreviewLink(file: string, prNumber: number): string {
  const docsPath = workflowInput.docsPath;

  let path = file.replace(`${docsPath}/`, "").replace(".md", "");
  const opaqueLeadingUrlSegments: Map<string, string> =
    workflowInput.opaqueLeadingUrlSegments;

  let queryString = "";
  for (let [key, query] of opaqueLeadingUrlSegments) {
    const segment = `${key}/`;
    if (path.startsWith(segment)) {
      path = path.replace(segment, "");
      queryString = query;
      break;
    }
  }

  const urlBasePath = workflowInput.urlBasePath;
  const qs = queryString ? `&${queryString}` : "";

  return `https://review.learn.microsoft.com/en-us/${urlBasePath}/${path}?branch=pr-en-us-${prNumber}${qs}`;
}

function buildMarkdownPreviewTable(
  prNumber: number,
  files: FileChange[],
  checksUrl: string,
  commitOid: string | undefined | null,
  exceedsMax: boolean = false
): string {
  const links = new Map<string, string>();
  files.forEach((file) => {
    links.set(file.path, toPreviewLink(file.path, prNumber));
  });

  let markdownTable = "#### Internal previews\n\n";
  const isCollapsible = (workflowInput.collapsibleAfter ?? 10) < links.size;
  if (isCollapsible) {
    markdownTable +=
      "<details><summary><strong>Toggle expand/collapse</strong></summary><br/>\n\n";
  }

  markdownTable += "| 📄 File | 🔗 Preview link |\n";
  markdownTable += "|:--|:--|\n";

  links.forEach((link, file) => {
    markdownTable += `| [${file}](${toGitHubLink(
      file,
      commitOid
    )}) | [${file.replace(".md", "")}](${link}) |\n`;
  });

  if (isCollapsible) {
    markdownTable += "\n</details>\n";
  }

  if (exceedsMax /* include footnote when we're truncating... */) {
    markdownTable += `\nThis table shows preview links for the ${workflowInput.maxRowCount} files with the most changes. For preview links for other files in this PR, select <strong>OpenPublishing.Build Details</strong> within [checks](${checksUrl}).\n`;
  }

  return markdownTable;
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

  return `${start}

---

${table}

${tail}`;
}

function appendTable(body: string, table: string) {
  return `${body}

${PREVIEW_TABLE_START}

---

${table}
${PREVIEW_TABLE_END}`;
}

export const exportedForTesting = {
  appendTable,
  buildMarkdownPreviewTable,
  getModifiedMarkdownFiles,
  isFilePreviewable,
  isPullRequestModifyingMarkdownFiles,
  PREVIEW_TABLE_END,
  PREVIEW_TABLE_START,
  replaceExistingTable,
  toPreviewLink,
};
