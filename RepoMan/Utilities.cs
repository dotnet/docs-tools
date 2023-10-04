using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace RepoMan;

internal static partial class Utilities
{
    private static HttpClient _httpClient;

    [GeneratedRegex("<meta name=\"(.*)\".*content=\"(.*)\".*/>")]
    public static partial Regex HtmlMetaRegex();

    [GeneratedRegex("\\* Content: \\[.*\\]\\((.*)\\)$")]
    public static partial Regex CommentMetaContentUrlRegex();

    public static HttpClient HttpClient => _httpClient ??= new HttpClient(new SocketsHttpHandler());

    public static async Task<Dictionary<string, string>> ScrapeArticleMetadata(Uri url, State state)
    {
        state.Logger.LogInformation($"Collecting article metadata from {url}");

        Dictionary<string, string> metadata = new Dictionary<string, string>();

        // Collect the metadata about the article
        var result = await HttpClient.GetAsync(url);

        if (result.IsSuccessStatusCode)
        {
            string content = await result.Content.ReadAsStringAsync();

            foreach (Match match in HtmlMetaRegex().Matches(content).Cast<Match>())
            {
                metadata[match.Groups[1].Value] = match.Groups[2].Value;
                state.Logger.LogInformation($"{match.Groups[1].Value} = {match.Groups[2].Value}");
            }
        }
        else
            state.Logger.LogError($"Failed to load {url} with response code {result.StatusCode}");

        return metadata;
    }

    public static bool MatchRegex(string pattern, string source, State state)
    {
        state.Logger.LogTrace($"Using regex: {pattern} to match {source}");
        return Regex.IsMatch(source, pattern);
    }

    public static string StripMarkdown(string content)
    {
        try
        {
            MarkdownDocument markdown = Markdig.Markdown.Parse(content);
            LinkInline? link = markdown.Descendants<ParagraphBlock>().SelectMany(x => x?.Inline?.Descendants<LinkInline>()!).FirstOrDefault();

            if (link == null)
                return Markdig.Markdown.ToPlainText(content);
            else
                return link.Url ?? "";
        }
        catch
        {
            return Markdig.Markdown.ToPlainText(content);
        }
    }

    public static bool TestStateJMES(string query, State state)
    {
        state.Logger.LogInformation($"Processing JMES test: {query}");

        try
        {
            string githubRequests = state.RequestBody().Root.ToString();
            DevLab.JmesPath.JmesPath jmesTest = new DevLab.JmesPath.JmesPath();

            string result = jmesTest.Transform(githubRequests, query);

            if (result != "null" && result != "false")
            {
                state.Logger.LogInformation("JMES Result: Pass");
                return true;
            }

            state.Logger.LogDebugger("JMES Result: Fail");
            return false;
        }
        catch (Exception e)
        {
            state.Logger.LogError(e, "JMES Result: Fail with error");
            return false;
        }
    }

    public static string GetJMESResult(string query, State state)
    {
        state.Logger.LogInformation($"Processing JMES query: {query}");

        try
        {
            string githubRequests = state.RequestBody().Root.ToString();
            DevLab.JmesPath.JmesPath jmesTest = new DevLab.JmesPath.JmesPath();
            
            string result = jmesTest.Transform(githubRequests, query);

            if (result.Equals("null", StringComparison.InvariantCultureIgnoreCase))
            {
                state.Logger.LogDebugger("JMES Result: Null return value");
                return string.Empty;
            }

            state.Logger.LogDebugger($"JMES Result: Returned {result}");
            return result;
        }
        catch (Exception e)
        {
            state.Logger.LogError(e, "JMES Result: Fail with error");
            return string.Empty;
        }
    }

}
