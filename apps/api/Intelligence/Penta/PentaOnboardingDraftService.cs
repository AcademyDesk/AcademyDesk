using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using AcademyDesk.Api.Infrastructure;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaOnboardingDraftView(Guid Id, Guid SessionId, long ContextVersion,
    long Revision, string Status, DateTime ExpiresAtUtc, string CanonicalPayload, string PayloadHash,
    bool ReadyForReview, bool CanExecute = false);
public sealed record PentaOnboardingDraftOutcome(int HttpStatus, PentaOnboardingDraftView? Draft = null,
    string? Error = null);

/// <summary>
/// Test-only host draft/review candidate. Its HTTP adapter has an independent
/// Testing-only switch; there is no model-accessible write-tool registration.
/// Separate execution candidate requires its own test switch and explicit creation
/// authorization; ordinary review confirmations never authorize domain writes.
/// </summary>
public sealed partial class PentaOnboardingDraftService(AcademyDeskDbContext db, IdentityDbContext identity,
    IDataProtectionProvider protection, TimeProvider clock, IConfiguration configuration, IHostEnvironment environment,
    StudentOnboardingService? onboarding = null,IPentaOnboardingInterpreter? interpreter = null,
    IServiceScopeFactory? gatewayScopes = null)
{
    public const string Tool = "AcademyDesk.StudentOnboarding.Create";
    private const string Policy = "academy.onboarding-review.v2";
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    public bool Enabled => environment.IsEnvironment("Testing") &&
        configuration.GetValue<bool>("Penta:OperationalDraftContractsEnabled");

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record Body(string Policy, Guid Id, Guid Academy, Guid Actor, Guid Session,
        long Context, long Revision, string Status, DateTime Expiry, string Authority,
        string Payload, string Hash, bool AuthorizesCreation = false, PentaOnboardingReceipt? Receipt = null,
        CoreRequest[]? CoreRequests = null, Guid? PreparationRequestId = null, CoreApproval? CoreApproval = null);
    private sealed record CoreRequest(Guid Id, string Digest, long BaseRevision, long Revision, string Kind = "prepare");
    private sealed record CoreApproval(Guid Id, DateTime ExpiresAtUtc, bool Approved);
    private static bool CoreOwned(Body body) => body.CoreRequests?.Any(x => x.Kind is "prepare" or "partial") == true;

    // Full canonical replacement, not a patch: clear/reset semantics belong to the
    // paired dialogue adapter. The caller must send retained fields explicitly.
    public async Task<PentaOnboardingDraftOutcome> SaveAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, long expectedRevision, long contextVersion, string payload, CancellationToken token)
    {
        if (!Enabled) return new(404);
        if (!SqlReady()) return new(503);
        db.ChangeTracker.Clear(); // owned unit of work: discard only unchanged cached rows
        if (draftId == Guid.Empty || expectedRevision is < 0 or >= 100 || contextVersion < 0 || !ValidPayload(payload))
            return new(400, Error: "Enter valid onboarding details and revision.");
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await LockAsync(academy, token);
        var authority = await AuthorityAsync(principal, academy, token);
        if (authority is null) return new(403);
        var (actor, authorityHash) = authority.Value;
        var session = await SessionAsync(academy, actor, sessionId, contextVersion, token);
        if (session is null) return new(409, Error: "The conversation is unavailable or changed.");
        var row = await Owned(academy, actor, sessionId, draftId).SingleOrDefaultAsync(token);
        var canonical = PentaOperationalPayload.Canonicalize(payload);
        CoreRequest[] history = [];
        if (row is not null)
        {
            var stored = Read(row);
            if (stored is null || stored.Authority != authorityHash || row.ContextVersion != contextVersion)
                return Conflict();
            history = stored.CoreRequests ?? [];
            if (row.Status is "Succeeded" or "Paused" or "Cancelled") return Conflict();
            if (row.ExpiresAtUtc <= Now) return new(410, Error: "This draft expired. Prepare a new draft.");
            if (row.Revision == expectedRevision + 1 && stored.Payload == canonical)
                return new(200, View(row, stored)); // identical save replay; no extra audit or expiry extension
            if (row.Revision != expectedRevision) return Conflict();
            row.Revision++;
            row.Status = "AwaitingReview"; // every accepted edit invalidates prior confirmation
            row.UpdatedAtUtc = Now;
        }
        else
        {
            // Globally colliding IDs are never adopted as another actor's draft.
            if (expectedRevision != 0 || await db.PentaOnboardingDrafts.AnyAsync(x => x.Id == draftId, token)) return Conflict();
            var today = Now.Date;
            var created = db.PentaOnboardingDrafts.Where(x => x.AcademyId == academy && x.CreatedAtUtc >= today);
            if (await created.CountAsync(token) >= 100 || await created.CountAsync(x => x.ActorUserId == actor, token) >= 20)
                return new(429, Error: "The onboarding draft limit has been reached.");
            row = new() { Id = draftId, AcademyId = academy, ActorUserId = actor, SessionId = sessionId,
                ContextVersion = contextVersion, Revision = 1, Status = "AwaitingReview", CreatedAtUtc = Now,
                ExpiresAtUtc = session.ExpiresAtUtc < Now.AddMinutes(10) ? session.ExpiresAtUtc : Now.AddMinutes(10), ProtectedBody = "" };
            db.PentaOnboardingDrafts.Add(row);
        }
        var body = Seal(row, authorityHash, canonical, coreRequests: history);
        Audit(row, "Saved");
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return new(201, View(row, body));
    }

    public async Task<PentaOnboardingDraftOutcome> GetAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, CancellationToken token) =>
        await ReviewAsync(principal, academy, sessionId, draftId, null, null, token);

    public async Task<PentaOnboardingDraftOutcome> ConfirmAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, long revision, string hash, CancellationToken token) =>
        await ReviewAsync(principal, academy, sessionId, draftId, revision, hash, token);

    private async Task<PentaOnboardingDraftOutcome> ReviewAsync(ClaimsPrincipal principal, Guid academy,
        Guid sessionId, Guid draftId, long? revision, string? hash, CancellationToken token, bool authorizeCreation = false)
    {
        if (!Enabled) return new(404);
        if (!SqlReady()) return new(503);
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await LockAsync(academy, token);
        var authority = await AuthorityAsync(principal, academy, token);
        if (authority is null) return new(403);
        var (actor, authorityHash) = authority.Value;
        var row = await Owned(academy, actor, sessionId, draftId).SingleOrDefaultAsync(token);
        if (row is null) return new(404);
        var body = Read(row);
        if (body is null || body.Authority != authorityHash ||
            await SessionAsync(academy, actor, sessionId, row.ContextVersion, token) is null) return Conflict();
        if (row.ExpiresAtUtc <= Now) return new(410, Error: "This draft expired. Prepare a new draft.");
        if (revision is null)
        {
            Audit(row, "Read");
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return new(200, View(row, body));
        }
        if (row.Revision != revision || hash != body.Hash) return Conflict();
        // A Core-origin draft cannot erase its approval boundary by editing it
        // through the older save endpoint. Its protected request history survives.
        if (CoreOwned(body)) return Conflict();
        if (row.Status is "Paused" or "Cancelled") return Conflict();
        if (!Ready(body.Payload)) return new(400, Error: "Complete the required student and guardian details before confirmation.");
        PentaOnboardingFields.TryRead(body.Payload, out var request);
        if (await StudentOnboardingRules.ValidateAsync(db, academy, request!, DateOnly.FromDateTime(Now), token) is { } invalid)
            return new(400, Error: invalid);
        var first = request!.StudentFirstName.Trim().ToUpperInvariant();
        var last = request.StudentLastName.Trim().ToUpperInvariant();
        if (await db.Students.AsNoTracking().AnyAsync(x => x.AcademyId == academy && x.DateOfBirth == request.DateOfBirth &&
            x.FirstName.Trim().ToUpper() == first && x.LastName.Trim().ToUpper() == last, token))
            return new(409, Error: "A possible matching student already exists. Review existing records in the manual workspace before creating another.");
        var targetStatus = authorizeCreation ? "Authorized" : "Confirmed";
        if (row.Status == targetStatus) return new(200, View(row, body));
        if (row.Status is "Succeeded" or "Authorized") return Conflict();
        row.Status = targetStatus;
        row.UpdatedAtUtc = Now;
        body = Seal(row, authorityHash, body.Payload, authorizeCreation, coreRequests: body.CoreRequests,
            preparationRequestId: body.PreparationRequestId);
        Audit(row, authorizeCreation ? "CreationAuthorized" : "Confirmed");
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return new(200, View(row, body));
    }

    private bool SqlReady()
    {
        if (db.ChangeTracker.HasChanges() || identity.ChangeTracker.HasChanges()) return false;
        try
        {
            // Existing host guard verifies idle SQL contexts and matching immutable
            // configured stores, not a model/client supplied connection or tenant.
            AcademyDesk.Api.Security.AcademyIdentityTransaction.Validate(db, identity);
            return true;
        }
        catch (InvalidOperationException) { return false; }
    }
    private Task<int> LockAsync(Guid academy, CancellationToken token) => db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academy}", token);
    private IQueryable<PentaOnboardingDraft> Owned(Guid academy, Guid actor, Guid session, Guid id) =>
        db.PentaOnboardingDrafts.Where(x => x.AcademyId == academy && x.ActorUserId == actor && x.SessionId == session && x.Id == id);
    private Task<PentaMiniSession?> SessionAsync(Guid academy, Guid actor, Guid session, long version, CancellationToken token) =>
        db.PentaMiniSessions.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academy && x.ActorUserId == actor &&
            x.Id == session && x.Version == version && x.PendingRequestId == null && x.ExpiresAtUtc > Now, token);

    private async Task<(Guid Actor, string Hash)?> AuthorityAsync(ClaimsPrincipal principal, Guid academy, CancellationToken token)
    {
        if (principal.Identity?.IsAuthenticated != true || !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return null;
        // Query current SQL records directly; neither model claims nor tracked Identity entities are authority.
        var user = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actor, token);
        if (user is null || !user.IsActive || user.IsPlatformOwner || user.AcademyId != academy) return null;
        var roles = await (from membership in identity.UserRoles.AsNoTracking()
            join role in identity.Roles.AsNoTracking() on membership.RoleId equals role.Id
            where membership.UserId == actor select role.Name).ToArrayAsync(token);
        if (!roles.Contains("Owner") && !roles.Contains("AcademyAdmin")) return null;
        var tenant = await db.Academies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == academy && x.IsActive, token);
        if (tenant is null || !AcademyDesk.Api.Security.SubscriptionPlanCatalog.Allows(tenant.EnabledModulesJson, "Core")) return null;
        return (actor, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            Policy, actor, academy, user.SecurityStamp, user.ConcurrencyStamp, roles = roles.Order(StringComparer.Ordinal),
            tenant.EnabledModulesJson, tenant.SubscriptionPlan, tenant.SubscriptionStatus, tenant.SubscriptionEndsAtUtc,
            tenant.StudentLimit, tenant.UpdatedAtUtc })) )));
    }

    private IDataProtector Protector(PentaOnboardingDraft row) => protection.CreateProtector(
        "PentaOnboardingDraft.v1", row.AcademyId.ToString("D"), row.ActorUserId.ToString("D"), row.SessionId.ToString("D"), row.Id.ToString("D"));
    private Body Seal(PentaOnboardingDraft row, string authority, string payload, bool authorizesCreation = false,
        PentaOnboardingReceipt? receipt = null, CoreRequest[]? coreRequests = null, Guid? preparationRequestId = null,
        CoreApproval? coreApproval = null)
    {
        var body = new Body(Policy, row.Id, row.AcademyId, row.ActorUserId, row.SessionId, row.ContextVersion,
            row.Revision, row.Status, row.ExpiresAtUtc, authority, payload, PentaOperationalPayload.Digest(payload), authorizesCreation, receipt,
            coreRequests, preparationRequestId, coreApproval);
        row.ProtectedBody = Protector(row).Protect(JsonSerializer.Serialize(body));
        return body;
    }
    private Body? Read(PentaOnboardingDraft row)
    {
        try
        {
            var body = JsonSerializer.Deserialize<Body>(Protector(row).Unprotect(row.ProtectedBody));
            return body is not null && body.Policy == Policy && body.Id == row.Id && body.Academy == row.AcademyId &&
                body.Actor == row.ActorUserId && body.Session == row.SessionId && body.Context == row.ContextVersion &&
                body.Revision == row.Revision && body.Status == row.Status && body.Expiry == row.ExpiresAtUtc &&
                row.Status is "AwaitingReview" or "Prepared" or "Confirmed" or "Authorized" or "Succeeded" or "Paused" or "Cancelled" &&
                body.AuthorizesCreation == (row.Status is "Authorized" or "Succeeded") &&
                (body.Receipt is not null) == (row.Status == "Succeeded") && ValidCoreHistory(row, body) && ValidCoreApproval(row, body) && ValidPayload(body.Payload) &&
                PentaOperationalPayload.MatchesDigest(body.Payload, body.Hash) ? body : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException) { return null; }
    }
    private static bool ValidCoreApproval(PentaOnboardingDraft row, Body body)
    {
        if (body.CoreApproval is not { } approval)
            return !CoreOwned(body) || row.Status is not ("Authorized" or "Succeeded");
        return body.PreparationRequestId is not null && approval.Id != Guid.Empty &&
            approval.ExpiresAtUtc > row.CreatedAtUtc && approval.ExpiresAtUtc <= row.ExpiresAtUtc &&
            row.Status is "Prepared" or "Authorized" or "Succeeded" &&
            approval.Approved == (row.Status is "Authorized" or "Succeeded");
    }
    private static bool ValidCoreHistory(PentaOnboardingDraft row, Body body)
    {
        var history = body.CoreRequests ?? [];
        return history.Length <= 99 && history.All(x => x is not null && x.Digest is { Length: 64 }) &&
            history.Select(x => x.Id).Distinct().Count() == history.Length &&
            history.Select(x => x.Revision).Distinct().Count() == history.Length && history.All(x => x.Id != Guid.Empty &&
                x.BaseRevision is >= 1 and < 100 && x.Revision == x.BaseRevision + 1 && x.Revision <= row.Revision &&
                x.Kind is "prepare" or "partial" or "request_pause" or "request_resume" or "request_cancel" &&
                x.Digest.Length == 64 && x.Digest.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f')) &&
            (body.PreparationRequestId is not { } id || history.Any(x => x.Id == id && x.Revision == row.Revision && x.Kind == "prepare")) &&
            (row.Status != "Prepared" || body.PreparationRequestId is not null) &&
            (row.Status is not ("AwaitingReview" or "Paused" or "Cancelled") || body.PreparationRequestId is null);
    }
    private PentaOnboardingDraftView View(PentaOnboardingDraft row, Body body) => new(row.Id, row.SessionId,
        // SQL datetime2 preserves UTC ticks but not DateTime.Kind. Label those
        // stored UTC ticks explicitly; never reinterpret them as server-local time.
        row.ContextVersion, row.Revision, row.Status, DateTime.SpecifyKind(row.ExpiresAtUtc, DateTimeKind.Utc), body.Payload, body.Hash, Ready(body.Payload));
    private void Audit(PentaOnboardingDraft row, string outcome) => db.AuditLogs.Add(new AuditLog {
        AcademyId = row.AcademyId, ActorUserId = row.ActorUserId, EntityType = "PentaOnboardingDraft", EntityId = row.Id,
        Action = "PentaOnboardingDraft" + outcome, OccurredAtUtc = Now,
        MetadataJson = JsonSerializer.Serialize(new { row.SessionId, row.ContextVersion, row.Revision, tool = Tool,
            policy = Policy, domainEffect = false }) });
    private static PentaOnboardingDraftOutcome Conflict() => new(409, Error: "The draft or your permissions changed. Prepare a fresh review.");

    private static bool ValidPayload(string payload) => PentaOnboardingFields.TryRead(payload, out _);
    private bool Ready(string payload) => PentaOnboardingFields.Ready(payload, DateOnly.FromDateTime(Now));
}
