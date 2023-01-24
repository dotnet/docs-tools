using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RepoMan;

public class Utilities
{
    public static bool MatchRegex(string pattern, string source, State state)
    {
        state.Logger.LogTrace($"Using regex: {pattern} to match {source}");
        return System.Text.RegularExpressions.Regex.IsMatch(source, pattern);
    }

    public static string StripMarkdown(string content)
    {
        try
        {
            var markdown = Markdig.Markdown.Parse(content);
            var link = markdown.Descendants<ParagraphBlock>().SelectMany(x => x?.Inline?.Descendants<LinkInline>()!).FirstOrDefault();

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
            var githubRequests = state.RequestBody().Root.ToString();
            var jmesTest = new DevLab.JmesPath.JmesPath();

            var result = jmesTest.Transform(githubRequests, query);

            if (result != "null" && result != "false")
            {
                state.Logger.LogInformation("JMES Result: Pass");
                return true;
            }

            state.Logger.LogDebug("JMES Result: Fail");
            return false;
        }
        catch (Exception e)
        {
            state.Logger.LogError(e, "JMES Result: Fail with error");
            return false;
        }
    }

}
