import { existsSync } from "fs";
import { readFile } from "fs/promises";

const h1regex: RegExp = /^# (?<h1>.+)/gim;

export async function getHeadingTextFrom(path: string): Promise<string | null> {
  if (!existsSync(path)) {
    console.log(`The file '${path}' doesn't exist.`);
    return null;
  }

  const content = await readFile(path, "utf-8");
  if (!!content) {
    try {
      let result: string | null = null;
      let match;
      while ((match = h1regex.exec(content)) !== null) {
        // This is necessary to avoid infinite loops with zero-width matches
        if (match.index === h1regex.lastIndex) {
          h1regex.lastIndex++;
        }

        result = match.groups?.h1 || null;
      }

      return result;
    } catch (error) {
      if (error) {
        console.log(error.toString());
      }
    }
  }

  return null;
}
