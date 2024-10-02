import fetch from "node-fetch";
import { EmptySearchResults, SearchResults } from "../commands/types/SearchResults";
import { ConfigReader } from "../configuration/config-reader";
import { ApiConfig, AppConfig } from "../configuration/types/AppConfig";
import { QuickPickItemKind, window } from "vscode";
import { tooManyResults } from "../consts";
import { ItemType } from "../commands/types/ItemType";
import { SearchResult } from "../commands/types/SearchResult";
import { ApiName, getSymbolIcon } from "../configuration/types/ApiName";

export class ApiService {
    public static async searchApi(searchTerm: string): Promise<SearchResults | EmptySearchResults> {
        const appConfig: AppConfig = ConfigReader.readConfig();

        if (!appConfig.apis || appConfig.apis.length === 0) {
            return EmptySearchResults.instance;
        }

        let apiConfig: ApiConfig | undefined;
        const enabledApis = appConfig.apis.filter(api => api.enabled);
        if (!enabledApis || enabledApis.length === 0) {
            return EmptySearchResults.instance;
        }

        if (enabledApis.length === 1) {
            apiConfig = enabledApis[0];
        } else {
            // Prompt the user to select an API.
            // Create the quick pick items.
            const apiQuickPickItems = enabledApis.map(api => {
                // @ts-ignore

                return {
                    label: `${getSymbolIcon(api.displayName)} ${api.displayName}`,
                    description: api.url,
                    apiName: api.name
                };
            });

            const item = await window.showQuickPick(apiQuickPickItems, {
                title: "✅ Select an API",
                placeHolder: "Select an API to search"
            });

            if (item) {
                apiConfig = appConfig.apis.find(api => {
                    // @ts-ignore
                    return api.name === item.apiName;
                });
            }
        }

        if (!apiConfig) {
            return EmptySearchResults.instance;
        }

        const searchApiUrl = appConfig.buildApiUrlWithSearchTerm(apiConfig.displayName!, searchTerm);

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
        if (!searchResults || searchResults.count === 0 || !searchResults.count) {
            return EmptySearchResults.instance;
        }

        if (appConfig.appendOverloads) {
            appendOverloads(searchResults);
        }

        searchResults.apiName = apiConfig.name;

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
                description: `The search results are limited to ${searchResults.results.length - 1} items. Try a more specific search term.`,
                url: searchResults["@nextLink"]
            });
        }

        return searchResults;
    }
}

/**
 * Iterate all search results, and when there are constructor or method overloads,
 * inject a single new result before all overloads that's a copy of the first overload.
 * @param searchResults
 */
function appendOverloads(searchResults: SearchResults) {
    let visitedDisplayName: string = "";
    let foundOverload: boolean = false;
    let previousOverload: SearchResult | undefined = undefined;
    const resultsToInject: Map<string, { targetIndex: number; result: SearchResult; }> = new Map();

    for (let index = searchResults.results.length - 1; index >= 0; index--) {
        const result = searchResults.results[index];

        if (foundOverload && previousOverload) {
            // Add a new result before the overload.
            resultsToInject.set(
                visitedDisplayName, {
                targetIndex: index + 1,
                result: {
                    displayName: `${visitedDisplayName}*`,
                    itemType: previousOverload.itemType,
                    description: previousOverload.description,
                    url: previousOverload.url
                }
            });

            foundOverload = false;
        }

        if (result.itemType !== ItemType.method &&
            result.itemType !== ItemType.constructor) {
            foundOverload = false;
            continue;
        }

        if (visitedDisplayName && result.displayName.startsWith(visitedDisplayName)) {
            // We have an overload, so add a new result.
            console.log(`found overload: ${result.displayName}`);
            previousOverload = result;
            foundOverload = true;
        }

        const i = result.displayName.indexOf('(');
        const j = result.displayName.indexOf('<');
        const d = result.displayName.substring(0, i > 0 && j > 0 ? Math.min(i, j) : i || j);

        visitedDisplayName = d;
    }

    const overloads = Array.from(resultsToInject.values()).reverse();

    for (let index = 0; index < overloads.length; index++) {
        const resultToInject = overloads[index];
        searchResults.results.splice(resultToInject.targetIndex + index, 0, resultToInject.result);
    }
}
