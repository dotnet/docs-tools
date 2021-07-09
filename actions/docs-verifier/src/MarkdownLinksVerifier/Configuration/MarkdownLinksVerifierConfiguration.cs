using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;

namespace MarkdownLinksVerifier.Configuration
{
    public record MarkdownLinksVerifierConfiguration(
        [property: JsonPropertyName("excludeStartingWith")] ImmutableArray<string> ExcludeStartingWith)
    {
        public bool IsLinkExcluded(string link)
            => !ExcludeStartingWith.IsDefaultOrEmpty
            && ExcludeStartingWith.Any(excludedPrefix => link.StartsWith(excludedPrefix, StringComparison.Ordinal));
    }
}
