import fetch from "node-fetch";
import { EmptySearchResults, SearchResults } from "../commands/types/SearchResults";
import { ConfigReader } from "../configuration/config-reader";
import { AppConfig } from "../configuration/types/AppConfig";
import { QuickPickItemKind } from "vscode";
import { tooManyResults } from "../consts";

export class ApiService {
    public static async searchApi(searchTerm: string): Promise<SearchResults | EmptySearchResults> {
        const appConfig: AppConfig = ConfigReader.readConfig();

        const searchApiUrl = appConfig.buildApiUrlWithSearchTerm(searchTerm);

        const response = await fetch(
            searchApiUrl, {
            headers: {
                // eslint-disable-next-line @typescript-eslint/naming-convention
                "Content-Type": "application/json",
            }
        });

        if (!response.ok) {
            return EmptySearchResults.instance;
        }

        const searchResults: SearchResults = await response.json() as SearchResults;
        if (!searchResults || searchResults.count === 0) {
            return EmptySearchResults.instance;
        }

        // When there are more results, let the user know they should refine their search.
        if (searchResults["@nextLink"] !== undefined) {
            searchResults.results.push({
                displayName: "",
                itemType: "",
                description: "",
                url: "",
                kind: QuickPickItemKind.Separator
            });

            searchResults.results.push({
                displayName: "$(warning) Too many results",
                itemType: tooManyResults,
                description: `The search results are limited to ${searchResults.results.length} items. Try a more specific search term.`,
                url: searchResults["@nextLink"]
            });
        }

        return searchResults;
    }
}