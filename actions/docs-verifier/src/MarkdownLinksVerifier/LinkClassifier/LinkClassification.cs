namespace MarkdownLinksVerifier.LinkClassifier
{
    internal enum LinkClassification
    {
        /// <summary>
        /// Indicates an http/https link.
        /// </summary>
        Online,
        /// <summary>
        /// Indicates a link to a local file.
        /// </summary>
        Local,
        /// <summary>
        /// Indicates a <code>mailto:</code> link.
        /// </summary>
        Mailto,
    }
}
