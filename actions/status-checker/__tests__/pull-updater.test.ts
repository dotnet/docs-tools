import { exportedForTesting } from "../src/pull-updater";
import { describe, expect, it } from "@jest/globals";

const {
  appendTable,
  buildMarkdownPreviewTable,
  getModifiedMarkdownFiles,
  isFileModified,
  isPullRequestModifyingMarkdownFiles,
  PREVIEW_TABLE_END,
  PREVIEW_TABLE_START,
  replaceExistingTable,
} = exportedForTesting;

describe("pull-updater", () => {
  it("appendTable correctly appends table", () => {
    const body = "...";
    const actual = appendTable(body, "[table]");

    expect(actual).toEqual(`...

${PREVIEW_TABLE_START}
[table]
${PREVIEW_TABLE_END}`);
  });

  it("replaceExistingTable correctly replaces table", () => {
    const body = `...${PREVIEW_TABLE_START}

        [existing-table]

${PREVIEW_TABLE_END}

testing...1, 2, 3!`;
    const actual = replaceExistingTable(body, "[updated-table]");

    expect(actual).toEqual(`...${PREVIEW_TABLE_START}

[updated-table]

${PREVIEW_TABLE_END}

testing...1, 2, 3!`);
  });

  it("appendTable followed by replaceExistingTable correctly replaces table", () => {
    const body = "...";
    let actual = appendTable(body, "[table]");
    let expectedBody = `...

${PREVIEW_TABLE_START}
[table]
${PREVIEW_TABLE_END}`;

    expect(actual).toEqual(expectedBody);
    actual = appendTable(body, "[updated-table]");
    expectedBody = `...

${PREVIEW_TABLE_START}
[updated-table]
${PREVIEW_TABLE_END}`;

  expect(actual).toEqual(expectedBody);
  });

  it("buildMarkdownPreviewTable builds preview table correctly", () => {
    const actual = buildMarkdownPreviewTable(7, ["test/markdown.md"]);
    expect(actual).toEqual(
      "#### Internal previews\n\n| 📄 File(s) | 🔗 Preview link(s) |\n|:--|:--|\n| _test/markdown.md_ | [Preview: test/markdown](https://review.learn.microsoft.com/en-us/dotnet/test/markdown?branch=pr-en-us-7) |\n"
    );
  });

  it("isFileModified returns false when no file change types match", () => {
    expect(
      isFileModified({
        node: {
          deletions: 1,
          additions: 1,
          changeType: "DELETED",
          path: "path/to/file.md",
        },
      })
    ).toBe(false);
  });

  it("isFileModified returns true when file change types match", () => {
    expect(
      isFileModified({
        node: {
          deletions: 1,
          additions: 1,
          changeType: "MODIFIED",
          path: "path/to/file.md",
        },
      })
    ).toBe(true);
  });

  it("getModifiedMarkdownFiles gets only modified files", () => {
    const actual = getModifiedMarkdownFiles({
      body: "",
      changedFiles: 3,
      files: {
        edges: [
          {
            node: {
              deletions: 1,
              additions: 1,
              changeType: "RENAMED",
              path: "path/to/renamed-file.md",
            },
          },
          {
            node: {
              deletions: 1,
              additions: 0,
              changeType: "DELETED",
              path: "path/to/deleted-file.md",
            },
          },
          {
            node: {
              deletions: 0,
              additions: 1,
              changeType: "MODIFIED",
              path: "path/to/modified-file.md",
            },
          },
          {
            node: {
              deletions: 0,
              additions: 1,
              changeType: "MODIFIED",
              path: "path/includes/modified-file.md",
            },
          },
        ],
      },
    });

    expect(actual).toEqual(["path/to/modified-file.md"]);
  });

  it("isPullRequestModifyingMarkdownFiles returns false when no modified .md files", () => {
    const actual = isPullRequestModifyingMarkdownFiles({
      body: "",
      changedFiles: 2,
      files: {
        edges: [
          {
            node: {
              deletions: 1,
              additions: 1,
              changeType: "RENAMED",
              path: "path/to/renamed-file.md",
            },
          },
          {
            node: {
              deletions: 1,
              additions: 0,
              changeType: "DELETED",
              path: "path/to/deleted-file.md",
            },
          },
        ],
      },
    });

    expect(actual).toBeFalsy();
  });

  it("isPullRequestModifyingMarkdownFiles returns true when modified .md files", () => {
    const actual = isPullRequestModifyingMarkdownFiles({
      body: "",
      changedFiles: 3,
      files: {
        edges: [
          {
            node: {
              deletions: 1,
              additions: 1,
              changeType: "RENAMED",
              path: "path/to/renamed-file.md",
            },
          },
          {
            node: {
              deletions: 1,
              additions: 0,
              changeType: "DELETED",
              path: "path/to/deleted-file.md",
            },
          },
          {
            node: {
              deletions: 0,
              additions: 1,
              changeType: "MODIFIED",
              path: "path/to/modified-file.md",
            },
          },
        ],
      },
    });

    expect(actual).toBeTruthy();
  });
});
