namespace Quest2GitHub.AzureDevOpsCommunications;

/// <summary>
/// The Opcode for the patch document.
/// </summary>
public enum Op
{
    /// <summary>
    /// Not valid.
    /// </summary>
    None,
    /// <summary>
    /// Add a key.
    /// </summary>
    Add,
    /// <summary>
    /// Copy a key
    /// </summary>
    Copy,
    /// <summary>
    /// Move an element
    /// </summary>
    Move,
    /// <summary>
    /// Remove an element.
    /// </summary>
    Remove,
    /// <summary>
    /// Replace an element
    /// </summary>
    Replace,
    /// <summary>
    /// Test an operation.
    /// </summary>
    Test,
}

/// <summary>
/// The representation of the JSON for a single patch document.
/// </summary>
/// <remarks>
/// Azure Dev Ops uses the PATCH verb for adding new elements and
/// updating an existing element.
/// </remarks>
public class JsonPatchDocument
{
    /// <summary>
    /// The operation.
    /// </summary>
    [JsonPropertyName("op")]
    public required Op Operation { get; init; }

    /// <summary>
    /// The path of the attribute.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>
    /// The old value (null is valid for many operations)
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; init; }

    /// <summary>
    /// The new value.
    /// </summary>
    [JsonPropertyName("value")]
    public required object? Value { get; init; }
}

/// <summary>
/// The fields required to Assign to an identity
/// </summary>
public class AzDoIdentity
{
    [JsonPropertyName("uniqueName")]
    public required string UniqueName { get; init; }

    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("descriptor")]
    public required string Descriptor { get; init; }

}
