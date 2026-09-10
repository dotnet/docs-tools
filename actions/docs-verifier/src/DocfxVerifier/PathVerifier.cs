using System.Text.Json;

namespace DocfxVerifier
{
    /// <summary>
    /// Validates file path entries declared in a docfx.json file.
    /// </summary>
    public static class PathVerifier
    {
        private static readonly JsonDocumentOptions s_jsonDocumentOptions = new()
        {
            AllowTrailingCommas = true
        };

        /// <summary>
        /// Verifies that file paths in the docfx.json file are valid.
        /// </summary>
        public static Task<bool> WriteResultsAsync(TextWriter writer) =>
            WriteResultsAsync(writer, configurationPath: null);

        /// <summary>
        /// Verifies that file paths in a specific docfx.json file are valid.
        /// </summary>
        public static async Task<bool> WriteResultsAsync(TextWriter writer, string? configurationPath)
        {
            ArgumentNullException.ThrowIfNull(writer, nameof(writer));

            configurationPath ??= FindDocfxConfigurationPath();
            if (configurationPath is null)
            {
                await writer.WriteLineAsync("::error::Unable to find docfx.json in the repository root or its immediate subdirectories.");
                return false;
            }

            if (!File.Exists(configurationPath))
            {
                await writer.WriteLineAsync($"::error::docfx.json file '{configurationPath}' does not exist.");
                return false;
            }

            using FileStream stream = File.OpenRead(configurationPath);
            using JsonDocument json = await JsonDocument.ParseAsync(stream, s_jsonDocumentOptions);

            string repositoryRoot = Directory.GetCurrentDirectory();
            string configurationDirectory = Path.GetDirectoryName(Path.GetFullPath(configurationPath)) ?? repositoryRoot;
            string configurationPathForLog = configurationPath.Replace('\\', '/');

            var errors = new List<string>();
            ValidateConfiguration(json.RootElement, repositoryRoot, configurationDirectory, errors);

            foreach (string error in errors)
            {
                await writer.WriteLineAsync($"::error file={configurationPathForLog}::{error}");
            }

            return errors.Count == 0;
        }

        private static void ValidateConfiguration(
                JsonElement element,
                string repositoryRoot,
                string configurationDirectory,
                List<string> errors)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if (element.TryGetProperty("build", out JsonElement buildSection)
                && buildSection.ValueKind == JsonValueKind.Object)
            {
                ValidateBuildSection(buildSection, "$.build", repositoryRoot, configurationDirectory, errors);
            }

            if (element.TryGetProperty("metadata", out JsonElement metadataSection))
            {
                ValidateFileMappingArray(metadataSection, "$.metadata", repositoryRoot, configurationDirectory, configurationDirectory, errors);
            }
        }

        private static void ValidateBuildSection(
            JsonElement buildSection,
            string jsonPath,
            string repositoryRoot,
            string configurationDirectory,
            List<string> errors)
        {
            ValidateStringProperty(buildSection, "dest", $"{jsonPath}.dest", repositoryRoot, configurationDirectory, errors);
            ValidateStringArrayProperty(buildSection, "template", $"{jsonPath}.template", repositoryRoot, configurationDirectory, errors);
            ValidateStringArrayProperty(buildSection, "xref", $"{jsonPath}.xref", repositoryRoot, configurationDirectory, errors);
            ValidateStringArrayProperty(buildSection, "globalMetadataFiles", $"{jsonPath}.globalMetadataFiles", repositoryRoot, configurationDirectory, errors);
            ValidateStringArrayProperty(buildSection, "fileMetadataFiles", $"{jsonPath}.fileMetadataFiles", repositoryRoot, configurationDirectory, errors);

            ValidateFileMappingArrayProperty(buildSection, "content", $"{jsonPath}.content", repositoryRoot, configurationDirectory, errors);
            ValidateFileMappingArrayProperty(buildSection, "resource", $"{jsonPath}.resource", repositoryRoot, configurationDirectory, errors);
            ValidateFileMappingArrayProperty(buildSection, "overwrite", $"{jsonPath}.overwrite", repositoryRoot, configurationDirectory, errors);

            if (buildSection.TryGetProperty("fileMetadata", out JsonElement fileMetadata)
                && fileMetadata.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty metadataProperty in fileMetadata.EnumerateObject())
                {
                    if (metadataProperty.Value.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    foreach (JsonProperty pathProperty in metadataProperty.Value.EnumerateObject())
                    {
                        ValidatePath(
                            pathProperty.Name,
                            $"{jsonPath}.fileMetadata.{metadataProperty.Name}.{pathProperty.Name}",
                            repositoryRoot,
                            configurationDirectory,
                            errors);
                    }
                }
            }
        }

        private static void ValidateStringProperty(
            JsonElement parent,
            string propertyName,
            string jsonPath,
            string repositoryRoot,
            string resolutionBaseDirectory,
            List<string> errors)
        {
            if (parent.TryGetProperty(propertyName, out JsonElement property)
                && property.ValueKind == JsonValueKind.String)
            {
                ValidatePath(property.GetString(), jsonPath, repositoryRoot, resolutionBaseDirectory, errors);
            }
        }

        private static void ValidateStringArrayProperty(
            JsonElement parent,
            string propertyName,
            string jsonPath,
            string repositoryRoot,
            string resolutionBaseDirectory,
            List<string> errors)
        {
            if (parent.TryGetProperty(propertyName, out JsonElement property))
            {
                ValidateStringArray(property, jsonPath, repositoryRoot, resolutionBaseDirectory, errors);
            }
        }

        private static void ValidateFileMappingArrayProperty(
            JsonElement parent,
            string propertyName,
            string jsonPath,
            string repositoryRoot,
            string configurationDirectory,
            List<string> errors)
        {
            if (parent.TryGetProperty(propertyName, out JsonElement property))
            {
                ValidateFileMappingArray(property, jsonPath, repositoryRoot, configurationDirectory, configurationDirectory, errors);
            }
        }

        private static void ValidateFileMappingArray(
            JsonElement mappings,
            string jsonPath,
            string repositoryRoot,
            string configurationDirectory,
            string resolutionBaseDirectory,
            List<string> errors)
        {
            if (mappings.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            int index = 0;
            foreach (JsonElement mapping in mappings.EnumerateArray())
            {
                string mappingPath = $"{jsonPath}[{index}]";
                if (mapping.ValueKind == JsonValueKind.String)
                {
                    ValidatePath(mapping.GetString(), mappingPath, repositoryRoot, resolutionBaseDirectory, errors);
                }
                else if (mapping.ValueKind == JsonValueKind.Object)
                {
                    ValidateFileMappingObject(mapping, mappingPath, repositoryRoot, configurationDirectory, errors);
                }

                index++;
            }
        }

        private static void ValidateFileMappingObject(
            JsonElement mapping,
            string jsonPath,
            string repositoryRoot,
            string configurationDirectory,
            List<string> errors)
        {
            string mappingSourceDirectory = configurationDirectory;

            if (mapping.TryGetProperty("src", out JsonElement src)
                && src.ValueKind == JsonValueKind.String)
            {
                string? srcPath = src.GetString();
                ValidatePath(srcPath, $"{jsonPath}.src", repositoryRoot, configurationDirectory, errors);

                string? effectiveSourceDirectory = TryResolvePathWithinRepository(srcPath, repositoryRoot, configurationDirectory);
                if (effectiveSourceDirectory is not null)
                {
                    mappingSourceDirectory = effectiveSourceDirectory;
                }
            }

            ValidateStringProperty(mapping, "dest", $"{jsonPath}.dest", repositoryRoot, configurationDirectory, errors);

            if (mapping.TryGetProperty("files", out JsonElement files))
            {
                ValidateStringArray(files, $"{jsonPath}.files", repositoryRoot, mappingSourceDirectory, errors);
            }

            if (mapping.TryGetProperty("exclude", out JsonElement exclude))
            {
                ValidateStringArray(exclude, $"{jsonPath}.exclude", repositoryRoot, mappingSourceDirectory, errors);
            }
        }

        private static void ValidateStringArray(
            JsonElement values,
            string jsonPath,
            string repositoryRoot,
            string resolutionBaseDirectory,
            List<string> errors)
        {
            if (values.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            int index = 0;
            foreach (JsonElement item in values.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    ValidatePath(item.GetString(), $"{jsonPath}[{index}]", repositoryRoot, resolutionBaseDirectory, errors);
                }

                index++;
            }
        }

        private static void ValidatePath(
            string? path,
            string jsonPath,
            string repositoryRoot,
        string resolutionBaseDirectory,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(path) || path is ".")
            {
                return;
            }

            if (Uri.TryCreate(path, UriKind.Absolute, out Uri? uri)
                && uri is not null
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return;
            }

            string normalizedPath = path.Replace('\\', '/');
            string nonWildcardPrefix = GetNonWildcardPrefix(normalizedPath);
            if (string.IsNullOrEmpty(nonWildcardPrefix))
            {
                return;
            }

            if (!ExistsInRepository(nonWildcardPrefix, repositoryRoot, resolutionBaseDirectory))
            {
                errors.Add($"{jsonPath}: Path '{path}' is invalid.");
            }
        }

        private static bool ExistsInRepository(string path, string repositoryRoot, string resolutionBaseDirectory)
        {
            string? combinedPath = TryResolvePathWithinRepository(path, repositoryRoot, resolutionBaseDirectory);
            return combinedPath is not null && (File.Exists(combinedPath) || Directory.Exists(combinedPath));
        }

        private static string? TryResolvePathWithinRepository(
            string? path,
            string repositoryRoot,
            string resolutionBaseDirectory)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string combinedPath = Path.GetFullPath(Path.Combine(resolutionBaseDirectory, path));
            string relative = Path.GetRelativePath(Path.GetFullPath(repositoryRoot), combinedPath);

            if (relative.Equals("..", StringComparison.Ordinal)
                || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
            {
                return null;
            }

            return combinedPath;
        }

        private static string GetNonWildcardPrefix(string path)
        {
            ReadOnlySpan<char> wildcardChars = ['*', '?', '[', ']', '{', '}'];
            string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var prefixSegments = new List<string>();
            foreach (string segment in segments)
            {
                if (segment.AsSpan().IndexOfAny(wildcardChars) >= 0)
                {
                    break;
                }

                prefixSegments.Add(segment);
            }

            return string.Join('/', prefixSegments);
        }

        private static string? FindDocfxConfigurationPath()
        {
            const string fileName = "docfx.json";
            if (File.Exists(fileName))
            {
                return fileName;
            }

            foreach (string directory in Directory.GetDirectories(".", "*", SearchOption.TopDirectoryOnly))
            {
                string candidate = Path.Combine(directory, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
