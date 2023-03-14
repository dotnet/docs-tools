import { exportedForTesting } from "../src/pull-updater";
import { describe, expect, it } from "@jest/globals";
import { WorkflowInput, workflowInput } from "../src/types/WorkflowInput";

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
    setInput("DOCS_PATH", "docs");
    setInput("URL_BASE_PATH", "dotnet");

    const actual = buildMarkdownPreviewTable(7, ["test/markdown.md"]);
    expect(actual).toEqual(
      "#### Internal previews\n\n| 📄 File | 🔗 Preview link |\n|:--|:--|\n| _test/markdown.md_ | [test/markdown](https://review.learn.microsoft.com/en-us/dotnet/test/markdown?branch=pr-en-us-7) |\n"
    );
  });

  it("options are correctly constructed with expected values from import", () => {
    setInput("COLLAPSIBLE_AFTER", "7");
    setInput("DOCS_PATH", "test/path");
    setInput("URL_BASE_PATH", "foundation");

    const opts: WorkflowInput = workflowInput;

    expect(opts).toBeDefined();
    expect(opts.collapsibleAfter).toBe(7);
    expect(opts.docsPath).toBe("test/path");
    expect(opts.urlBasePath).toBe("foundation");
  });

  it("buildMarkdownPreviewTable builds preview table correctly with collapsible HTML elements.", () => {
    setInput("COLLAPSIBLE_AFTER", "3");
    setInput("DOCS_PATH", "docs");
    setInput("URL_BASE_PATH", "dotnet");

    const actual = buildMarkdownPreviewTable(7, [
      "1/one.md",
      "2/two.md",
      "3/three.md",
      "4/four.md",
      "5/five.md",
    ]);
    expect(actual).toEqual(
      "#### Internal previews\n\n<details><summary><strong>Toggle Expand/Collapse</strong></summary><br/>\n\n| 📄 File | 🔗 Preview link |\n|:--|:--|\n| _1/one.md_ | [1/one](https://review.learn.microsoft.com/en-us/dotnet/1/one?branch=pr-en-us-7) |\n| _2/two.md_ | [2/two](https://review.learn.microsoft.com/en-us/dotnet/2/two?branch=pr-en-us-7) |\n| _3/three.md_ | [3/three](https://review.learn.microsoft.com/en-us/dotnet/3/three?branch=pr-en-us-7) |\n| _4/four.md_ | [4/four](https://review.learn.microsoft.com/en-us/dotnet/4/four?branch=pr-en-us-7) |\n| _5/five.md_ | [5/five](https://review.learn.microsoft.com/en-us/dotnet/5/five?branch=pr-en-us-7) |\n\n</details>\n"
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
              path: "includes/modified-file.md",
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

const setInput = (name: string, value: string) => {
  const key = `INPUT_${name.replace(/ /g, "_").toUpperCase()}`;
  process.env[key] = value;
};
