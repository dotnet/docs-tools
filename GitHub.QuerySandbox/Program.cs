Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;

// The GitHub repository to query, for example; dotnet/docs.
string owner = "dotnet";
string name = "docs";

// Try getting the GitHub Action repo.
string? repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
if (repository is { Length: > 0 } && repository.Contains('/'))
{
    var repo = repository.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (repo.Length is 2)
    {
        owner = repo[0];
        name = repo[1];
    }
}

// Start reporting status.
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Smiley)
    .StartAsync($"{SpectreEmoji.Known.NerdFace} Querying the [bold grey]GitHub API[/] for answers...", async (ctx) =>
    {
        AnsiConsole.MarkupLine(
            $"{SpectreEmoji.Known.Gear} Configured to query the [link=https://github.com/{owner}/{name}][bold aqua]{owner}/{name}[/][/] repo...");

        // Create a GitHub client to use given the token in environment variables.
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        var credentials = new InMemoryCredentialStore(new Credentials(token));
        var client = new GitHubClient(
            new ProductHeaderValue("GitHub.QuerySandbox"), credentials);

        AnsiConsole.MarkupLine(
            $"{SpectreEmoji.Known.ExclamationQuestionMark} Requesting all [italic dim]pull requests[/] from the [bold aqua]{owner}/{name}[/] for the past year...");
        ctx.Spinner = new TimeTravelSpinner();

        // Get all issues for a repository, for the past year.
        var issues = await client.Issue.GetAllForRepository(
            owner, name, new RepositoryIssueRequest
            {
                Since = DateTimeOffset.Now.AddYears(-1),
                State = ItemStateFilter.All
            });

        static string LabelNameSelector(Label label)
        {
            return label.Name;
        }

        AnsiConsole.MarkupLine(
            $"{SpectreEmoji.Known.Label} Requesting all [italic dim]labels[/] from the [bold aqua]{owner}/{name}[/] repo...");
        ctx.Spinner = Spinner.Known.Monkey;

        // Get all the labels from the returned issues, as a distinct set.
        var issueLabels =
            issues.SelectMany(i => i.Labels).DistinctBy(LabelNameSelector);

        // Get all the labels from the repository.
        var allLabels =
            await client.Issue.Labels.GetAllForRepository(owner, name);

        // List all of the labels that haven't been used in the past year.
        var unusedLabels = allLabels.ExceptBy(
            issueLabels.Select(LabelNameSelector), LabelNameSelector);

        static void ConfigureTableColumn(TableColumn column)
        {
            column.Alignment(Justify.Left)
                .Padding(new Padding(2));
        }

        var table = new Table()
            .Border(TableBorder.Heavy)
            .BorderColor(Color.Fuchsia)
            .Expand()
            .AddColumn("[bold invert]Label[/]", ConfigureTableColumn)
            .AddColumn("[bold invert]Id[/]", ConfigureTableColumn);

        var total = 0;
        foreach (var label in unusedLabels.OrderBy(LabelNameSelector))
        {
            table.AddRow(label.Name, label.Id.ToString());
            ++ total;
        }

        AnsiConsole.MarkupLine(
            $"{SpectreEmoji.Known.Bullseye} Found {total} unused labels in the [bold aqua]{owner}/{name}[/] repo...");

        table.Title = new TableTitle(
            $"{total} Unused GitHub Labels (Within The Last Year)", Color.LightGreen);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);

        var chart = new BarChart()
            .Label("[green bold underline]Label Totals[/]")
            .CenterLabel()
            .AddItem("Used Labels", allLabels.Count - total, Color.Green)
            .AddItem("Unused Labels", total, Color.DarkOrange);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(chart);
    });