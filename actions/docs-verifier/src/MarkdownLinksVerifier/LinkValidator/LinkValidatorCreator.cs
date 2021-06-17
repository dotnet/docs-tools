using System;
using System.Collections.Generic;
using MarkdownLinksVerifier.LinkClassifier;

namespace MarkdownLinksVerifier.LinkValidator
{
    internal static class LinkValidatorCreator
    {
        private static readonly OnlineLinkValidator s_onlineValidator = new();
        private static readonly MailtoLinkValidator s_mailtoValidator = new();
        private static readonly Dictionary<string, LocalLinkValidator> s_localValidatorDictionary = new();

        public static ILinkValidator Create(LinkClassification classification, string baseDirectory)
            => classification switch
            {
                LinkClassification.Online => s_onlineValidator,
                LinkClassification.Local => GetLocalLinkValidator(baseDirectory),
                LinkClassification.Mailto => s_mailtoValidator,
                _ => throw new ArgumentException($"Invalid {nameof(classification)}.", nameof(classification))
            };

        private static LocalLinkValidator GetLocalLinkValidator(string baseDirectory)
        {
            if (s_localValidatorDictionary.TryGetValue(baseDirectory, out LocalLinkValidator? cachedValidator))
            {
                return cachedValidator;
            }

            var validator = new LocalLinkValidator(baseDirectory);
            s_localValidatorDictionary.Add(baseDirectory, validator);
            return validator;
        }
    }
}
