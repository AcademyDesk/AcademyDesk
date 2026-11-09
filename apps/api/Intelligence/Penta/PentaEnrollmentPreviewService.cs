using System.Data;
using System.Security.Claims;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaEnrollmentPreview(string Tool, EnrollmentCreatePreview Admission, DateTimeOffset AsOfUtc,
    string Status = "PreviewOnly", bool RequiresApproval = true);
internal sealed record PentaEnrollmentPreviewOutcome(int HttpStatus, PentaEnrollmentPreview? Preview = null, string? Error = null);

public sealed class PentaEnrollmentPreviewService(AcademyDeskDbContext db, IdentityDbContext identity,
    EnrollmentMutationService enrollments, PentaPilotPolicy policy, UserManager<ApplicationUser> users,
    IConfiguration configuration, IHostEnvironment environment, TimeProvider clock)
{
    internal async Task<PentaEnrollmentPreviewOutcome> PreviewAsync(ClaimsPrincipal principal, Guid academy,
        string payload, CancellationToken token)
    {
        if (!environment.IsEnvironment("Testing") || !policy.IsAvailable ||
            !configuration.GetValue<bool>("Penta:EnrollmentPreviewEnabled")) return new(404);
        if (!await policy.AllowsAsync(principal, academy, token)) return new(403);
        if (!PentaEnrollmentFields.TryCreate(payload, out var request))
            return new(400, Error: "Enter a valid typed enrollment request.");

        string originalIdentityConnection;
        try { originalIdentityConnection = AcademyIdentityTransaction.Validate(db, identity); }
        catch (InvalidOperationException) { return new(503, Error: "Enrollment preview is unavailable."); }
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, originalIdentityConnection, token);
        var securityStamp = principal.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        var result = await enrollments.PreviewCreateAsync(principal, academy, request!, securityStamp, token);
        if (result.Preview is not { } preview) return new(result.HttpStatus, Error: result.Error);

        var now = clock.GetUtcNow();
        db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!),
            Action = "PentaEnrollmentPreviewed", EntityType = "Academy", EntityId = academy, OccurredAtUtc = now.UtcDateTime });
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return new(200, new(PentaEnrollmentFields.CreateTool, preview, now));
    }
}
