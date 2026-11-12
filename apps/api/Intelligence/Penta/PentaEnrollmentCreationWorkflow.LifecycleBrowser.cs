using System.Data;
using System.Security.Claims;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaEnrollmentLifecycleRow(Guid SourceId, Guid StudentId, string StudentName, Guid BatchId,
    string BatchName, string Status, DateOnly StartDate, DateOnly? EndDate, string SourcePath);
public sealed record PentaEnrollmentLifecycleSearch(string Tool, string Capability, Guid AcademyId,
    DateTime AsOfUtc, int MaxRows, bool HasMore, IReadOnlyList<PentaEnrollmentLifecycleRow> Rows);

public sealed partial class PentaEnrollmentCreationWorkflow
{
    public bool LifecycleBrowserEnabled => Enabled && LifecycleEnabled && configuration.GetValue<bool>("Penta:EnrollmentLifecycleBrowserContractsEnabled");

    internal Task<PentaConversationOutcome> LifecycleBrowserReadinessAsync(ClaimsPrincipal user, Guid academy, CancellationToken token) =>
        LifecycleBrowserAuthorityAsync(user, academy, (_, _) => Task.FromResult(new PentaConversationOutcome(200,
            new { protocol = "enrollment-lifecycle-browser.v1", requiresApproval = true,
                tools = new[] { PentaEnrollmentFields.UpdateTool, PentaEnrollmentFields.TransferTool } })), token);

    internal static bool LifecycleSearchQuery(string? input, out string query)
    {
        query = input?.Trim() ?? "";
        return query.Length is >= 2 and <= 80 && !query.Any(char.IsControl);
    }

    internal async Task<PentaConversationOutcome> LifecycleBrowserSearchAsync(ClaimsPrincipal user, Guid academy, string? input, CancellationToken token)
    {
        if (!LifecycleBrowserEnabled) return new(404);
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out _) ||
            string.IsNullOrEmpty(user.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType)) ||
            !await policy.AllowsAsync(user, academy, token)) return new(403);
        if (!LifecycleSearchQuery(input, out var query)) return new(400, Error: "Enter 2 to 80 searchable characters.");
        return await LifecycleBrowserAuthorityAsync(user, academy, async (actor, _) =>
        {
            var records = await (from enrollment in db.Enrollments.AsNoTracking()
                join student in db.Students.AsNoTracking() on enrollment.StudentId equals student.Id
                join batch in db.Batches.AsNoTracking() on enrollment.BatchId equals batch.Id
                where enrollment.AcademyId == academy && student.AcademyId == academy && batch.AcademyId == academy &&
                    (student.FirstName.Contains(query) || student.LastName.Contains(query) || batch.Name.Contains(query))
                orderby student.FirstName, student.LastName, batch.Name, enrollment.Id
                select new PentaEnrollmentLifecycleRow(enrollment.Id, student.Id, student.FirstName + " " + student.LastName,
                    batch.Id, batch.Name, enrollment.Status, enrollment.StartDate, enrollment.EndDate, "/enrollments"))
                .Take(11).ToListAsync(token);
            var now = Now;
            db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityType = "Academy", EntityId = academy,
                Action = "PentaEnrollmentLifecycleSearched", OccurredAtUtc = now, MetadataJson = "{\"tool\":\"enrollment.search.v1\"}" });
            await db.SaveChangesAsync(token);
            return new(200, new PentaEnrollmentLifecycleSearch("enrollment.search.v1", "executor", academy, now, 10, records.Count > 10, records.Take(10).ToArray()));
        }, token);
    }

    internal Task<PentaConversationOutcome> LifecycleBrowserStartAsync(ClaimsPrincipal user, Guid academy, Guid request, bool create, CancellationToken token)
    {
        if (!LifecycleBrowserEnabled) return Task.FromResult(new PentaConversationOutcome(404));
        if (request == Guid.Empty) return Task.FromResult(new PentaConversationOutcome(400));
        return LifecycleBrowserAuthorityAsync(user, academy, async (actor, stamp) =>
        {
            var id = PentaEnrollmentLifecycleBrowserSession.Key(academy, actor, request);
            var session = await db.PentaMiniSessions.AsNoTracking().SingleOrDefaultAsync(x =>
                x.Id == id && x.AcademyId == academy && x.ActorUserId == actor, token);
            var binding = new PentaEnrollmentLifecycleBrowserSession(protection);
            if (session is not null)
            {
                if (!binding.Matches(session, stamp)) return new(404);
                return session.ExpiresAtUtc > Now ? BrowserSessionView(200, session) : new(410);
            }
            if (!create) return new(404);
            var now = Now;
            var recent = db.PentaMiniSessions.AsNoTracking().Where(x => x.AcademyId == academy && x.CreatedAtUtc >= now.Date);
            if (await recent.CountAsync(token) >= 100 || await recent.CountAsync(x => x.ActorUserId == actor, token) >= 20) return new(429);
            session = new PentaMiniSession { Id = id, AcademyId = academy, ActorUserId = actor,
                CreatedAtUtc = now, ExpiresAtUtc = now.AddHours(24), ProtectedState = "" };
            session.ProtectedOnboardingState = binding.Seal(session, stamp);
            db.PentaMiniSessions.Add(session);
            db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityId = id,
                EntityType = "PentaEnrollmentLifecycleConversation", Action = "PentaEnrollmentLifecycleConversationStarted", OccurredAtUtc = now });
            await db.SaveChangesAsync(token);
            if (session.ExpiresAtUtc <= Now) return new(410);
            return BrowserSessionView(201, session);
        }, token);
    }

    private static bool LifecyclePayload(string? payload) => PentaEnrollmentFields.TryUpdate(payload, out _, out _) || PentaEnrollmentFields.TryTransfer(payload, out _, out _);

    internal Task<PentaEnrollmentDraftOutcome> LifecycleBrowserPrepareAsync(ClaimsPrincipal user, Guid academy, Guid session,
        Guid request, long context, string payload, CancellationToken token) => LifecycleBrowserEnabled
            ? BrowserDraft(RunAsync(Action.Prepare, user, academy, session, request, null, context, payload, token, browser: true, lifecycleBrowser: true))
            : Task.FromResult(new PentaEnrollmentDraftOutcome(404));
    internal Task<PentaEnrollmentDraftOutcome> LifecycleBrowserReadAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request, CancellationToken token) => LifecycleBrowserEnabled
            ? BrowserDraft(RunAsync(Action.Read, user, academy, session, request, null, null, null, token, browser: true, lifecycleBrowser: true))
            : Task.FromResult(new PentaEnrollmentDraftOutcome(404));
    internal Task<PentaEnrollmentDraftOutcome> LifecycleBrowserApproveAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaEnrollmentBinding binding, string intent, CancellationToken token) => LifecycleBrowserEnabled
            ? BrowserDraft(RunAsync(Action.Approve, user, academy, session, request, binding, null, null, token, intent, browser: true, lifecycleBrowser: true))
            : Task.FromResult(new PentaEnrollmentDraftOutcome(404));
    internal Task<PentaEnrollmentDraftOutcome> LifecycleBrowserExecuteAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaEnrollmentBinding binding, CancellationToken token) => LifecycleBrowserEnabled
            ? BrowserDraft(RunAsync(Action.Execute, user, academy, session, request, binding, null, null, token, browser: true, lifecycleBrowser: true))
            : Task.FromResult(new PentaEnrollmentDraftOutcome(404));

    private async Task<PentaConversationOutcome> LifecycleBrowserAuthorityAsync(ClaimsPrincipal principal, Guid academy,
        Func<Guid, string, Task<PentaConversationOutcome>> run, CancellationToken token)
    {
        if (!LifecycleBrowserEnabled) return new(404);
        var stamp = principal.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        if (string.IsNullOrEmpty(stamp) || !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ||
            !await policy.AllowsAsync(principal, academy, token)) return new(403);
        var original = AcademyIdentityTransaction.Validate(db, identity);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        if (await enrollments.CurrentHostRefusalAsync(principal, academy, stamp, token) is { } refusal) return new(refusal);
        if (!LifecycleBrowserEnabled) return new(404);
        var outcome = await run(actor, stamp);
        if (outcome.HttpStatus is 200 or 201) await transaction.CommitAsync(token);
        return outcome;
    }
}
