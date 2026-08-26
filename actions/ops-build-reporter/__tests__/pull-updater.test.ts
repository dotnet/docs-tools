import { exportedForTesting } from "../src/pull-updater";
import { beforeAll, describe, expect, it } from "@jest/globals";
import { WorkflowInput, workflowInput } from "../src/types/WorkflowInput";

const {
    appendTable,
    buildMarkdownPreviewTableFromExtractedLinks,
    calculateMaxPollAttempts,
    extractChangedLinesFromPatch,
    extractDiagnosticsFromBuildReport,
    extractPreviewLinksFromBuildReport,
    filterDiagnosticsToChangedLines,
    PREVIEW_TABLE_END,
    PREVIEW_TABLE_START,
    replaceExistingTable,
} = exportedForTesting;

beforeAll(() => {
    process.env["GITHUB_REPOSITORY"] = "dotnet/docs";
});

describe("pull-updater", () => {
    it("appendTable correctly appends table", () => {
        const body = "...";
        const actual = appendTable(body, "[table]");

        expect(actual).toEqual(`...

${PREVIEW_TABLE_START}

---

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

---

[updated-table]

${PREVIEW_TABLE_END}

testing...1, 2, 3!`);
    });

    it("appendTable followed by replaceExistingTable correctly replaces table", () => {
        const body = "...";
        let actual = appendTable(body, "[table]");
        let expectedBody = `...

${PREVIEW_TABLE_START}

---

[table]
${PREVIEW_TABLE_END}`;

        expect(actual).toEqual(expectedBody);
        actual = appendTable(body, "[updated-table]");
        expectedBody = `...

${PREVIEW_TABLE_START}

---

[updated-table]
${PREVIEW_TABLE_END}`;

        expect(actual).toEqual(expectedBody);
    });

    it("options are correctly constructed with expected values from import", () => {
        setInput("COLLAPSIBLE_AFTER", "7");
        setInput("MAX_ROW_COUNT", "42");
        setInput("MAX_WAIT_TIME_MINUTES", "15");
        setInput("annotate_file_warnings", "true");
        setInput("REPO_TOKEN", "test-token");

        const opts: WorkflowInput = workflowInput;

        expect(opts).toBeDefined();
        expect(opts.collapsibleAfter).toBe(7);
        expect(opts.maxRowCount).toBe(42);
        expect(opts.maxWaitTimeMinutes).toBe(15);
        expect(opts.annotateFiles).toBe(true);
        expect(opts.repoToken).toBe("test-token");
    });

    it("calculates OPS poll attempts from the maximum wait time", () => {
        expect(calculateMaxPollAttempts(15, 30_000)).toBe(31);
    });

    it("extractPreviewLinksFromBuildReport parses file to preview URL map", () => {
        const html = `
<html>
    <body>
        <table class="MsoNormalTable">
            <tr>
                <td>File</td><td>Status</td><td>Preview URL</td>
            </tr>
            <tr>
                <td><a href="https://example.com/file1">docs/a.md</a></td>
                <td>Updated</td>
                <td><a href="https://review.learn.microsoft.com/en-us/dotnet/a?branch=pr-en-us-99">a</a></td>
            </tr>
            <tr>
                <td><a href="https://example.com/file2">docs/b.yml</a></td>
                <td>Updated</td>
                <td><a href="https://review.learn.microsoft.com/en-us/dotnet/b?branch=pr-en-us-99">b</a></td>
            </tr>
        </table>
    </body>
</html>`;

        const actual = extractPreviewLinksFromBuildReport(html);

        expect(actual.size).toBe(2);
        expect(actual.get("docs/a.md")).toBe(
            "https://review.learn.microsoft.com/en-us/dotnet/a?branch=pr-en-us-99"
        );
        expect(actual.get("docs/b.yml")).toBe(
            "https://review.learn.microsoft.com/en-us/dotnet/b?branch=pr-en-us-99"
        );
    });

    it("extracts errors, warnings, and suggestions from validated file details", () => {
        const html = `
<table>
    <tr>
        <td>File</td><td>Status</td><td>Preview URL</td><td>Details</td>
    </tr>
    <tr>
        <td>articles/create.md</td><td>Warning</td><td>View</td>
        <td>
            Line 61: [Warning] Multiple H1s are not allowed.<br />
            Line 83: [Error] Another top-level heading was found.
        </td>
    </tr>
    <tr>
        <td>articles/overview.md</td><td>Warning</td><td>View</td>
        <td>
            Line 92: [Warning] Duplicate heading: 'Next step'.<br />
            Line 97: [Suggestion] Consider adding a description.
        </td>
    </tr>
</table>`;

        const diagnostics = extractDiagnosticsFromBuildReport(html);

        expect(diagnostics).toEqual([
            {
                path: "articles/create.md",
                line: 61,
                severity: "Warning",
                message: "Multiple H1s are not allowed.",
            },
            {
                path: "articles/create.md",
                line: 83,
                severity: "Error",
                message: "Another top-level heading was found.",
            },
            {
                path: "articles/overview.md",
                line: 92,
                severity: "Warning",
                message: "Duplicate heading: 'Next step'.",
            },
            {
                path: "articles/overview.md",
                line: 97,
                severity: "Suggestion",
                message: "Consider adding a description.",
            },
        ]);
    });

    it("filters diagnostics to added lines in the PR patch", () => {
        const createChangedLines =
            extractChangedLinesFromPatch(`@@ -60,2 +60,3 @@
 unchanged line
+new line 61
 unchanged line`);
        const overviewChangedLines =
            extractChangedLinesFromPatch(`@@ -91,1 +91,2 @@
 unchanged line
+new line 92`);
        const diagnostics = [
            {
                path: "articles/create.md",
                line: 61,
                severity: "Warning" as const,
                message: "Changed warning",
            },
            {
                path: "articles/create.md",
                line: 83,
                severity: "Warning" as const,
                message: "Unchanged warning",
            },
            {
                path: "articles/overview.md",
                line: 92,
                severity: "Error" as const,
                message: "Changed error",
            },
        ];

        const filtered = filterDiagnosticsToChangedLines(
            diagnostics,
            new Map([
                ["articles/create.md", createChangedLines],
                ["articles/overview.md", overviewChangedLines],
            ])
        );

        expect(filtered.map(({ path, line }) => `${path}:${line}`)).toEqual([
            "articles/create.md:61",
            "articles/overview.md:92",
        ]);
    });

    it("buildMarkdownPreviewTableFromExtractedLinks creates table from build report links", () => {
        setInput("COLLAPSIBLE_AFTER", "10");
        const links = new Map<string, string>([
            [
                "docs/a.md",
                "https://review.learn.microsoft.com/en-us/dotnet/a?branch=pr-en-us-7",
            ],
            [
                "docs/b.yml",
                "https://review.learn.microsoft.com/en-us/dotnet/b?branch=pr-en-us-7",
            ],
        ]);

        const actual = buildMarkdownPreviewTableFromExtractedLinks(
            links,
            "oid",
            "https://github.com/dotnet/docs/pull/7/checks"
        );

        expect(actual).toEqual(
            "#### Internal previews\n\n" +
                "| File | Preview link |\n" +
                "|:--|:--|\n" +
                "| [docs/a.md](https://github.com/dotnet/docs/blob/oid/docs/a.md) | [Learn preview](https://review.learn.microsoft.com/en-us/dotnet/a?branch=pr-en-us-7) |\n" +
                "| [docs/b.yml](https://github.com/dotnet/docs/blob/oid/docs/b.yml) | [Learn preview](https://review.learn.microsoft.com/en-us/dotnet/b?branch=pr-en-us-7) |\n"
        );
    });

    it("buildMarkdownPreviewTableFromExtractedLinks supports collapsible output", () => {
        setInput("COLLAPSIBLE_AFTER", "1");
        const links = new Map<string, string>([
            ["docs/a.md", "https://review.learn.microsoft.com/en-us/dotnet/a"],
            ["docs/b.md", "https://review.learn.microsoft.com/en-us/dotnet/b"],
        ]);

        const actual = buildMarkdownPreviewTableFromExtractedLinks(
            links,
            "oid",
            "https://github.com/dotnet/docs/pull/7/checks"
        );

        expect(actual).toContain(
            "<details><summary><strong>Toggle expand/collapse</strong></summary><br/>"
        );
        expect(actual).toContain("</details>");
    });
});

const setInput = (name: string, value: string) => {
    const key = `INPUT_${name.replace(/ /g, "_").toUpperCase()}`;
    process.env[key] = value;
};
