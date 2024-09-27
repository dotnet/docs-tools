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
            this.label = `${this.getSymbolIcon(result.itemType)}`;
            this.itemType = result.itemType as ItemType;
            const isOverload = result.displayName.endsWith("*");
            const description = isOverload
                ? `${result.displayName} — ${this.itemType} overloads`
                : `${result.displayName} — ${this.itemType}`;

            this.description = description;
        } else {
            this.label = result.displayName;
            this.itemType = result.itemType;
            this.description = result.description;
        }

        this.url = result.url;
        this.kind = result.kind ?? QuickPickItemKind.Default;
    }

    private getSymbolIcon(itemType: ItemType | string): string {
        switch (itemType) {
            case ItemType.class:
                return "$(symbol-class)";
            case ItemType.constructor:
                return "$(symbol-constructor)";
            case ItemType.namespace:
                return "$(symbol-namespace)";
            case ItemType.field:
                return "$(symbol-field)";
            case ItemType.enum:
                return "$(symbol-enum)";
            case ItemType.delegate:
                return "$(symbol-type-parameter)";
            case ItemType.interface:
                return "$(symbol-interface)";
            case ItemType.method:
                return "$(symbol-method)";
            case ItemType.operator:
                return "$(symbol-operator)";
            case ItemType.struct:
                return "$(symbol-struct)";
            case ItemType.operator:
                return "$(symbol-operator)";
            case ItemType.property:
            case ItemType.attachedProperty:
                return "$(symbol-property)";
            case ItemType.event:
            case ItemType.attachedEvent:
                return "$(symbol-event)";

            case ItemType.command:
            case ItemType.function:
                return "$(symbol-method)";
            case ItemType.module:
                return "$(symbol-module)";
            case ItemType.typeAlias:
                return "$(symbol-type-parameter)";
            case ItemType.rootImportPackage:
            case ItemType.importPackage:
            case ItemType.package:
                return "$(symbol-package)";

            default:
                return "$(symbol-misc)";
        }
    }

}
