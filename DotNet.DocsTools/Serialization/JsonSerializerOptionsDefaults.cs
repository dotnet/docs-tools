using System.Text.Json;

namespace DotNetDocs.Tools.Serialization;

internal static class JsonSerializerOptionsDefaults
{
    /// <inheritdoc cref="JsonSerializerDefaults.Web" />
    internal static JsonSerializerOptions Shared { get; } = 
        new(JsonSerializerDefaults.Web);
}
