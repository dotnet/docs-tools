using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace MarkdownLinksVerifier.LinkValidator
{
    internal class MailtoLinkValidator : ILinkValidator
    {
        private static readonly EmailAddressAttribute emailAddressAttribute = new();

        public bool IsValid(string link, string filePath)
        {
            Debug.Assert(link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase));
            return emailAddressAttribute.IsValid(link["mailto:".Length..]);
        }
    }
}
