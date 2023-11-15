using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

public abstract class Issue(JsonElement element)
{
    /// <summary> 
    /// Retrieve the issue number.
    /// </summary>
    public int Number => element.GetProperty("number").GetInt32();
}
