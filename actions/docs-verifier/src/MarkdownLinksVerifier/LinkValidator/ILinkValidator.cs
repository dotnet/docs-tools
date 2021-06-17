namespace MarkdownLinksVerifier.LinkValidator
{
    internal interface ILinkValidator
    {
        bool IsValid(string link, string filePath);
    }
}
