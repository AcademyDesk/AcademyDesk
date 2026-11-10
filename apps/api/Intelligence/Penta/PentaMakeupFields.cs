using System.Text.Json;
using AcademyDesk.Api.Controllers;

namespace AcademyDesk.Api.Intelligence.Penta;

internal static class PentaMakeupFields
{
    internal const string Tool = "AcademyDesk.Makeup.Create";

    internal static CreateMakeupRequest? Read(string payload)
    {
        if (payload.Length > 8000) return null;
        try
        {
            using var document = JsonDocument.Parse(PentaOperationalPayload.Canonicalize(payload));
            var root = document.RootElement;
            if (root.GetProperty("tool_key").GetString() != Tool || root.GetProperty("tool_revision").GetInt64() != 1) return null;
            var fields = root.GetProperty("fields");
            if (fields.EnumerateObject().Any(x => x.Name is not ("StudentId" or "BatchId" or "TeacherId" or
                "StartUtc" or "DeliveryMode" or "Venue" or "MeetingLink" or "UseNextScheduledClass" or "Notes"))) return null;
            var student = Reference("StudentId");
            var batch = Reference("BatchId");
            var next = Field("UseNextScheduledClass", "boolean")?.GetBoolean() ?? false;
            var startField = Field("StartUtc", "text");
            DateTime? start = null;
            if (startField is { } date)
            {
                if (!date.TryGetDateTime(out var value) || value.Kind != DateTimeKind.Utc) return null;
                start = value;
            }
            if (student is null || batch is null || !next && start is null) return null;
            return new(student.Value, batch.Value, Reference("TeacherId"), start, Text("DeliveryMode", 30),
                Text("Venue", 250), Text("MeetingLink", 1000), next, Text("Notes", 2000));

            JsonElement? Field(string name, string kind)
            {
                if (!fields.TryGetProperty(name, out var field)) return null;
                if (field.GetProperty("kind").GetString() != kind) throw new ArgumentException();
                return field.GetProperty("value");
            }

            Guid? Reference(string name)
            {
                var text = Field(name, "reference")?.GetString();
                if (text is null) return null;
                if (!Guid.TryParseExact(text, "D", out var id) || id == Guid.Empty || id.ToString("D") != text) throw new ArgumentException();
                return id;
            }

            string? Text(string name, int limit)
            {
                var text = Field(name, "text")?.GetString();
                if (text is not null && (text.Length > limit || text.Any(char.IsControl))) throw new ArgumentException();
                return text;
            }
        }
        catch (Exception error) when (error is ArgumentException or JsonException or FormatException) { return null; }
    }
}
