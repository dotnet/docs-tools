namespace MarkdownLinksVerifier.LinkValidator
{
    internal interface ILinkValidator
    {
        ValidationResult Validate(string link, string filePath);
    }
}
