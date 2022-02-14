using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdownLinksVerifier.Configuration;
using MarkdownLinksVerifier.LinkClassifier;
using MarkdownLinksVerifier.LinkValidator;

namespace MarkdownLinksVerifier
{
    public record LinkError(string File, string Link, string RelativeTo);

    public static class MarkdownFilesAnalyzer
    {
        public static async Task<List<LinkError>> GetResultsAsync(
            MarkdownLinksVerifierConfiguration? config, string? rootDirectory = null)
        {
            rootDirectory ??= Directory.GetCurrentDirectory();
            var result = new List<LinkError>();
            foreach (string file in Directory.EnumerateFiles(rootDirectory, "*.md", SearchOption.AllDirectories))
            {
                string? directory = Path.GetDirectoryName(file);
                if (directory is null)
                {
                    throw new InvalidOperationException($"Cannot get directory for '{file}'.");
                }

                MarkdownDocument document = Markdown.Parse(await File.ReadAllTextAsync(file));
                foreach (LinkInline link in document.Descendants<LinkInline>())
                {
                    LinkClassification classification = Classifier.Classify(link.Url);
                    ILinkValidator validator = LinkValidatorCreator.Create(classification, directory);
                    if (!IsLinkExcluded(config, link.Url) && !validator.IsValid(link.Url, file))
                    {
                        result.Add(new LinkError(file, link.Url, directory));
                    }
                }
            }

            return result;

            static bool IsLinkExcluded(MarkdownLinksVerifierConfiguration? config, string url)
            {
                if (config is null)
                {
                    return false;
                }

                return config.IsLinkExcluded(url);
            }
        }
    }
}
