import { CancellationToken, Position, ProviderResult, TextDocument, CompletionItemProvider, CompletionContext, CompletionItem, CompletionList } from "vscode";
import { insertXrefLinkCommandName } from '../consts';

export const xrefStarterAutoComplete: CompletionItemProvider = {
    provideCompletionItems: function (document: TextDocument,
                                      position: Position,
                                      token: CancellationToken,
                                      context: CompletionContext)
                                      : ProviderResult<CompletionList<CompletionItem> | CompletionItem[]> {
        
        const range = document.getWordRangeAtPosition(position, /<xref:/) || document.getWordRangeAtPosition(position, /\(xref:/);

        if (range) {
            return [
                {
                    command: { command: insertXrefLinkCommandName, title: "Search for API"},
                    label: "Search for API",
                    insertText: "",
                }
            ];
        }

        return undefined;
    }
}
