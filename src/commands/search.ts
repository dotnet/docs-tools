import { SearchResultQuickPickItem } from "./types/SearchResultQuickPickItem";
import EmptySearchResults from "./types/SearchResults";
import { UrlFormat } from "./types/UrlFormat";
import { window, QuickPickItem } from "vscode";
import { xrefLinkFormatter } from "./formatters/xrefLinkFormatter";
import { ApiService } from "../services/api-service";

export async function startApiSearch() {
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
    quickPick.items = searchResults.results.map(result => new SearchResultQuickPickItem(result));
    quickPick.title = `Search results for '${searchTerm}'`;
    quickPick.placeholder = 'Select a type or member to insert a link to.';

    let searchResultSelection: SearchResultQuickPickItem | undefined;

    quickPick.onDidChangeSelection(async (items) => {
        // Represents the selected item
        const selectedItem = items[0];

        if (selectedItem instanceof SearchResultQuickPickItem) {
            // Use has selected a search result.
            searchResultSelection = selectedItem;

            quickPick.items = [
                { label: UrlFormat.default, description: 'Only displays the API name.' },
                { label: UrlFormat.fullName, description: 'Displays the fully qualified name.' },
                { label: UrlFormat.nameWithType, description: 'Displays the type and name in the format "Type.Name".' },
                { label: UrlFormat.customName, description: 'Allows the user to enter a custom name' },
            ];
            quickPick.title = 'Select URL format.';
            quickPick.placeholder = 'Select the format of the URL to insert.';
            quickPick.show();

        } else if (!!selectedItem) {
            const url = await xrefLinkFormatter(selectedItem.label as UrlFormat, searchResultSelection!.result);

            // Insert the URL into the active text editor
            if (!insertUrlIntoActiveTextEditor(url)) {
                window.setStatusBarMessage(
                    `Failed to insert URL into the active text editor.`, 3000);
            }

            quickPick.hide();
            quickPick.dispose();
        }
    });

    quickPick.show();
}

/**
 * Inserts the URL into the @type `window.activeTextEditor`
 * @param url The URL to insert into the active text editor.
 * @returns `boolean`
 */
function insertUrlIntoActiveTextEditor(url?: string | undefined): boolean {
    if (!url) {
        return false;
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