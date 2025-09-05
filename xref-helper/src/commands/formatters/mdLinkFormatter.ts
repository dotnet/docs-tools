import { window } from "vscode";
import { SearchResult } from "../types/SearchResult";
import { UrlFormat } from "../types/UrlFormat";
import { getUserSelectedText } from "../../utils";
import { SearchOptions } from "../types/SearchOptions";

/**
 * When MD links are enabled, the URL should be in the format:
 *   `[name](url)`, where name is the name of the type or member and url is the URL.
 *   For example, `[String](/dotnet/api/system.string)`.
 * @param urlFormat The {@link UrlFormat format} of the URL to insert.
 * @param searchResult The {@link SearchResult search result} to insert as a link.
 * @param options The {@link SearchOptions options} to use when inserting the link.
 * @returns A `string` that represents the Markdown URL link as a Promise.
 */
export const mdLinkFormatter = async (
    urlFormat: UrlFormat,
    searchResult: SearchResult,
    options: SearchOptions | undefined): Promise<string | undefined> => {

    let url = searchResult.url;
    const displayName = searchResult.displayName;

    if (displayName.endsWith('*')) {
        // Chop the bookmark off the URL.
        url = url.split('#')[0];
    }

    switch (urlFormat) {
        // Allows the user to enter a custom name:
        //   [the .NET SmtpClient class](/dotnet/api/system.net.mail.smtpclient)
        case UrlFormat.customName: {
            // Try getting the selected text from the active text editor.
            const selectedText: string | undefined = getUserSelectedText();

            // Default to the display name of the search result.
            let fallbackDisplayName = searchResult.displayName;

            // If there isn't selected text, prompt the user to enter a custom name.
            if (!selectedText) {
                const inputDisplayName = await window.showInputBox({
                    title: 'Enter custom link text',
                    placeHolder: 'Enter the link text to display.'
                });

                return `[${inputDisplayName || fallbackDisplayName}](${url})`;
            }

            return `[${fallbackDisplayName}](${url})`;
        }

        default:
            return undefined;
    }
};
