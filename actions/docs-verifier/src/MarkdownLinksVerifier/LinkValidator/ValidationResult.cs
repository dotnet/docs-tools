namespace MarkdownLinksVerifier
{
    /// <summary>
    /// For a link on the form <c>[Text](./path/to/file.md#heading-reference)</c>:
    /// <list type="bullet">
    /// <item>If file.md is found and the heading reference is correct, result will be <see cref="Valid"/></item>
    /// <item>If file.md is found but the heading reference is invalid, result will be <see cref="HeadingNotFound"/></item>
    /// <item>If file.md is not found, result will be <see cref="LinkNotFound"/></item>
    /// </list>
    /// </summary>
    internal enum ValidationState
    {
        Valid,
        LinkNotFound,
        HeadingNotFound,
    }

    internal struct ValidationResult
    {
        public ValidationState State;

        /// <summary>
        /// The expected absolute path of the linked file.
        /// </summary>
        public string AbsolutePathWithoutHeading;
    }
}
