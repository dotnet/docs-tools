using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.FileSystemGlobbing;

namespace RedirectionVerifier
{
    public sealed class DocfxConfigurationReader
    {
        private static readonly JsonSerializerOptions s_options = new() { AllowTrailingCommas = true };
        private DocfxConfiguration? cachedDocfxConfiguration;
        private readonly string _docfxConfigurationFileName;

        public DocfxConfigurationReader(string docfxConfigurationFileName)
        {
            _docfxConfigurationFileName = docfxConfigurationFileName;
        }

        /// <summary>
        /// Retrieves the path patterns excluded from publishing, which don't require a redirection when deleted/moved.
        /// </summary>
        /// <exception cref="InvalidOperationException">Failed to read <c>docfx.json</c>.</exception>
        public ImmutableArray<Matcher> GetMatchers()
        {
            string docfxPathRelativeToWorkspace = Path.GetRelativePath(relativeTo: Directory.GetCurrentDirectory(), _docfxConfigurationFileName);
            // If there are cached configuration values for "docfx", use 'em.
            if (cachedDocfxConfiguration is not null)
            {
                return cachedDocfxConfiguration.GetMatchers(docfxPathRelativeToWorkspace);
            }

            // File should be existing. We first read open publishing configuration file, taking `build_source_folder`s, which should contain docfx.json.
            string json = File.ReadAllText(_docfxConfigurationFileName);
            DocfxConfiguration? configuration = JsonSerializer.Deserialize<DocfxConfiguration>(json, s_options);
            if (configuration is null)
            {
                throw new InvalidOperationException($"Failed to read '{_docfxConfigurationFileName}'.");
            }

            cachedDocfxConfiguration = configuration;
            return cachedDocfxConfiguration.GetMatchers(docfxPathRelativeToWorkspace);
        }
    }
}
