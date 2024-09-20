import { parseStringPromise } from "xml2js";

export class RawGitService {
    public static async getRawGitUrl(url: string): Promise<string | undefined> {
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

        // Parse the HTML content and extract the `gitcommit` meta tag
        // <meta name="gitcommit" content="https://github.com/dotnet/dotnet-api-docs/blob/a614b2eb43c0f9f739cc9b19b2e1a0203237623d/xml/Microsoft.CSharp/CSharpCodeProvider.xml">

        for (const line of htmlLines) {
            if (line.includes('gitcommit')) {
                const xml = await parseStringPromise(line);
                if (xml) {
                    const gitUrl = xml?.meta['$'].content;
                    
                    return gitUrl
                        ?.replace('https://github.com/', 'https://raw.githubusercontent.com/')
                        ?.replace('/blob/', '/');
                }
            }
        }

        return undefined;
    }
}