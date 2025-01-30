# CleanRepo

This command-line tool helps you clean up a DocFx-based content repo. It can:

- Find and delete markdown files that aren't linked from a TOC file.
- Find and delete orphaned image (.png, .jpg, .gif, .svg) files.
- Map images to the files that reference them and return text from those images.
- Find and delete orphaned "shared" markdown files (includes).
- Find and delete orphaned snippet (.cs, .vb, .cpp, .fs, and .xaml) files.
- Find and replace links to redirected files.
- Remove daisy chains (or hops) within the redirection files for the docset.
- Replace site-relative links with file-relative links (includes image links).
- Filter image list based on strings found in images.
- Compare `ms.date` metadata and recent commit data.

## Usage

If you're running the app locally, it's easiest to set the configuration settings in the `appsettings.json` file.
You can also set some or all of the configuration settings in the command line, for example, `CleanRepo.exe /Options:Function FindOrphanedArticles /Options:DocFxDirectory c:\path\to\docs\repo /Options:TargetDirectory c:\path\to\docs\repo\subfolder /Options:UrlBasePath /dotnet`.

The available functions are described in the following table.

| Function | Description |
| - | - |
| FindOrphanedArticles | Find orphaned .md articles. |
| FindOrphanedImages | Find orphaned .png, .gif, .svg, or .jpg files.<br/>**Note:** The tool does not know if images are referenced in a code sample project, so we recommend not running this option on a directory that contains samples. |
| FindOrphanedSnippets | Find orphaned .cs, .vb, .cpp, .fs, and .xaml files. |
| FindOrphanedIncludes | Find orphaned INCLUDE files. |
| CatalogImages | Map images to the markdown/YAML files that reference them. This option generates a JSON file with the output. |
| ReplaceRedirectTargets | Find backlinks to redirected files and replace with new target. |
| RemoveRedirectHops | Remove daisy chains within the redirection files for the docset. |
| ReplaceWithRelativeLinks | Replace site-relative links with file-relative links. |
| CatalogImagesWithText | Map images to the markdown/YAML files that reference them, with all text found in images. The output file is prefixed with `OcrImageFiles-`. |
| FilterImagesForText | Filter images for text. The output file is prefixed with `FilteredOcrImageFiles-`. |
| AuditMSDate | Compare `ms.date` metadata to most recent commits. This can take a long time on a full repo. It also requires a [GitHub PAT](https://github.com/settings/tokens) with read privileges for the repository you want to check. Store this PAT in an environment variable named `GITHUB_KEY`.

## Image to text examples

The text-to-image functionality supported in the `CatalogImagesWithText` and `FilterImagesForText` options is 
provided by the [Tesseract](https://www.nuget.org/packages/tesseract/) NuGet package.
You must determine which Tesseract models you want to use and install them on your system.
Tesseract models are generated per operating system. Tesseract models come in a variety of sizes. 
You also need to download the language data files for Tesseract 4.0.0 or later 
from [tesseract-tessdata](https://github.com/tesseract-ocr/tessdata/).


