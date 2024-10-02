/**
 * Enum for API names
 */
export enum ApiName {
    /**
     * .NET API
     * - The .NET API is used to search the .NET API documentation.
     */
    dotnet = ".NET",

    /**
     * Java API
     * - The Java API is used to search the Java API documentation.
     */
    java = "Java",

    /**
     * JavaScript API
     * - The JavaScript API is used to search the JavaScript API documentation.
     */
    javascript = "JavaScript",

    /**
     * Python API
     * - The Python API is used to search the Python API documentation.
     */
    python = "Python",

    /**
     * PowerShell API
     * - The PowerShell API is used to search the PowerShell API documentation.
     */
    powershell = "PowerShell"
}

export function getSymbolIcon(apiName: ApiName) {
    switch (apiName) {
        case ApiName.dotnet:
            return "$(heart-filled)";
        case ApiName.java:
            return "$(coffee)";
        case ApiName.javascript:
            return "$(json)";
        case ApiName.python:
            return "$(snake)";
        case ApiName.powershell:
            return "$(terminal-powershell)";
    }
}
