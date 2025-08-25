using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualBasic.FileIO;
using Octokit;
using Spectre.Console;

namespace RepoMan;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        bool runApp = false;

        Argument<string?> inputFile = new("file")
        {
            Description = "Path to the RepoMan config file to process. Can be a local file or URL.",
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                {
                    result.AddError("Invalid value passed to input parameter");
                    return default;
                }

                string value = result.Tokens.Single().Value;

                if (Path.Exists(value))
                    return value;

                result.AddError($"Path is invalid: {value}");

                return null;
            }
        };

        Argument<Uri?> httpLink = new("httpLink")
        {
            Description = "A URL to a rules file.",
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                {
                    result.AddError("Invalid value passed to httpLink parameter");
                    return null;
                }

                string value = result.Tokens.Single().Value;

                if (Uri.TryCreate(value, UriKind.Absolute, out Uri? webUri))
                    return webUri;

                result.AddError($"URL is invalid: {value}");

                return null;
            }
        };

        Argument<string?> githubRepo = new("repository")
        {
            Description = "The name of a GitHub repository.",
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                {
                    result.AddError("Invalid value passed to repository parameter");
                    return null;
                }

                string value = result.Tokens.Single().Value.Trim();

                if (!string.IsNullOrEmpty(value))
                    return value;

                result.AddError($"Repository is invalid: {value}");

                return null;
            }
        };

        Argument<string?> githubOwner = new("owner")
        {
            Description = "The owner name of the GitHub repository.",
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                {
                    result.AddError("Invalid value passed to owner parameter");
                    return null;
                }

                string value = result.Tokens.Single().Value.Trim();

                if (!string.IsNullOrEmpty(value))
                    return value;

                result.AddError($"Owner is invalid: {value}");

                return null;
            }
        };

        RootCommand rootCommand = new("Tests and validates RepoMan config files.");

        // CHECK command
        Command checkCommand = new("check", "Validates a RepoMan config file");
        checkCommand.Aliases.Add("validate");

        Command checkFileCommand = new("file", "Loads the config from a local file.");
        checkFileCommand.Arguments.Add(inputFile);
        checkFileCommand.SetAction((ParseResult parseResult, CancellationToken token) =>
        {
            CommandCheck.HandlerFile(parseResult.GetValue(inputFile));
            return Task.CompletedTask;
        });
        checkCommand.Subcommands.Add(checkFileCommand);

        Command checkHttpCommand = new("http", "Loads the config from a URL.");
        checkHttpCommand.Arguments.Add(httpLink);
        checkHttpCommand.SetAction((ParseResult parseResult, CancellationToken token) =>
        {
            CommandCheck.HandlerHttp(parseResult.GetValue(httpLink));
            return Task.CompletedTask;
        });
        checkCommand.Subcommands.Add(checkHttpCommand);

        Command checkGithubCommand = new("github", "Loads the config from a GitHub repository.");
        checkGithubCommand.Arguments.Add(githubOwner);
        checkGithubCommand.Arguments.Add(githubRepo);
        checkGithubCommand.SetAction((ParseResult parseResult, CancellationToken token) =>
        {
            CommandCheck.HandlerGithub(parseResult.GetValue(githubOwner), parseResult.GetValue(githubRepo));
            return Task.CompletedTask;
        });
        checkCommand.Subcommands.Add(checkGithubCommand);

        rootCommand.Subcommands.Add(checkCommand);

        // RUN command
        Command runCommand = new("run", "Processes a RepoMan config file in GitHub and simulate an action.");
        runCommand.Arguments.Add(githubRepo);
        runCommand.Arguments.Add(githubOwner);
        runCommand.SetAction((ParseResult parseResult, CancellationToken token) =>
        {
            CommandRun.HandlerGithub(parseResult.GetValue(githubRepo), parseResult.GetValue(githubOwner));
            return Task.CompletedTask;
        });

        rootCommand.Subcommands.Add(runCommand);

        return await rootCommand.Parse(args).InvokeAsync();
    }

    internal static bool TryReadGithubContent(string owner, string repository, State state, [NotNullWhen(true)] out string? content)
    {
        AnsiConsole.Write($"Attempting to connect to GitHub repository...");

        state.Client = new(new ProductHeaderValue("adegeo-ms-repoman-tester", "1.0"));

        try
        {
            state.RepositoryId = state.Client.Repository.Get(owner, repository).Result.Id;
            AnsiConsole.MarkupLine("[green]Success![/]");
            IReadOnlyList<RepositoryContent> rulesResponse = state.Client.Repository.Content.GetAllContents(state.RepositoryId, Function1.RulesFileName).Result;

            if (rulesResponse.Count == 0)
            {
                AnsiConsole.Write($"Rules file ({Function1.RulesFileName}) not found.");
                content = null;
                return false;
            }

            content = rulesResponse[0].Content;

            if (content == null)
            {
                AnsiConsole.MarkupLine($"\n[red]Unable to read content of repo rules file.[/]");
                content = null;
                return false;
            }

            return true;
        }
        catch (AggregateException e)
        {
            e.Handle(x => {

                if (x is Octokit.NotFoundException)
                {
                    AnsiConsole.MarkupLine($"\n[red]Repository not found.[/]");
                    return true;
                }
                else
                    AnsiConsole.MarkupLine($"\n[red]Unknown error occurred:[/]\n[deepskyblue4_2]{e.Message}[/]");

                return false;
            });
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"\n[red]Unknown error occurred:[/]\n[deepskyblue4_2]{e.Message}[/]");
        }

        content = null;
        return false;
    }

    internal static bool TryReadHttpContent(Uri url, [NotNullWhen(true)] out string? content)
    {
        AnsiConsole.Write($"Attempting to download...");
        using HttpClient client = new();

        try
        {
            HttpResponseMessage message = client.GetAsync(url, HttpCompletionOption.ResponseContentRead).Result;
            AnsiConsole.Write($"Completed.\nAttempting to read content from HTTP message...");
            content = message.Content.ReadAsStringAsync().Result;
            AnsiConsole.WriteLine($"Completed.");
            return true;
        }
        catch (System.Net.Sockets.SocketException e)
        {
            AnsiConsole.MarkupLine($"\n[red]Unable to contact URL.[/] Error code: [deepskyblue4_2]{e.ErrorCode}[/]");
        }
        catch (HttpRequestException e)
        {
            AnsiConsole.MarkupLine($"\n[red]Unable to retrieve file.[/] Status code: [deepskyblue4_2]{e.StatusCode}[/]");
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"\n[red]Unable to retrieve file.[/]\nMessage: [deepskyblue4_2]{e.Message}[/]");
        }

        content = null;
        return false;
    }

    internal static bool ReadFileContentIntoObject(string content, State state)
    {
        // Read the config file
        try
        {
            // Read the config file
            AnsiConsole.Write($"Reading RepoMan config file...");
            state.ReadYamlContent(content);
            AnsiConsole.MarkupLine("[green]Success![/]");
            return true;
        }
        catch (YamlDotNet.Core.SyntaxErrorException e)
        {
            AnsiConsole.MarkupLine("[red]Failed![/]");
            AnsiConsole.Markup($"\n\n[underline red]Unable to parse content[/]\n");

            Grid grid = new Grid()
                .AddColumns(2)
                .AddRow(new Text("Description:", new Style(Color.Olive)), new Text(e.Message))
                .AddRow(new Text("Line info:", new Style(Color.Olive)), new Text(e.Start.ToString()))
                ;

            AnsiConsole.Write(grid);
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[red]Unknown error![/]");
            AnsiConsole.WriteLine(e.Message);
        }

        return false;
    }

    internal static int? PrintRulesInfo(State state)
    {
        int revision = state.RepoRulesYaml["revision"].ToInt();
        int schemaVersion = state.RepoRulesYaml["schema-version"].ToInt();
        string contact = state.RepoRulesYaml["owner-ms-alias"].ToString();

        Grid grid = new Grid()
            .AddColumns(2)
            .AddRow(new Text("Revision:", new Style(Color.Olive)), new Text(revision.ToString()))
            .AddRow(new Text("Schema Version:", new Style(Color.Olive)), new Text(schemaVersion.ToString()))
            .AddRow(new Text("Owner:", new Style(Color.Olive)), new Text(contact))
            ;

        AnsiConsole.Write(grid);
        
        return grid.Width;
    }
}
