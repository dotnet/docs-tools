// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import { insertLink } from './commands/insertLink';
import { insertApiRefLinkCommandName, insertXrefLinkCommandName, toolName } from './consts';
import { LinkType } from './commands/types/LinkType';

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {
  // Use the console to output diagnostic information (console.log) and errors (console.error)
  // This line of code will only be executed once when your extension is activated
  console.log(
    `Congratulations, your extension "${toolName}" is now active!`
  );

  // The command has been defined in the package.json file
  // Now provide the implementation of the command with registerCommand
  // The commandId parameter must match the command field in package.json
  const apiRefLinkCommandRegistration = vscode.commands.registerCommand(
    insertApiRefLinkCommandName,
    () => insertLink(LinkType.Markdown)
  );

  const xrefLinkCommandRegistration = vscode.commands.registerCommand(
    insertXrefLinkCommandName,
    () => insertLink(LinkType.Xref)
  );

  context.subscriptions.push(apiRefLinkCommandRegistration);
  context.subscriptions.push(xrefLinkCommandRegistration);
}

// This method is called when your extension is deactivated
export function deactivate() {}
