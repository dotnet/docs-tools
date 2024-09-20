import { CancellationToken, Position, ProviderResult, TextDocument, CompletionItemProvider, CompletionContext, CompletionItem, CompletionList, CompletionItemKind, InlineCompletionItemProvider, InlineCompletionContext, InlineCompletionItem, InlineCompletionList, Range, CodeActionProvider, CodeActionContext, Selection, CodeAction, WorkspaceEdit, CodeActionKind } from "vscode";
import { insertXrefLinkCommandName } from '../consts';
import { SearchOptions } from './types/SearchOptions';
import { ApiService } from "../services/api-service";
import { EmptySearchResults } from "./types/SearchResults";
import { RawGitService } from "../services/raw-git-service";
import { DocIdService } from "../services/docid-service";
import { ItemType } from "./types/ItemType";

export const xrefStarterAutoComplete: CompletionItemProvider = {
    provideCompletionItems: (
        document: TextDocument,
        position: Position,
        token: CancellationToken,
        context: CompletionContext): ProviderResult<CompletionList<CompletionItem> | CompletionItem[]> => {

        const range = document.getWordRangeAtPosition(position, /[<(]xref:/);
        if (range) {

            const text = document.getText(range);

            const searchOptions: SearchOptions = {
                skipBrackets: true,
                skipDisplayStyle: text.startsWith('('),
                hideCustomDisplayStyle: text.startsWith('<')
            };

            return [
                {
                    command: {
                        command: insertXrefLinkCommandName,
                        title: "🔍 Search APIs",
                        arguments: [searchOptions]
                    },
                    label: " — Search APIs...",
                    insertText: "",
                    kind: CompletionItemKind.Text,
                }
            ];
        }

        return undefined;
    }
}

export const xrefDisplayTypeAutoComplete: CompletionItemProvider = {
    provideCompletionItems: (
        document: TextDocument,
        position: Position,
        token: CancellationToken,
        context: CompletionContext): ProviderResult<CompletionList<CompletionItem> | CompletionItem[]> => {

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
                    label: '$(array) Full name',
                    insertText: 'displayProperty=fullName',
                    detail: fullName
                },
                {
                    label: '$(bracket-dot) Name with type',
                    insertText: 'displayProperty=nameWithType',
                    detail: nameWithType
                },
            ];
        }

        return undefined;
    }
}

export const xrefInlineAutoComplete: InlineCompletionItemProvider = {
    provideInlineCompletionItems: async (
        document: TextDocument,
        position: Position,
        context: InlineCompletionContext,
        token: CancellationToken): Promise<InlineCompletionItem[] | InlineCompletionList | undefined> => {

        const regexp = /[<(]xref:(.+)[>)]/;
        if (position.line <= 0) {
            return undefined;
        }

        const range = document.getWordRangeAtPosition(position, regexp);
        if (!range) {
            return undefined;
        }

        const line = document.getText(range);
        const match = line.match(regexp);
        const text = match?.[1];

        if (text && !token.isCancellationRequested) {

            const results = await ApiService.searchApi(text);
            if (results instanceof EmptySearchResults && results.isEmpty === true) {
                return undefined;
            }

            if (token.isCancellationRequested) {
                console.log(`Cancellation requested after search results found.`);
                return undefined;
            }

            const firstResult = results.results[0];

            const rawUrl = await RawGitService.getRawGitUrl(firstResult.url);
            if (!rawUrl || token.isCancellationRequested) {
                console.log(`Cancellation requested after raw git URL found.`);
                return undefined;
            }

            const docId = await DocIdService.getDocId(
                firstResult.displayName, firstResult.itemType as ItemType, rawUrl);
            if (docId && token.isCancellationRequested === false) {
                console.log(`Added completion for ${docId}.`);

                const insertText = docId.substring(text.length);

                const item = new InlineCompletionItem(
                    insertText, range);

                item.filterText = docId;

                return [item];
            }
        }

        return undefined;
    }
}