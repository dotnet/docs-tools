import { WorkspaceConfiguration } from "vscode";
import { StringPair } from "./StringPair";
import { nameof } from "../../utils";

/**
 * Represents the configuration settings for the extension.
 */
export class AppConfig {
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

    /**
     * Whether to append the overloads to the search results.
     * - When enabled, the search results will include the overloads.
     * @default true
     */
    appendOverloads: boolean | undefined = true;

    /**
     * Whether to allow GitHub session to be used for API requests. 
     * Enables scenarios where XREF metadata is in a private GitHub repo.
     * - When enabled, the GitHub session is used to authenticate the API requests.
     * @default true
     */
    allowGitHubSession: boolean | undefined = true;

    constructor(workspaceConfig: WorkspaceConfiguration) {

        const apiUrl = workspaceConfig.get<string>(nameof<AppConfig>("apiUrl"));
        if (apiUrl !== undefined) {
            this.apiUrl = apiUrl;
        }

        const queryParams = workspaceConfig.get<StringPair[]>(nameof<AppConfig>("queryStringParameters"));
        if (queryParams !== undefined) {
            this.queryStringParameters = queryParams;
        }

        const appendOverloads = workspaceConfig.get<boolean>(nameof<AppConfig>("appendOverloads"));
        if (appendOverloads !== undefined) {
            this.appendOverloads = appendOverloads;
        }

        const allowGitHubSession = workspaceConfig.get<boolean>(nameof<AppConfig>("allowGitHubSession"));
        if (allowGitHubSession !== undefined) {
            this.allowGitHubSession = allowGitHubSession;
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

