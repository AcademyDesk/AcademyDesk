using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/compliance")]
public sealed class ComplianceController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet("documents")]
    public async Task<ActionResult> Documents(Guid academyId, CancellationToken cancellationToken) =>
        Ok(await db.PersonDocuments.AsNoTracking().Where(document => document.AcademyId == academyId)
            .OrderBy(document => document.ExpiryDate).ThenBy(document => document.DocumentType).ToListAsync(cancellationToken));

    [HttpPost("documents")]
    public async Task<ActionResult> AddDocument(Guid academyId, DocumentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentType) || string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest("Document type and file name are required.");

        var document = new PersonDocument
        {
            AcademyId = academyId, StudentId = request.StudentId, GuardianId = request.GuardianId,
            DocumentType = request.DocumentType.Trim(), FileName = request.FileName.Trim(),
            SecureReference = request.SecureReference?.Trim(), ExpiryDate = request.ExpiryDate,
            Visibility = string.IsNullOrWhiteSpace(request.Visibility) ? "AdminOnly" : request.Visibility.Trim()
        };
        db.PersonDocuments.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(document);
    }

    [HttpPatch("documents/{documentId:guid}/review")]
    public async Task<ActionResult> ReviewDocument(Guid academyId, Guid documentId, DocumentReviewRequest request, CancellationToken cancellationToken)
    {
        var document = await db.PersonDocuments.FirstOrDefaultAsync(item => item.Id == documentId && item.AcademyId == academyId, cancellationToken);
        if (document is null) return NotFound();
        var allowedStatuses = new[] { "PendingReview", "Approved", "Rejected", "Expired" };
        if (!allowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest("Invalid review status.");
        document.Status = allowedStatuses.Single(status => status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        document.ReviewedDate = request.ReviewedDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(document);
    }

    [HttpGet("consents")]
    public async Task<ActionResult> Consents(Guid academyId, CancellationToken cancellationToken) =>
        Ok(await db.ConsentRecords.AsNoTracking().Where(consent => consent.AcademyId == academyId)
            .OrderByDescending(consent => consent.RecordedAtUtc).ToListAsync(cancellationToken));

    [HttpPost("consents")]
    public async Task<ActionResult> AddConsent(Guid academyId, ConsentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ConsentType) || (request.StudentId is null && request.GuardianId is null))
            return BadRequest("A consent type and student or guardian are required.");
        var consent = new ConsentRecord
        {
            AcademyId = academyId, StudentId = request.StudentId, GuardianId = request.GuardianId,
            ConsentType = request.ConsentType.Trim(), Granted = request.Granted,
            EvidenceReference = request.EvidenceReference?.Trim(), WithdrawnAtUtc = request.Granted ? null : DateTime.UtcNow
        };
        db.ConsentRecords.Add(consent);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(consent);
    }

    [HttpPatch("consents/{consentId:guid}/withdraw")]
    public async Task<ActionResult> WithdrawConsent(Guid academyId, Guid consentId, CancellationToken cancellationToken)
    {
        var consent = await db.ConsentRecords.FirstOrDefaultAsync(item => item.Id == consentId && item.AcademyId == academyId, cancellationToken);
        if (consent is null) return NotFound();
        consent.Granted = false;
        consent.WithdrawnAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(consent);
    }
}

public sealed record DocumentRequest(Guid? StudentId, Guid? GuardianId, string DocumentType, string FileName, string? SecureReference, DateOnly? ExpiryDate, string? Visibility);
public sealed record DocumentReviewRequest(string Status, DateOnly? ReviewedDate);
public sealed record ConsentRequest(Guid? StudentId, Guid? GuardianId, string ConsentType, bool Granted, string? EvidenceReference);
