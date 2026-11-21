# .NET Docs: Tools and Sequester Action

[![CodeQL](https://github.com/dotnet/docs-tools/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/dotnet/docs-tools/actions/workflows/codeql-analysis.yml) [![dotnet build and test](https://github.com/dotnet/docs-tools/actions/workflows/dotnet-build-validation.yml/badge.svg)](https://github.com/dotnet/docs-tools/actions/workflows/dotnet-build-validation.yml) [![quest import](https://github.com/dotnet/docs-tools/actions/workflows/quest.yml/badge.svg)](https://github.com/dotnet/docs-tools/actions/workflows/quest.yml)

This repo contains the sequester GitHub Action and other tools that the .NET docs team uses to maintain quality. Many of the tools are designed to be invoked on DocFx repositories.

The other .NET docs GitHub Actions are maintained in [dotnet/docs-actions](https://github.com/dotnet/docs-actions).

For information about the code of conduct, see [Code of conduct](CODE-OF-CONDUCT.md).

## Code snippet importer (prototype)

`CodeSnippetImporter` runs after ECMAXML generation to copy missing example files
from a local source checkout and update their documentation references. It supports
XML `<code source="..." region="..." />` elements and Markdown
`[!code-csharp[](...#region)]` references, including Markdown in CDATA.

Run it with the .NET 10 SDK:

```shell
dotnet run --project /path/to/docs-tools/CodeSnippetImporter -- /path/to/api/xml /path/to/runtime /path/to/api/snippets /path/to/source-map.json --dry-run
```

Remove `--dry-run` to copy files and update ECMAXML **in place**. Review the
resulting diff before publishing. The importer preserves XML whitespace and CDATA,
but XML serialization can normalize formatting such as attribute quotes.

The source map is a JSON object mapping the `DocId` signature of each API whose
documentation references a snippet to the original source file, relative to the
checkout root. For example:

```json
{
  "T:System.Reflection.Context.CustomReflectionContext": "src/libraries/System.Reflection.Context/src/System/Reflection/Context/CustomReflectionContext.cs",
  "M:System.Text.RegularExpressions.Regex.Match(System.String,System.Int32)": "src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/Regex.Match.cs"
}
```

Member documentation needs its own mapping; the importer does not guess which file
in a partial type contains a member's comment. Use a checkout at the revision used
to generate the API documentation. The prototype does not verify its revision.

Snippet paths are resolved relative to the mapped API source file. Whole files
are copied under the snippet directory using their source-repository-relative
paths, so shared references use one copy. Region attributes and Markdown fragments
are preserved; named C# selectors are checked for a matching `#region` or
`// <name>` start marker. Markdown line selectors such as `#L1-L5` are also validated.
Existing local references, URLs, absolute paths, and `~/` documentation-root
references are left unchanged.

The importer rejects paths escaping the source or output roots, symbolic links,
overlapping source/output directories, XML DTDs, and conflicting destination files.
Identical destination files are reused. It prints counts and diagnostics, returning
`0` on success, `1` for import/input errors, and `2` for invalid command-line syntax.
Successful imports are retained if other references fail; failed references remain
unchanged. Dry runs perform validation without creating directories or changing files.
Rerunning an import leaves already-resolving references unchanged.

Automatic SourceLink mapping, remote downloads, a provenance manifest, project-file
copying, and compilation are deferred. SourceLink integration needs verification
against the metadata produced by the API-generation pipeline; explicit mappings
make the prototype usable without assuming an unverified metadata schema.
