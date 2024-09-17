/**
 * The format of the URL to insert.
 * - Default: Only displays the API name.
 * - Full name: Displays the fully qualified name.
 * - Name with type: Displays the type with the name.
 * - Custom name: Allows the user to enter a custom name.
 * @link https://review.learn.microsoft.com/en-us/help/platform/links-how-to?branch=main#display-properties
 */
export enum UrlFormat {
    /**
     * Only displays the API name.
     * @description When formatting an `xref` link, no query strings are used.
     * @example <xref:System.String.Trim> -> "Trim()"
     */
    default = 'Default',

    /**
     * Displays the fully qualified name.
     * @description When formatting an `xref` link, the `?displayProperty=fullName` 
     * query parameter is used to display the fully qualified name.
     * @example <xref:System.String.Trim?displayProperty=fullName> -> "System.String.Trim()"
     */
    fullName = 'Full name',

    /**
     * Displays the name with its type.
     * @description When formatting an `xref` link, the `?displayProperty=nameWithType`
     * query parameter is used to display the type with the name.
     * @example <xref:System.String.Trim?displayProperty=nameWithType> -> "String.Trim()"
     */
    nameWithType = 'Name with type',

    /**
     * Allows the user to enter a custom name.
     * @description When formatting an `xref` link with custom naming, no query strings are used.
     * @example [The string.Trim() method](xref:System.String.Trim) -> "The string.Trim() method"
     */
    customName = 'Custom name'
}
