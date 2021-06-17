using System;

namespace MarkdownLinksVerifier.LinkClassifier
{
    internal static class Classifier
    {
        public static LinkClassification Classify(string link)
        {
            if (link.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                link.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
            {
                return LinkClassification.Online;
            }

            if (link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                return LinkClassification.Mailto;
            }

            return LinkClassification.Local;
        }
    }
}
