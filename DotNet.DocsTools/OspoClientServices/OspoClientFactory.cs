using CliWrap;
using CliWrap.Buffered;
using Microsoft.DotnetOrg.Ospo;

namespace DotNet.DocsTools.OspoClientServices;

public static class OspoClientFactory
{
    public static async Task<OspoClient> CreateAsync(bool useCache)
    {
        await LoginAsync();

        var token = await GetTokenAsync();

        return new OspoClient(token, useCache);
    }

    private static async ValueTask LoginAsync()
    {
        // az login
        var result = await Cli.Wrap("az")
            .WithArguments(
            [
                "login",
                "--scope",
                "links"
            ])
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        if (result.IsSuccess == false)
        {
            Console.WriteLine($"The 'az login' command supposedly failed with {result.ExitCode} after {result.RunTime}.");
        }
    }

    private static async ValueTask<string> GetTokenAsync()
    {
        //var resource = CommandLineUtility.GetEnvVariable(
        //    "OSMP_API_AUDIENCE", "Unable to get the scoped/resource.", null);

        // az account get-access-token
        //   --query 'accessToken'
        //   -o tsv
        //   --scope "links"
        //   --resource "Some test audience"
        var tokenResult = await Cli.Wrap("az")
            .WithArguments(
            [
                "account",
                "get-access-token",
                "--query", "accessToken",
                "-o", "tsv",
                "--scope", "links",
                //"--resource", "resource"
            ])
            .ExecuteBufferedAsync();

        var token = tokenResult.StandardOutput.Trim();

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine($"az account get-access-token failed");
        }

        return token;
    }
}
