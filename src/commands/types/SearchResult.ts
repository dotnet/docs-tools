import { QuickPickItemKind } from "vscode";
import { ItemType } from "./ItemType";

export type SearchResult = {
    description: string;
    displayName: string;
    itemType: ItemType | string;
    url: string;
    kind?: QuickPickItemKind
};
