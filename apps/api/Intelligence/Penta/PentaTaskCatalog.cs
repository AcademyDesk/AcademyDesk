using System.Collections.ObjectModel;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaTaskField(string Key, string Kind, bool Required, int MaximumLength = 2000,
    string? ReferenceEntity = null, string[]? Values = null, long? Minimum = null, long? Maximum = null);
internal sealed record PentaTaskIssue(string? Field, string Code, string Message);
internal sealed record PentaTaskDefinition(string Key, string Entity, string Source, string Binding,
    IReadOnlyList<PentaTaskField> Fields, IReadOnlyDictionary<string, string> Outputs);

/// <summary>
/// Host-owned draft descriptors over existing operational payloads. Not a permission
/// grant, live model catalog, dispatch registry or replacement for domain validation.
/// An authenticated integration must filter these using CURRENT host policy and
/// reauthorize at preparation/commit. Unbound operations must never be dispatched.
/// </summary>
internal static class PentaTaskCatalog
{
    internal const string BatchCreate = "AcademyDesk.Batch.Create";
    internal const string ClassCreate = "AcademyDesk.ClassSession.Create";
    internal const string ClassReschedule = "AcademyDesk.ClassSession.Reschedule";

    internal static IReadOnlyList<string> Keys { get; } = Array.AsReadOnly(new[] {
        PentaOnboardingFields.Tool, PentaTeacherCreationFields.Tool, BatchCreate,
        PentaEnrollmentFields.CreateTool, PentaEnrollmentFields.UpdateTool, PentaEnrollmentFields.TransferTool,
        ClassCreate, ClassReschedule, PentaPaymentFields.Tool });

    internal static string Payload(string key, IReadOnlyDictionary<string, JsonElement> fields) =>
        PentaOperationalPayload.Canonicalize(JsonSerializer.Serialize(new { tool_key = key, tool_revision = 1, fields }));

    internal static PentaTaskDefinition Describe(string key, IReadOnlyDictionary<string, JsonElement> fields, DateOnly today)
    {
        PentaTaskField T(string name, int length = 2000, bool required = false, params string[] values) =>
            new(name, "text", required, length, Values: values.Length == 0 ? null : values);
        PentaTaskField R(string name, string entity, bool required = false) => new(name, "reference", required, 36, entity);
        PentaTaskField D(string name, bool required = false) => new(name, "date", required, 10);
        PentaTaskField I(string name, bool required = false, long? minimum = null, long? maximum = null) =>
            new(name, "integer", required, Minimum: minimum, Maximum: maximum);
        PentaTaskDefinition Definition(string entity, string source, string binding, PentaTaskField[] specs,
            params (string Key, string Entity)[] outputs) => new(key, entity, source, binding, Array.AsReadOnly(specs),
                new ReadOnlyDictionary<string, string>(outputs.ToDictionary(x => x.Key, x => x.Entity, StringComparer.Ordinal)));

        if (key == PentaOnboardingFields.Tool)
        {
            var payload = Payload(key, fields);
            if (!PentaOnboardingFields.TryRead(payload, out var request)) throw Invalid();
            var specs = ExistingFields(PentaOnboardingFields.Describe(payload, today, []));
            // An invalid future birth date cannot establish the child's age or
            // trigger a useful guardian clarification. Ask to correct DOB first.
            if (request!.DateOfBirth > today)
                specs = specs.Select(field => field.Key.StartsWith("Parent", StringComparison.Ordinal)
                    ? field with { Required = false } : field).ToArray();
            return Definition("student", "Infrastructure/StudentOnboardingService.cs", "protected-adapter",
                specs, ("StudentId", "student"));
        }
        if (key == PentaTeacherCreationFields.Tool)
            return Definition("teacher", "Infrastructure/TeacherCreationService.cs", "protected-adapter",
                ExistingFields(PentaTeacherCreationFields.Describe([])), ("TeacherId", "teacher"));
        if (key == BatchCreate)
            return Definition("batch", "Controllers/BatchesController.cs", "draft-only",
                [T("Name", 200, true), T("BatchCode", 50), R("CourseId", "course", true), R("TeacherId", "teacher"),
                 R("BranchId", "branch"), I("Capacity", true, 1, 1000), I("WaitlistCapacity", minimum: 0, maximum: 1000),
                 T("DeliveryMode", 30, false, "InPerson", "Online", "Hybrid"), T("MeetingPattern", 240), T("RoomName", 120),
                 T("EnrollmentStatus", 30, false, "Open", "Waitlist", "Closed"), T("AdminNotes"), D("StartDate"), D("EndDate"),
                 T("ClassType", 30), I("SessionMinutes"), I("SessionsPerWeek"), T("MeetingDaysJson", 500),
                 T("MeetingLink", 1000, Text(fields, "DeliveryMode") is "Online" or "Hybrid")], ("BatchId", "batch"));
        if (key == PentaEnrollmentFields.CreateTool)
            return Definition("enrollment", "Infrastructure/EnrollmentMutationOperations.cs", "protected-adapter",
                [R("StudentId", "student", true), R("BatchId", "batch", true), D("StartDate"),
                 T("Status", 30, false, "Active", "Waitlisted")], ("EnrollmentId", "enrollment"));
        if (key == PentaEnrollmentFields.UpdateTool)
            return Definition("enrollment", "Infrastructure/EnrollmentMutationOperations.cs", "protected-adapter",
                [R("EnrollmentId", "enrollment", true), T("Status", 30, true, "Active", "Waitlisted", "Completed", "Withdrawn", "Cancelled", "Paused"),
                 D("EndDate"), T("LifecycleReason", 1000, Text(fields, "Status") is { } status && status != "Active")]);
        if (key == PentaEnrollmentFields.TransferTool)
            return Definition("enrollment", "Infrastructure/EnrollmentMutationOperations.cs", "protected-adapter",
                [R("EnrollmentId", "enrollment", true), R("TargetBatchId", "batch", true), D("TransferDate")]);
        if (key is ClassCreate or ClassReschedule)
            return Definition("class-session", "Infrastructure/ClassSessionSchedulingService.cs", "protected-adapter",
                [R(key == ClassCreate ? "BatchId" : "SessionId", key == ClassCreate ? "batch" : "class-session", true),
                 ..(key == ClassCreate ? new[] { R("TeacherId", "teacher"), R("BranchId", "branch") } : []),
                 T("StartUtc", 40, true), T("EndUtc", 40, true), T("DeliveryMode", 30, true, "Offline", "InPerson", "Online", "Hybrid"),
                 T("RoomName", 120), T("MeetingLink", 120, Text(fields, "DeliveryMode") is "Online" or "Hybrid")], ("SessionId", "class-session"));
        if (key == PentaPaymentFields.Tool)
            return Definition("payment", "Infrastructure/InvoicePaymentCollection.cs", "protected-adapter",
                [R("InvoiceId", "invoice", true), new("Amount", "decimal", true, 24),
                 T("Method", 30, true, "Cash", "Offline", "UPI", "BankTransfer", "Card", "Cheque")], ("PaymentId", "payment"));
        throw Invalid();
    }

    // Reuse the actual existing onboarding/teacher field declarations, including
    // conditional minor/guardian requirements; do not maintain a second form schema.
    private static PentaTaskField[] ExistingFields(object[] descriptors) => descriptors.Select(value => {
        var field = JsonSerializer.SerializeToElement(value);
        var key = field.GetProperty("key").GetString()!;
        return new PentaTaskField(key, field.GetProperty("kind").GetString()!, field.GetProperty("required").GetBoolean(),
            field.GetProperty("max_length").GetInt32(), key == "BranchId" ? "branch" : null);
    }).ToArray();

    internal static IReadOnlyList<PentaTaskIssue> Inspect(PentaTaskDefinition definition,
        IReadOnlyDictionary<string, JsonElement> fields, IReadOnlySet<string> boundFields, DateOnly today)
    {
        var issues = new List<PentaTaskIssue>();
        foreach (var spec in definition.Fields)
        {
            if (!fields.TryGetValue(spec.Key, out var field)) {
                if (spec.Required && !boundFields.Contains(spec.Key)) issues.Add(new(spec.Key, "required", "This detail is required."));
                continue;
            }
            if (field.GetProperty("kind").GetString() != spec.Kind) throw Invalid();
            var value = field.GetProperty("value");
            if (value.ValueKind == JsonValueKind.String) {
                var text = value.GetString()!;
                if (text.Length > spec.MaximumLength || text.Any(c => char.IsControl(c) && c is not '\n' and not '\r' and not '\t')) throw Invalid();
                if (spec.Required && string.IsNullOrWhiteSpace(text)) issues.Add(new(spec.Key, "required", "This detail is required."));
                else if (spec.Values is not null && !spec.Values.Contains(text, StringComparer.Ordinal))
                    issues.Add(new(spec.Key, "invalid", "Choose a supported value."));
                if (spec.ReferenceEntity is not null && (!Guid.TryParseExact(text, "D", out var id) || id == Guid.Empty || text != id.ToString("D"))) throw Invalid();
            }
            if (spec.Kind == "integer" && (spec.Minimum is { } minimum && value.GetInt64() < minimum ||
                spec.Maximum is { } maximum && value.GetInt64() > maximum)) issues.Add(new(spec.Key, "invalid", "The value is outside the supported range."));
        }
        if (definition.Key == PentaOnboardingFields.Tool && PentaOnboardingFields.TryRead(Payload(definition.Key, fields), out var request) &&
            StudentOnboardingRules.Validate(request!, today) is { } error && issues.Count == 0)
            issues.Add(new(null, "business_rule", error));
        return issues.AsReadOnly();
    }

    internal static string? Text(IReadOnlyDictionary<string, JsonElement> fields, string key) =>
        fields.TryGetValue(key, out var value) && value.GetProperty("kind").GetString() == "text" ? value.GetProperty("value").GetString() : null;
    internal static ArgumentException Invalid() => new("The PENTA task draft could not be validated.");
}
