using System.Xml.Linq;
using CodeSnippetImporter;
using Xunit;

namespace DotnetDocsTools.Tests;

public sealed class SnippetImporterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "snippet-importer-tests", Guid.NewGuid().ToString("N"));
    private string Docs => Path.Combine(_root, "docs");
    private string Source => Path.Combine(_root, "source");
    private string Output => Path.Combine(Docs, "snippets");
    private string DocPath => Path.Combine(Docs, "Example.xml");
    private string ImportedPath => Path.Combine(Output, "tests", "Example.cs");
    private static Dictionary<string, string> SourceMap => new() { ["T:Example"] = "src/Example.cs" };

    public SnippetImporterTests()
    {
        Directory.CreateDirectory(Docs);
        Directory.CreateDirectory(Path.Combine(Source, "src"));
        Directory.CreateDirectory(Path.Combine(Source, "tests"));
        File.WriteAllText(Path.Combine(Source, "src", "Example.cs"), "public class Example {}");
        File.WriteAllText(Path.Combine(Source, "tests", "Example.cs"), "#region Sample\nConsole.WriteLine(1);\n#endregion\n");
    }

    [Fact]
    public void ImportsXmlAndMarkdownOnceAndPreservesSelectors()
    {
        WriteDocumentation("""
            <example>
              <code lang="cs" source="../tests/Example.cs" region="Sample" />
              <format type="text/markdown"><![CDATA[[!code-csharp[](../tests/Example.cs#Sample)]]]></format>
            </example>
            """);

        ImportResult result = Run();

        Assert.Empty(result.Errors);
        Assert.Equal(1, result.ImportedFiles);
        Assert.Equal(2, result.UpdatedReferences);
        Assert.Equal(File.ReadAllBytes(Path.Combine(Source, "tests", "Example.cs")), File.ReadAllBytes(ImportedPath));
        XDocument document = XDocument.Load(DocPath);
        XElement code = Assert.Single(document.Descendants("code"));
        Assert.Equal("snippets/tests/Example.cs", code.Attribute("source")!.Value);
        Assert.Equal("Sample", code.Attribute("region")!.Value);
        Assert.Equal("[!code-csharp[](snippets/tests/Example.cs#Sample)]", Assert.Single(document.Descendants("format")).Value);
        Assert.IsType<XCData>(Assert.Single(document.Descendants("format")).FirstNode);

        byte[] before = File.ReadAllBytes(DocPath);
        ImportResult second = Run();
        Assert.Empty(second.Errors);
        Assert.Equal(0, second.ImportedFiles);
        Assert.Equal(0, second.UpdatedReferences);
        Assert.Equal(before, File.ReadAllBytes(DocPath));
    }

    [Fact]
    public void DryRunReportsChangesWithoutWriting()
    {
        WriteDocumentation("""<code source="../tests/Example.cs" />""");
        byte[] before = File.ReadAllBytes(DocPath);

        ImportResult result = Run(dryRun: true);

        Assert.Empty(result.Errors);
        Assert.Equal(1, result.ImportedFiles);
        Assert.Equal(1, result.UpdatedReferences);
        Assert.False(Directory.Exists(Output));
        Assert.Equal(before, File.ReadAllBytes(DocPath));
    }

    [Theory]
    [InlineData("L1")]
    [InlineData("L1-L3")]
    [InlineData("L1-3")]
    public void MarkdownLineSelectorsArePreserved(string selector)
    {
        WriteDocumentation($"<format><![CDATA[[!code-csharp[](../tests/Example.cs#{selector})]]]></format>");

        ImportResult result = Run();

        Assert.Empty(result.Errors);
        Assert.Contains($"snippets/tests/Example.cs#{selector}", File.ReadAllText(DocPath));
    }

    [Theory]
    [InlineData("L0")]
    [InlineData("L3-L1")]
    [InlineData("L1-L100")]
    public void InvalidMarkdownLineSelectorsAreReported(string selector)
    {
        WriteDocumentation($"<format><![CDATA[[!code-csharp[](../tests/Example.cs#{selector})]]]></format>");

        ImportResult result = Run();

        Assert.Contains("Invalid line selector", Assert.Single(result.Errors));
        Assert.Equal(0, result.UpdatedReferences);
    }

    [Fact]
    public void CommentRegionMarkersAreSupported()
    {
        File.WriteAllText(Path.Combine(Source, "tests", "Example.cs"), "// <Sample>\nConsole.WriteLine(1);\n// </Sample>");
        WriteDocumentation("""<code source="../tests/Example.cs" region="Sample" />""");

        Assert.Empty(Run().Errors);
    }

    [Theory]
    [InlineData("../tests/Missing.cs", null, "Snippet not found")]
    [InlineData("../tests/Example.cs", "Missing", "Region 'Missing' not found")]
    [InlineData("../../outside.cs", null, "Path escapes")]
    public void UnresolvedReferenceIsReportedAndUnchanged(string reference, string? region, string diagnostic)
    {
        WriteDocumentation(new XElement("code", new XAttribute("source", reference),
            region == null ? null : new XAttribute("region", region)).ToString());
        byte[] before = File.ReadAllBytes(DocPath);

        ImportResult result = Run();

        Assert.Contains(diagnostic, Assert.Single(result.Errors));
        Assert.Equal(0, result.ImportedFiles);
        Assert.Equal(0, result.UpdatedReferences);
        Assert.Equal(before, File.ReadAllBytes(DocPath));
    }

    [Fact]
    public void MissingMappingDoesNotGuessFromType()
    {
        WriteDocumentation("""<code source="../tests/Example.cs" />""");

        ImportResult result = new SnippetImporter().Import(Docs, Source, Output);

        Assert.Contains("No source mapping", Assert.Single(result.Errors));
        Assert.False(File.Exists(ImportedPath));
    }

    [Fact]
    public void MemberUsesItsOwnMappingForPartialTypes()
    {
        File.WriteAllText(DocPath, """
            <Type>
              <TypeSignature Language="DocId" Value="T:Example" />
              <Docs />
              <Members><Member>
                <MemberSignature Language="DocId" Value="M:Example.Method" />
                <Docs><code source="../tests/Example.cs" /></Docs>
              </Member></Members>
            </Type>
            """);
        var map = new Dictionary<string, string>(SourceMap) { ["M:Example.Method"] = "src/Example.cs" };

        ImportResult result = new SnippetImporter().Import(Docs, Source, Output, map);

        Assert.Empty(result.Errors);
        Assert.Equal(1, result.UpdatedReferences);
    }

    [Fact]
    public void MemberWithoutMappingDoesNotInheritTypeMapping()
    {
        File.WriteAllText(DocPath, """
            <Type>
              <TypeSignature Language="DocId" Value="T:Example" />
              <Members><Member>
                <MemberSignature Language="DocId" Value="M:Example.Method" />
                <Docs><code source="../tests/Example.cs" /></Docs>
              </Member></Members>
            </Type>
            """);

        Assert.Contains("No source mapping for M:Example.Method", Assert.Single(Run().Errors));
    }

    [Fact]
    public void ConflictingDestinationIsNotOverwritten()
    {
        WriteDocumentation("""<code source="../tests/Example.cs" />""");
        Directory.CreateDirectory(Path.GetDirectoryName(ImportedPath)!);
        File.WriteAllText(ImportedPath, "existing");

        ImportResult result = Run();

        Assert.Contains("different content", Assert.Single(result.Errors));
        Assert.Equal("existing", File.ReadAllText(ImportedPath));
        Assert.Equal(0, result.UpdatedReferences);
    }

    [Fact]
    public void MatchingDestinationIsReused()
    {
        WriteDocumentation("""<code source="../tests/Example.cs" />""");
        Directory.CreateDirectory(Path.GetDirectoryName(ImportedPath)!);
        File.Copy(Path.Combine(Source, "tests", "Example.cs"), ImportedPath);

        ImportResult result = Run();

        Assert.Empty(result.Errors);
        Assert.Equal(0, result.ImportedFiles);
        Assert.Equal(1, result.UpdatedReferences);
    }

    [Fact]
    public void SuccessfulReferencesAreImportedWhenAnotherReferenceFails()
    {
        WriteDocumentation("""
            <code source="../tests/Missing.cs" />
            <code source="../tests/Example.cs" />
            """);

        ImportResult result = Run();

        Assert.Single(result.Errors);
        Assert.Equal(1, result.ImportedFiles);
        Assert.Equal(1, result.UpdatedReferences);
        string[] references = XDocument.Load(DocPath).Descendants("code")
            .Select(code => code.Attribute("source")!.Value).ToArray();
        Assert.Equal(["../tests/Missing.cs", "snippets/tests/Example.cs"], references);
    }

    [Fact]
    public void SourceMapCannotEscapeCheckout()
    {
        WriteDocumentation("""<code source="../tests/Example.cs" />""");
        var map = new Dictionary<string, string> { ["T:Example"] = "../outside.cs" };

        ImportResult result = new SnippetImporter().Import(Docs, Source, Output, map);

        Assert.Contains("Path escapes", Assert.Single(result.Errors));
        Assert.Equal(0, result.ImportedFiles);
    }

    [Theory]
    [InlineData("https://example.com/sample.cs")]
    [InlineData("~/snippets/sample.cs")]
    public void ExternalAndDocumentationRootReferencesAreUnchanged(string reference)
    {
        WriteDocumentation(new XElement("code", new XAttribute("source", reference)).ToString());
        byte[] before = File.ReadAllBytes(DocPath);

        ImportResult result = Run();

        Assert.Empty(result.Errors);
        Assert.Equal(0, result.UpdatedReferences);
        Assert.Equal(before, File.ReadAllBytes(DocPath));
    }

    [Fact]
    public void SourceSymlinkCannotEscapeCheckout()
    {
        if (OperatingSystem.IsWindows())
            return;
        WriteDocumentation("""<code source="../tests/Linked.cs" />""");
        string outside = Path.Combine(_root, "outside.cs");
        File.WriteAllText(outside, "not a snippet");
        File.CreateSymbolicLink(Path.Combine(Source, "tests", "Linked.cs"), outside);

        ImportResult result = Run();

        Assert.Contains("Symbolic links", Assert.Single(result.Errors));
        Assert.Equal(0, result.ImportedFiles);
    }

    [Fact]
    public void DestinationSymlinkIsRejected()
    {
        if (OperatingSystem.IsWindows())
            return;
        WriteDocumentation("""<code source="../tests/Example.cs" />""");
        Directory.CreateSymbolicLink(Output, Path.Combine(_root, "not-created"));

        Assert.Throws<InvalidDataException>(() => Run());
    }

    [Fact]
    public void DtdIsRejected()
    {
        File.WriteAllText(DocPath, """<!DOCTYPE Type [<!ENTITY example "text">]><Type><Docs>&example;</Docs></Type>""");

        ImportResult result = Run();

        Assert.Single(result.Errors);
        Assert.Equal(0, result.UpdatedReferences);
    }

    private ImportResult Run(bool dryRun = false) => new SnippetImporter().Import(Docs, Source, Output, SourceMap, dryRun);

    private void WriteDocumentation(string content) => File.WriteAllText(DocPath, $"""
        <Type Name="Example">
          <TypeSignature Language="DocId" Value="T:Example" />
          <Docs>{content}</Docs>
        </Type>
        """);

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
