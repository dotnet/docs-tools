using CliWrap;
using CliWrap.Buffered;
using Microsoft.DotnetOrg.Ospo;

namespace DotNet.DocsTools.OspoClientServices;

public static class OspoClientFactory
{
    public static async Task<OspoClient?> CreateAsync(string? clientID, string? tenantID, string? resourceAudience, bool useCache)
    {
        if (string.IsNullOrEmpty(clientID) ||
            string.IsNullOrEmpty(tenantID) ||
            string.IsNullOrEmpty(resourceAudience))
        {
            return null;
        }
        await LoginAsync(clientID, tenantID);

        var token = await GetTokenAsync(resourceAudience);

        return new OspoClient(token, useCache);
    }

    private static async ValueTask LoginAsync(string clientID, string tenantID)
    {
        // az login
        var result = await Cli.Wrap("az")
            .WithArguments(
            [
                "login",
                "--clientID", clientID,
                "--tenantID", tenantID,
                "--scope", "links"
            ])
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        if (result.IsSuccess == false)
        {
            Console.WriteLine($"The 'az login' command supposedly failed with {result.ExitCode} after {result.RunTime}.");
        }
    }

    private static async ValueTask<string> GetTokenAsync(string resourceAudience)
    {

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
                "--resource", resourceAudience
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
