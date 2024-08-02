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

## Usage

| Command | Description |
| - | - |
| --orphaned-articles | Find orphaned .md articles. |
| --orphaned-images | Find orphaned .png, .gif, .svg, or .jpg files.<br/>**Note:** The tool does not know if images are referenced in a code sample project, so we recommend not running this option on a directory that contains samples. |
| --orphaned-snippets | Find orphaned .cs, .vb, .cpp, .fs, and .xaml files. |
| --orphaned-includes | Find orphaned INCLUDE files. |
| --catalog-images | Map images to the markdown/YAML files that reference them. This option generates a JSON file with the output. |
| --replace-redirects | Find backlinks to redirected files and replace with new target. |
| --remove-hops | Remove daisy chains within the redirection files for the docset. |
| --relative-links | Replace site-relative links with file-relative links. |
| --catalog-images-with-text | Map images to the markdown/YAML files that reference them, with all text found in images. Must set --ocr-model-directory path. |
| --filter-images-for-text | Filter images for text. Must set --ocr-model-directory and --filter-text-json-file paths. |
| --ocr-model-directory | Directory that contains the OCR (Tesseract) models for image scanning. |
| --filter-text-json-file | JSON file of array of strings to filter OCR results with. |

## Usage examples

- Find orphaned articles recursively (that is, in the specified directory and any subdirectories):

  ```
  CleanRepo.exe --orphaned-articles
  ```
  
  The tool will prompt you for any additional information it needs for that function, for example, the directory to look in. However, you can also pass that option in with the initial command.
  
  ```
  CleanRepo.exe --orphaned-articles --articles-directory c:\repos\visualstudio-docs-pr\docs\ide
  ```

- Find and delete orphaned .png/.gif/.jpg/.svg files:

  ```
  CleanRepo.exe --orphaned-images
  ```

## Image to text examples

The text-to-image functionality supported in the `--catalog-images-with-text` and `--filter-images-for-text` options is provided by the [Tesseract](https://www.nuget.org/packages/tesseract/) NuGet package.

### Get the Tesseract models

You must determine which Tesseract models you want to use and install them on your system. Tesseract models are generated per operating system. Tesseract models come in a variety of sizes. You also need to download the language data files for Tesseract 4.0.0 or later from [tesseract-tessdata](https://github.com/tesseract-ocr/tessdata/). Use the `--ocr-model-directory` value to set the path.

### Catalog images with text

To catalog all the images in a specified directory along with the text shown in each image:

```shell
CleanRepo.exe --catalog-images-with-text --url-base-path=/azure/developer/javascript 
--articles-directory=c:\azure-docs-pr\articles --media-directory=c:\azure-docs-pr\articles\javascript\media --ocr-model-directory=c:\tesseract\tessdata_fast
```

The output file is prefixed with `OcrImageFiles-`

### Filter images with text

To filter images based on one or more strings, use the `--filter-text-json-file` path to the JSON file with the text to filter for:

```json
["Azure","Microsoft"]
```

```shell
CleanRepo.exe --filter-images-for-text --filter-text-json-file=c:\filter-text.json --url-base-path=/azure/developer/javascript --ocr-model-directory=c:\tesseract\tessdata_fast --articles-directory=c:\azure-docs-pr\articles --media-directory=c:\azure-docs-pr\articles\javascript\media
```

The output file is prefixed with `FilteredOcrImageFiles-`.
