using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkdownLinksVerifier.LinkValidator
{
    internal class LocalLinkValidator : ILinkValidator
    {
        private readonly string _baseDirectory;

        // https://github.com/dotnet/docfx/blob/64fa7e4ed1c1f416e672f2aee2307fd98a21383e/src/Microsoft.DocAsCode.Dfm/Rules/DfmIncludeBlockRule.cs#L44
        // private static readonly Regex s_incRegex = new Regex(@"^\[!INCLUDE\+?\s*\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([^)]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)\]\s*(\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));

        // https://github.com/dotnet/docfx/blob/1f9cbcea04556f5bfd1cfdeae8d17e48545553de/src/Microsoft.DocAsCode.Dfm/Rules/DfmIncludeInlineRule.cs#L14
        private static readonly Regex s_inlineIncludeRegex = new(@"^\[!INCLUDE\s*\-?\s*\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([^)]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline, TimeSpan.FromSeconds(10));


        public LocalLinkValidator(string baseDirectory) => _baseDirectory = baseDirectory;

        public bool IsValid(string link, string filePath)
        {
            // Consider [text]() as valid. It redirects to the current directory.
            if (string.IsNullOrEmpty(link))
            {
                return true;
            }

            if (link.StartsWith('#'))
            {
                return IsHeadingValid(filePath, link[1..]);
            }

            link = link.Replace("%20", " ", StringComparison.Ordinal);
            link = AdjustLinkPath(link, _baseDirectory);

            string? headingIdWithoutHash = null;
            int lastIndex = link.LastIndexOf('#');
            if (lastIndex != -1)
            {
                // TODO: Add a warning if headingIdWithoutHash is empty here?
                // e.g: [Text](file.md#)
                headingIdWithoutHash = link[(lastIndex + 1)..];
                link = link.Substring(0, lastIndex);
            }

            // Remove query parameters.
            lastIndex = link.LastIndexOf('?');
            if (lastIndex != -1)
            {
                link = link.Substring(0, lastIndex);
            }

            if (headingIdWithoutHash is null)
            {
                return File.Exists(link) || Directory.Exists(link);
            }
            else
            {
                return File.Exists(link) && IsHeadingValid(link, headingIdWithoutHash);
            }
        }

        private static string AdjustLinkPath(string link, string baseDirectory)
        {
            string relativeTo = baseDirectory;
            if (link.StartsWith('/') || link.StartsWith("~/", StringComparison.Ordinal))
            {
                // The leading slash doesn't matter whether it exists or not.
                // Only the ~ is problematic and we want to remove it.
                link = link[1..];
                // Links that start with / are relative to the repository root.
                // TODO: Does it work locally (e.g. in VS Code)? Consider a warning for it.
                relativeTo = Directory.GetCurrentDirectory();
            }

            return Path.GetFullPath(Path.Join(relativeTo, link));
        }

        private static bool IsHeadingValid(string path, string headingIdWithoutHash)
        {
            if (headingIdWithoutHash.StartsWith("tab/", StringComparison.Ordinal))
            {
                // MSDocs-specific syntax. This syntax works only in an H1.
                // e.g: `# [Visual Studio 15.6 and earlier](#tab/vs156)`
                // But I'm returning true for everything.
                return true;
            }

            if (!path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/includes/", StringComparison.OrdinalIgnoreCase))
            {
                // includes aren't guaranteed to be valid. For now, ignore them for simplicity.
                return true;
            }

            // TODO: PERF: Optimize reading files from disk and parsing markdown. These should be cached.
            string fileContents = File.ReadAllText(path);

            // Files that may contain the heading we're looking for.
            // TODO: Revisist suppression.
            IEnumerable<string> potentialFiles = new[] { path }.Concat(GetIncludes(fileContents, Path.GetDirectoryName(path)!));
            foreach (string potentialFile in potentialFiles)
            {
                if (!File.Exists(potentialFile))
                {
                    continue;
                }

                MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAutoIdentifiers(AutoIdentifierOptions.GitHub).Build(); // TODO: Is AutoIdentifierOptions.GitHub the correct value to use?
                MarkdownDocument document = Markdown.Parse(File.ReadAllText(potentialFile), pipeline);
                if (document.Descendants<HeadingBlock>().Any(heading => headingIdWithoutHash == heading.GetAttributes().Id) ||
                    document.Descendants<HtmlInline>().Any(html => IsValidHtml(html.Tag, headingIdWithoutHash)) ||
                    document.Descendants<HtmlBlock>().Any(block => block.Lines.Lines.Any(line => IsValidHtml(line.Slice.ToString(), headingIdWithoutHash))))
                {
                    return true;
                }
            }

            return false;

            // Hacky approach!
            static bool IsValidHtml(string tag, string headingIdWithoutHawsh)
            {
                return Regex.Match(tag, @"^<(a|span)\s+?(name|id)\s*?=\s*?""(.+?)""").Groups[3].Value == headingIdWithoutHawsh;
            }
        }

        private static IEnumerable<string> GetIncludes(string fileContents, string baseDirectory)
        {
            return s_inlineIncludeRegex.Matches(fileContents).Select(m => AdjustLinkPath(m.Groups[2].Value, baseDirectory));
        }
    }
}
