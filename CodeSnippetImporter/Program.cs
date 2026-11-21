using System.Text.Json;
using CodeSnippetImporter;

if (args.Contains("--help"))
{
    PrintUsage();
    return 0;
}

bool dryRun = args.Contains("--dry-run");
string[] paths = args.Where(arg => arg != "--dry-run").ToArray();
if (paths.Length is < 3 or > 4 || paths.Any(path => path.StartsWith("--", StringComparison.Ordinal)))
{
    PrintUsage();
    return 2;
}

try
{
    Dictionary<string, string>? sourceMap = paths.Length == 4
        ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(paths[3]))
            ?? throw new InvalidDataException("The source map must be a JSON object.")
        : null;
    ImportResult result = new SnippetImporter().Import(paths[0], paths[1], paths[2], sourceMap, dryRun);
    foreach (string error in result.Errors)
        Console.Error.WriteLine(error);
    Console.WriteLine($"{(dryRun ? "Would import" : "Imported")} {result.ImportedFiles} file(s), " +
        $"{(dryRun ? "would update" : "updated")} {result.UpdatedReferences} reference(s), {result.Errors.Count} error(s).");
    return result.Errors.Count == 0 ? 0 : 1;
}
catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or JsonException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static void PrintUsage() => Console.WriteLine(
    "Usage: CodeSnippetImporter <ecmaxml-directory> <source-checkout> <snippet-directory> [source-map.json] [--dry-run]");
