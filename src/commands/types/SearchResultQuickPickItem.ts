import { QuickPickItem } from "vscode";
import { SearchResult } from "./SearchResult";
import { ItemType } from "./ItemType";

/**
 * Represents a search result as a quick pick item.
 */
export class SearchResultQuickPickItem implements QuickPickItem {
    label: string;
    description?: string | undefined;
    itemType: ItemType;
    url: string;

    constructor(public readonly result: SearchResult) {
        this.label = result.displayName;
        this.description = result.itemType.toString();
        this.itemType = result.itemType;
        this.url = result.url;
    }
}
