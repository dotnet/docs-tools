using System.Text.Json;

namespace DocfxVerifier
{
    /// <summary>
    /// Validates file path entries declared in a docfx.json file.
    /// </summary>
    public static class PathVerifier
    {
        private static readonly HashSet<string> s_pathStringPropertyNames =
        [
            "src",
            "dest"
        ];

        private static readonly HashSet<string> s_pathArrayPropertyNames =
        [
            "files",
            "exclude",
            "xref",
            "template",
            "resource",
            "overwrite",
            "globalMetadataFiles",
            "fileMetadataFiles"
        ];

        private static readonly HashSet<string> s_pathObjectKeyPropertyNames =
        [
            "fileMetadata",
            "open_to_public_contributors",
            "ms.collection",
            "ms.custom",
            "ms.update-cycle",
            "no-loc"
        ];

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
            using JsonDocument json = await JsonDocument.ParseAsync(stream);

            string repositoryRoot = Directory.GetCurrentDirectory();
            string configurationDirectory = Path.GetDirectoryName(Path.GetFullPath(configurationPath)) ?? repositoryRoot;
            string configurationPathForLog = configurationPath.Replace('\\', '/');

            var errors = new List<string>();
            ValidateElement(json.RootElement, null, "$", repositoryRoot, configurationDirectory, errors);

            foreach (string error in errors)
            {
                await writer.WriteLineAsync($"::error file={configurationPathForLog}::{error}");
            }

            return errors.Count == 0;
        }

        private static void ValidateElement(
            JsonElement element,
            string? propertyName,
            string jsonPath,
            string repositoryRoot,
            string configurationDirectory,
            List<string> errors)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    string childPath = $"{jsonPath}.{property.Name}";
                    ValidateElement(property.Value, property.Name, childPath, repositoryRoot, configurationDirectory, errors);
                }

                if (string.Equals(propertyName, "fileMetadata", StringComparison.Ordinal))
                {
                    foreach (JsonProperty metadataProperty in element.EnumerateObject())
                    {
                        if (metadataProperty.Value.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        foreach (JsonProperty pathProperty in metadataProperty.Value.EnumerateObject())
                        {
                            ValidatePath(
                                pathProperty.Name,
                                $"{jsonPath}.{metadataProperty.Name}.{pathProperty.Name}",
                                repositoryRoot,
                                configurationDirectory,
                                errors);
                        }
                    }
                }
                else if (propertyName is not null && s_pathObjectKeyPropertyNames.Contains(propertyName))
                {
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        ValidatePath(property.Name, $"{jsonPath}.{property.Name}", repositoryRoot, configurationDirectory, errors);
                    }
                }

                return;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                if (propertyName is not null && s_pathArrayPropertyNames.Contains(propertyName))
                {
                    int index = 0;
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            ValidatePath(item.GetString(), $"{jsonPath}[{index}]", repositoryRoot, configurationDirectory, errors);
                        }

                        index++;
                    }
                }
                else
                {
                    int index = 0;
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        ValidateElement(item, propertyName, $"{jsonPath}[{index}]", repositoryRoot, configurationDirectory, errors);
                        index++;
                    }
                }

                return;
            }

            if (element.ValueKind == JsonValueKind.String
                && propertyName is not null
                && s_pathStringPropertyNames.Contains(propertyName))
            {
                ValidatePath(element.GetString(), jsonPath, repositoryRoot, configurationDirectory, errors);
            }
        }

        private static void ValidatePath(
            string? path,
            string jsonPath,
            string repositoryRoot,
            string configurationDirectory,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(path) || path is ".")
            {
                return;
            }

            if (Uri.TryCreate(path, UriKind.Absolute, out _))
            {
                return;
            }

            string normalizedPath = path.Replace('\\', '/');
            string nonWildcardPrefix = GetNonWildcardPrefix(normalizedPath);
            if (string.IsNullOrEmpty(nonWildcardPrefix))
            {
                return;
            }

            bool existsRelativeToRoot = ExistsInRepository(repositoryRoot, nonWildcardPrefix);
            bool existsRelativeToConfig = ExistsInRepository(configurationDirectory, nonWildcardPrefix);
            if (!existsRelativeToRoot && !existsRelativeToConfig)
            {
                errors.Add($"{jsonPath}: Path '{path}' is invalid. Checked '{nonWildcardPrefix}' relative to repository root and config directory.");
            }
        }

        private static bool ExistsInRepository(string baseDirectory, string path)
        {
            string combinedPath = Path.GetFullPath(Path.Combine(baseDirectory, path));
            return File.Exists(combinedPath) || Directory.Exists(combinedPath);
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
