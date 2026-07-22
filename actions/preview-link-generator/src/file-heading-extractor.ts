import { Context } from "@actions/github/lib/context";
import { readFile } from "fs/promises";

const h1RegExp = /^# (?<h1>.*$)/gim;
const titleRegExp = /^title:\s*(?<title>.*)$/gim;

export async function getHeadingTextFromRaw(
    path: string,
    context: Context,
    commitOid: string | undefined | null
): Promise<string | null> {
    try {
        const owner = context.actor;
        const repo = context.repo.repo;
        const raw = `https://raw.githubusercontent.com/${owner}/${repo}/${commitOid}/${path}`;

        const response = await fetch(raw);
        if (response && response.ok) {
            const content = await response.text();
            if (!content) {
                console.log(`Unable to read content for '${path}'.`);
                return null;
            }

            return tryGetTextFromContent(content, path);
        } else {
            console.log(
                `Error reading content for '${path}'. Status: ${response.status}`
            );
        }
    } catch (error) {
        if (error) {
            console.log(error.toString());
        } else {
            console.log(`Unknown error reading content for '${path}'.`);
        }
    }

    return null;
}

export async function getHeadingTextFrom(path: string): Promise<string | null> {
    try {
        const content = await readFile(path, "utf-8");
        if (!content) {
            console.log(`Unable to read content for '${path}'.`);
            return null;
        }

        return tryGetTextFromContent(content, path);
    } catch (error) {
        if (error) {
            console.log(error.toString());
        } else {
            console.log(`Unknown error reading content for '${path}'.`);
        }
    }

    return null;
}

const xrefRegExp = /<xref:([^>]+)>/gim;

function tryGetTextFromContent(content: string, path: string) {
    let result: string | null =
        tryGetRegExpMatch(h1RegExp, "h1", content) ??
        tryGetRegExpMatch(titleRegExp, "title", content);

    console.log(`Found ${result} from '${path}' contents.`);

    if (result && result.indexOf("<xref:") > -1) {
        result = normalizeHeadingOrTitleText(result);
        console.log(`  normalized as ${result}`);
    }

    return result;
}

function normalizeHeadingOrTitleText(headingText: string): string {
    // If contains xref markdown, extract only the text from it.
    // Example: "<xref:System.Globalization.CompareInfo> class"
    //       or "<xref:System.Globalization.CompareInfo /> class"
    // Result: "`System.Globalization.CompareInfo` class"
    const xrefMatch = xrefRegExp.exec(headingText);

    if (xrefMatch && xrefMatch[1]) {
        headingText = headingText.replace(xrefRegExp, `\`${xrefMatch[1]}\``);
    }

    return headingText;
}

function tryGetRegExpMatch(
    expression: RegExp,
    groupName: string,
    content: string
): string | null {
    let result: string | null = null;

    const match = expression.exec(content);
    if (match && match.groups) {
        result = match.groups?.[groupName] || null;
    }

    return result;
}
