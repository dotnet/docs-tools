using System.Text.Json;

namespace DocfxVerifier
{
    /// <summary>
    /// Validates file path entries declared 
    /// under build.fileMetadata in a docfx.json file.
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
            ValidateFileMetadataPaths(json.RootElement, repositoryRoot, configurationDirectory, errors);

            foreach (string error in errors)
            {
                await writer.WriteLineAsync($"::error file={configurationPathForLog}::{error}");
            }

            return errors.Count == 0;
        }

        private static void ValidateFileMetadataPaths(
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
                ValidateBuildFileMetadataSection(buildSection, "$.build", repositoryRoot, configurationDirectory, errors);
            }
        }

        private static void ValidateBuildFileMetadataSection(
            JsonElement buildSection,
            string jsonPath,
            string repositoryRoot,
            string configurationDirectory,
            List<string> errors)
        {
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
