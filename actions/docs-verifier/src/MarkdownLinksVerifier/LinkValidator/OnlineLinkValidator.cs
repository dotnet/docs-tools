namespace MarkdownLinksVerifier.LinkValidator
{
    // Singleton?
    internal class OnlineLinkValidator : ILinkValidator
    {
        public ValidationResult Validate(string link, string filePath)
        {
            // TODO: implement this.
            return new ValidationResult { State = ValidationState.Valid, AbsolutePathWithoutHeading = link };
        }
    }
}
