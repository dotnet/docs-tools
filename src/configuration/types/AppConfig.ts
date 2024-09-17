import { WorkspaceConfiguration } from "vscode";
import { UrlFormat } from "../../commands/types/UrlFormat";
import { StringPair } from "./StringPair";
import { nameof } from "../../utils";

/**
 * Represents the configuration settings for the extension.
 */
export class AppConfig {
    /**
     * The format of the URL to insert.
     * - Default: Only displays the API name.
     * - Full name: Displays the fully qualified name.
     * - Name with type: Displays the name and its type.
     * - Custom name: Allows the user to enter a custom name.
     * @default `undefined`
     */
    defaultUrlFormat: UrlFormat | undefined;

    /**
     * The default API URL to query.
     * - This URL is used to query the API for search results.
     * @default "https://learn.microsoft.com/api/apibrowser/dotnet/search"
     */
    apiUrl: string | undefined;

    /**
     * The default query string parameters to include in the API URL.
     * - These parameters are used to filter the search results.
     * @default [ { "api-version": "0.2" }, { "locale": "en-us" } ]
     */
    queryStringParameters: StringPair[] | undefined;

    constructor(workspaceConfig: WorkspaceConfiguration) {
        const urlFormat = workspaceConfig.get<UrlFormat>(nameof<AppConfig>("defaultUrlFormat"))
        if (urlFormat) {
            this.defaultUrlFormat = urlFormat;
        }

        const apiUrl = workspaceConfig.get<string>(nameof<AppConfig>("apiUrl"))
        if (apiUrl) {
            this.apiUrl = apiUrl;
        }

        const queryParams = workspaceConfig.get<StringPair[]>(nameof<AppConfig>("queryStringParameters"))
        if (queryParams) {
            this.queryStringParameters = queryParams;
        }
    }

    /**
     * Builds the API URL with the search term.
     * @param searchTerm The search term to include in the API URL.
     * @returns A fully qualified URL with query string parameters that includes the search term.
     */
    public buildApiUrlWithSearchTerm = (searchTerm: string): string => {
        const queryString = (this.queryStringParameters ?? [])
            .map((pair) => Object.entries(pair).map(([key, value]) => `${key}=${value}`).join("&"))
            .join("&");

        return `${this.apiUrl}?${queryString}&search=${searchTerm}`;
    };
};

