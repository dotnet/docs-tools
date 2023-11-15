using GitHubJwt;
using System.Runtime.CompilerServices;
using System.Text.Json;

// Our unit tests validate these utilities:
[assembly: InternalsVisibleTo("DotnetDocsTools.Tests")]

namespace DotNet.DocsTools.GitHubObjects;

internal static class ResponseExtractors
{
    internal static string LoginFromAuthorNode(JsonElement authorNode) => 
        StringPropertyOrEmpty(authorNode, "login");

    internal static string NameFromAuthorNode(JsonElement authorNode) => 
        StringPropertyOrEmpty(authorNode, "name");

    private static string StringPropertyOrEmpty(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new ArgumentException("element is not a Json Object.", nameof(element));

        if (element.TryGetProperty(propertyName, out var propertyString))
        {
            return propertyString.GetString() ?? string.Empty;
        }
        throw new ArgumentException($"Property {propertyName} not found in Json element. Did you possibly access the parent node?", nameof(element));
    }
}
