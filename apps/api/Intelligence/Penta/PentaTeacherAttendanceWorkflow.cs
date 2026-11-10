using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaTeacherAttendanceReceipt(Guid Id, Guid AuditId, Guid AttendanceId, DateTime MarkedAtUtc, int InAppQueued);
public sealed record PentaTeacherAttendanceDraft(Guid RequestId, Guid ClassSessionId, Guid BatchId, DateTime SessionStartUtc,
    string Status, DateTime ExpiresAtUtc, TeacherAttendanceChange Change, string PayloadHash, PentaTeacherAttendanceReceipt? Receipt);

public sealed class PentaTeacherAttendanceWorkflow(AcademyDeskDbContext db, TeacherAttendanceService attendance, PentaPilotPolicy policy,
    IConfiguration configuration, IHostEnvironment environment, IDataProtectionProvider protection, TimeProvider clock)
{
    internal const string ConfirmIntent = "ConfirmTeacherAttendanceAndQueueInApp";
    private const string Purpose = "academy.assigned-teacher-attendance.v1";
    private const string AuditAction = "PentaTeacherAttendanceMarked";
    private sealed record Slot(Guid Id, Guid BatchId, DateTime StartUtc, DateTime EndUtc, string Status);
    private sealed record Body(string RequestHash, Guid TeacherId, string Stamp, Slot Session, TeacherAttendanceChange Change,
        string PreviousHash, PentaTeacherAttendanceReceipt? Receipt = null);

    public bool Enabled => environment.IsEnvironment("Testing") && policy.IsAvailable && configuration.GetValue<bool>("Penta:TeacherAttendanceEnabled");
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    internal Task<TeacherAttendanceOutcome<PentaTeacherAttendanceDraft>> RunAsync(ClaimsPrincipal principal, Guid sessionId, Guid requestId,
        string? payload, string? confirmationHash, CancellationToken token)
    {
        if (!Enabled) return Task.FromResult(new TeacherAttendanceOutcome<PentaTeacherAttendanceDraft>(404));
        if (sessionId == Guid.Empty || requestId == Guid.Empty) return Task.FromResult(new TeacherAttendanceOutcome<PentaTeacherAttendanceDraft>(400));
        return attendance.InAssignedSessionAsync<PentaTeacherAttendanceDraft>(principal, sessionId, async (actor, session) =>
        {
            var id = new Guid(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
                new { Purpose, actor.AcademyId, actor.UserId, sessionId, requestId })).AsSpan(0, 16));
            var row = await db.PentaOnboardingDrafts.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == actor.AcademyId &&
                x.ActorUserId == actor.UserId && x.SessionId == id, token);
            if (row is null)
                return payload is null ? new(404) : await PrepareAsync(actor, session, requestId, id, payload, token);

            var body = Open(row);
            if (body is null || body.TeacherId != actor.TeacherId || body.Stamp != actor.Stamp ||
                payload is not null && !PentaOperationalPayload.MatchesDigest(payload, body.RequestHash) ||
                confirmationHash is not null && confirmationHash != Digest(new { body.Session, body.Change })) return Changed();
            var now = Now;
            if (body.Receipt is null && row.ExpiresAtUtc <= now ||
                body.Receipt is not null && row.CreatedAtUtc.AddHours(24) <= now) return new(410);

            // Re-check roster membership before exposing even an earlier receipt.
            var planned = await attendance.PlanAsync(session, [new(body.Change.StudentId, body.Change.Status, body.Change.Notes)], false, token);
            if (planned.Value is not { } plan) return Changed();
            if (body.Receipt is { } receipt)
                return await ReceiptExistsAsync(actor, session.Id, body.Change.StudentId, receipt, token) ? View(200, requestId, row, body) : Changed();
            if (body.Session != SessionSlot(session) || body.PreviousHash != Digest(plan.Previous)) return Changed();
            if (confirmationHash is null) return View(200, requestId, row, body);

            var saved = await attendance.SaveAsync(actor, session, plan, AuditAction, token);
            row.Status = "Succeeded";
            row.UpdatedAtUtc = Now;
            body = body with { Receipt = new(Guid.NewGuid(), saved.AuditId, saved.RecordIds[0], saved.MarkedAtUtc, 1) };
            row.ProtectedBody = Protect(row, body);
            await db.SaveChangesAsync(token);
            return row.ExpiresAtUtc <= Now ? new(410) : View(201, requestId, row, body);
        }, token);
    }

    private async Task<TeacherAttendanceOutcome<PentaTeacherAttendanceDraft>> PrepareAsync(TeacherAttendanceActor actor, ClassSession session,
        Guid requestId, Guid id, string payload, CancellationToken token)
    {
        var request = ReadPayload(payload);
        if (request is null) return new(400, Error: "Enter one student, Present or Absent, and optional attendance notes.");
        var today = Now.Date;
        var recent = db.PentaOnboardingDrafts.Where(x => x.AcademyId == actor.AcademyId && x.CreatedAtUtc >= today);
        if (await recent.CountAsync(token) >= 100 || await recent.CountAsync(x => x.ActorUserId == actor.UserId, token) >= 20)
            return new(429, Error: "The daily attendance draft limit was reached.");
        var planned = await attendance.PlanAsync(session, [request], false, token);
        if (planned.Value is not { } plan) return new(planned.Status, Error: planned.Error);
        var now = Now;
        var row = new PentaOnboardingDraft { Id = id, AcademyId = actor.AcademyId, ActorUserId = actor.UserId, SessionId = id,
            ContextVersion = 0, Revision = 1, Status = "Prepared", CreatedAtUtc = now, ExpiresAtUtc = now.AddMinutes(10), ProtectedBody = "" };
        var body = new Body(PentaOperationalPayload.Digest(payload), actor.TeacherId, actor.Stamp, SessionSlot(session),
            plan.Changes[0], Digest(plan.Previous));
        row.ProtectedBody = Protect(row, body);

        // A purpose-bound anchor lets the existing draft store enforce actor and
        // tenant ownership without enabling teacher access to general Mini sessions.
        db.PentaMiniSessions.Add(new PentaMiniSession { Id = id, AcademyId = actor.AcademyId, ActorUserId = actor.UserId,
            CreatedAtUtc = now, ExpiresAtUtc = now.AddHours(24), ProtectedState = protection.CreateProtector(Purpose, "anchor").Protect(id.ToString("D")) });
        db.PentaOnboardingDrafts.Add(row);
        db.AuditLogs.Add(new AuditLog { AcademyId = actor.AcademyId, ActorUserId = actor.UserId, EntityId = session.Id,
            EntityType = "ClassSession", Action = "PentaTeacherAttendancePrepared", OccurredAtUtc = now });
        await db.SaveChangesAsync(token);
        return View(201, requestId, row, body);
    }

    private static TeacherBulkAttendanceItem? ReadPayload(string payload)
    {
        if (payload.Length > 4000) return null;
        try
        {
            using var parsed = JsonDocument.Parse(PentaOperationalPayload.Canonicalize(payload));
            var root = parsed.RootElement;
            if (root.GetProperty("tool_key").GetString() != "AcademyDesk.TeacherAttendance.Mark" ||
                root.GetProperty("tool_revision").GetInt64() != 1) return null;
            var fields = root.GetProperty("fields");
            if (fields.EnumerateObject().Any(x => x.Name is not ("StudentId" or "Status" or "Notes"))) return null;
            var student = Field("StudentId", "reference");
            var status = Field("Status", "text");
            var notes = Field("Notes", "text")?.Trim();
            return Guid.TryParseExact(student, "D", out var studentId) && studentId != Guid.Empty && studentId.ToString("D") == student &&
                status is "Present" or "Absent" && (notes is null || notes.Length <= 500 && !notes.Any(char.IsControl))
                ? new(studentId, status, notes) : null;

            string? Field(string name, string kind)
            {
                if (!fields.TryGetProperty(name, out var field)) return null;
                if (field.GetProperty("kind").GetString() != kind) throw new ArgumentException();
                return field.GetProperty("value").GetString();
            }
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or FormatException) { return null; }
    }

    private IDataProtector Protector(PentaOnboardingDraft row) => protection.CreateProtector(Purpose, row.Id.ToString("D"),
        row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.SessionId.ToString("D"),
        FormattableString.Invariant($"{row.ContextVersion}:{row.Revision}:{row.Status}:{row.CreatedAtUtc.Ticks}:{row.ExpiresAtUtc.Ticks}"));
    private string Protect(PentaOnboardingDraft row, Body body) => Protector(row).Protect(JsonSerializer.Serialize(body));
    private Body? Open(PentaOnboardingDraft row)
    {
        try
        {
            var body = JsonSerializer.Deserialize<Body>(Protector(row).Unprotect(row.ProtectedBody));
            return row.Status switch
            {
                "Prepared" when body is { Receipt: null } => body,
                "Succeeded" when body is { Receipt: not null } => body,
                _ => null
            };
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException) { return null; }
    }

    private async Task<bool> ReceiptExistsAsync(TeacherAttendanceActor actor, Guid session, Guid student, PentaTeacherAttendanceReceipt receipt,
        CancellationToken token) =>
        await db.AttendanceRecords.AsNoTracking().AnyAsync(x => x.Id == receipt.AttendanceId && x.AcademyId == actor.AcademyId &&
            x.ClassSessionId == session && x.StudentId == student, token) &&
        await db.AuditLogs.AsNoTracking().AnyAsync(x => x.Id == receipt.AuditId && x.AcademyId == actor.AcademyId &&
            x.ActorUserId == actor.UserId && x.EntityType == "ClassSession" && x.EntityId == session && x.Action == AuditAction, token);

    private static Slot SessionSlot(ClassSession session) => new(session.Id, session.BatchId, session.StartUtc, session.EndUtc, session.Status);
    private static string Digest<T>(T value) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static TeacherAttendanceOutcome<PentaTeacherAttendanceDraft> View(int code, Guid requestId, PentaOnboardingDraft row, Body body) =>
        new(code, new(requestId, body.Session.Id, body.Session.BatchId, body.Session.StartUtc, row.Status, row.ExpiresAtUtc,
            body.Change, Digest(new { body.Session, body.Change }), body.Receipt));
    private static TeacherAttendanceOutcome<PentaTeacherAttendanceDraft> Changed() =>
        new(409, Error: "The reviewed attendance could not be verified. Review the current attendance before preparing a new draft.");
}
