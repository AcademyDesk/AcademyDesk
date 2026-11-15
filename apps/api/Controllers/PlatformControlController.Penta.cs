using System.Data;
using System.Data.Common;
using System.Security.Claims;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PlatformControlController
{
    [HttpGet("penta/{kind}"), Authorize(AuthenticationSchemes = "Identity.Bearer")]
    public async Task<ActionResult> PentaPlatformRead(string kind, [FromServices] IdentityDbContext identity,
        [FromServices] IConfiguration config, [FromServices] IHostEnvironment environment, [FromServices] TimeProvider clock,
        CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        if (!environment.IsEnvironment("Testing") || !config.GetValue<bool>("Penta:Enabled") ||
            !config.GetValue<bool>("Penta:PlatformReadsEnabled")) return NotFound();
        if (kind is not ("readiness" or "health" or "onboarding" or "support")) return NotFound();

        try
        {
            var stamp = User.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
            if (!Guid.TryParse(users.GetUserId(User), out var id) || string.IsNullOrEmpty(stamp)) return Forbid();
            var connection = AcademyIdentityTransaction.Validate(db, identity);
            identity.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, connection, token);
            var actor = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token);
            if (actor is not { IsActive: true, IsPlatformOwner: true } || actor.SecurityStamp != stamp) return Forbid();

            if (kind == "readiness") return Ok(new { protocol = "platform-reads.v1", scope = "PlatformOwner" });

            var asOfUtc = clock.GetUtcNow().UtcDateTime;
            object metrics = kind switch
            {
                "health" => await ReadPentaHealthAsync(token),
                "onboarding" => await ReadPentaOnboardingAsync(token),
                _ => await ReadPentaSupportAsync(token),
            };
            db.PlatformAuditEntries.Add(new PlatformAuditEntry
            {
                ActorUserId = actor.Id, ActorName = actor.DisplayName, EntityType = "Platform",
                Action = kind switch
                {
                    "health" => "PentaPlatformHealthRead", "onboarding" => "PentaPlatformOnboardingRead", _ => "PentaPlatformSupportRead",
                },
                OccurredAtUtc = asOfUtc,
            });
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return Ok(new { scope = "PlatformOwner", kind, asOfUtc, metrics, sourcePath = "/platform/control" });
        }
        catch (Exception error) when (error is DbException or DbUpdateException or InvalidOperationException)
        {
            return StatusCode(503, new { message = "PENTA could not verify platform records. Open Manual Platform Control." });
        }
    }

    private async Task<object> ReadPentaHealthAsync(CancellationToken token)
    {
        var maintenance = await db.PlatformSettings.AsNoTracking().OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.Id).Select(x => (bool?)x.MaintenanceMode).FirstOrDefaultAsync(token);
        return new { api = "Operational", database = "Operational", maintenanceMode = maintenance ?? false };
    }

    private async Task<object> ReadPentaOnboardingAsync(CancellationToken token)
    {
        var academies = db.Academies.AsNoTracking();
        var profiles = db.TenantOnboardingProfiles.AsNoTracking().Where(x => academies.Any(a => a.Id == x.AcademyId));
        return new
        {
            academyCount = await academies.CountAsync(token),
            withProfile = await profiles.Select(x => x.AcademyId).Distinct().CountAsync(token),
            inProgress = await profiles.Where(x => x.Status == "InProgress").Select(x => x.AcademyId).Distinct().CountAsync(token),
            completed = await profiles.Where(x => x.Status == "Completed").Select(x => x.AcademyId).Distinct().CountAsync(token),
        };
    }

    private async Task<object> ReadPentaSupportAsync(CancellationToken token)
    {
        var open = db.PlatformSupportCases.AsNoTracking().Where(x => x.Status != "Resolved" && x.Status != "Closed");
        return new
        {
            open = await open.CountAsync(token),
            highOrUrgent = await open.CountAsync(x => x.Priority == "High" || x.Priority == "Urgent", token),
        };
    }
}
