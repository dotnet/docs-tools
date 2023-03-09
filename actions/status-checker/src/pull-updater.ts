import { context, getOctokit } from "@actions/github";
import { ChangeType } from "./types/ChangeType";
import { FileChange } from "./types/FileChange";
import { Pull } from "./types/Pull";
import { PullRequestDetails } from "./types/PullRequestDetails";
import { NodeOf } from "./types/NodeOf";

const PREVIEW_TABLE_START = '<!-- PREVIEW-TABLE-START -->';
const PREVIEW_TABLE_END = '<!-- PREVIEW-TABLE-END -->';

export async function tryUpdatePullRequestBody(token: string) {
  try {
    const prNumber: number = context.payload.number;
    console.log(`Update pull ${prNumber} request body.`);

    const details = await getPullRequest(token);
    if (details) {
      console.log(details);

      const pr = details.data?.repository?.pullRequest;
      if (pr) {
        if (pr.changedFiles == 0) {
          console.log('No files changed at all...');
          return;
        }

        if (isPullRequestModifyingMarkdownFiles(pr) == false) {
          console.log('No updated markdown files...');
          return;
        }

        const modifiedMarkdownFiles = getModifiedMarkdownFiles(pr);
        const markdownTable = buildMarkdownPreviewTable(prNumber, modifiedMarkdownFiles);

        let updatedBody = '';
        if (pr.body.includes(PREVIEW_TABLE_START) && pr.body.includes(PREVIEW_TABLE_END)) {
          // Replace existing preview table.
          updatedBody = replaceExistingTable(pr.body, markdownTable);
        } else {
          // Append preview table to bottom.
          updatedBody = appendTable(pr.body, markdownTable);
        }

        const octokit = getOctokit(token);
        const response = await octokit.rest.pulls.update({
          owner: context.repo.owner,
          repo: context.repo.repo,
          pull_number: prNumber,
          body: updatedBody
        });

        if (response && response.status === 200) {
          console.log('Pull request updated...');
        } else {
          console.log('Unable to update pull request...')
        }
      } else {
        console.log('Unable to pull request details from object-graph.');
      }
    } else {
      console.log('Unable to get the pull request from GitHub GraphQL');
    }
  } catch (error) {
    console.log(`Unable to process markdown preview: ${error}`);
    const e: Error = error as Error;
    if (e) {
      console.log(e.message);
    }
  } finally {
    console.log('Finished attempting to generate preview.');
  }
}

async function getPullRequest(token: string): Promise<PullRequestDetails> {
  const octokit = getOctokit(token);
  return await octokit.graphql<PullRequestDetails>({
    query: `query getPullRequest($name: String!, $owner: String!, $number: Int!) {
      repository(name: $name, owner: $owner) {
        pullRequest(number: $number) {
          body
          changedFiles
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
    number: context.payload.number
  });
}

function isFileModified(_: NodeOf<FileChange>) {
  return _.node.changeType == ChangeType.ADDED
    || _.node.changeType == ChangeType.CHANGED
    || _.node.changeType == ChangeType.MODIFIED;
}

function isPullRequestModifyingMarkdownFiles(pr: Pull): boolean {
  return pr
    && pr.changedFiles > 0
    && pr.files
    && pr.files.edges
    && pr.files.edges.length > 0
    && pr.files.edges.some(_ => isFileModified(_) && _.node.path.endsWith(".md"));
}

function getModifiedMarkdownFiles(pr: Pull): string[] {
  return pr.files.edges
    .filter(_ => _.node.path.endsWith(".md") && isFileModified(_))
    .map(_ => _.node.path);
}

function buildMarkdownPreviewTable(prNumber: number, files: string[]): string {
  // Given: docs/orleans/resources/nuget-packages.md
  // https://review.learn.microsoft.com/en-us/dotnet/orleans/resources/nuget-packages?branch=pr-en-us-34443
  // TODO: consider being a bit smarter about this, don't assume "dotnet" and "docs".
  const toLink = (file: string): string => {
    const path = file.replace('docs/', '').replace('.md', '');
    return `https://review.learn.microsoft.com/en-us/dotnet/${path}?branch=pr-en-us-${prNumber}`;
  };

  const links = new Map<string, string>();
  files.sort((a, b) => a.localeCompare(b)).forEach(file => {
    links.set(file, toLink(file));
  });

  let markdownTable = '| File | Preview |\n';
  markdownTable += '|:--|:--|\n';

  links.forEach((link, file) => {
    markdownTable += `| 📄 _${file}_ | 🔗 [Preview: ${file.replace('.md', '')}](${link}) |\n`;
  });

  return markdownTable;
}

function replaceExistingTable(body: string, table: string) {
  const startIndex = body.indexOf(PREVIEW_TABLE_START);
  if (startIndex === -1) {
    return "Unable to parse starting index of existing markdown table."
  }
  const endIndex = body.lastIndexOf(PREVIEW_TABLE_END);
  if (endIndex === -1) {
    return "Unable to parse ending index of existing markdown table."
  }
  const start = body.substring(0, startIndex + PREVIEW_TABLE_START.length);
  const tail = body.substring(endIndex);

  return `${start}

${table}

${tail}`;
}

function appendTable(body: string, table: string) {
  return `${body}

${table}`;
}

export const exportedForTesting = {
  appendTable,
  buildMarkdownPreviewTable,
  getModifiedMarkdownFiles,
  isFileModified,
  isPullRequestModifyingMarkdownFiles,
  PREVIEW_TABLE_END,
  PREVIEW_TABLE_START,
  replaceExistingTable,
}