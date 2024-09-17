import { QuickPickItem, QuickPickItemKind } from "vscode";
import { SearchResult } from "./SearchResult";
import { ItemType } from "./ItemType";

/**
 * Represents a search result as a quick pick item.
 */
export class SearchResultQuickPickItem implements QuickPickItem {
    label: string;
    description?: string | undefined;
    itemType: ItemType | string;
    url: string;
    kind?: QuickPickItemKind;

    constructor(public readonly result: SearchResult) {
        if (Object.values(ItemType).includes(result.itemType as ItemType)) {
            this.label = `${this.getSymbolIcon(result.itemType)} ${result.displayName}`;
            this.itemType = result.itemType as ItemType;
        } else {
            this.label = result.displayName;
            this.itemType = result.itemType;
        }        

        this.kind = result.kind ?? QuickPickItemKind.Default;
        this.description = this.itemType.toString();
        this.url = result.url;
    }

    private getSymbolIcon(itemType: ItemType | string): string {
        switch (itemType) {
            case ItemType.class:
                return "$(symbol-class)";
            case ItemType.event:
                return "$(symbol-event)";
            case ItemType.constructor:
                return "$(symbol-constructor)";
            case ItemType.namespace:
                return "$(symbol-namespace)";
            case ItemType.field:
                return "$(symbol-field)";
            case ItemType.enum:
                return "$(symbol-enum)";
            case ItemType.interface:
                return "$(symbol-interface)";
            case ItemType.method:
                return "$(symbol-method)";
            case ItemType.property:
                return "$(symbol-property)";
            case ItemType.struct:
                return "$(symbol-struct)";
            default:
                return "";
        }
    }

}
