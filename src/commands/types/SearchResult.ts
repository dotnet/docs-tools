import { ItemType } from "./ItemType";

export type SearchResult = {
    description: string;
    displayName: string;
    itemType: ItemType;
    url: string;
};
