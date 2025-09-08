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
        let match = text.match(/<(xref):.+\?displayProperty=(\w+)>/si);
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

        // The display property is not set if we've gotten this far...
        match = text.match(/(<xref:.+?)[`|(](.+?)>/si);
        // If it's a match, the xref is a method signature.
        if (match) {
            return [
                this.createFix(
                    document,
                    range,
                    `*>`,
                    'Convert to overload syntax, for example, <xref:System.String.Trim*>.',
                    this.getReplacementRangeForMethodOverload)
            ];
        }

        return undefined;
    }

    private createFix(
        document: TextDocument,
        range: Range,
        newText: string,
        title: string,
        getRange: (d: TextDocument, r: Range) => Range = this.getReplacementRange): CodeAction {

        const action = new CodeAction(title);
        action.edit = new WorkspaceEdit();
        const targetRange = getRange(document, range);
        action.edit.replace(document.uri, targetRange, newText);

        return action;
    }

    private getReplacementRangeForMethodOverload(document: TextDocument, range: Range): Range {
        // targetRange:
        //   <xref:System.Uri.GetObjectData(System.Runtime.Serialization.SerializationInfo,System.Runtime.Serialization.StreamingContext)>
        const targetRange = document.getWordRangeAtPosition(
            range.start, /(<xref:.+?)[`|(](.+?)>/si);

        const text = document.getText(targetRange);
        const match = text.match(/(<xref:.+?)[`|(](.+?)>/si);
        // match:
        //   0 <xref:System.Uri.GetObjectData(System.Runtime.Serialization.SerializationInfo,System.Runtime.Serialization.StreamingContext)>
        //   1 <xref:System.Uri.GetObjectData
        //   2 (System.Runtime.Serialization.SerializationInfo,System.Runtime.Serialization.StreamingContext)>

        if (targetRange && match) {
            const start = targetRange.start.translate(0, match[1].length);
            const end = targetRange.end.translate(0, match[2].length);

            return new Range(start, end);
        }

        return range;
    }

    private getReplacementRange(document: TextDocument, range: Range): Range {
        // targetRange:
        //   <xref:System.Net.Mail.SmtpClient.Port?displayProperty=fullName>
        const targetRange = document.getWordRangeAtPosition(
            range.start, /<(xref):.+\?displayProperty=.+>/si);

        const text = document.getText(targetRange);
        const match = text.match(/(<(xref):.+)(\?displayProperty=.+>)/si);
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
