using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcademyDesk.Api.Infrastructure.Meetings;

// DateTimeOffset's default reader accepts a missing offset using the server zone.
// Meeting requests must instead explicitly identify the instant supplied by user.
public sealed class MeetingStartConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("An explicit ISO meeting start offset is required.");
        var value = reader.GetString();
        if (value is null || value.Length is < 20 or > 40 ||
            !(value.EndsWith('Z') || value.Length >= 6 && value[^6] is '+' or '-' && value[^3] == ':') ||
            !reader.TryGetDateTimeOffset(out var start)) throw new JsonException("An explicit ISO meeting start offset is required.");
        return start;
    }
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("O", CultureInfo.InvariantCulture));
}
