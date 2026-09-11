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

            List<int> fileMetadataPathLineNumbers = await GetFileMetadataPathLineNumbersAsync(configurationPath);

            // Validate file metadata paths and collect errors.
            var errors = new List<ValidationError>();
            ValidateFileMetadataPaths(
                json.RootElement,
                repositoryRoot,
                configurationDirectory,
                externalContentSourceDirectories,
                fileMetadataPathLineNumbers,
                errors);

            foreach (ValidationError error in errors)
            {
                await WriteErrorAsync(writer, configurationPathForLog, error.LineNumber, $"Invalid path '{error.Path}'.");
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
            List<int> fileMetadataPathLineNumbers,
            List<ValidationError> errors)
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
                    repositoryRoot,
                    configurationDirectory,
                    externalContentSourceDirectories,
                    fileMetadataPathLineNumbers,
                    errors);
            }
        }

        private static void ValidateBuildFileMetadataSection(
            JsonElement buildSection,
            string repositoryRoot,
            string configurationDirectory,
            HashSet<string> externalContentSourceDirectories,
            List<int> fileMetadataPathLineNumbers,
            List<ValidationError> errors)
        {
            int pathEntryIndex = 0;

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
                        int? lineNumber = pathEntryIndex < fileMetadataPathLineNumbers.Count
                            ? fileMetadataPathLineNumbers[pathEntryIndex]
                            : null;
                        pathEntryIndex++;

                        ValidatePath(
                            pathProperty.Name,
                            repositoryRoot,
                            configurationDirectory,
                            externalContentSourceDirectories,
                            lineNumber,
                            errors);
                    }
                }
            }
        }

        /// <summary>
        /// Validates a single file path entry in the docfx.json file.
        /// </summary>
        /// <param name="path">The file path to validate.</param>
        /// <param name="repositoryRoot">The root directory of the repository.</param>
        /// <param name="resolutionBaseDirectory">The base directory for resolving relative paths.</param>
        /// <param name="externalContentSourceDirectories">A set of directories containing external content sources.</param>
        /// <param name="lineNumber">The line number for the path entry in docfx.json, if known.</param>
        /// <param name="errors">The list to which validation errors are added.</param>
        private static void ValidatePath(
            string? path,
            string repositoryRoot,
            string resolutionBaseDirectory,
            HashSet<string> externalContentSourceDirectories,
            int? lineNumber,
            List<ValidationError> errors)
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

            string nonWildcardPrefix = GetNonWildcardPrefix(scopePath, out bool hasWildcard);
            if (string.IsNullOrEmpty(nonWildcardPrefix))
            {
                return;
            }

            if (!ExistsInRepository(nonWildcardPrefix, repositoryRoot, resolutionBaseDirectory))
            {
                if (IsPathUnderExternalContentSource(
                    nonWildcardPrefix,
                    externalContentSourceDirectories,
                    hasWildcard))
                {
                    return;
                }

                errors.Add(new ValidationError(lineNumber, path));
            }
        }

        private static async Task<List<int>> GetFileMetadataPathLineNumbersAsync(string configurationPath)
        {
            byte[] content = await File.ReadAllBytesAsync(configurationPath);
            if (content.Length >= 3
                 && content[0] == 0xEF
                 && content[1] == 0xBB
                 && content[2] == 0xBF)
             {
                 content = content[3..];
             }
            var lineStarts = new List<int> { 0 };
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == (byte)'\n')
                {
                    lineStarts.Add(i + 1);
                }
            }

            int GetLineNumber(long tokenStartIndex)
            {
                int index = (int)tokenStartIndex;
                int lineStartIndex = lineStarts.BinarySearch(index);
                if (lineStartIndex < 0)
                {
                    lineStartIndex = ~lineStartIndex - 1;
                }

                return lineStartIndex + 1;
            }

            var lineNumbers = new List<int>();
            var reader = new Utf8JsonReader(content, new JsonReaderOptions { AllowTrailingCommas = true });
            var containerPath = new List<string>();
            string? currentPropertyName = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString() ?? string.Empty;

                    if (containerPath.Count >= 3
                        && containerPath[^2] == "fileMetadata"
                        && containerPath[^3] == "build")
                    {
                        lineNumbers.Add(GetLineNumber(reader.TokenStartIndex));
                    }

                    currentPropertyName = propertyName;
                }
                else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    containerPath.Add(currentPropertyName ?? string.Empty);
                    currentPropertyName = null;
                }
                else if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
                {
                    if (containerPath.Count > 0)
                    {
                        containerPath.RemoveAt(containerPath.Count - 1);
                    }

                    currentPropertyName = null;
                }
            }

            return lineNumbers;
        }

        private static Task WriteErrorAsync(TextWriter writer, string filePath, int? lineNumber, string message)
        {
            string annotationFilePath = GetAnnotationFilePath(filePath);
            string escapedMessage = EscapeCommandData(message);
            return lineNumber.HasValue
                ? writer.WriteLineAsync($"::error file={annotationFilePath},line={lineNumber.Value}::{escapedMessage}")
                : writer.WriteLineAsync($"::error file={annotationFilePath}::{escapedMessage}");
        }

        private static string GetAnnotationFilePath(string filePath)
        {
            string normalizedPath = NormalizePath(filePath);
            if (!Path.IsPathRooted(filePath))
            {
                return EscapeCommandProperty(normalizedPath);
            }

            string repositoryRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
            string fullPath = Path.GetFullPath(filePath);
            string relativePath = NormalizePath(Path.GetRelativePath(repositoryRoot, fullPath));
            bool isUnderRepository = !relativePath.Equals("..", StringComparison.Ordinal)
                && !relativePath.StartsWith("../", StringComparison.Ordinal);
            string pathForAnnotation = isUnderRepository ? relativePath : normalizedPath;
            return EscapeCommandProperty(pathForAnnotation);
        }

        private static string EscapeCommandProperty(string value)
            => value
                .Replace("%", "%25", StringComparison.Ordinal)
                .Replace("\r", "%0D", StringComparison.Ordinal)
                .Replace("\n", "%0A", StringComparison.Ordinal)
                .Replace(":", "%3A", StringComparison.Ordinal)
                .Replace(",", "%2C", StringComparison.Ordinal);

        private static string EscapeCommandData(string value)
            => value
                .Replace("%", "%25", StringComparison.Ordinal)
                .Replace("\r", "%0D", StringComparison.Ordinal)
                .Replace("\n", "%0A", StringComparison.Ordinal);

        private readonly record struct ValidationError(int? LineNumber, string Path);

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
                string sourceScopePath = normalizedSourcePath.StartsWith("./", StringComparison.Ordinal)
                    ? normalizedSourcePath[2..]
                    : normalizedSourcePath;
                string? resolvedPath = TryResolvePathWithinRepository(sourceScopePath, repositoryRoot, configurationDirectory);
                if (resolvedPath is null || (!Directory.Exists(resolvedPath) && !File.Exists(resolvedPath)))
                {
                    result.Add(sourceScopePath.TrimEnd('/'));
                }
            }

            return result;
        }

        private static bool IsPathUnderExternalContentSource(
            string pathPrefix,
            HashSet<string> externalContentSourceDirectories,
            bool allowAncestorMatch)
        {
            foreach (string sourceDirectory in externalContentSourceDirectories)
            {
                if (pathPrefix.Equals(sourceDirectory, StringComparison.Ordinal)
                    || pathPrefix.StartsWith(sourceDirectory + "/", StringComparison.Ordinal)
                    || (allowAncestorMatch && sourceDirectory.StartsWith(pathPrefix + "/", StringComparison.Ordinal)))
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

        private static string GetNonWildcardPrefix(string path, out bool hasWildcard)
        {
            ReadOnlySpan<char> wildcardChars = ['*', '?', '[', ']', '{', '}'];
            string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            hasWildcard = false;

            var prefixSegments = new List<string>();
            foreach (string segment in segments)
            {
                if (segment.AsSpan().IndexOfAny(wildcardChars) >= 0)
                {
                    hasWildcard = true;
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
