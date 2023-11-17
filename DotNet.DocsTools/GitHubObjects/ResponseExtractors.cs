using DotNetDocs.Tools.GraphQLQueries;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;


// TODO: Make reasonable responses for NULL nodes, etc.
internal static class ResponseExtractors
{
    internal static JsonElement GetAuthorChildElement(JsonElement element) => ChildElement(element, "author");

    internal static string GetIdValue(JsonElement node) => 
        StringProperty(node, "id");

    internal static string GetTitleValue(JsonElement node) => 
        StringProperty(node, "title");

    internal static string GetBodyValue(JsonElement node) =>
        StringProperty(node, "body");

    internal static DateTime GetCreatedAtValue(JsonElement element) => 
        DateProperty(element, "createdAt");

    internal static string[] GetChildArrayNames(JsonElement element) => 
        (from label in element.Descendent("labels", "nodes").EnumerateArray()
         select StringProperty(label, "name"))
        .ToArray();

    private static JsonElement ChildElement(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new ArgumentException("element is not a Json Object.", nameof(element));

        if (element.TryGetProperty(propertyName, out var childProperty))
        {
            return childProperty;
        }
        throw new ArgumentException($"Property {propertyName} not found in Json element. Did you possibly access the parent node?", nameof(element));
    }

    internal static string OptionalStringProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new ArgumentException("element is not a Json Object.", nameof(element));

        if (element.TryGetProperty(propertyName, out var stringProperty))
        {
            return stringProperty.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    internal static string StringProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new ArgumentException("element is not a Json Object.", nameof(element));

        if (element.TryGetProperty(propertyName, out var stringProperty))
        {
            return stringProperty.GetString() ?? throw new ArgumentException("Requested property shouldn't be null", nameof(propertyName));
        }
        throw new ArgumentException($"Property {propertyName} not found in Json element. Did you possibly access the parent node?", nameof(element));
    }

    private static DateTime DateProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new ArgumentException("element is not a Json Object.", nameof(element));

        if (element.TryGetProperty(propertyName, out var dateProperty))
        {
            return dateProperty.GetDateTime();
        }
        throw new ArgumentException($"Property {propertyName} not found in Json element. Did you possibly access the parent node?", nameof(element));
    }
}
