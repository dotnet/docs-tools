export type SearchResults = {
    "@nextLink": string;
    count: number;
    results: SearchResult[];
};

export type SearchResult = {
    description: string;
    displayName: string;
    itemName: string;
    url: string
};