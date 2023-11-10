using GitHubJwt;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

internal static class ResponseExtractors
{
    internal static string LoginFromAuthorNode(JsonElement authorNode) => 
        StringPropertyOrEmpty(authorNode, "login");

    internal static string NameFromAuthorNode(JsonElement authorNode) => 
        StringPropertyOrEmpty(authorNode, "name");

    private static string StringPropertyOrEmpty(JsonElement element, string propertyName)
    {
        if ((element.ValueKind == JsonValueKind.Object) && element.TryGetProperty(propertyName, out var login))
        {
            return login.GetString() ?? string.Empty;
        }
        return string.Empty;
    }
}
