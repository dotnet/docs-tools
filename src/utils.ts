import { window, Range, InputBoxValidationMessage, InputBoxValidationSeverity } from "vscode";

/**
 * The `nameof` operator returns the name of a variable, type, or member as a `string`.
 * @param name The name of the variable, type, or member.
 * @returns A `string` that represents the name of the variable, type, or member.
 */
export const nameof = <T>(name: keyof T): string => name as string;

/**
 * Gets the text that the user has selected in the active editor.
 * @returns The text that the user has selected in the active editor.
 */
export const getUserSelectedText = (): string | undefined => {
    const editor = window.activeTextEditor;
    const selection = editor?.selection;

    if (selection && !selection.isEmpty) {
        const selectionRange = new Range(
            selection.start.line,
            selection.start.character,
            selection.end.line,
            selection.end.character);

        const highlighted = editor.document.getText(selectionRange);

        return highlighted;
    }

    return undefined;
};

/**
 * Replaces the user's selected text with the specified `replacement`.
 * @param replacement Replaces the user's selected text with the specified `replacement`.
 */
export const replaceUserSelectedText = (replacement: string): void => {
    const editor = window.activeTextEditor;
    const selection = editor?.selection;

    if (selection && !selection.isEmpty) {
        editor.edit((editBuilder) => {
            editBuilder.replace(selection, replacement);
        });
    }
};

/**
 * Used to validate search term input.
 */
export function searchTermInputValidation(text: string): InputBoxValidationMessage | null {
    if (!text) {
        return {
            message: `You must provide a search term.`,
            severity: InputBoxValidationSeverity.Info
        }
    }

    if (text.includes(' ')) {
        return {
            message: `Your search cannot contain spaces.`,
            severity: InputBoxValidationSeverity.Warning
        }
    }

    // Angle bracket count must be the same...
    const openingAngles = text.match(/</g || [])?.length ?? 0;
    const closingAngles = text.match(/>/g || [])?.length ?? 0;

    if (openingAngles != closingAngles) {
        return {
            message: `Your search must include pairs of opening and closing angle brackets.`,
            severity: InputBoxValidationSeverity.Error
        }
    }

    return null;
}