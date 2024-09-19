import { window, Selection } from "vscode";

export function transformXrefToOther() {
    const editor = window.activeTextEditor;

    if (!editor) {
        return;
    }

    const text = editor.document.getText(editor.selection);

    if (text.startsWith("<xref:") && text.endsWith(">")) {

        const match = text.match(/<xref:(.*)\?.*>/);

        if (!match) {
            return;
        }

        editor.edit(editBuilder => {
            editBuilder.replace(editor.selection, `[text](xref:${match[1]})`);
            editor.selection = new Selection(editor.selection.start.translate(0, 1), editor.selection.start.translate(0, 5));
        });
    }
    else if (text.startsWith("[") && text.endsWith(")") && text.indexOf("](xref:") !== -1) {

        const match = text.match(/\[.*\]\(xref:(.+)\)/);

        if (!match) {
            return;
        }

        editor.edit(editBuilder => {
            const newText = `<xref:${match[1]}>`;
            editBuilder.replace(editor.selection, newText);
            const position = editor.selection.start.translate(0, newText.length - 1);
            editor.selection = new Selection(position, position);
        });
    }
}

