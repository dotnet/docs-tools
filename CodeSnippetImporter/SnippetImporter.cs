using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace CodeSnippetImporter;

public sealed record ImportResult(int ImportedFiles, int UpdatedReferences, IReadOnlyList<string> Errors);

public sealed class SnippetImporter
{
    private static readonly Regex MarkdownReference = new(
        @"\[!code-[\w-]+\[[^\]\r\n]*\]\((?<target>[^)\r\n]+)\)\]",
        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public ImportResult Import(string documentationDirectory, string sourceDirectory, string snippetDirectory,
        IReadOnlyDictionary<string, string>? sourceMap = null, bool dryRun = false)
    {
        string docsRoot = Path.GetFullPath(documentationDirectory);
        string sourceRoot = Path.GetFullPath(sourceDirectory);
        string outputRoot = Path.GetFullPath(snippetDirectory);
        EnsureSafePath(docsRoot, docsRoot);
        EnsureSafePath(sourceRoot, sourceRoot);
        EnsureSafePath(outputRoot, outputRoot);
        if (!Directory.Exists(docsRoot) || !Directory.Exists(sourceRoot))
            throw new DirectoryNotFoundException("Documentation and source directories must exist.");
        if (IsWithin(sourceRoot, outputRoot) || IsWithin(outputRoot, sourceRoot))
            throw new InvalidDataException("Source and snippet directories must not overlap.");

        var errors = new List<string>();
        var imported = new HashSet<string>(PathComparer);
        int updated = 0;
        var enumeration = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = false
        };
        foreach (string docPath in Directory.EnumerateFiles(docsRoot, "*.xml", enumeration).Order(StringComparer.Ordinal))
        {
            try
            {
                EnsureSafePath(docsRoot, docPath);
                XDocument document;
                using (var reader = XmlReader.Create(docPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
                    document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
                bool changed = false;
                int documentUpdates = 0;
                foreach (XElement docs in document.Descendants("Docs"))
                {
                    foreach (XAttribute reference in docs.Descendants("code").Attributes("source").ToArray())
                    {
                        string replacement = Resolve(reference.Value, docs, reference.Parent?.Attribute("region")?.Value);
                        if (replacement != reference.Value)
                        {
                            reference.Value = replacement;
                            changed = true;
                        }
                    }
                    foreach (XText text in docs.DescendantNodes().OfType<XText>().ToArray())
                    {
                        string replacement = MarkdownReference.Replace(text.Value, match =>
                        {
                            Group target = match.Groups["target"];
                            string resolved = Resolve(target.Value, docs, null);
                            return match.Value[..(target.Index - match.Index)] + resolved +
                                match.Value[(target.Index - match.Index + target.Length)..];
                        });
                        if (replacement != text.Value)
                        {
                            text.Value = replacement;
                            changed = true;
                        }
                    }
                }
                if (changed && !dryRun)
                {
                    using var writer = XmlWriter.Create(docPath, new XmlWriterSettings
                    {
                        OmitXmlDeclaration = document.Declaration == null,
                        Indent = false
                    });
                    document.Save(writer);
                }
                updated += documentUpdates;

                string Resolve(string reference, XElement docs, string? region)
                {
                    try
                    {
                        int fragmentIndex = reference.IndexOf('#');
                        string path = fragmentIndex < 0 ? reference : reference[..fragmentIndex];
                        string fragment = fragmentIndex < 0 ? "" : reference[fragmentIndex..];
                        if (string.IsNullOrWhiteSpace(path) || path.StartsWith('~') ||
                            Path.IsPathRooted(path) || Uri.TryCreate(path, UriKind.Absolute, out _))
                            return reference;
                        path = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                        if (File.Exists(Path.GetFullPath(path, Path.GetDirectoryName(docPath)!)))
                            return reference;

                        string sourceFile = ResolveSourceFile(docs, sourceMap);
                        string original = Path.GetFullPath(sourceFile.Replace('\\', '/'), sourceRoot);
                        EnsureSafePath(sourceRoot, original);
                        if (!File.Exists(original))
                            throw new FileNotFoundException($"Original API source file not found: {sourceFile}");
                        string snippet = Path.GetFullPath(path, Path.GetDirectoryName(original)!);
                        EnsureSafePath(sourceRoot, snippet);
                        if (!File.Exists(snippet))
                            throw new FileNotFoundException($"Snippet not found: {snippet}");
                        string? selector = region ?? (fragment.Length > 1 ? fragment[1..] : null);
                        ValidateSelector(snippet, selector, region == null);

                        string destination = Path.Combine(outputRoot, Path.GetRelativePath(sourceRoot, snippet));
                        EnsureSafePath(outputRoot, destination);
                        if (File.Exists(destination))
                        {
                            if (!File.ReadAllBytes(destination).AsSpan().SequenceEqual(File.ReadAllBytes(snippet)))
                                throw new InvalidDataException($"Destination has different content: {destination}");
                        }
                        else if (!imported.Contains(destination))
                        {
                            if (!dryRun)
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                                File.Copy(snippet, destination, overwrite: false);
                            }
                            imported.Add(destination);
                        }
                        documentUpdates++;
                        return Path.GetRelativePath(Path.GetDirectoryName(docPath)!, destination).Replace('\\', '/') + fragment;
                    }
                    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
                    {
                        errors.Add($"{docPath}: '{reference}': {ex.Message}");
                        return reference;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or XmlException or RegexMatchTimeoutException)
            {
                errors.Add($"{docPath}: {ex.Message}");
            }
        }
        return new ImportResult(imported.Count, updated, errors);
    }

    private static string ResolveSourceFile(XElement docs, IReadOnlyDictionary<string, string>? sourceMap)
    {
        XElement scope = docs.Parent ?? throw new InvalidDataException("Documentation has no API owner.");
        string? docId = scope.Elements().Where(e => e.Name.LocalName is "MemberSignature" or "TypeSignature")
            .FirstOrDefault(e => (string?)e.Attribute("Language") == "DocId")?.Attribute("Value")?.Value;
        if (docId != null && sourceMap != null && sourceMap.TryGetValue(docId, out string? mapped) &&
            !string.IsNullOrWhiteSpace(mapped))
            return mapped;
        throw new InvalidDataException($"No source mapping for {docId ?? scope.Name.LocalName}. Supply a DocId source map.");
    }

    private static void ValidateSelector(string snippet, string? selector, bool allowLineSelection)
    {
        if (string.IsNullOrEmpty(selector))
            return;
        Match lines = Regex.Match(selector, @"^L(?<start>\d+)(?:-L?(?<end>\d+))?$",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        if (allowLineSelection && lines.Success)
        {
            if (!int.TryParse(lines.Groups["start"].Value, out int start) ||
                !int.TryParse(lines.Groups["end"].Success ? lines.Groups["end"].Value : lines.Groups["start"].Value, out int end) ||
                start < 1 || end < start || end > File.ReadLines(snippet).Count())
                throw new InvalidDataException($"Invalid line selector '{selector}' in {snippet}");
        }
        else if (Path.GetExtension(snippet).Equals(".cs", StringComparison.OrdinalIgnoreCase) &&
            !File.ReadLines(snippet).Any(line => Regex.IsMatch(line,
                @"^\s*(?:#region\s+" + Regex.Escape(selector) + @"\s*|//\s*<" + Regex.Escape(selector) + @">\s*)$",
                RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))))
        {
            throw new InvalidDataException($"Region '{selector}' not found in {snippet}");
        }
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static bool IsWithin(string root, string path)
    {
        string relative = Path.GetRelativePath(root, path);
        return !Path.IsPathRooted(relative) && relative != ".." &&
            !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static void EnsureSafePath(string root, string path)
    {
        if (!IsWithin(root, path))
            throw new InvalidDataException($"Path escapes the allowed directory: {path}");
        for (string? current = path; current != null; current = Path.GetDirectoryName(current))
        {
            if (new FileInfo(current).LinkTarget != null ||
                ((File.Exists(current) || Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0))
                throw new InvalidDataException($"Symbolic links are not supported: {current}");
        }
    }
}
