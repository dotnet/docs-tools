import { window, QuickPickItem } from "vscode";
import { SearchResult, SearchResults } from "./search-results";
import fetch from "node-fetch";

export async function showSearch() {
    const input = await window.showInputBox({
        title: "Search .NET API",
        placeHolder: "Search for a .NET type or member by name."
    });

    if (!input) {
        return;
    }

    // Example URL:
    //   https://learn.microsoft.com/api/apibrowser/dotnet/search?api-version=0.2&search=SmtpClient&locale=en-us&$filter=monikers/any(t:%20t%20eq%20%27net-8.0%27)
    const response = await fetch(
        `https://learn.microsoft.com/api/apibrowser/dotnet/search?api-version=0.2&search=${input}&locale=en-us`, {
        headers: {
            "Content-Type": "application/json",
        }
    });

    if (!response.ok) {
        window.showWarningMessage(`Failed to search for '${input}'.`);
        return;
    }

    const searchResults: SearchResults = await response.json() as SearchResults;
    if (!searchResults || searchResults.count === 0) {
        return;
    }

    const quickPick = window.createQuickPick<SearchResultQuickPickItem | QuickPickItem>();
    quickPick.items = searchResults.results.map(result => new SearchResultQuickPickItem(result));
    quickPick.title = `Search results for '${input}'`;
    quickPick.placeholder = 'Select a type or member to insert a link to.';

    let selection: SearchResultQuickPickItem | undefined;
    
    quickPick.onDidChangeSelection((items) => {
        const item = items[0];
        if (item instanceof SearchResultQuickPickItem) {
            selection = item;

            quickPick.items = [ 
                { label: 'Default', description: 'Only displays the API name.' }, 
                { label: 'Full name', description: 'Displays the fully qualified name.' }, 
                { label: 'Type with name', description: 'Displays the type with the name.' }
            ];
            quickPick.title = 'Select URL format.';
            quickPick.placeholder = 'Select the format of the URL to insert.';
            quickPick.show();

        } else if (!!item) {
            const displayName = toDisplayName(item, selection!.result);

            // Insert the URL into the active text editor
            insertLink(displayName, selection);

            quickPick.hide();
            quickPick.dispose();
        }
    });

    quickPick.show();
}

const toDisplayName = (type: QuickPickItem, result: SearchResult) => {
    // TODO: splat query string on xref
    switch (type.label) {
        case 'Default': 
            return result.displayName.substring(result.displayName.lastIndexOf('.') + 1);
        
        case 'Full name': 
            return result.displayName;
        
        default:
        {
            const segments = result.displayName.split('.');
            return `${segments[segments.length - 2]}.${segments[segments.length - 1]}`;
        };
    }
};

class SearchResultQuickPickItem implements QuickPickItem {
    label: string;
    description?: string | undefined;

    constructor(public readonly result: SearchResult) {
        this.label = result.displayName;        
        this.description = result.itemType
    }
}

function insertLink(displayName: string, selection: SearchResultQuickPickItem | undefined) {
    if (window.activeTextEditor) {
        const editor = window.activeTextEditor;

        const url = `[${displayName}](${selection!.result.url})`;

        editor.edit((editBuilder) => {
            editBuilder.insert(editor.selection.active, url);
        });
    }
}
