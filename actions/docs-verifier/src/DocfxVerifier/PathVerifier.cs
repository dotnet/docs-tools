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
        /// Verifies that glob file paths in a specific docfx.json file are valid.
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
            HashSet<string> externalContentSourceDirectories = GetExternalContentSourceDirectories(
                json.RootElement,
                repositoryRoot,
                configurationDirectory);

            var errors = new List<string>();
            ValidateFileMetadataPaths(
                json.RootElement,
                repositoryRoot,
                configurationDirectory,
                externalContentSourceDirectories,
                errors);

            foreach (string error in errors)
            {
                await writer.WriteLineAsync($"::error file={configurationPathForLog}::{error}");
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Validates the file metadata paths in the given JSON element.
        /// </summary>
        /// <param name="element">The JSON element to validate.</param>
        /// <param name="repositoryRoot">The root directory of the repository.</param>
        /// <param name="configurationDirectory">The directory containing the configuration file.</param>
        /// <param name="externalContentSourceDirectories">A set of directories containing external content sources.</param>
        /// <param name="errors">The list to which validation errors are added.</param>
        private static void ValidateFileMetadataPaths(
            JsonElement element,
            string repositoryRoot,
            string configurationDirectory,
            HashSet<string> externalContentSourceDirectories,
            List<string> errors)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if (element.TryGetProperty("build", out JsonElement buildSection)
                && buildSection.ValueKind == JsonValueKind.Object)
            {
                ValidateBuildFileMetadataSection(
                    buildSection,
                    "$.build",
                    repositoryRoot,
                    configurationDirectory,
                    externalContentSourceDirectories,
                    errors);
            }
        }

        private static void ValidateBuildFileMetadataSection(
            JsonElement buildSection,
            string jsonPath,
            string repositoryRoot,
            string configurationDirectory,
            HashSet<string> externalContentSourceDirectories,
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
                            externalContentSourceDirectories,
                            errors);
                    }
                }
            }
        }

        /// <summary>
        /// Validates a single file path entry in the docfx.json file.
        /// </summary>
        /// <param name="path">The file path to validate.</param>
        /// <param name="jsonPath">The JSON path of the file path entry.</param>
        /// <param name="repositoryRoot">The root directory of the repository.</param>
        /// <param name="resolutionBaseDirectory">The base directory for resolving relative paths.</param>
        /// <param name="externalContentSourceDirectories">A set of directories containing external content sources.</param>
        /// <param name="errors">The list to which validation errors are added.</param>
        private static void ValidatePath(
            string? path,
            string jsonPath,
            string repositoryRoot,
            string resolutionBaseDirectory,
            HashSet<string> externalContentSourceDirectories,
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

            string normalizedPath = NormalizePath(path);
            string scopePath = normalizedPath.StartsWith("./", StringComparison.Ordinal)
                ? normalizedPath[2..]
                : normalizedPath;

            string nonWildcardPrefix = GetNonWildcardPrefix(scopePath);
            if (string.IsNullOrEmpty(nonWildcardPrefix))
            {
                return;
            }

            if (!ExistsInRepository(nonWildcardPrefix, repositoryRoot, resolutionBaseDirectory))
            {
                if (IsPathUnderExternalContentSource(nonWildcardPrefix, externalContentSourceDirectories))
                {
                    return;
                }

                errors.Add($"{jsonPath}: Path '{path}' is invalid.");
            }
        }

        private static HashSet<string> GetExternalContentSourceDirectories(
            JsonElement root,
            string repositoryRoot,
            string configurationDirectory)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);

            if (root.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            if (!root.TryGetProperty("build", out JsonElement buildSection)
                || buildSection.ValueKind != JsonValueKind.Object
                || !buildSection.TryGetProperty("content", out JsonElement content)
                || content.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (JsonElement mapping in content.EnumerateArray())
            {
                if (mapping.ValueKind != JsonValueKind.Object
                    || !mapping.TryGetProperty("src", out JsonElement src)
                    || src.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                string? srcPath = src.GetString();
                if (string.IsNullOrWhiteSpace(srcPath) || srcPath == ".")
                {
                    continue;
                }

                string normalizedSourcePath = NormalizePath(srcPath);
                string? resolvedPath = TryResolvePathWithinRepository(normalizedSourcePath, repositoryRoot, configurationDirectory);
                if (resolvedPath is null || (!Directory.Exists(resolvedPath) && !File.Exists(resolvedPath)))
                {
                    result.Add(normalizedSourcePath.TrimEnd('/'));
                }
            }

            return result;
        }

        private static bool IsPathUnderExternalContentSource(string pathPrefix, HashSet<string> externalContentSourceDirectories)
        {
            foreach (string sourceDirectory in externalContentSourceDirectories)
            {
                if (pathPrefix.Equals(sourceDirectory, StringComparison.Ordinal)
                    || pathPrefix.StartsWith(sourceDirectory + "/", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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

        private static string NormalizePath(string path)
            => path.Replace('\\', '/');

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
