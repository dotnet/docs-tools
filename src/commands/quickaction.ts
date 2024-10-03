import { CodeActionProvider, TextDocument, Range, Selection, CodeActionContext, CancellationToken, ProviderResult, CodeAction, WorkspaceEdit } from "vscode";

export class DisplayPropertyChanger implements CodeActionProvider {
    provideCodeActions(
        document: TextDocument,
        range: Range | Selection,
        context: CodeActionContext,
        token: CancellationToken): ProviderResult<CodeAction[]> {

        if (range.isSingleLine === false) {
            return undefined;
        }

        const line = document.lineAt(range.start.line);
        const text = line.text;
        const match = text.match(/<(xref|Xref|XRef|XREF):.+\?displayProperty=(\w+)>/);
        if (match) {
            const displayProperty = match[2];

            return [
                displayProperty === 'fullName'
                    ? this.createFix(
                        document,
                        range,
                        '?displayProperty=nameWithType',
                        'Format as name with type, for example, "String.Trim()".')

                    : this.createFix(
                        document,
                        range,
                        '?displayProperty=fullName',
                        'Format as full name, for example, "System.String.Trim()".')
            ];
        }

        return undefined;
    }

    private createFix(
        document: TextDocument,
        range: Range,
        newText: string,
        title: string): CodeAction {

        const action = new CodeAction(title);
        action.edit = new WorkspaceEdit();
        const targetRange = this.getReplacementRange(document, range);
        action.edit.replace(document.uri, targetRange, newText);

        return action;
    }

    private getReplacementRange(document: TextDocument, range: Range): Range {
        // targetRange:
        //   <xref:System.Net.Mail.SmtpClient.Port?displayProperty=fullName>
        const targetRange = document.getWordRangeAtPosition(
            range.start, /<(xref|Xref|XRef|XREF):.+\?displayProperty=.+>/);

        const text = document.getText(targetRange);
        const match = text.match(/(<(xref|Xref|XRef|XREF):.+)(\?displayProperty=.+>)/);
        // match:
        //   0 <xref:System.Net.Mail.SmtpClient.Port?displayProperty=fullName>
        //   1 <xref:System.Net.Mail.SmtpClient.Port
        //   2 xref
        //   3 ?displayProperty = fullName >

        if (targetRange && match) {
            const start = targetRange.start.translate(0, match[1].length);
            const end = targetRange.end.translate(0, -1);

            return new Range(start, end);
        }

        return range;
    }
}
