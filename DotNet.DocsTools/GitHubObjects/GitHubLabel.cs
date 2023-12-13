using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// Record type for a GitHub label
/// </summary>
public sealed record GitHubLabel
{
    /// <summary>
    /// The name of the label.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The node ID for the label.
    /// </summary
    /// <remarks>
    /// If it turns out that a common record type can be used for 
    /// all GH objects, this would be in the common base type.
    /// </remarks>
    public string Id { get; }

    /// <summary>
    /// Construct a GitHub label from a JsonElement
    /// </summary>
    /// <param name="labelElement"></param>
    /// <exception cref="ArgumentException"></exception>
    public GitHubLabel(JsonElement labelElement)
    {
        if (labelElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Must be an object", nameof(labelElement));
        }
        Name = ResponseExtractors.StringProperty(labelElement, "name")!;
        Id = ResponseExtractors.StringProperty(labelElement, "id")!;
    }
}

