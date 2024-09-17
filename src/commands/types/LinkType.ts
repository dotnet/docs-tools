/**
 * Enum for the type of link
 */
export enum LinkType {
    /**
     * The link is a markdown link.
     * @example `[link text](link url)`
     */
    Markdown,

    /**
     * The link is an API reference link.
     * @example `<xref:uid>`
     */
    Xref
}