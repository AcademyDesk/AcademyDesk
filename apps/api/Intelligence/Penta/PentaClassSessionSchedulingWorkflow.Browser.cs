using System.Data;
using System.Security.Claims;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaClassBrowserBatch(Guid SourceId, string Name, Guid? TeacherId, Guid? BranchId, string SourcePath);
public sealed record PentaClassBrowserSource(Guid SourceId, Guid BatchId, string BatchName, DateTime StartUtc, DateTime EndUtc, string Status, string SourcePath);
public sealed record PentaClassBrowserLookup<T>(string Tool, string Capability, Guid AcademyId, DateTime AsOfUtc, int MaxRows, bool HasMore, IReadOnlyList<T> Rows);

public sealed partial class PentaClassSessionSchedulingWorkflow
{
    public bool BrowserEnabled => Enabled && ReschedulingEnabled && configuration.GetValue<bool>("Penta:ClassSchedulingBrowserContractsEnabled");
    internal static bool BrowserQuery(string? input, out string query) { query = input?.Trim() ?? ""; return query.Length is >= 2 and <= 80 && !query.Any(char.IsControl); }

    internal Task<PentaConversationOutcome> BrowserReadinessAsync(ClaimsPrincipal user, Guid academy, CancellationToken token) => BrowserAuthorityAsync(user, academy, (_, _) => Task.FromResult(new PentaConversationOutcome(200,
        new { protocol = "class-scheduling-browser.v1", requiresApproval = true, tools = new[] { "AcademyDesk.ClassSession.Create", PentaClassSessionFields.RescheduleTool } })), token);

    internal async Task<PentaConversationOutcome> BrowserLookupAsync(ClaimsPrincipal user, Guid academy, string? input, bool sessions, CancellationToken token)
    {
        if (!BrowserEnabled) return new(404);
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out _) || string.IsNullOrEmpty(user.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType)) || !await policy.AllowsAsync(user, academy, token)) return new(403);
        if (!BrowserQuery(input, out var query)) return new(400, Error: "Enter 2 to 80 searchable characters.");
        return await BrowserAuthorityAsync(user, academy, async (actor, _) =>
        {
            var now = Now;
            object response;
            string tool;
            if (sessions)
            {
                var rows = await (from s in db.ClassSessions.AsNoTracking()
                    join b in db.Batches.AsNoTracking() on s.BatchId equals b.Id
                    where s.AcademyId == academy && b.AcademyId == academy && s.Status == "Scheduled" && s.StartUtc > now && b.Name.Contains(query) &&
                        (!s.TeacherId.HasValue || db.Teachers.Any(t => t.Id == s.TeacherId && t.AcademyId == academy)) &&
                        (!s.BranchId.HasValue || db.Branches.Any(a => a.Id == s.BranchId && a.AcademyId == academy))
                    orderby s.StartUtc, b.Name, s.Id
                    select new PentaClassBrowserSource(s.Id, b.Id, b.Name, s.StartUtc, s.EndUtc, s.Status, "/sessions"))
                    .Take(11).ToListAsync(token);
                tool = "class-session.search.v1";
                response = new PentaClassBrowserLookup<PentaClassBrowserSource>(tool, "executor", academy, now, 10, rows.Count > 10,
                    rows.Take(10).Select(x => x with { StartUtc = DateTime.SpecifyKind(x.StartUtc, DateTimeKind.Utc), EndUtc = DateTime.SpecifyKind(x.EndUtc, DateTimeKind.Utc) }).ToArray());
            }
            else
            {
                var rows = await db.Batches.AsNoTracking().Where(b => b.AcademyId == academy && b.Name.Contains(query))
                    .OrderBy(b => b.Name).ThenBy(b => b.Id).Select(b => new PentaClassBrowserBatch(b.Id, b.Name,
                        db.Teachers.Any(t => t.Id == b.TeacherId && t.AcademyId == academy) ? b.TeacherId : null,
                        db.Branches.Any(a => a.Id == b.BranchId && a.AcademyId == academy) ? b.BranchId : null, "/sessions"))
                    .Take(11).ToListAsync(token);
                tool = "class-scheduling.batch-options.v1";
                response = new PentaClassBrowserLookup<PentaClassBrowserBatch>(tool, "executor", academy, now, 10, rows.Count > 10, rows.Take(10).ToArray());
            }
            db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityType = "Academy", EntityId = academy,
                Action = "PentaClassSchedulingSearched", OccurredAtUtc = now, MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { tool }) });
            await db.SaveChangesAsync(token);
            return new(200, response);
        }, token);
    }

    internal Task<PentaConversationOutcome> BrowserStartAsync(ClaimsPrincipal user, Guid academy, Guid request, bool create, CancellationToken token)
    {
        if (!BrowserEnabled) return Task.FromResult(new PentaConversationOutcome(404));
        if (request == Guid.Empty) return Task.FromResult(new PentaConversationOutcome(400));
        return BrowserAuthorityAsync(user, academy, async (actor, stamp) =>
        {
            var id = PentaClassSessionBrowserSession.Key(academy, actor, request);
            var session = await db.PentaMiniSessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id && s.AcademyId == academy && s.ActorUserId == actor, token);
            var binding = new PentaClassSessionBrowserSession(protection);
            if (session is not null) { if (!binding.Matches(session, stamp)) return new(404); return session.ExpiresAtUtc > Now ? BrowserSessionView(200, session) : new(410); }
            if (!create) return new(404);
            var now = Now; var recent = db.PentaMiniSessions.AsNoTracking().Where(s => s.AcademyId == academy && s.CreatedAtUtc >= now.Date);
            if (await recent.CountAsync(token) >= 100 || await recent.CountAsync(s => s.ActorUserId == actor, token) >= 20) return new(429);
            session = new PentaMiniSession { Id = id, AcademyId = academy, ActorUserId = actor, CreatedAtUtc = now, ExpiresAtUtc = now.AddHours(24), ProtectedState = "" };
            session.ProtectedOnboardingState = binding.Seal(session, stamp);
            db.PentaMiniSessions.Add(session);
            db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityType = "PentaClassSchedulingConversation", EntityId = id, Action = "PentaClassSchedulingConversationStarted", OccurredAtUtc = now });
            await db.SaveChangesAsync(token);
            return session.ExpiresAtUtc > Now ? BrowserSessionView(201, session) : new(410);
        }, token);
    }

    internal Task<PentaClassSessionOutcome> BrowserPrepareAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request, long context, string payload, CancellationToken token) => BrowserEnabled ? BrowserDraft(RunAsync(Action.Prepare, user, academy, session, request, context, payload, null, token, browser: true)) : Task.FromResult(new PentaClassSessionOutcome(404));
    internal Task<PentaClassSessionOutcome> BrowserReadAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request, CancellationToken token) => BrowserEnabled ? BrowserDraft(RunAsync(Action.Read, user, academy, session, request, null, null, null, token, browser: true)) : Task.FromResult(new PentaClassSessionOutcome(404));
    internal Task<PentaClassSessionOutcome> BrowserApproveAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request, PentaClassSessionBinding binding, string intent, CancellationToken token) => BrowserEnabled ? BrowserDraft(RunAsync(Action.Approve, user, academy, session, request, null, null, binding, token, intent, browser: true)) : Task.FromResult(new PentaClassSessionOutcome(404));
    internal Task<PentaClassSessionOutcome> BrowserExecuteAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request, PentaClassSessionBinding binding, CancellationToken token) => BrowserEnabled ? BrowserDraft(RunAsync(Action.Execute, user, academy, session, request, null, null, binding, token, browser: true)) : Task.FromResult(new PentaClassSessionOutcome(404));
    private static async Task<PentaClassSessionOutcome> BrowserDraft(Task<PentaClassSessionOutcome> outcome) => BrowserView(await outcome);
    internal static PentaClassSessionOutcome BrowserView(PentaClassSessionOutcome outcome) => outcome.Draft is not { } d ? outcome : outcome with { Draft = d with
    {
        ExpiresAtUtc = DateTime.SpecifyKind(d.ExpiresAtUtc, DateTimeKind.Utc),
        Plan = d.Plan with { StartUtc = DateTime.SpecifyKind(d.Plan.StartUtc, DateTimeKind.Utc), EndUtc = DateTime.SpecifyKind(d.Plan.EndUtc, DateTimeKind.Utc) },
        Source = d.Source is not { } s ? null : s with { StartUtc = DateTime.SpecifyKind(s.StartUtc, DateTimeKind.Utc), EndUtc = DateTime.SpecifyKind(s.EndUtc, DateTimeKind.Utc) },
        Receipt = d.Receipt is not { } r ? null : r with { CompletedAtUtc = DateTime.SpecifyKind(r.CompletedAtUtc, DateTimeKind.Utc) }
    } };

    private async Task<PentaConversationOutcome> BrowserAuthorityAsync(ClaimsPrincipal user, Guid academy, Func<Guid, string, Task<PentaConversationOutcome>> action, CancellationToken token)
    {
        if (!BrowserEnabled) return new(404);
        var stamp = user.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        if (string.IsNullOrEmpty(stamp) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) || !await policy.AllowsAsync(user, academy, token)) return new(403);
        var original = AcademyIdentityTransaction.Validate(db, identity); identity.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await scheduler.LockAsync(academy, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        var current = await identity.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == actor, token);
        if (current?.SecurityStamp != stamp || !await policy.AllowsAsync(user, academy, token)) return new(403);
        var modules = await db.Academies.AsNoTracking().Where(a => a.Id == academy && a.IsActive).Select(a => a.EnabledModulesJson).SingleOrDefaultAsync(token);
        if (!SubscriptionPlanCatalog.Allows(modules, SubscriptionPlanCatalog.ModuleForController(nameof(ClassSessionsController)))) return new(403);
        if (!BrowserEnabled) return new(404);
        var result = await action(actor, stamp);
        if (result.HttpStatus is 200 or 201) await transaction.CommitAsync(token);
        return result;
    }
    private static PentaConversationOutcome BrowserSessionView(int status, PentaMiniSession session) => new(status, new MiniSessionView(session.Id, session.Version, DateTime.SpecifyKind(session.ExpiresAtUtc, DateTimeKind.Utc)));
}
