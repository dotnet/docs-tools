import fetch from "node-fetch";
import { EmptySearchResults, SearchResults } from "../commands/types/SearchResults";
import { ConfigReader } from "../configuration/config-reader";
import { AppConfig } from "../configuration/types/AppConfig";

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

        return searchResults;
    }
}