using System;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileSystemGlobbing;

namespace RedirectionVerifier
{
#pragma warning disable CA1812 // Avoid uninstantiated internal classes → JSON
    internal sealed record DocfxConfiguration(
        [property: JsonPropertyName("build")] DocfxBuild? Build)
    {
        private Matcher? _matcher;

        private DocfxContent? GetDocfxContent()
            // TODO: repo name, or do the hard work of reading open publishing build config file, determining **ALL** `docfx.json`, and read them.
            => Build?.Contents?.FirstOrDefault(content => content.Source is null or "." or "docs");

        public Matcher GetMatcher()
        {
            if (_matcher is null)
            {
                _matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                DocfxContent? content = GetDocfxContent();
                if (content?.Files is not null)
                {
                    foreach (string includePattern in content.Files)
                    {
                        _matcher.AddInclude(includePattern);
                    }
                }
                else
                {
                    _matcher.AddInclude("**");
                }

                if (content?.Excludes is not null)
                {
                    foreach (string excludePattern in content.Excludes)
                    {
                        _matcher.AddExclude(excludePattern);
                    }
                }
            }

            return _matcher;
        }
    }

    internal sealed record DocfxBuild(
        [property: JsonPropertyName("content")] DocfxContent[]? Contents);

    internal sealed record DocfxContent(
        [property: JsonPropertyName("src")] string? Source,
        [property: JsonPropertyName("files")] string[]? Files,
        [property: JsonPropertyName("exclude")] string[]? Excludes);
}
