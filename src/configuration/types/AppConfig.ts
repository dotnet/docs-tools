import { WorkspaceConfiguration } from "vscode";
import { StringPair } from "./StringPair";
import { nameof } from "../../utils";
import { ApiName } from "./ApiName";

export class ApiConfig {
    /**
     * Whether the API is enabled.
     * - When enabled, the API is used to query search results.
     */
    enabled: boolean | undefined;

    /**
     * The name of the API, e.g. "dotnet".
     * - This name is used to identify the API in the extension.
     */
    name: ApiName | undefined;

    /**
     * The display name of the API, e.g. ".NET".
     */
    displayName: string | undefined;

    /**
     * The default API URL to query.
     * - This URL is used to query the API for search results.
     * @default "https://learn.microsoft.com/api/apibrowser/dotnet/search"
     */
    url: string | undefined;

    /**
     * The default query string parameters to include in the API URL.
     * - These parameters are used to filter the search results.
     * @default [ { "api-version": "0.2" }, { "locale": "en-us" } ]
     */
    queryStringParameters: StringPair[] | undefined;
}

/**
 * Represents the configuration settings for the extension.
 */
export class AppConfig {
    /**
     * The APIs to use for searching.
     * - These APIs are used to query the search results.
     */
    apis: ApiConfig[] | undefined;

    /**
     * Whether to append the overloads to the search results.
     * - When enabled, the search results will include the overloads.
     * @default true
     */
    appendOverloads: boolean | undefined = true;

    /**
     * Whether to prompt the user for GitHub auth to allow the GitHub
     * session to be used for API requests. Enables scenarios where XREF
     * metadata is in a private GitHub repo.
     * - When enabled, the GitHub session is used to authenticate the API requests.
     * @default false
     */
    allowGitHubSession: boolean | undefined = false;

    constructor(workspaceConfig: WorkspaceConfiguration) {

        const apis = workspaceConfig.get<ApiConfig[]>(nameof<AppConfig>("apis"));
        if (apis !== undefined) {
            this.apis = apis;
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
     * @param name The name of the API to use.
     * @param searchTerm The search term to include in the API URL.
     * @returns A fully qualified URL with query string parameters that includes the search term.
     */
    public buildApiUrlWithSearchTerm = (name: string, searchTerm: string, top: number = 25): string => {
        const api = this.apis?.find((api) => api.displayName === name);
        if (!api || !api.enabled) {
            return "";
        }

        const queryString = (api.queryStringParameters ?? [])
            .map((pair) => Object.entries(pair).map(([key, value]) => `${key}=${value}`).join("&"))
            .join("&");

        return `${api.url}?${queryString}&search=${searchTerm}&$top=${top}`;
    };
};

