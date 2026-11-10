using System.Text.Json;
using AcademyDesk.Api.Controllers;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaClassSessionMove(Guid SessionId, UpdateClassSessionRequest Request);
internal sealed record PentaClassSessionInput(CreateClassSessionRequest? Create, PentaClassSessionMove? Move);

internal static class PentaClassSessionFields
{
    private const string CreateTool = "AcademyDesk.ClassSession.Create";
    internal const string RescheduleTool = "AcademyDesk.ClassSession.Reschedule";

    internal static PentaClassSessionInput? Read(string payload)
    {
        if (payload.Length > 4000) return null;
        try
        {
            using var document = JsonDocument.Parse(PentaOperationalPayload.Canonicalize(payload));
            var root = document.RootElement;
            var tool = root.GetProperty("tool_key").GetString();
            if (tool is not (CreateTool or RescheduleTool) || root.GetProperty("tool_revision").GetInt64() != 1) return null;
            var moving = tool == RescheduleTool;
            var fields = root.GetProperty("fields");
            if (fields.EnumerateObject().Any(x => x.Name switch
                {
                    "StartUtc" or "EndUtc" or "DeliveryMode" or "RoomName" or "MeetingLink" => false,
                    "SessionId" => !moving,
                    "BatchId" or "TeacherId" or "BranchId" => moving,
                    _ => true
                })) return null;

            var target = Reference(moving ? "SessionId" : "BatchId");
            var start = Date("StartUtc");
            var end = Date("EndUtc");
            var mode = Field("DeliveryMode", "text")?.GetString() switch
            {
                "Offline" or "InPerson" => "InPerson",
                "Online" => "Online",
                "Hybrid" => "Hybrid",
                _ => null
            };
            if (target is null || start is null || end is null || end <= start || mode is null) return null;

            var room = Field("RoomName", "text")?.GetString()?.Trim();
            var link = Field("MeetingLink", "text")?.GetString()?.Trim();
            if (mode == "InPerson" && link is not null ||
                mode != "InPerson" && (room is not null || !Uri.TryCreate(link, UriKind.Absolute, out var url) ||
                    url.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(url.Host) || url.UserInfo.Length != 0)) return null;
            var location = mode == "InPerson" ? room : link;
            if (location is not null && (location.Length > 120 || location.Any(char.IsControl))) return null;
            if (string.IsNullOrEmpty(location)) location = null;
            return moving
                ? new(null, new(target.Value, new(start.Value, end.Value, mode, location, "Scheduled")))
                : new(new(target.Value, Reference("TeacherId"), Reference("BranchId"), start.Value, end.Value, mode, location), null);

            JsonElement? Field(string name, string kind)
            {
                if (!fields.TryGetProperty(name, out var field)) return null;
                if (field.GetProperty("kind").GetString() != kind) throw new ArgumentException();
                return field.GetProperty("value");
            }

            Guid? Reference(string name)
            {
                var value = Field(name, "reference")?.GetString();
                if (value is null) return null;
                if (!Guid.TryParseExact(value, "D", out var id) || id == Guid.Empty || id.ToString("D") != value) throw new ArgumentException();
                return id;
            }

            DateTime? Date(string name) => Field(name, "text") is { } field && field.TryGetDateTime(out var value) &&
                value.Kind == DateTimeKind.Utc ? value : null;
        }
        catch (Exception error) when (error is ArgumentException or JsonException or FormatException) { return null; }
    }
}
