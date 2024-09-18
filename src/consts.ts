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
 * The quick pick items for selecting the URL format.
 */
export const urlFormatQuickPickItems: QuickPickItem[] =
[
    { 
        label: `$(check) ${UrlFormat.default}`, 
        description: 'Only displays the API name. For example, "Trim()".'
    },
    { 
        label: `$(array) ${UrlFormat.fullName}`, 
        description: 'Displays the fully qualified name. For example, "System.String.Trim()".'
    },
    { 
        label: `$(bracket-dot) ${UrlFormat.nameWithType}`, 
        description: 'Displays the type and name in the format. For example, "String.Trim()".',
    },
    { 
        label: `$(edit) ${UrlFormat.customName}`, 
        description: 'Lets you enter custom link text. For example, "The string.Trim() method".'
    },
];