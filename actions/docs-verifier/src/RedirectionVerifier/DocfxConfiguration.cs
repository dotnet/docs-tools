using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileSystemGlobbing;

namespace RedirectionVerifier
{
#pragma warning disable CA1812 // Avoid uninstantiated internal classes → JSON
    internal sealed record DocfxConfiguration(
        [property: JsonPropertyName("build")] DocfxBuild? Build)
    {
        private ImmutableArray<Matcher>? _matchers;

        /// <summary>
        /// Get matchers for the contents specified in docfx.json
        /// </summary>
        /// <param name="basePath">The base directory for matcher. This is the directory where docfx.json exists.</param>
        public ImmutableArray<Matcher> GetMatchers(string basePath)
        {
            if (_matchers is not null)
            {
                return _matchers.Value;
            }

            if (Build is null)
            {
                throw new InvalidOperationException("Docfx.json configuration didn't contain a root 'build' object.");
            }

            if (Build.Contents is null)
            {
                throw new InvalidOperationException("Docfx.json configuration didn't contain a root 'contents' array under 'build'.");
            }

            if (Build.Contents.Length == 0)
            {
                throw new InvalidOperationException("Docfx.json configuration contains an empty 'contents' array under 'build'.");
            }

            ImmutableArray<Matcher>.Builder builder = ImmutableArray.CreateBuilder<Matcher>();
            basePath = basePath.StartsWith("./", StringComparison.Ordinal) ? basePath[2..] : throw new InvalidOperationException($"Expected {nameof(basePath)} to start in ./");

            foreach (DocfxContent content in Build.Contents)
            {
                Console.WriteLine("Adding matcher:");
                var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                // TODO: I'm not sure what "Exludes" and "Files" are relative to?
                // Are these relative to "src"? Are these relative to the directory containing docfx.json?
                // Need to confirm before merge.
                if (content.Excludes is not null)
                {
                    foreach (string exclude in content.Excludes)
                    {
                        Console.WriteLine($"Excluding: {basePath}{exclude}");
                        matcher.AddExclude($"{basePath}{exclude}");
                    }
                }

                if (content.Files is not null)
                {
                    foreach (string file in content.Files)
                    {
                        Console.WriteLine($"Including: {basePath}{file}");
                        matcher.AddInclude($"{basePath}{file}");
                    }
                }
                else
                {
                    string source = content.Source ?? "**";
                    Console.WriteLine($"Including: {basePath}{source}");
                    matcher.AddInclude($"{basePath}{source}");
                    builder.Add(matcher);
                }
            }

            _matchers = builder.ToImmutableArray();
            return _matchers.Value;
        }
    }

    internal sealed record DocfxBuild(
        [property: JsonPropertyName("content")] DocfxContent[]? Contents);

    internal sealed record DocfxContent(
        [property: JsonPropertyName("src")] string? Source,
        [property: JsonPropertyName("files")] string[]? Files,
        [property: JsonPropertyName("exclude")] string[]? Excludes);
}
