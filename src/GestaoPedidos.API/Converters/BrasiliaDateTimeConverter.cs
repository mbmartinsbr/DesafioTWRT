using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestaoPedidos.API.Converters;

public class BrasiliaDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly TimeZoneInfo _brasilia =
        TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(
            value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _brasilia);

        writer.WriteStringValue(local.ToString("yyyy-MM-ddTHH:mm:ss"));
    }
}
