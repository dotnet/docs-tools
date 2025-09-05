/**
 * Enum for the type of link
 */
export enum LinkType {
    /**
     * The link is a markdown link.
     * @example <caption>Example usage of Markdown link.</caption>
     * // This is a markdown link
     * [link text](link url)
     */
    Markdown,

    /**
     * The link is an API reference link.
     * @example <caption>Example usage of XREF link.</caption>
     * // This is an XREF link
     * <xref:uid>
     */
    Xref
}