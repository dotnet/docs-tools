
/**
 * Options to use when searching the API Browser.
 */
export type SearchOptions = {
    /**
     * When true, the resulting string should only contain the UID and none of the <xref:...> brackets.
     */
    SkipBrackets: boolean,

    /**
     * When true, the Insert XREF Link command should not display the display style options.
     */
    SkipDisplayStyle: boolean,

    /**
     * When true, the Insert XREF Link command should not display the custom display style option.
     */
    HideCustomDisplayStyle: boolean
};
