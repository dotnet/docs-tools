using Newtonsoft.Json;
using System.Globalization;
using System.Text.Json;

namespace GitHub.RepositoryExplorer.Models;

internal sealed class DateOnlyConverter : System.Text.Json.Serialization.JsonConverter<DateOnly>
{
    private const string DateFormat = "yyyy-MM-dd";
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => 
        DateOnly.ParseExact(reader.GetString() ?? throw new InvalidOperationException("String not read from reader"), 
            DateFormat, CultureInfo.InvariantCulture);

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateFormat, CultureInfo.InvariantCulture));
    }
}

internal sealed class NewtonsoftDateOnlyConverter : Newtonsoft.Json.JsonConverter<DateOnly>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override DateOnly ReadJson(JsonReader reader, Type objectType, DateOnly existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer) => 
        DateOnly.ParseExact(reader.Value?.ToString() ?? throw new InvalidOperationException("Reader value was null"), 
            DateFormat, CultureInfo.InvariantCulture);

    public override void WriteJson(JsonWriter writer, DateOnly value, Newtonsoft.Json.JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString(DateFormat, CultureInfo.InvariantCulture));
    }
}
