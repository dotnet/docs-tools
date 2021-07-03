using System;
using System.Collections.Generic;
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

        private IEnumerable<string> GetExcludes()
            // This method retrieves all the "exclude" arrays under each of the "content"s.
            // This can have "false negatives", since we are excluding patterns that can be meant to apply only to cross-reference repositories.
            // For example, https://github.com/dotnet/docs/blob/bc8e38479d9867c7ed4b308be596ee7642422754/docfx.json#L48-L69
            // The exclude patterns in the link above are meant only for dotnet/csharplang repository. But we're taking them into account.
            // If a file in the docs repo happen to match one of these patterns, it won't require a redirection, while it should.
            // TODO: Fix that. One way to fix that is to add a flag to docfx.json for "content"s that are published.
            // Another way is to not require any changes in docfx.json, but try to somehow find which "content" should be taken.
            => Build?.Contents?.SelectMany(s => s.Excludes ?? Array.Empty<string>()) ?? Array.Empty<string>();

        public Matcher GetMatcher()
        {
            if (_matcher is null)
            {
                _matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                foreach (string exclude in GetExcludes())
                {
                    _matcher.AddExclude(exclude);
                }
            }

            return _matcher;
        }
    }

    internal sealed record DocfxBuild(
        [property: JsonPropertyName("content")] DocfxContent[]? Contents);

    internal sealed record DocfxContent(
        [property: JsonPropertyName("exclude")] string[]? Excludes);
}
