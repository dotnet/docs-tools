using System.Text.Json.Serialization;
using Microsoft.Extensions.FileSystemGlobbing;

namespace RedirectionVerifier;

public sealed record DocfxConfiguration(
    [property: JsonPropertyName("build")] DocfxBuild? Build)
{
    private IEnumerable<Matcher>? _matchers;
    
    private IEnumerable<DocfxContent> GetDocfxContents()
    {
        if (Build is null)
        {
            throw new InvalidOperationException("A root object 'build' was expected to exist in 'docfx.json'.");
        }

        if (Build.Contents is null)
        {
            throw new InvalidOperationException("An object array 'contents' was expected to exist under 'build' in 'docfx.json'.");
        }

        // TODO: repo name, or do the hard work of reading open publishing build config file, determining **ALL** `docfx.json`, and read them.
        return Build.Contents.Where(content => content.Source is null or "." or "docs");
    }

    public IEnumerable<Matcher> GetMatchers(string? configDirectory = null)
    {
        if (_matchers is null)
        {
            var list = new List<Matcher>();
            IEnumerable<DocfxContent> contents = GetDocfxContents();
            foreach (DocfxContent content in contents)
            {
                var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

                // Adjust the source path based on where docfx.json was found
                string effectiveSource = GetEffectiveSource(content.Source, configDirectory);

                if (content.Files is not null)
                {
                    foreach (string includePattern in content.Files)
                    {
                        matcher.AddInclude($"{effectiveSource}/{includePattern}");
                    }
                }
                else
                {
                    matcher.AddInclude("**");
                }

                if (content.Excludes is not null)
                {
                    foreach (string excludePattern in content.Excludes)
                    {
                        matcher.AddExclude(excludePattern);
                    }
                }

                list.Add(matcher);
            }

            _matchers = list;
        }

        return _matchers;
    }

    private static string GetEffectiveSource(string? source, string? configDirectory)
    {
        // If no config directory (docfx.json at root), use source as-is
        if (string.IsNullOrEmpty(configDirectory))
        {
            return source ?? ".";
        }

        // If source is "." or null, it means the content is relative to where docfx.json is
        // So we need to prepend the config directory
        if (source is null or ".")
        {
            return configDirectory;
        }

        // Otherwise, combine config directory with the specified source
        return $"{configDirectory}/{source}";
    }
}

public sealed record DocfxBuild(
    [property: JsonPropertyName("content")] IList<DocfxContent>? Contents);

public sealed record DocfxContent(
    [property: JsonPropertyName("src")] string? Source,
    [property: JsonPropertyName("files")] IList<string>? Files,
    [property: JsonPropertyName("exclude")] IList<string>? Excludes);
