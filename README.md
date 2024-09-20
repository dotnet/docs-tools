# API Reference Link Helper

This extension is primarily used to help developers quickly find and navigate to the official Microsoft API reference documentation for .NET types and members. It uses the [Microsoft Learn API Browser](https://learn.microsoft.com/api/apibrowser/dotnet/search) to search for the requested type or member. It exposes the ability to insert a markdown link to the documentation in the active editor. It supports `<xref:uid>` format, `?displayProperty` editing and quick management of API links.

## Getting Started

To install the extension, download the [latest _xref-helper.vsix_](https://github.com/IEvangelist/xref-helper/blob/main/dist/xref-helper.vsix) file from the _.dist/_ folder, and from Visual Studio Code, right-click the file and select _Install Extension VSIX_.

## Features

```
// TODO: Add features
```

## Requirements

- Visual Studio Code

## Extension Settings

In Visual Studio Code settings, search for `"XREF Helper"` (or paste this filter into the search bar `@ext:ievangelist.xref-helper`). You can configure the following settings:

| Setting | Description | Default |
| --- | --- | --- |
| **Xref Helper: Api Url** <br/> `xref-helper.apiUrl` | The URL to use when searching for XREFs. (Defaults to the .NET API search URL.) | `https://learn.microsoft.com/api/apibrowser/dotnet/search` |
| **Xref Helper: Append Overloads** <br/> `xref-helper.appendOverloads` | Whether to append overloads to the search results. Applies to methods and constructors. | `true` |
| **Xref Helper: Query String Parameters** <br/> `xref-helper.queryStringParameters` | The query string parameters to include when searching for XREFs. | `[ { "name": "api-version", "value": "0.2" }, { "name": "locale", "value": "en-us" } ]` |

![Settings Screenshot](https://raw.githubusercontent.com/IEvangelist/xref-helper/main/images/settings.png)
