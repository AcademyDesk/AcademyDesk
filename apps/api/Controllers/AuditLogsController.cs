using AcademyDesk.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/audit-logs")]
public sealed class AuditLogsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogSummary>>> List(Guid academyId, string? action, string? entityType, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var query = dbContext.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academyId);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);
        if (fromUtc.HasValue) query = query.Where(x => x.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.OccurredAtUtc < toUtc.Value);
        return Ok(await query.OrderByDescending(x => x.OccurredAtUtc).Take(200).Select(x => new AuditLogSummary(x.Id, x.ActorUserId, x.Action, x.EntityType, x.EntityId, x.MetadataJson, x.IpAddress, x.OccurredAtUtc)).ToListAsync(cancellationToken));
    }
    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv(Guid academyId, string? action, string? entityType, DateTime? fromUtc, DateTime? toUtc, CancellationToken token)
    { var query=dbContext.AuditLogs.AsNoTracking().Where(x=>x.AcademyId==academyId); if(!string.IsNullOrWhiteSpace(action))query=query.Where(x=>x.Action==action); if(!string.IsNullOrWhiteSpace(entityType))query=query.Where(x=>x.EntityType==entityType); if(fromUtc.HasValue)query=query.Where(x=>x.OccurredAtUtc>=fromUtc); if(toUtc.HasValue)query=query.Where(x=>x.OccurredAtUtc<toUtc); var rows=await query.OrderByDescending(x=>x.OccurredAtUtc).Take(200).ToListAsync(token); static string Q(string? v)=>$"\"{(v??"").Replace("\"","\"\"")}\""; var csv="OccurredAtUtc,Action,EntityType,EntityId,ActorUserId,Metadata\r\n"+string.Join("\r\n",rows.Select(x=>string.Join(",",Q(x.OccurredAtUtc.ToString("O")),Q(x.Action),Q(x.EntityType),Q(x.EntityId?.ToString()),Q(x.ActorUserId?.ToString()),Q(x.MetadataJson)))); return File(System.Text.Encoding.UTF8.GetBytes(csv),"text/csv","academydesk-audit-log.csv"); }
}

public sealed record AuditLogSummary(Guid Id, Guid? ActorUserId, string Action, string EntityType, Guid? EntityId, string? MetadataJson, string? IpAddress, DateTime OccurredAtUtc);
