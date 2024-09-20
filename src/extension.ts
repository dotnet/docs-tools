// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import { insertLink } from './commands/insertLink';
import { insertApiRefLinkCommandName, insertXrefLinkCommandName, transformXrefToOtherCommandName, toolName } from './consts';
import { LinkType } from './commands/types/LinkType';
import { xrefStarterAutoComplete, xrefDisplayTypeAutoComplete } from './commands/autocomplete';
import { SearchOptions } from './commands/types/SearchOptions';
import { transformXrefToOther } from './commands/transform';
import { DisplayPropertyChanger } from './commands/quickaction';

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {
  // Use the console to output diagnostic information (console.log) and errors (console.error)
  // This line of code will only be executed once when your extension is activated
  console.log(
    `The "${toolName}" is now active.`
  );

  // The command has been defined in the package.json file
  // Now provide the implementation of the command with registerCommand
  // The commandId parameter must match the command field in package.json
  context.subscriptions.push(
    vscode.commands.registerCommand(
      insertApiRefLinkCommandName, () => insertLink(LinkType.Markdown, undefined)),

    vscode.commands.registerCommand(
      insertXrefLinkCommandName, (args: SearchOptions | undefined) => insertLink(LinkType.Xref, args)),

    vscode.commands.registerCommand(
      transformXrefToOtherCommandName, () => transformXrefToOther()),

    vscode.languages.registerCompletionItemProvider('markdown', xrefStarterAutoComplete, ':'),
    vscode.languages.registerCompletionItemProvider('markdown', xrefDisplayTypeAutoComplete, '?'),
    // vscode.languages.registerInlineCompletionItemProvider('markdown', xrefInlineAutoComplete),

    vscode.languages.registerCodeActionsProvider('markdown', new DisplayPropertyChanger(), {
      providedCodeActionKinds: [vscode.CodeActionKind.QuickFix]
    })
  );
}

// This method is called when your extension is deactivated
export function deactivate() { }
