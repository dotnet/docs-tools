import { exportedForTesting } from "../src/pull-updater";
import { describe, expect, it } from "@jest/globals";
import { WorkflowInput, workflowInput } from "../src/types/WorkflowInput";

const {
    appendTable,
    buildMarkdownPreviewTableFromExtractedLinks,
    extractPreviewLinksFromBuildReport,
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
        setInput("DOCS_PATH", "test/path");
        setInput("URL_BASE_PATH", "foundation");
        setInput(
            "opaque_leading_url_segments",
            "net:view=netdesktop-7.0,framework:view=netframeworkdesktop-4.8"
        );

        const opts: WorkflowInput = workflowInput;

        expect(opts).toBeDefined();
        expect(opts.collapsibleAfter).toBe(7);
        expect(opts.docsPath).toBe("test/path");

        const compareMaps = <T1, T2>(
            expected: Map<T1, T2>,
            actual: Map<T1, T2>
        ) => {
            expect(expected).toBeDefined();
            expect(actual).toBeDefined();

            expect(expected.size).toBe(actual.size);

            for (let [key, value] of expected) {
                expect(actual.has(key));
                expect(actual.get(key)).toBe(value);
            }
        };

        var map: Map<string, string> = new Map();
        map.set("net", "view=netdesktop-7.0");
        map.set("framework", "view=netframeworkdesktop-4.8");

        compareMaps(map, opts.opaqueLeadingUrlSegments);
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
                "| 📄 File | 🔗 Preview link |\n" +
                "|:--|:--|\n" +
                "| [docs/a.md](https://github.com/dotnet/docs/blob/oid/docs/a.md) | [docs/a](https://review.learn.microsoft.com/en-us/dotnet/a?branch=pr-en-us-7) |\n" +
                "| [docs/b.yml](https://github.com/dotnet/docs/blob/oid/docs/b.yml) | [docs/b](https://review.learn.microsoft.com/en-us/dotnet/b?branch=pr-en-us-7) |\n"
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

        expect(actual).toContain("<details><summary><strong>Toggle expand/collapse</strong></summary><br/>");
        expect(actual).toContain("</details>");
    });
});

const setInput = (name: string, value: string) => {
    const key = `INPUT_${name.replace(/ /g, "_").toUpperCase()}`;
    process.env[key] = value;
};
