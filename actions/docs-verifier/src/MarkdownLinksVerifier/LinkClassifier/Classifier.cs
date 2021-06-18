using System;

namespace MarkdownLinksVerifier.LinkClassifier
{
    internal static class Classifier
    {
        internal static LinkClassification Classify(string link)
        {
            if (Uri.TryCreate(link, UriKind.Absolute, out Uri? uri))
            {
                return uri.Scheme switch
                {
                    "http" => LinkClassification.Online,
                    "https" => LinkClassification.Online,
                    "ftp" => LinkClassification.Online,
                    "mailto" => LinkClassification.Mailto,
                    _ => LinkClassification.Local
                };
            }

            return LinkClassification.Local;
        }
    }
}
