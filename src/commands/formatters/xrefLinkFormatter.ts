import { window } from "vscode";
import { SearchResult } from "../types/SearchResult";
import { UrlFormat } from "../types/UrlFormat";
import { getUserSelectedText } from "../../utils";

/**
 * When XREF links are enabled, the URL should be in the format:
 *   `<xref:{uid}>`, where `{uid}` is the unique identifier of the type or member.
 *   For example, `<xref:System.String>`.
 * @param urlFormat 
 * @param searchResult 
 * @returns A `string` that represents the XREF link as a Promise.
 */
export const xrefLinkFormatter = async (
    urlFormat: UrlFormat,
    searchResult: SearchResult): Promise<string | undefined> => {

    const encodedDisplayName = encodeURIComponent(searchResult.displayName);

    // TODO:
    // 1. Construct the learn.microsoft.com URL from the search result.
    // 2. Fetch the HTML content and parse:
    //    <meta name="gitcommit" content="https://github.com/{owner}/{repo}/blob/{sha}/path/to.xml">
    // 3. Request the raw XML file from GitHub.
    // 4. Parse the XML file and return the DocId's that are closest to the search result.

    switch (urlFormat) {
        // Displays the API name: 
        //   <xref:System.Net.Mail.SmtpClient>
        case UrlFormat.default:
            return `<xref:${encodedDisplayName}>`;

        // Displays the fully qualified name:
        //   <xref:System.Net.Mail.SmtpClient?displayProperty=fullName>
        case UrlFormat.fullName:
            return `<xref:${encodedDisplayName}?displayProperty=fullName>`;

        case UrlFormat.nameWithType:
            return `<xref:${encodedDisplayName}?displayProperty=nameWithType>`;

        case UrlFormat.customName:
            {
                // Try getting the selected text from the active text editor
                const selectedText: string | undefined = getUserSelectedText();

                // Default to the display name of the search result
                let fallbackDisplayName = searchResult.displayName;

                // If there isn't selected text, prompt the user to enter a custom name
                if (!selectedText) {
                    const inputDisplayName = await window.showInputBox({
                        title: 'Enter custom link text',
                        placeHolder: 'Enter the custom link text to display.'
                    });

                    return `[${inputDisplayName ?? fallbackDisplayName}](xref:${encodedDisplayName})`;
                }

                return `[${selectedText ?? fallbackDisplayName}](xref:${encodedDisplayName})`;
            }

        default:
            return undefined;
    }
};