import { ApiService } from "../services/api-service";
import { EmptySearchResults } from "./types/SearchResults";
import { ItemType } from "./types/ItemType";
import { LinkType } from "./types/LinkType";
import { mdLinkFormatter } from "./formatters/mdLinkFormatter";
import { SearchResultQuickPickItem } from "./types/SearchResultQuickPickItem";
import { UrlFormat } from "./types/UrlFormat";
import { window, QuickPickItem, QuickPick } from "vscode";
import { xrefLinkFormatter } from "./formatters/xrefLinkFormatter";
import { SearchResult } from "./types/SearchResult";
import { getUserSelectedText, replaceUserSelectedText } from "../utils";

export async function insertLink(linkType: LinkType) {
    const searchTerm = await window.showInputBox({
        title: "Search APIs",
        placeHolder: "Search for a type or member by name."
    });

    if (!searchTerm) {
        return;
    }

    const searchResults = await ApiService.searchApi(searchTerm);
    if (searchResults instanceof EmptySearchResults && searchResults.isEmpty === true) {
        window.showWarningMessage(`Failed to find results for '${searchTerm}'.`);
    }

    // Create a quick pick to display the search results, allowing the user to select a type or member.
    const quickPick = window.createQuickPick<SearchResultQuickPickItem | QuickPickItem>();
    quickPick.items = searchResults.results.map(
        (result: SearchResult) => new SearchResultQuickPickItem(result));
    quickPick.title = `Search results for '${searchTerm}'`;
    quickPick.placeholder = 'Select a type or member to insert a link to.';

    let searchResultSelection: SearchResultQuickPickItem | undefined;

    quickPick.onDidChangeSelection(async (items) => {
        // Represents the selected item
        const selectedItem = items[0];

        if (selectedItem instanceof SearchResultQuickPickItem) {
            // User has selected a search result.
            searchResultSelection = selectedItem;

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

            if (searchResultSelection.itemType === ItemType.namespace) {
                await createAndInsertLink(
                    linkType,
                    UrlFormat.fullName,
                    searchResultSelection,
                    quickPick);

                return;
            }

            quickPick.items = [
                { label: UrlFormat.default, description: 'Only displays the API name.' },
                { label: UrlFormat.fullName, description: 'Displays the fully qualified name.' },
                { label: UrlFormat.nameWithType, description: 'Displays the type and name in the format "Type.Name".' },
                { label: UrlFormat.customName, description: 'Lets you enter custom link text.' },
            ];
            quickPick.title = 'Select URL format.';
            quickPick.placeholder = 'Select the format of the URL to insert.';
            quickPick.show();

        } else if (!!selectedItem) {
            // At this point, the selectedItem.label is a UrlFormat enum value.
            await createAndInsertLink(
                linkType,
                selectedItem.label as UrlFormat,
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

    const url = linkType === LinkType.Xref
        ? await xrefLinkFormatter(format, searchResultSelection!.result)
        : await mdLinkFormatter(format, searchResultSelection!.result);

    // Insert the URL into the active text editor
    if (!insertUrlIntoActiveTextEditor(url, isTextReplacement)) {
        window.setStatusBarMessage(
            `Failed to insert URL into the active text editor.`, 3000);
    }

    quickPick.hide();
    quickPick.dispose();
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
