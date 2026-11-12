using System.Data;
using System.Security.Claims;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed partial class PentaEnrollmentCreationWorkflow
{
    public bool BrowserEnabled => Enabled && configuration.GetValue<bool>("Penta:EnrollmentBrowserContractsEnabled");

    internal Task<PentaConversationOutcome> BrowserReadinessAsync(ClaimsPrincipal user, Guid academy, CancellationToken token) =>
        BrowserAuthorityAsync(user, academy, (_, _) => Task.FromResult(new PentaConversationOutcome(200,
            new { protocol = "enrollment-create-browser.v1", requiresApproval = true })), token);

    internal Task<PentaConversationOutcome> BrowserStartAsync(ClaimsPrincipal user, Guid academy, Guid request, bool create, CancellationToken token)
    {
        if (!BrowserEnabled) return Task.FromResult(new PentaConversationOutcome(404));
        if (request == Guid.Empty) return Task.FromResult(new PentaConversationOutcome(400));
        return BrowserAuthorityAsync(user, academy, async (actor, stamp) =>
        {
            var id = PentaEnrollmentBrowserSession.Key(academy, actor, request);
            var session = await db.PentaMiniSessions.AsNoTracking().SingleOrDefaultAsync(x =>
                x.Id == id && x.AcademyId == academy && x.ActorUserId == actor, token);
            var binding = new PentaEnrollmentBrowserSession(protection);
            if (session is not null)
            {
                if (!binding.Matches(session, stamp)) return new(404);
                return session.ExpiresAtUtc > Now ? BrowserSessionView(200, session) : new(410);
            }
            if (!create) return new(404);
            var now = Now;
            var recent = db.PentaMiniSessions.AsNoTracking().Where(x => x.AcademyId == academy && x.CreatedAtUtc >= now.Date);
            if (await recent.CountAsync(token) >= 100 || await recent.CountAsync(x => x.ActorUserId == actor, token) >= 20)
                return new(429);
            session = new PentaMiniSession { Id = id, AcademyId = academy, ActorUserId = actor,
                CreatedAtUtc = now, ExpiresAtUtc = now.AddHours(24), ProtectedState = "" };
            session.ProtectedOnboardingState = binding.Seal(session, stamp);
            db.PentaMiniSessions.Add(session);
            db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityId = id,
                EntityType = "PentaEnrollmentConversation", Action = "PentaEnrollmentConversationStarted", OccurredAtUtc = now });
            await db.SaveChangesAsync(token);
            if (session.ExpiresAtUtc <= Now) return new(410);
            return BrowserSessionView(201, session);
        }, token);
    }

    internal Task<PentaEnrollmentDraftOutcome> BrowserPrepareAsync(ClaimsPrincipal user, Guid academy, Guid session,
        Guid request, long context, string payload, CancellationToken token)
    {
        if (!BrowserEnabled) return Task.FromResult(new PentaEnrollmentDraftOutcome(404));
        return RunAsync(Action.Prepare, user, academy, session, request, null, context, payload, token, browser: true);
    }

    internal Task<PentaEnrollmentDraftOutcome> BrowserReadAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request, CancellationToken token) =>
        BrowserEnabled ? RunAsync(Action.Read, user, academy, session, request, null, null, null, token, browser: true)
            : Task.FromResult(new PentaEnrollmentDraftOutcome(404));
    internal Task<PentaEnrollmentDraftOutcome> BrowserApproveAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaEnrollmentBinding binding, CancellationToken token) => BrowserEnabled
            ? RunAsync(Action.Approve, user, academy, session, request, binding, null, null, token, ApprovalIntent, browser: true)
            : Task.FromResult(new PentaEnrollmentDraftOutcome(404));
    internal Task<PentaEnrollmentDraftOutcome> BrowserExecuteAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaEnrollmentBinding binding, CancellationToken token) => BrowserEnabled
            ? RunAsync(Action.Execute, user, academy, session, request, binding, null, null, token, browser: true)
            : Task.FromResult(new PentaEnrollmentDraftOutcome(404));

    private async Task<PentaConversationOutcome> BrowserAuthorityAsync(ClaimsPrincipal principal, Guid academy,
        Func<Guid, string, Task<PentaConversationOutcome>> run, CancellationToken token)
    {
        if (!BrowserEnabled) return new(404);
        var stamp = principal.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        if (string.IsNullOrEmpty(stamp) || !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ||
            !await policy.AllowsAsync(principal, academy, token)) return new(403);
        var original = AcademyIdentityTransaction.Validate(db, identity);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        if (await enrollments.CurrentHostRefusalAsync(principal, academy, stamp, token) is { } refusal) return new(refusal);
        if (!BrowserEnabled) return new(404);
        var outcome = await run(actor, stamp);
        if (outcome.HttpStatus is 200 or 201) await transaction.CommitAsync(token);
        return outcome;
    }

    private static PentaConversationOutcome BrowserSessionView(int status, PentaMiniSession session) =>
        new(status, new MiniSessionView(session.Id, session.Version, DateTime.SpecifyKind(session.ExpiresAtUtc, DateTimeKind.Utc)));
}
