using System.Text.Json.Serialization;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace Snippets5000;

/*
Sample snippets.5000.json file
{
    "host": "visualstudio",
    "expectederrors": [
        {
            "file": "samples/snippets/csharp/VS_Snippets_VBCSharp/csprogguideindexedproperties/cs/Program.cs",
            "line": 5,
            "error": "CS0234"
        }
    ]
}
*/

// TODO: This is a hybrid mix of the snippets.5000.json file and the state of the object as it flows
//       through the snippets testing system. This should be broken out to where the snippets.5000.json
//       object is a property of the state object.

internal class SnippetsConfigFile
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = "dotnet";

    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("expectederrors")]
    public ExpectedError[] ExpectedErrors { get; set; } = Array.Empty<ExpectedError>();

    public string RunOutput { get; set; } = string.Empty;

    public string? RunTargetFile { get; set; }

    public int RunExitCode { get; set; }

    public bool RunConsideredGood { get; set; } = true;

    public bool RunErrorIsStructural { get; set; } = false;

    public List<DetectedError> DetectedBuildErrors { get; set; } = new();

    public static SnippetsConfigFile Load(string file)
    {
        JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true, Converters = { new JsonStringEnumConverter() }, ReadCommentHandling = JsonCommentHandling.Skip };

        return JsonSerializer.Deserialize<SnippetsConfigFile>(System.IO.File.ReadAllText(file), options)!;
    }

    /// <summary>
    /// An expected error as defined in the JSON config file.
    /// </summary>
    /// <param name="File">The file containing the compiler error.</param>
    /// <param name="Line">The line of the compiler error.</param>
    /// <param name="Column">The column of the compiler error.</param>
    /// <param name="Error">The error identifier.</param>
    internal record ExpectedError(
        [property: JsonPropertyName("file")] string File,
        [property: JsonPropertyName("line")] int Line,
        [property: JsonPropertyName("column")] int Column,
        [property: JsonPropertyName("error")] string Error);

    /// <summary>
    /// The error detected from scanning MSBuild outputs.
    /// </summary>
    internal class DetectedError
    {
        /// <summary>
        /// The error code from the build error, such as MSB3202.
        /// </summary>
        public string ErrorCode { get; }
        
        /// <summary>
        /// The line from the build output where the error code was discovered.
        /// </summary>
        public string ErrorLine { get; }

        /// <summary>
        /// When <see langword="true"/>, indicates that this error is known and can be skipped; otherwise <see langword="false"/>.
        /// </summary>
        public bool IsSkipped { get; set; }

        /// <summary>
        /// Creates the object with the specified values.
        /// </summary>
        /// <param name="errorCode">The MSBuild error code.</param>
        /// <param name="errorLine">The output line the error code was parsed from.</param>
        /// <param name="isSkipped">Indicates that the error was known and can be skipped.</param>
        public DetectedError(string errorCode, string errorLine, bool isSkipped)
        {
            ErrorCode = errorCode;
            ErrorLine = errorLine;
            IsSkipped = isSkipped;
        }
    }

    internal class DetectedErrorComparer: IEqualityComparer<DetectedError>
    {
        public bool Equals(DetectedError? x, DetectedError? y)
        {
            if (x is null | y is null) return false;
            if (x is null && y is null) return true;

            return string.Equals(x!.ErrorLine, y!.ErrorLine, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.ErrorCode, y.ErrorCode, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode([DisallowNull] DetectedError obj) =>
            new { obj.ErrorCode, obj.ErrorLine }.GetHashCode();
    }
}
