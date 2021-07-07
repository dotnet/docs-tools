using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.FileSystemGlobbing;

namespace RedirectionVerifier
{
    public static class DocfxConfigurationReader
    {
        private static readonly JsonSerializerOptions s_options = new() { AllowTrailingCommas = true };
        private static DocfxConfiguration? s_cachedDocfxConfiguration;
        private static bool? s_fileExists;
        private static Matcher s_matchAllMatcher = new Matcher().AddInclude("**");
        private const string DocfxConfigurationFileName = "docfx.json";

        /// <summary>
        /// Retrieves the path patterns excluded from publishing, which don't require a redirection when deleted/moved/renamed.
        /// </summary>
        /// <exception cref="InvalidOperationException">Failed to read <c>docfx.json</c>.</exception>
        public static Matcher GetMatcher()
        {
            // Only check if the file exists one time.
            if (s_fileExists is null)
            {
                s_fileExists = File.Exists(DocfxConfigurationFileName);
            }

            // If there are cached configuration values for "docfx", use 'em.
            if (s_cachedDocfxConfiguration is not null)
            {
                return s_cachedDocfxConfiguration.GetMatcher();
            }

            if (s_fileExists.Value)
            {
                string json = File.ReadAllText(DocfxConfigurationFileName);
                DocfxConfiguration? configuration = JsonSerializer.Deserialize<DocfxConfiguration>(json, s_options);
                if (configuration is null)
                {
                    throw new InvalidOperationException($"Failed to read '{DocfxConfigurationFileName}'.");
                }

                s_cachedDocfxConfiguration = configuration;
            }

            return s_cachedDocfxConfiguration?.GetMatcher() ?? s_matchAllMatcher;
        }
    }
}
