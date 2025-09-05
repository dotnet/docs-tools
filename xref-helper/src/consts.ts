import { QuickPickItem } from "vscode";
import { UrlFormat } from "./commands/types/UrlFormat";

/**
 * The text used to indicate when there are too many search results.
 */
export const tooManyResults: string = "Try a more specific search term...";

/**
 * The name of the tool.
 * @constant `"xrefHelper"`
 */
export const toolName: string = "xrefHelper";

/**
 * The name of the insert API reference link command.
 * @constant `"xrefHelper.insertApiReferenceLink"`
 */
export const insertApiRefLinkCommandName: string = `${toolName}.insertApiReferenceLink`;

/**
 * The name of the insert API reference link command.
 * @constant `"xrefHelper.insertXrefLink"`
 */
export const insertXrefLinkCommandName: string = `${toolName}.insertXrefLink`;

/**
 * The name of the transform xref to the opposite version command.
 * @constant `"xrefHelper.transformXrefToOther"`
 */
export const transformXrefToOtherCommandName: string = `${toolName}.transformXrefToOther`;

/**
 * The quick pick items for selecting the URL format.
 * Excludes the custom name option.
 */
export const urlFormatQuickPickItems: QuickPickItem[] =
    [
        {
            label: `$(check) ${UrlFormat.default}`,
            description: 'Displays only the API name. For example, "Trim".'
        },
        {
            label: `$(bracket-dot) ${UrlFormat.nameWithType}`,
            description: 'Displays the type and name (or namespace and type). For example, "String.Trim".',
        },
        {
            label: `$(array) ${UrlFormat.fullName}`,
            description: 'Displays the fully qualified name. For example, "System.String.Trim".'
        },
    ];

/**
 * The quick pick items for selecting the URL format.
 * Includes the custom name option.
 */
export const allUrlFormatQuickPickItems: QuickPickItem[] =
    [
        ...urlFormatQuickPickItems,
        {
            label: `$(edit) ${UrlFormat.customName}`,
            description: 'Lets you enter custom link text. For example, "The string.Trim() method".'
        }
    ];
