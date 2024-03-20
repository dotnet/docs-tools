using Spectre.Console;
using StarodubOleg.GPPG.Runtime;

namespace RepoMan;

internal static class CommandCheck
{
    public static void HandlerFile(string? inputFileValue)
    {
        if (inputFileValue != null)
        {
            State state = new();
            if (Program.ReadFileContentIntoObject(File.ReadAllText(inputFileValue), state))
                Program.PrintRulesInfo(state);
        }
    }

    public static void HandlerHttp(Uri? httpLink)
    {
        if (httpLink != null)
        {
            State state = new();

            // Read the config file
            if (!Program.TryReadHttpContent(httpLink, out string? configFileContent))
                return;

            if (Program.ReadFileContentIntoObject(configFileContent, state))
                Program.PrintRulesInfo(state);
        }
    }

    public static void HandlerGithub(string? githubOwner, string? githubRepository)
    {
        if (githubOwner != null && githubRepository != null)
        {
            State state = new();

            // Read the config file
            if (!Program.TryReadGithubContent(githubOwner, githubRepository, state, out string? configFileContent))
                return;

            if (Program.ReadFileContentIntoObject(configFileContent, state))
                Program.PrintRulesInfo(state);
        }
    }
}
