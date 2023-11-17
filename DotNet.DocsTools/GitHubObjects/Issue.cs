using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

public abstract record Issue
{
    public Issue(JsonElement element)
    {
        Number = element.GetProperty("number").GetInt32();
    }

    /// <summary> 
    /// Retrieve the issue number.
    /// </summary>
    public int Number { get; }
}
