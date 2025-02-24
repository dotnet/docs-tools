import { LanguageModelChatSelector, QuickPickItem } from "vscode";
import { UrlFormat } from "./commands/types/UrlFormat";
import { Issue } from "./services/github-api";

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
 * The name of the copy AI stream to clipboard command.
 * @constant `"xrefHelper.copyAIStreamToClipboard"`
 */
export const copyAIStreamToClipboard: string = `${toolName}.copyAIStreamToClipboard`;

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

export const BASE_PROMPT = `Your job is to help the user create new .NET developer content and documentation. Be creative, but always provide accurate information based on factual data. Always use .NET best practices. Within content, apply the following style standards: File/directory names are italicized, code are formatted as code, UI elements are bold, and after headings there's always an extra newline.`;

export const MODEL_SELECTOR: LanguageModelChatSelector = {
    vendor: "copilot",
    family: "gpt-4o"
};

export function getBreakingChangePrompt(issue: Issue, issueUrl: string): string {
    return `Please create a "breaking changes" document from the following GitHub issue:
    
    "${issue.body}"

    The document should be in Markdown format. All headings and titles should be in sentence case. Rephrase all content to be clear and concise using correct grammar and spelling. Use active voice. Always describe previous behavior in past tense and new behavior in present tense. Do not use "we" or "our". Avoid colloquial language and try to sound professional.

    The document should start with the following header, including --- characters. Replace placeholders denoted by parentheses with the appropriate content from the issue.

    ---
    title: "Breaking change - ${issue.title}"
    description: "Learn about the breaking change in (product/version) where (very brief description)."
    ms.date: ${new Date().toLocaleDateString('en-US')}
    ai-usage: ai-assisted
    ms.custom: ${issueUrl}
    ---

    After the header, include the following sections in this order. Use the description in parentheses as a guide for the content of each section.

    - h1: "${issue.title}"
      (An introductory paragraph summarizing the breaking change.)
    - h2: Version introduced
      (The version in which the breaking change was introduced.)
    - h2: Previous behavior
      (A brief description of the behavior before the change, including an example code snippet if applicable.)
    - h2: New behavior
      (A brief description of the behavior after the change, including an example code snippet if applicable.)
    - h2: Type of breaking change
      (The following sentence: "This is a []() change." where the link text is the type of breaking change from the issue. The link should point to ../../categories.md and add the appropriate bookmark from this list: #behavioral-change #binary-compatibility #source-compatibility)
    - h2: Reason for change
      (The complete reasoning behind the change, including any relevant links.)
    - h2: Recommended action
      (A brief description of the action or actions that users should take, including example code snippets if applicable.)
    - h2: Affected APIs
      (A bullet list of APIs that are affected by the change. If there are no affected APIs (or "No response") write "None.".)`
}   
