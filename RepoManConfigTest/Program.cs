using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using YamlDotNet.RepresentationModel;
using Octokit;
using System.Text;
using System.Threading.Tasks;

namespace RepoMan;

class Program
{
    static async Task Main(string[] args)
    {
        // Read config for the repo
        var text = System.IO.File.ReadAllText(".repoman.yml");

        var client = new GitHubClient(new ProductHeaderValue("adegeo-ms-repoman", "1.0"))
        {
            Credentials = new Credentials("Not a real key, which would be needed for accessing the API.")
        };

        //text = client.Repository.Content.GetAllContents(144316735, ".repoman.yml").Result?.FirstOrDefault()?.Content;
        using var reader = new System.IO.StringReader(text);

        var parser = new YamlStream();
        parser.Load(reader);

        // Create logger
        var state = new State();
        using var factory =
            LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.IncludeScopes = true;
                }
                ));

        state.Logger = factory.CreateLogger("RepoMan Test");
        
        // Get start of YAML config
        state.RepoRulesYaml = (YamlMappingNode)parser.Documents[0].RootNode;

        var key = state.RepoRulesYaml.Children.Keys.First();

        // Load the revision of the config for logging purposes
        int revision = state.RepoRulesYaml["revision"].ToInt();
        int schemaVersion = state.RepoRulesYaml["schema-version"].ToInt();

        // Load the settings from the config
        state.LoadSettings(state.RepoRulesYaml["config"]);

        

        var githubEvent = "pull_request";
        var githubAction = "opened";

        if (state.RepoRulesYaml.Exists(githubEvent))
        {
            state.Logger.LogInformation($"Processing event: {githubEvent}");

            if (state.RepoRulesYaml[githubEvent].AsMappingNode().Exists(githubAction, out YamlSequenceNode? actionNode))
            {
                state.Logger.LogInformation($"Processing action: {githubAction}");

                await Runner.Build(actionNode, state).Run(state);
            }

            await state.RunPooledActions();
        }


    }

    private static string UnicodeToUTF8(string strFrom)
    {
        byte[] bytSrc;
        byte[] bytDestination;
        string strTo = String.Empty;

        bytSrc = Encoding.Unicode.GetBytes(strFrom);
        bytDestination = Encoding.Convert(Encoding.Unicode, Encoding.ASCII, bytSrc);
        strTo = Encoding.ASCII.GetString(bytDestination);

        return strTo;
    }

}

//public static bool IsCheck(YamlMappingNode node) =>
//    node.Children.Keys.First().ToString() == "";


