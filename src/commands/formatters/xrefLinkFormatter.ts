import { window } from "vscode";
import { UrlFormat } from "../types/UrlFormat";
import { getUserSelectedText } from "../../utils";

/**
 * When XREF links are enabled, the URL should be in the format:
 *   `<xref:{uid}>`, where `{uid}` is the unique identifier of the type or member.
 *   For example, `<xref:System.String>`.
 * @param urlFormat The {@link UrlFormat format} of the URL to insert.
 * @param uid The unique identifier of the type or member.
 * @returns A `string` that represents the XREF link as a Promise.
 */
export const xrefLinkFormatter = async (
    urlFormat: UrlFormat,
    uid: string): Promise<string | undefined> => {

    switch (urlFormat) {
        // Displays the API name:
        //   <xref:System.Net.Mail.SmtpClient>
        case UrlFormat.default:
            return `<xref:${uid}>`;

        // Displays the fully qualified name:
        //   <xref:System.Net.Mail.SmtpClient?displayProperty=fullName>
        case UrlFormat.fullName:
            return `<xref:${uid}?displayProperty=fullName>`;

        case UrlFormat.nameWithType:
            return `<xref:${uid}?displayProperty=nameWithType>`;

        case UrlFormat.customName:
            {
                // Try getting the selected text from the active text editor
                const selectedText: string | undefined = getUserSelectedText();

                // Default to the display name of the search result
                let fallbackDisplayName = uid;

                // If there isn't selected text, prompt the user to enter a custom name
                if (!selectedText) {
                    const inputDisplayName = await window.showInputBox({
                        title: 'Enter custom link text',
                        placeHolder: 'Enter the custom link text to display.'
                    });

                    return `[${inputDisplayName ?? fallbackDisplayName}](xref:${uid})`;
                }

                return `[${selectedText ?? fallbackDisplayName}](xref:${uid})`;
            }

        default:
            return undefined;
    }
};
