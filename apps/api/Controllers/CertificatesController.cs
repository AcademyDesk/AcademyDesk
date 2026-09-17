using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/certificates")]
public sealed class CertificatesController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CertificateSummary>>> List(Guid academyId, CancellationToken token) => Ok(await dbContext.Certificates.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.IssuedDate).Select(x => new CertificateSummary(x.Id, x.CertificateNumber, x.StudentId, x.BatchId, x.Title, x.IssuedDate, x.Status, x.Notes)).ToListAsync(token));

    [HttpPost]
    public async Task<ActionResult<CertificateSummary>> Issue(Guid academyId, IssueCertificateRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Certificate title is required." });
        if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The student does not belong to this academy." });
        if (request.BatchId.HasValue && !await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The batch does not belong to this academy." });
        var number = $"CERT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        var certificate = new Certificate { AcademyId = academyId, CertificateNumber = number, StudentId = request.StudentId, BatchId = request.BatchId, Title = request.Title.Trim(), IssuedDate = request.IssuedDate ?? DateOnly.FromDateTime(DateTime.UtcNow), Notes = request.Notes?.Trim() };
        dbContext.Certificates.Add(certificate);
        dbContext.AuditLogs.Add(new AuditLog { AcademyId = academyId, Action = "CertificateIssued", EntityType = "Certificate", EntityId = certificate.Id });
        await dbContext.SaveChangesAsync(token);
        return Created($"/api/academies/{academyId}/certificates/{certificate.Id}", new CertificateSummary(certificate.Id, certificate.CertificateNumber, certificate.StudentId, certificate.BatchId, certificate.Title, certificate.IssuedDate, certificate.Status, certificate.Notes));
    }
}
public sealed record IssueCertificateRequest(Guid StudentId, Guid? BatchId, string Title, DateOnly? IssuedDate, string? Notes);
public sealed record CertificateSummary(Guid Id, string CertificateNumber, Guid StudentId, Guid? BatchId, string Title, DateOnly IssuedDate, string Status, string? Notes);
