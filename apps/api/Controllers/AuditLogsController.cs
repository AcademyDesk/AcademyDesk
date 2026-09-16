using AcademyDesk.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/audit-logs")]
public sealed class AuditLogsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogSummary>>> List(Guid academyId, CancellationToken cancellationToken) => Ok(await dbContext.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.OccurredAtUtc).Take(200).Select(x => new AuditLogSummary(x.Id, x.ActorUserId, x.Action, x.EntityType, x.EntityId, x.MetadataJson, x.IpAddress, x.OccurredAtUtc)).ToListAsync(cancellationToken));
}

public sealed record AuditLogSummary(Guid Id, Guid? ActorUserId, string Action, string EntityType, Guid? EntityId, string? MetadataJson, string? IpAddress, DateTime OccurredAtUtc);
