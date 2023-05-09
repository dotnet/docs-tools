import { existsSync } from "fs";
import { readFile } from "fs/promises";

const h1regex: RegExp = /^# (?<h1>.*$)/gim;

export async function getHeadingTextFrom(path: string): Promise<string | null> {
  if (!existsSync(path)) {
    console.log(`The file '${path}' doesn't exist.`);
    return null;
  }

  const content = await readFile(path, "utf-8");
  if (!!content) {
    try {
      let result: string | null = null;
      const match = h1regex.exec(content);
      if (match && match.groups) {
        result = match.groups?.h1 || null;
      }

      console.log(`Found ${result} from '${path}' contents.`);

      return result;
    } catch (error) {
      if (error) {
        console.log(error.toString());
      } else {
        console.log(`Unknown error reading content for '${path}'.`);
      }
    }
  } else {
    console.log(`Unable to read content for '${path}'.`);
  }

  return null;
}
