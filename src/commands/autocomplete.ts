import { CancellationToken, Position, ProviderResult, TextDocument, CompletionItemProvider, CompletionContext, CompletionItem, CompletionList } from "vscode";
import { insertXrefLinkCommandName, urlFormatQuickPickItems } from '../consts';
import { UrlFormat } from './types/UrlFormat';
import { SearchOptions } from './types/SearchOptions';

export const xrefStarterAutoComplete: CompletionItemProvider = {
    provideCompletionItems: function (document: TextDocument,
                                      position: Position,
                                      token: CancellationToken,
                                      context: CompletionContext)
                                      : ProviderResult<CompletionList<CompletionItem> | CompletionItem[]> {
        
        const range = document.getWordRangeAtPosition(position, /<xref:/) || document.getWordRangeAtPosition(position, /\(xref:/);

        if (range) {

            const text = document.getText(range);

            const searchOptions : SearchOptions = {
                SkipBrackets: true,
                SkipDisplayStyle: text.startsWith('('),
                HideCustomDisplayStyle: text.startsWith('<')
            };

            return [
                {
                    command: { command: insertXrefLinkCommandName, title: "Search for API", arguments: [searchOptions]},
                    label: "Search for API",
                    insertText: "",
                }
            ];
        }

        return undefined;
    }
}

export const xrefDisplayTypeAutoComplete: CompletionItemProvider = {
    provideCompletionItems: function (document: TextDocument,
                                      position: Position,
                                      token: CancellationToken,
                                      context: CompletionContext)
                                      : ProviderResult<CompletionList<CompletionItem> | CompletionItem[]> {
        
        const range = document.getWordRangeAtPosition(position, /<xref:[^\s]+/);

        if (range) {

            const text = document.getText(range);

            return [
                {
                    label: `$(array) Full name`,
                    insertText: "displayProperty=fullName",
                    detail: text
                },
                {
                    label: `$(bracket-dot) Type with name`,
                    insertText: "displayProperty=nameWithType",
                    detail: text
                },
            ];
        }

        return undefined;
    }
}