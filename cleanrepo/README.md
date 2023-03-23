# CleanRepo

This command-line tool helps you clean up a DocFx-based content repo. It can:

- Find and delete markdown files that aren't linked from a TOC file.
- Find and delete orphaned image (.png, .jpg, .gif, .svg) files.
- Map images to the files that reference them.
- Find and delete orphaned "shared" markdown files (includes).
- Find and delete orphaned snippet (.cs, .vb, .cpp, and .fs) files.
- Find and replace links to redirected files.
- Remove daisy chains (or hops) within the redirection files for the docset.
- Replace site-relative links with file-relative links (includes image links).

## Usage

| Command | Description |
| - | - |
| --orphaned-articles | Find orphaned .md articles. |
| --orphaned-images | Find orphaned .png, .gif, .svg, or .jpg files.<br/>**Note:** The tool does not know if images are referenced in a code sample project, so we recommend not running this option on a directory that contains samples. |
| --orphaned-snippets | Find orphaned .cs, .vb, .cpp, and .fs files. |
| --orphaned-includes | Find orphaned INCLUDE files. |
| --catalog-images | Map images to the markdown/YAML files that reference them. This option generates a JSON file with the output. |
| --replace-redirects | Find backlinks to redirected files and replace with new target. |
| --remove-hops | Remove daisy chains within the redirection files for the docset. |
| --relative-links | Replace site-relative links with file-relative links. |

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
