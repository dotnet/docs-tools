using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedirectionVerifier
{
    public static class OpenPublishingConfigurationReader
    {
        private static readonly JsonSerializerOptions s_options = new() { AllowTrailingCommas = true };
        private static OpenPublishingConfiguration? s_cachedOpenPublishingConfiguration;
        private const string OpenPublishingConfigFileName = ".openpublishing.publish.config.json";

        /// <summary>
        /// Retrieves the docsets from <c>.openpublishing.publish.config.json</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Failed to read <c>.openpublishing.publish.config.json</exception>
        private static ImmutableArray<DocsetToPublish> GetDocsets()
        {
            // If there are cached redirections that were previously read and parsed, use 'em.
            if (s_cachedOpenPublishingConfiguration is not null)
            {
                return s_cachedOpenPublishingConfiguration.DocsetsToPublish;
            }

            if (!File.Exists(OpenPublishingConfigFileName))
            {
                throw new InvalidOperationException($"File '{OpenPublishingConfigFileName}' was not found.");
            }

            string json = File.ReadAllText(OpenPublishingConfigFileName);
            OpenPublishingConfiguration? config = JsonSerializer.Deserialize<OpenPublishingConfiguration>(json, s_options);
            if (config is null)
            {
                throw new InvalidOperationException($"Failed to read '{OpenPublishingConfigFileName}'.");
            }

            s_cachedOpenPublishingConfiguration = config;
            return s_cachedOpenPublishingConfiguration.DocsetsToPublish;
        }

        public static IEnumerable<DocfxConfigurationReader> GetDocfxConfigurations()
        {
            foreach (DocsetToPublish docset in GetDocsets())
            {
                string docfxPath = Path.GetFullPath(Path.Join(Directory.GetCurrentDirectory(), docset.BuildSourceFolder, "docfx.json"));
                yield return new DocfxConfigurationReader(docfxPath);
            }
        }
    }

    public record OpenPublishingConfiguration(
        [property: JsonPropertyName("docsets_to_publish")] ImmutableArray<DocsetToPublish> DocsetsToPublish);

    public record DocsetToPublish(
        [property: JsonPropertyName("docset_name")] string DocsetName,
        [property: JsonPropertyName("build_source_folder")] string BuildSourceFolder,
        [property: JsonPropertyName("build_output_subfolder")] string BuildOutputSubfolder);
}
