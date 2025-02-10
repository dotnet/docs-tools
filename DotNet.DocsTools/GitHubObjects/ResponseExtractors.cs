using DotNetDocs.Tools.GraphQLQueries;
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

    internal static DateTime GetUpdatedAtValueOrNow(JsonElement element) =>
        OptionalDateProperty(element, "updatedAt") ?? DateTime.Now;

    internal static T[] GetChildArrayElements<T>(
        JsonElement element,
        string elementName,
        Func<JsonElement, T> selector)
    {
        var array = element.Descendent(elementName, "nodes");

        // Debugging code:
        if (array.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine($"No array of {elementName} found!");
        }
        return array.ValueKind == JsonValueKind.Array ? 
            (from child in array.EnumerateArray()
             let childElement = selector(child)
             where childElement != null
                select childElement).ToArray() :
                [];
    }

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

    private static DateTime? OptionalDateProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new ArgumentException("element is not a Json Object.", nameof(element));

        if (element.TryGetProperty(propertyName, out var dateProperty))
        {
            return dateProperty.GetDateTime();
        }
        else
        {
            return null;
        }
    }

    internal static int IntProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new ArgumentException("element is not a Json Object.", nameof(element));

        if (element.TryGetProperty(propertyName, out var intProperty))
        {
            return intProperty.GetInt32();
        }
        throw new ArgumentException($"Property {propertyName} not found in Json element. Did you possibly access the parent node?", nameof(element));
    }

    internal static DateTime DateTimeProperty(JsonElement element, string propertyName)
    {
        return OptionalDateProperty(element, propertyName)
            ?? throw new ArgumentException("Requested property shouldn't be null", nameof(propertyName));
    }
}
