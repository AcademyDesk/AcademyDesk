using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaAcademyContext(
    string Tool, string Capability, Guid AcademyId, string Name,
    string CountryCode, string TimeZone, DateTime AsOfUtc,
    string Source, string SourcePath);

/// <summary>A fixed, minimal R0 projection; it never invokes a model or domain write tool.</summary>
public sealed class PentaAcademyContextService(AcademyDeskDbContext db, TimeProvider clock)
{
    public const string ToolName = "academy.context.v1";

    public async Task<PentaAcademyContext?> ReadAsync(Guid academyId, Guid actorId, CancellationToken token)
    {
        var academy = await db.Academies.AsNoTracking()
            .Where(x => x.Id == academyId && x.IsActive)
            .Select(x => new { x.Id, x.Name, x.CountryCode, x.TimeZone })
            .SingleOrDefaultAsync(token);
        if (academy is null) return null;

        var asOfUtc = clock.GetUtcNow().UtcDateTime;
        // If audit persistence fails, do not return academy data as an unrecorded AI read.
        db.AuditLogs.Add(new AuditLog
        {
            AcademyId = academyId,
            ActorUserId = actorId,
            Action = "PentaAcademyContextRead",
            EntityType = "Academy",
            EntityId = academyId,
            OccurredAtUtc = asOfUtc,
            MetadataJson = "{\"tool\":\"academy.context.v1\"}"
        });
        await db.SaveChangesAsync(token);

        return new PentaAcademyContext(ToolName, "twin", academy.Id, academy.Name,
            academy.CountryCode, academy.TimeZone, asOfUtc,
            "Academy", "/api/academies");
    }
}
