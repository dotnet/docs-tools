using System.Text.Json.Serialization;
using System.Text.Json;

namespace LocateProjects
{
/*
Sample snippets.5000.json file
{
    "host": "visualstudio",
    "expectederrors": [
        {
            "file": "samples/snippets/csharp/VS_Snippets_VBCSharp/csprogguideindexedproperties/cs/Program.cs",
            "line": 5,
            "column": 25,
            "error": "CS0234"
        }
    ]
}
*/
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
        public record ExpectedError(
            [property: JsonPropertyName("file")] string File,
            [property: JsonPropertyName("line")] int Line,
            [property: JsonPropertyName("column")] int Column,
            [property: JsonPropertyName("error")] string Error);
    }
}
