using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace DotNetDocs.RepoMan;

internal static partial class Utilities
{
    private static HttpClient? s_httpClient;

    [GeneratedRegex("<meta name=\"(.*)\".*content=\"(.*)\".*/>")]
    public static partial Regex HtmlMetaRegex();

    public static HttpClient HttpClient => s_httpClient ??= new HttpClient(new SocketsHttpHandler());

    public static async Task<Dictionary<string, string>> ScrapeArticleMetadata(Uri url, InstanceData data)
    {
        data.Logger.LogInformation("Collecting article metadata from {url}", url);

        Dictionary<string, string> metadata = [];

        // Collect the metadata about the article
        HttpResponseMessage result = await HttpClient.GetAsync(url);

        if (result.IsSuccessStatusCode)
        {
            string content = await result.Content.ReadAsStringAsync();

            foreach (Match match in HtmlMetaRegex().Matches(content).Cast<Match>())
            {
                metadata[match.Groups[1].Value] = match.Groups[2].Value;
                data.Logger.LogDebug("{name} = {value}", match.Groups[1].Value, match.Groups[2].Value);
            }

            data.Logger.LogInformation("Metadata items found: {count}", metadata.Count);
        }
        else
            data.Logger.LogError("Failed to load {url} with response code {code}", url, result.StatusCode);

        return metadata;
    }

    public static bool MatchRegex(string pattern, string source, InstanceData data)
    {
        data.Logger.LogTrace("Using regex: {pattern} to match {source}", pattern.Replace("\n","\\n"), source);
        source = source.Replace("\r", null);
        return Regex.IsMatch(source.Replace("\r", null), pattern);
    }


    public static bool TestStateJMES(string query, InstanceData data)
    {
        data.Logger.LogInformation("RUN CHECK: Processing JMES test: {query}", query);

        try
        {
            string githubRequests = data.GetJSONObject().Root.ToString();
            DevLab.JmesPath.JmesPath jmesTest = new();

            string result = jmesTest.Transform(githubRequests, query);

            if (result != "null" && result != "false")
            {
                data.Logger.LogInformation("JMES Result: Pass");
                return true;
            }

            data.Logger.LogDebug("JMES Result: Fail");
            return false;
        }
        catch (Exception e)
        {
            data.Logger.LogError(e, "JMES Result: Fail with error");
            data.HasFailure = true;
            data.FailureMessage = "JMES check failed to evaluate";
            return false;
        }
    }

    public static string GetJMESResult(string query, InstanceData data)
    {
        data.Logger.LogInformation("Processing JMES query: {query}", query);

        try
        {
            string githubRequests = data.GetJSONObject().Root.ToString();
            DevLab.JmesPath.JmesPath jmesTest = new();

            string result = jmesTest.Transform(githubRequests, query);

            if (result.Equals("null", StringComparison.InvariantCultureIgnoreCase))
            {
                data.Logger.LogDebug("JMES Result: Null return value");
                return string.Empty;
            }

            data.Logger.LogDebug("JMES Result: Returned {result}", result);
            return result;
        }
        catch (Exception e)
        {
            data.Logger.LogError(e, "JMES Result: Fail with error");
            data.HasFailure = true;
            data.FailureMessage = "JMES check failed to evaluate";
            return string.Empty;
        }
    }
}
