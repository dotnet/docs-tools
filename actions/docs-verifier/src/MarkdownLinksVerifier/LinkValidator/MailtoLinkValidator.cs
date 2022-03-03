using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace MarkdownLinksVerifier.LinkValidator
{
    internal class MailtoLinkValidator : ILinkValidator
    {
        private static readonly EmailAddressAttribute emailAddressAttribute = new();

        public ValidationResult Validate(string link, string filePath)
        {
            Debug.Assert(link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase));
            return new ValidationResult { AbsolutePathWithoutHeading = link, State = emailAddressAttribute.IsValid(link["mailto:".Length..]) ? ValidationState.Valid : ValidationState.LinkNotFound };
        }
    }
}
