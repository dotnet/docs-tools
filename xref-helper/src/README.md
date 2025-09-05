# 🔗 API reference link helper

This handy-dandy extension helps content developers quickly insert and edit [cross-reference ("xref") links](https://learn.microsoft.com/contribute/content/how-to-write-links?branch=main#xref-cross-reference-links) to the official Microsoft API reference documentation for .NET types and members. It uses the [Microsoft Learn API Browser](https://learn.microsoft.com/api/apibrowser/dotnet/search) to search for a requested type or member. The extension:

- Exposes the ability to insert a Markdown link in the Markdown or XML file that's open in the active editor.
- Supports different link text variations: display only the type or member name, the member name with its type, the fully qualified name, or custom text.

## Requirements

- Visual Studio Code

## Example usage

1. With your cursor positioned where you want the link to be inserted in your document, open the command palette <kbd>F1</kbd> and search for **Insert XREF Link**.
2. Select the command, and when prompted, enter the type or member name you want to link to.

   ![Insert XREF link](images/command-pallette-insert-xref.png)

3. After a valid search term is entered, the extension searches for the configured API and displays the most relevant results.

   ![Results](images/command-pallette-insert-xref-results.png)

4. Once you select a result, you're prompted to choose the format of the link you want to insert.

   ![URL formats](images/command-pallette-insert-xref-all-formats.png)

   The extension inserts the selected link format into the active editor.

> [!TIP]
> You can also preselect text that you want converted to an xref hyperlink before you select the command. In this case, whatever text you selected is used as the link text. Nifty!

## Get started

To install the extension, download the [latest _xref-helper.vsix_](https://github.com/IEvangelist/xref-helper/blob/main/dist/xref-helper.vsix) file from the _.dist/_ folder, and from Visual Studio Code, right-click the file and select _Install Extension VSIX_.

Alternatively, to install the VSIX extension, open the command palette <kbd>F1</kbd> and select **Extensions: Install from VSIX...**. Then, browse to the downloaded _xref-helper.vsix_ file and select it.

> [!TIP]
> Since this extension isn't published to the Visual Studio Code Marketplace, you'll need to download the VSIX file from the GitHub repository every time an update is made available and manually reinstall it.

## Features

The following URL formats are supported, given the example `System.String.Format` method when selecting the [overloads option](#overloads-option) and **Method overloads** search result:

| Format | Resulting Markdown | Example HTML |
|--|--|--|
| Default | `<xref:System.String.Format*>` | `<a href="https://learn.microsoft.com/dotnet/api/system.string.format">Format</a>` |
| Name with type | `<xref:System.String.Format*?displayProperty=nameWithType>` | `<a href="https://learn.microsoft.com/dotnet/api/system.string.format">String.Format</a>` |
| Full name | `<xref:System.String.Format*?displayProperty=fullName>` | `<a href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</a>` |
| Custom link text | `[The string.Format method](xref:System.String.Format*)` | `<a href="https://learn.microsoft.com/dotnet/api/system.string.format">The string.Format method</a>` |

_Command pallette commands:_

- **Insert XREF Link**: Inserts a markdown (`<xref:uid>`) link to the official Microsoft API reference documentation for the selected type or member.
- **Insert API Reference Link**: Inserts a markdown (`[Name](URL link)`) link to the official Microsoft API reference documentation for the selected type or member.
- **Transform XREF to Other**: Convert XREF link between `[](xref:)` and `<xref:>`.

_Auto-completions:_

- **XREF Link Completion**: When typing `<xref:`, the extension will suggest the **Insert XREF Link** as a completion.
- **XREF Display Property Completion**: When typing `<xref:uid?`, the extension will suggest the possible display properties as completions.

_Code actions:_

- **XREF Display Property Switch**: The quick-edit code action (💡) is displayed when your cursor is on a line that contains an `xref` link with an existing display property, allowing you to quickly change the display property.

_Context menu commands:_

- **Convert XREF Link**: When a full `xref` link is selected, a context menu command is available to convert the link between `[](xref:)` and `<xref:>`.

## Search validations

After you start typing, if you delete the text, there's a validation error that will appear. The extension doesn't allow you to insert an empty search term:

![Empty](images/command-pallette-insert-xref-validation-empty.png)

The search term cannot contain spaces:

![Spaces](images/command-pallette-insert-xref-validation-space.png)

If an opening bracket is specified (i.e. if you're looking for a generic API), the search term must also include the closing bracket:

![Brackets](images/command-pallette-insert-xref-validation-brackets.png)

## Extension settings

In Visual Studio Code settings, search for `"XREF Helper"` (or paste this filter into the search bar `@ext:ievangelist.xref-helper`). You can configure the following settings:

| Setting | Description | Default |
|--|--|--|
| **Xref Helper: Allow Git Hub Session** <br/> `xref-helper.allowGitHubSession` | Whether to prompt the user for GitHub auth to allow the GitHub session to be used for API requests. Enables scenarios where XREF metadata is in a private GitHub repo. | `false` |
| **Xref Helper: Apis** <br/> `xref-helper.apis` | The APIs to search for xref links. | [See defaults below](#api-defaults). |
| <a name="overloads-option" />**Xref Helper: Append Overloads** <br/> `xref-helper.appendOverloads` | Whether to append overloads to the search results. Applies to methods and constructors. | `true` |

![Settings Screenshot](images/settings.png)

<details>
<summary id="api-defaults">API defaults</summary>

By default, only .NET is enabled. To enable other APIs, update the `xref-helper.apis` setting JSON:

```json
[
   {
      "name": ".NET",
      "enabled": true,
      "url": "https://learn.microsoft.com/api/apibrowser/dotnet/search",
      "queryStringParameters": [
         {
         "name": "api-version",
         "value": "0.2"
         },
         {
         "name": "locale",
         "value": "en-us"
         }
      ]
   },
   {
      "name": "Java",
      "enabled": false,
      "url": "https://learn.microsoft.com/api/apibrowser/java/search",
      "queryStringParameters": [
         {
         "name": "api-version",
         "value": "0.2"
         },
         {
         "name": "locale",
         "value": "en-us"
         }
      ]
   },
   {
      "name": "JavaScript",
      "enabled": false,
      "url": "https://learn.microsoft.com/api/apibrowser/javascript/search",
      "queryStringParameters": [
         {
         "name": "api-version",
         "value": "0.2"
         },
         {
         "name": "locale",
         "value": "en-us"
         }
      ]
   },
   {
      "name": "Python",
      "enabled": false,
      "url": "https://learn.microsoft.com/api/apibrowser/python/search",
      "queryStringParameters": [
         {
         "name": "api-version",
         "value": "0.2"
         },
         {
         "name": "locale",
         "value": "en-us"
         }
      ]
   },
   {
      "name": "PowerShell",
      "enabled": false,
      "url": "https://learn.microsoft.com/api/apibrowser/powershell/search",
      "queryStringParameters": [
         {
         "name": "api-version",
         "value": "0.2"
         },
         {
         "name": "locale",
         "value": "en-us"
         }
      ]
   }
]
```

</details>

![Settings JSON Screenshot](images/settings-json.png)
