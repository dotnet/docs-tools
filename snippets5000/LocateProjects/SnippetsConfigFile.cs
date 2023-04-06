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
        public ExpectedError[]? ExpectedErrors { get; set; }

        public string RunOutput { get; set; } = string.Empty;

        public string? RunTargetFile { get; set; }

        public int RunExitCode { get; set; }

        public static SnippetsConfigFile Load(string file)
        {
            JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true, Converters = { new JsonStringEnumConverter() }, ReadCommentHandling = JsonCommentHandling.Skip };

            return JsonSerializer.Deserialize<SnippetsConfigFile>(System.IO.File.ReadAllText(file), options)!;
        }

        public class ExpectedError
        {
            [JsonPropertyName("file")]
            public string? File { get; set; }

            [JsonPropertyName("line")]
            public int Line { get; set; }
            
            [JsonPropertyName("column")]
            public int Column { get; set; }
            
            [JsonPropertyName("error")]
            public string? Error { get; set; }
        }
    }
}
