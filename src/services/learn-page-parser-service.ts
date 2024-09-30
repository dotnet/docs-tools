import { parseStringPromise } from "xml2js";

export class LearnPageParserService {
    public static async getPageUid(url: string): Promise<string | undefined> {
        return await this.getMetaTagContent(url, 'uid');
    }

    public static async getRawGitUrl(url: string): Promise<string | undefined> {
        const gitUrl = await this.getMetaTagContent(url, 'gitcommit');
        return gitUrl
            ?.replace('https://github.com/', 'https://raw.githubusercontent.com/')
            ?.replace('/blob/', '/');
    }

    private static async getMetaTagContent(url: string, metaTagName: string): Promise<string | undefined> {
        const fullUrl = new URL(`https://learn.microsoft.com${url}`);

        const response = await fetch(fullUrl.href, {
            headers: {
                // eslint-disable-next-line @typescript-eslint/naming-convention
                "Content-Type": "text/html",
            }
        });

        if (!response.ok) {
            return undefined;
        }

        const html: string = await response.text();
        const htmlLines = html.split('\n');

        for (const line of htmlLines) {
            if (line.includes(metaTagName)) {
                const xml = await parseStringPromise(line);
                if (xml) {
                    return xml?.meta['$'].content;
                }
            }
        }

        return undefined;
    }
}