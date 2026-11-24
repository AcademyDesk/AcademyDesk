using System.Globalization;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaQuerySchedule(DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string Status, string? Teacher);
public sealed record PentaQueryCalendarScope(string TimeZone, string From, string Through);

/// <summary>Host-owned local-date interpretation. No model UTC arithmetic or
/// caller-supplied time zone; a bounded calendar read never changes scheduling.</summary>
public static class PentaQueryCalendar
{
    public static bool Date(string? text, out DateOnly value) => DateOnly.TryParseExact(text, "yyyy-MM-dd",
        CultureInfo.InvariantCulture, DateTimeStyles.None, out value) && value.Year < 9999;
    public static bool Status(string text) => text is "Scheduled" or "InProgress" or "Completed" or "Cancelled" or "Rescheduled";
    public static bool ValidFilter(string key, string value) => key switch {
        "name" or "teacher" => true,
        "from" => value is "Today" or "Tomorrow" or "Yesterday" or "Next7Days" || Date(value, out _),
        "through" => Date(value, out _), "status" => Status(value), _ => false };
    public static bool ValidRange(IReadOnlyDictionary<string, string> filters) {
        if (!filters.TryGetValue("through", out var through)) return true;
        return filters.TryGetValue("from", out var from) && Date(from, out var start) && Date(through, out var end) &&
            end >= start && end.DayNumber - start.DayNumber < 31;
    }
    public static bool Window(string? zoneId, IReadOnlyDictionary<string, string> filters, DateTimeOffset now,
        out PentaQueryCalendarScope? scope, out DateTime fromUtc, out DateTime toUtc) {
        scope = null; fromUtc = toUtc = default;
        try {
            if (string.IsNullOrWhiteSpace(zoneId) || !ValidRange(filters)) return false;
            var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
            var displayId = TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var iana) ? iana : zone.Id;
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
            var from = filters.GetValueOrDefault("from") ?? "Next7Days";
            DateOnly start;
            if (!Date(from, out start)) start = from switch {
                "Today" or "Next7Days" => today, "Tomorrow" => today.AddDays(1), "Yesterday" => today.AddDays(-1),
                _ => throw new ArgumentException("Unsupported date expression.") };
            var end = filters.TryGetValue("through", out var through) && Date(through, out var parsed)
                ? parsed : from == "Next7Days" ? start.AddDays(6) : start;
            if (end < start || end.DayNumber - start.DayNumber >= 31 || end.Year >= 9999) return false;
            var localStart = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            var localEnd = end.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(localStart) || zone.IsAmbiguousTime(localStart) ||
                zone.IsInvalidTime(localEnd) || zone.IsAmbiguousTime(localEnd)) return false;
            fromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, zone); toUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, zone);
            scope = new(displayId!, start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            return true;
        } catch (Exception error) when (error is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException) { return false; }
    }
    public static bool ValidScope(PentaQueryCalendarScope? scope) => scope is not null && scope.TimeZone is { Length: > 0 and <= 100 } &&
        Date(scope.From, out _) && Date(scope.Through, out _) &&
        Window(scope.TimeZone, new Dictionary<string,string> { ["from"] = scope.From, ["through"] = scope.Through }, DateTimeOffset.UnixEpoch, out _, out _, out _);
    public static bool ValidSchedule(PentaQuerySchedule? row, PentaQueryCalendarScope? scope) =>
        row is not null && ValidScope(scope) && row.StartUtc.Kind == DateTimeKind.Utc && row.EndUtc.Kind == DateTimeKind.Utc &&
        row.EndUtc > row.StartUtc && row.DeliveryMode is "InPerson" or "Online" or "Hybrid" && Status(row.Status) &&
        (row.Teacher is null || row.Teacher is { Length: > 0 and <= 240 } && !row.Teacher.Any(char.IsControl)) &&
        Window(scope!.TimeZone, new Dictionary<string,string> { ["from"] = scope.From, ["through"] = scope.Through }, DateTimeOffset.UnixEpoch, out _, out var from, out var to) &&
        row.StartUtc >= from && row.StartUtc < to;
}
