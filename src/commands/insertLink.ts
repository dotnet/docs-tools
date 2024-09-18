import { ApiService } from "../services/api-service";
import { EmptySearchResults } from "./types/SearchResults";
import { ItemType } from "./types/ItemType";
import { LinkType } from "./types/LinkType";
import { mdLinkFormatter } from "./formatters/mdLinkFormatter";
import { SearchResultQuickPickItem } from "./types/SearchResultQuickPickItem";
import { UrlFormat } from "./types/UrlFormat";
import { window, QuickPickItem, QuickPick, ProgressLocation } from "vscode";
import { xrefLinkFormatter } from "./formatters/xrefLinkFormatter";
import { SearchResult } from "./types/SearchResult";
import { getUserSelectedText, replaceUserSelectedText, searchTermInputValidation } from "../utils";
import { tooManyResults, urlFormatQuickPickItems, urlFormatQuickPickOverloadItems } from "../consts";
import { RawGitService } from "../services/raw-git-service";
import { DocIdService } from "../services/docid-service";

export async function insertLink(linkType: LinkType) {
    const searchTerm = await window.showInputBox({        
        title: "🔍 Search APIs",
        placeHolder: `Search for a type or member by name, for example; "HttpClient".`,
        validateInput: searchTermInputValidation
    });

    // This should never happen, since we're validating, but it also doesn't hurt to have this check.
    if (!searchTerm) {
        return;
    }

    const searchResults = await ApiService.searchApi(searchTerm);
    if (searchResults instanceof EmptySearchResults && searchResults.isEmpty === true) {
        window.showWarningMessage(`We didn't find any results for the "${searchTerm}" search term.`);
        return;
    }

    // Create a quick pick to display the search results, allowing the user to select a type or member.
    const quickPick = window.createQuickPick<SearchResultQuickPickItem | QuickPickItem>();

    quickPick.items = searchResults.results.map(
        (result: SearchResult) => new SearchResultQuickPickItem(result));
    quickPick.title = `📌 Search results for "${searchTerm}"`;
    quickPick.placeholder = 'Type to filter by name, and select a type or member to insert a link to.';
    quickPick.matchOnDescription = true;
    quickPick.matchOnDetail = true;

    let searchResultSelection: SearchResultQuickPickItem | undefined;

    quickPick.onDidChangeSelection(async (items) => {
        // Represents the selected item
        const selectedItem = items[0];

        if (selectedItem instanceof SearchResultQuickPickItem) {
            // User has selected a search result.
            searchResultSelection = selectedItem;

            // When the user selects the too many results item, hide and dispose the quick pick.
            if (searchResultSelection.itemType === tooManyResults) {
                quickPick.dispose();

                return;
            }

            // When the user has selected text in the active text editor,
            // create a custom name link with the selected text.
            const selectedText = getUserSelectedText();
            if (selectedText) {
                await createAndInsertLink(
                    linkType,
                    UrlFormat.customName,
                    searchResultSelection,
                    quickPick,
                    true);

                return;
            }

            // When the user selects a namespace, create a link using the default format.
            // Namespaces are always displayed as fully qualified names.
            if (searchResultSelection.itemType === ItemType.namespace) {
                await createAndInsertLink(
                    linkType,
                    UrlFormat.default,
                    searchResultSelection,
                    quickPick);

                return;
            }

            // If the user wants a Markdown link, then we assume they want custom link text.
            if (linkType === LinkType.Markdown) {
                await createAndInsertLink(
                    linkType,
                    UrlFormat.customName,
                    searchResultSelection,
                    quickPick);

                return;
            }

            // If we make it here, the user will now be prompted to select the URL format.
            quickPick.items = urlFormatQuickPickItems;
            quickPick.title = '🔗 Select URL format';
            quickPick.value = ''; // Remove user text filtering...
            quickPick.placeholder = 'Select the format of the URL to insert.';
            quickPick.show();

        } else if (!!selectedItem) {
            // At this point, the selectedItem.label is a UrlFormat enum value
            // with a leading icon, e.g. "$(check) default".
            const match = selectedItem.label.match(/\$\(.*\) (.+)/);
            const urlFormat: UrlFormat = match?.[1] as UrlFormat;

            await createAndInsertLink(
                linkType,
                urlFormat,
                searchResultSelection!,
                quickPick);
        }
    });

    quickPick.show();
}

async function createAndInsertLink(
    linkType: LinkType,
    format: UrlFormat,
    searchResultSelection: SearchResultQuickPickItem,
    quickPick: QuickPick<SearchResultQuickPickItem | QuickPickItem>,
    isTextReplacement: boolean = false) {

    quickPick.busy = true;

    await window.withProgress({
        location: ProgressLocation.Notification,
        title: 'Generating Link',
        cancellable: true
    }, async (progress, token) => {

        token.onCancellationRequested(() => {
            quickPick.dispose();
        });

        const result = searchResultSelection.result;

        progress.report({
            message: `Requesting metadata for selection...`
        });

        const rawUrl = await RawGitService.getRawGitUrl(result.url);
        if (!rawUrl || token.isCancellationRequested) {
            token.isCancellationRequested = true;
            quickPick.dispose();
            return;
        }

        progress.report({
            message: `Requesting document ID...`
        });

        const docId = await DocIdService.getDocId(result.displayName, result.itemType as ItemType, rawUrl)
        if (!docId || token.isCancellationRequested) {
            token.isCancellationRequested = true;
            quickPick.dispose();
            return;
        }

        let url;
        if (linkType === LinkType.Xref) {
            // Replace some special characters.
            const encodedDocId = docId.replaceAll('#', '%23')
                .replaceAll('<', '{')
                .replaceAll('>', '}');

            url = await xrefLinkFormatter(format, encodedDocId);
        }
        else {
            url = await mdLinkFormatter(format, searchResultSelection!.result);
        }

        // Insert the URL into the active text editor
        if (!token.isCancellationRequested &&
            !insertUrlIntoActiveTextEditor(url, isTextReplacement)) {
            token.isCancellationRequested = true;
            window.setStatusBarMessage(
                `Failed to insert URL into the active text editor.`, 3000);
        }

        quickPick.dispose();
    });
}

/**
 * Inserts the URL into the @type `window.activeTextEditor`
 * @param url The URL to insert into the active text editor.
 * @returns `boolean`
 */
function insertUrlIntoActiveTextEditor(
    url?: string | undefined,
    isTextReplacement: boolean = false): boolean {
    if (!url) {
        return false;
    }

    if (isTextReplacement) {
        replaceUserSelectedText(url);
    }

    if (window.activeTextEditor || window.activeTextEditor!.selection) {
        const editor = window.activeTextEditor!;

        editor.edit((editBuilder) => {
            editBuilder.insert(editor.selection.active, url);
        });

        return true;
    } else {
        window.showWarningMessage(
            `No active text editor to insert "${url}" into.`
        );
    }

    return false;
};
