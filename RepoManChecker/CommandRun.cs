using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Diagnostics;
using System.Globalization;

namespace RepoMan;

internal static class CommandRun
{
    const string QUIT = "(quit)";
    const string BACK = "(back)";

    public static void HandlerGithub(string? githubOwner, string? githubRepository)
    {
        if (githubOwner != null && githubRepository != null)
        {
            State state = new();

            // Read the config file
            if (!Program.TryReadGithubContent(githubOwner, githubRepository, state, out string? configFileContent))
                return;

            Program.ReadFileContentIntoObject(configFileContent, state);
            Program.PrintRulesInfo(state);

            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            AnsiConsole.Clear();

            string[] skippedKeys = ["revision", "schema-version", "owner-ms-alias", "config"];

            bool exit = false;

            Style textItemSelected = new(Color.Black, Color.White);
            Style textItemNotSelected = new(Color.Default);

            List<string> triggerTypes = [];
            List<string> eventTypes = [];

            foreach (var item in state.RepoRulesYaml)
            {
                if (!skippedKeys.Contains(item.Key.ToString(), StringComparer.InvariantCultureIgnoreCase))
                    triggerTypes.Add(item.Key.ToString());
            }

            // List and select one trigger
            list_github_trigger:

            string selectedTrigger = AnsiConsole.Prompt(
                                        new SelectionPrompt<string>()
                                            .Title("Select trigger")
                                            .AddChoices(triggerTypes.Append(QUIT))
                                            );

            if (selectedTrigger == QUIT)
                return;

            // List and select one event
            list_github_event:
            eventTypes.Clear();
            foreach (var item in state.RepoRulesYaml[triggerTypes[triggerTypes.IndexOf(selectedTrigger)]].AsMappingNode())
                eventTypes.Add(item.Key.ToString());

            string selectedEvent = AnsiConsole.Prompt(
                                    new SelectionPrompt<string>()
                                        .Title($"({selectedTrigger}) Select event")
                                        .AddChoices(eventTypes.Append(BACK))
                                        );

            if (selectedEvent == BACK)
                goto list_github_trigger;

            // Look for event file
            string eventFile = $"state-{selectedTrigger}-{selectedEvent}.json";
            if (!File.Exists(eventFile))
            {
                AnsiConsole.MarkupLine($"[red]Event file doesn't exist:[/] {eventFile}");
                AnsiConsole.WriteLine("Press any key to continue...");
                Console.ReadKey();
                AnsiConsole.Clear();
                goto list_github_event;
            }

            // Load the state object file
            JObject stateJson = JObject.Parse(File.ReadAllText(eventFile));

            // Hydrate the issue/pr
            HydrateStateIssue(state, stateJson);

            return;

        }
    }

    private static void HydrateStateIssue(State state, JObject json)
    {
        JToken obj = json["Issue"]!;

        state.Issue = new(
                obj["url"].Value<string>(),
                obj["html_url"].Value<string>(),
                obj["comments_url"].Value<string>(),
                obj["events_url"].Value<string>(),
                obj["number"].Value<int>(),
                obj["state"].Value<string>() == "open" ? Octokit.ItemState.Open : Octokit.ItemState.Closed,
                obj["title"].Value<string>(),
                obj["body"].Value<string>(),
                HydrateUser(obj["closed_by"]),
                HydrateUser(obj["user"]),
                obj["labels"].Select(t => HydrateLabel(t)).ToList().AsReadOnly(),
                HydrateUser("assignee"),
                obj["assignees"].Select(u => HydrateUser(u)).ToList().AsReadOnly(),
                null, // TODO: MILESTONE
                obj["comments"].Value<int>(),
                null, // PullRequest
                null, // Closed at
                obj["created_at"].Value<DateTimeOffset>(),
                obj["updated_at"].Value<DateTimeOffset>(),
                obj["id"].Value<int>(),
                obj["node_id"].Value<string>(),
                obj["locked"].Value<bool>(),
                null, // Repository
                null, // Reactions
                null, // Lock reason
                null // item state reason
                );
    }

    private static Octokit.User? HydrateUser(JToken? json)
    {
        if (json.FirstOrDefault() == null)
            return null;

        return new("", "", "", 0, "", DateTimeOffset.MinValue, DateTimeOffset.MinValue, 0, "", 0, 0, false, "", 0,
            json["id"].Value<int>(),
            "",
            json["login"].Value<string>(),
            "",
            json["node_id"].Value<string>(),
            0,
            new Octokit.Plan(),
            0, 0, 0,
            json["url"].Value<string>(),
            new Octokit.RepositoryPermissions(), false, "", DateTimeOffset.MinValue
            );
    }

    private static Octokit.Label HydrateLabel(JToken json)
    {
        return new(
            json["id"].Value<long>(),
            json["url"].Value<string>(),
            json["name"].Value<string>(),
            json["node_id"].Value<string>(),
            json["color"].Value<string>(),
            json["description"].Value<string>(),
            json["default"].Value<bool>());
    }

    private enum LogicPosition
    {
        Root,
        SelectedObjectType,
        SelectedEvent,
        Exit
    }

    private struct SelectionPair
    {
        public int ItemCount { get; set; }
        public int SelectedIndex { get; set; }

        public void NavigateUp()
        {
            SelectedIndex--;

            if (SelectedIndex < 0)
                SelectedIndex = ItemCount - 1;
        }

        public void NavigateDown()
        {
            SelectedIndex++;

            if (SelectedIndex == ItemCount)
                SelectedIndex = 0;
        }
    }
}
