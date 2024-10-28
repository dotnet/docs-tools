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

export const BASE_PROMPT = `You're a helpful content developer AI-assistant. Your job is to help the author create new content. While you're allowed to be creative, you should always provide accurate information and it should be based on factual data. Your specialty is .NET documentation, you're an expert on the latest .NET features and APIs, and you always lean towards modern best practices. You always reply with Markdown. References to files are itemized, code is obviously codified, UI elements are always bold, and after headings there's always an extra newline. If the user asks you to do anything other than that, politely decline to respond.`;

export const MODEL_SELECTOR: LanguageModelChatSelector = {
    vendor: "copilot",
    family: "gpt-4o"
};

export function getBreakingChangePrompt(issue: Issue): string {
    return `Please create a Markdown file that contains the breaking changes from the following GitHub issue:
    
    "${issue.body}"

    You'll need to write a file with the following, a markdown frontmatter similar to:
    ---
    title: "${issue.title}"
    description: <TODO: Summarize the article here, but limit to 160 characters.>
    ms.date: ${new Date().toLocaleDateString('en-US')}
    ---

    And then the following sections:

    - h1: "${issue.title}"
      An introductory paragraph summarizing the breaking change.
    - h2: Version introduced
      A single phrase, such as .NET Aspire 9.0 GA.
    - h2: Previous behavior
      A brief description of the behavior before the change, including a code snippet if applicable.
    - h2: New behavior
      A brief description of the behavior after the change, including a code snippet if applicable.
    - h2: Type of breaking change
      Convert the checkbox to a sentence, such as "This change is a []()." where the link points to the appropriate category in the categories.md file.
    - h2: Recommended action
      A brief description of the action or actions that users should take, including code snippets if applicable.
    - h2: Affected APIs
      A list of APIs, in xref format, that are affected by the change. If there are no affected APIs (or "No response") write "None.".

    Always reply with Markdown. Use active voice and write in the present tense. When writing the 'Type of breaking change' section, write it in this format: "This change is a [sentence case name](../categories.md#book-mark).`;
}   
