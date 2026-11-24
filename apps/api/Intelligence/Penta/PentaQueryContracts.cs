using System.Text.Json.Serialization;
using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

// Entity-aware reads are a separate version. Legacy learner/fee receipts and
// task drafts retain their original wire contracts and protection purposes.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PentaQueryPlan([property: JsonRequired, JsonPropertyName("contract")] string Contract,
    [property: JsonRequired, JsonPropertyName("kind")] string Kind, [property: JsonRequired, JsonPropertyName("entity")] string? Entity,
    [property: JsonRequired, JsonPropertyName("filters")] Dictionary<string, string> Filters, [property: JsonRequired, JsonPropertyName("position")] int? Position);
public sealed record PentaQueryContext(string? Entity, Dictionary<string, string> Filters, Guid[] ResultIds)
{
    public static PentaQueryContext Empty => new(null, [], []);
}
public sealed record PentaQueryRecord(Guid Id, string Label, string? Code, bool Active,
    string[] Subjects, string SourcePath,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] MiniExactBalance[]? Balances = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] PentaQuerySchedule? Schedule = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] PentaQueryAttendance? Attendance = null);
public sealed record PentaQueryResult(string Entity, PentaQueryRecord[] Records, bool HasMore, DateTime AsOfUtc,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? BalanceScope = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] PentaQueryCalendarScope? CalendarScope = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AttendanceScope = null);
public sealed record PentaQueryDecision(string Kind, string Message, PentaQueryContext Context, PentaQueryResult? Result = null);
public sealed record PentaQueryTurnReceipt(Guid ConversationId, Guid RequestId, long Version, string Kind,
    string Message, PentaQueryContext Context, PentaQueryResult? Result, string InputText,
    string Protocol = PentaQueryContract.Version);
public sealed record PentaQueryHistory(Guid ConversationId, long Version, DateTime ExpiresAtUtc,
    IReadOnlyList<PentaQueryTurnReceipt> Turns, Guid? PendingRequestId, bool HasMore);

public static class PentaQueryContract
{
    public const string Version = "academy-query.v1";
    public const string BalanceScope = "All outstanding fees for each student; subject filters select students, not course-specific invoices.";
    public const string AttendanceScope = "Recorded student attendance by class start date. Missing records are unmarked, not absent. Marked time is an update timestamp, not arrival.";
    internal static JsonElement Request(string text, PentaQueryContext context, IReadOnlySet<string> tools) =>
        JsonSerializer.SerializeToElement(new { contract = Version, text,
            context = new { entity = context.Entity, filters = context.Filters,
                result_labels = Enumerable.Range(1, context.ResultIds.Length).Select(index => "Result " + index).ToArray() },
            tools = PentaQueryAccess.Describe(tools) });
    // Adding a name here does not implement a domain. Each descriptor below
    // has a matching typed host reader and a source controller/module policy.
    public static readonly IReadOnlyDictionary<string, string> Controllers =
        new Dictionary<string, string>(StringComparer.Ordinal) {
            ["teacher"] = "TeachersController", ["student"] = "StudentsController", ["batch"] = "BatchesController",
            ["student-fees"] = "InvoicesController", ["session"] = "ClassSessionsController", ["attendance"] = "AttendanceController" };
    public static string[] Fields(string entity) => entity switch {
        "teacher" => ["name", "subject", "status"], "student-fees" => ["name", "subject", "balance_status"],
        "session" => ["name", "teacher", "from", "through", "status"],
        "attendance" => ["name", "batch", "teacher", "from", "through", "status"], _ => ["name", "status"] };
    public static bool SameReferences(string? first, string? second) => first is not null && second is not null &&
        (first == second || first is "student" or "student-fees" && second is "student" or "student-fees");
    public static string Tool(string entity, string kind) => $"AcademyDesk.{entity}.{kind}";
    public static bool ValidContext(PentaQueryContext? value) => value is not null &&
        ValidFilters(value.Entity, value.Filters) && value.ResultIds is { Length: <= 10 } &&
        value.ResultIds.All(id => id != Guid.Empty) && value.ResultIds.Distinct().Count() == value.ResultIds.Length &&
        (value.Entity is not null || value.ResultIds.Length == 0);
    public static bool ValidFilters(string? entity, Dictionary<string, string>? values) => values is { Count: <= 6 } &&
        (entity is null ? values.Count == 0 : Controllers.ContainsKey(entity)) && values.All(pair =>
            pair.Value is { Length: > 0 and <= 100 } && !string.IsNullOrWhiteSpace(pair.Value) && !pair.Value.Any(char.IsControl) &&
            (entity == "attendance" ? PentaQueryAttendance.ValidFilter(pair.Key, pair.Value) : entity == "session" ? PentaQueryCalendar.ValidFilter(pair.Key, pair.Value) : pair.Key == "name" || entity is "teacher" or "student-fees" && pair.Key == "subject" ||
             entity == "student-fees" && pair.Key == "balance_status" && pair.Value is "Pending" or "Clear" ||
             entity != "student-fees" && pair.Key == "status" && pair.Value is "Active" or "Inactive")) &&
        (entity is not ("session" or "attendance") || PentaQueryCalendar.ValidRange(values));
    public static bool ValidPlan(PentaQueryPlan? value) => value is not null && value.Contract == Version &&
        value.Kind is "search" or "open" or "clarify" or "unsupported" && ValidFilters(value.Entity, value.Filters) &&
        (value.Kind == "search" ? value.Entity is not null && value.Position is null :
         value.Kind == "open" ? value.Entity is not null && value.Filters.Count == 0 && value.Position is >= 1 and <= 10 :
         value.Filters.Count == 0 && value.Position is null);
    public static string Path(string entity, Guid id) => entity switch {
        "teacher" => $"/teacher-profile?teacherId={id:D}",
        "student" or "student-fees" => $"/student-management?studentId={id:D}",
        "batch" => "/batch-setup", "session" => "/schedule", "attendance" => "/attendance", _ => throw new ArgumentException("Unknown read entity.") };
    public static bool ValidReceipt(PentaQueryTurnReceipt? value, Guid session, Guid request, long version) =>
        value is not null && value.Protocol == Version && value.ConversationId == session && value.RequestId == request &&
        value.Version == version && version > 0 && value.InputText is { Length: > 0 and <= 2000 } &&
        value.Message is { Length: > 0 and <= 500 } && ValidContext(value.Context) &&
        value.Kind is "RESULT" or "ERROR" or "UNSUPPORTED" or "CLARIFICATION_REQUIRED" &&
        (value.Result is null ? value.Kind != "RESULT" : value.Kind == "RESULT" &&
         value.Result.Entity == value.Context.Entity && value.Result.Records is { Length: <= 10 } &&
         value.Result.AsOfUtc.Kind == DateTimeKind.Utc &&
         (value.Result.Entity == "student-fees" ? value.Result.BalanceScope == BalanceScope : value.Result.BalanceScope is null) &&
         (value.Result.Entity == "attendance" ? value.Result.AttendanceScope == AttendanceScope : value.Result.AttendanceScope is null) &&
         (value.Result.Entity is "session" or "attendance" ? PentaQueryCalendar.ValidScope(value.Result.CalendarScope) : value.Result.CalendarScope is null) &&
         (value.Result.Entity is not ("session" or "attendance") || value.Context.Filters.GetValueOrDefault("from") == value.Result.CalendarScope!.From &&
             value.Context.Filters.GetValueOrDefault("through") == value.Result.CalendarScope.Through) &&
         value.Result.Records.All(row =>
             row.Id != Guid.Empty && row.Label is { Length: > 0 and <= 240 } && row.Subjects is not null &&
             row.Subjects.Length <= (value.Result.Entity == "student-fees" ? 5 : 1) &&
             row.Subjects.All(x => x.Length <= 200) && row.Code is null or { Length: <= 100 } &&
             (value.Result.Entity == "student-fees" ? PentaQueryMoney.Valid(row.Balances) : row.Balances is null) &&
             (value.Result.Entity is "session" or "attendance" ? PentaQueryCalendar.ValidSchedule(row.Schedule, value.Result.CalendarScope) : row.Schedule is null) &&
             (value.Result.Entity == "attendance" ? PentaQueryAttendance.Valid(row.Attendance) : row.Attendance is null) &&
             (value.Result.Entity != "session" || row.Active == (row.Schedule!.Status is "Scheduled" or "InProgress")) &&
             row.SourcePath == Path(value.Result.Entity, row.Id)) &&
         value.Result.Records.Select(x => x.Id).SequenceEqual(value.Context.ResultIds));
}
