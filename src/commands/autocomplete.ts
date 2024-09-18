import { CancellationToken, Position, ProviderResult, TextDocument, CompletionItemProvider, CompletionContext, CompletionItem, CompletionList, CompletionItemKind } from "vscode";
import { insertXrefLinkCommandName } from '../consts';
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
        
        const range = document.getWordRangeAtPosition(position, /<xref:[^\s>]+>/);

        if (range) {

            // Get the full name sans trailing * for overloads
            let fullName = document.getText(range).replace('%2A', '').replace('*', '');
            
            // Trim off the ending regex result of ?>
            fullName = fullName.substring(6, fullName.length - 2);
            
            let nameWithType = fullName;
            
            // If the full name has method () trim it down to (...) for display
            if (nameWithType.indexOf('(') !== -1) {
                nameWithType = `${nameWithType.substring(0, nameWithType.indexOf('('))}(…)`;
            }

            // If the full name has . in it, trim it down to the last two parts for name with type display
            if (nameWithType.indexOf('.') !== -1) {
                const items = nameWithType.split('.');
                nameWithType = `${items.at(-2)}.${items.at(-1)}`;
            }

            return [
                {
                    label: 'Full name',
                    insertText: 'displayProperty=fullName',
                    detail: fullName,
                    kind: CompletionItemKind.Unit
                },
                {
                    label: 'Type with name',
                    insertText: 'displayProperty=nameWithType',
                    detail: nameWithType,
                    kind: CompletionItemKind.Unit
                },
            ];
        }

        return undefined;
    }
}