import { readFile } from "fs/promises";

const h1RegExp = /^# (?<h1>.*$)/gim;
const titleRegExp = /^title:\s*(?<title>.*)$/gim;

export async function getHeadingTextFrom(path: string): Promise<string | null> {
  try {
    const content = await readFile(path, "utf-8");
    if (!content) {
      console.log(`Unable to read content for '${path}'.`);
      return null;
    }

    const result: string | null =
      tryGetRegExpMatch(h1RegExp, "h1", content) ??
      tryGetRegExpMatch(titleRegExp, "title", content);

    console.log(`Found ${result} from '${path}' contents.`);

    return result;
  } catch (error) {
    if (error) {
      console.log(error.toString());
    } else {
      console.log(`Unknown error reading content for '${path}'.`);
    }
  }

  return null;
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
