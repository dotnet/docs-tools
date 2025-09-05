import { ApiName } from "../../configuration/types/ApiName";
import { SearchResult } from "./SearchResult";

export type SearchResults = {
    // eslint-disable-next-line @typescript-eslint/naming-convention
    "@nextLink"?: string | undefined;
    count: number;
    results: SearchResult[];
    apiName: ApiName | undefined;
};

export class EmptySearchResults implements SearchResults {
    // eslint-disable-next-line @typescript-eslint/naming-convention
    "@nextLink"?: string | undefined = undefined;
    count: number = 0;
    results: SearchResult[] = [];
    isEmpty: boolean = true;
    apiName: ApiName | undefined = undefined;

    private static readonly _instance: EmptySearchResults = new EmptySearchResults();

    public static get instance(): EmptySearchResults {
        return EmptySearchResults._instance;
    }

    private constructor() { }
};