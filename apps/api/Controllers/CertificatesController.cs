using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/certificates")]
public sealed class CertificatesController(AcademyDeskDbContext dbContext, IWebHostEnvironment environment) : ControllerBase
{
    private static readonly HashSet<string> TemplateKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "classic", "modern", "minimal", "navy", "academic", "gold", "silver", "bronze",
        "performance", "completion", "excellence", "independence-day", "republic-day", "diwali", "ganesh-festival"
    };
    private static readonly HashSet<string> DesignKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "laurels", "music-notes", "medal-ribbon", "starburst", "geometric", "academy-seal",
        "tricolour", "diya", "ganesh", "celebration"
    };

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CertificateSummary>>> List(Guid academyId, CancellationToken token) =>
        Ok(await dbContext.Certificates.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.IssuedDate)
            .Select(x => new CertificateSummary(x.Id, x.CertificateNumber, x.StudentId, x.BatchId, x.Title, x.TemplateKey, x.DesignKey, x.ArtworkX, x.ArtworkY, x.ArtworkSize, x.VerificationCode, x.IssuedDate, x.Status, x.Notes)).ToListAsync(token));

    [HttpGet("branding")]
    public async Task<ActionResult<CertificateBranding>> GetBranding(Guid academyId, CancellationToken token)
    {
        var academy = await dbContext.Academies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == academyId, token);
        return academy is null ? NotFound() : Ok(ToBranding(academy));
    }

    [HttpPut("branding")]
    public async Task<ActionResult<CertificateBranding>> UpdateBranding(Guid academyId, UpdateCertificateBrandingRequest request, CancellationToken token)
    {
        var academy = await dbContext.Academies.SingleOrDefaultAsync(x => x.Id == academyId, token);
        if (academy is null) return NotFound();
        academy.CertificateAccentColor = NormalizeAccent(request.AccentColor);
        academy.CertificateSignatoryName = Clean(request.SignatoryName, 140);
        await dbContext.SaveChangesAsync(token);
        return Ok(ToBranding(academy));
    }

    [HttpPost("branding/logo")]
    [RequestSizeLimit(2_500_000)]
    public async Task<ActionResult<CertificateBranding>> UploadLogo(Guid academyId, [FromForm] IFormFile logo, CancellationToken token)
    {
        var academy = await dbContext.Academies.SingleOrDefaultAsync(x => x.Id == academyId, token);
        if (academy is null) return NotFound();
        if (logo is null || logo.Length == 0 || logo.Length > 2_500_000)
            return BadRequest(new { message = "Upload a PNG, JPG or WebP logo smaller than 2.5 MB." });
        var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();
        var expectedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".webp"] = "image/webp" };
        if (!expectedTypes.TryGetValue(extension, out var expectedType) || !string.Equals(logo.ContentType, expectedType, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only PNG, JPG and WebP logo files are supported." });

        var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var directory = Path.Combine(root, "academy-assets", academyId.ToString("N"), "certificates");
        Directory.CreateDirectory(directory);
        var fileName = $"logo-{Guid.NewGuid():N}{extension}";
        await using var output = System.IO.File.Create(Path.Combine(directory, fileName));
        await logo.CopyToAsync(output, token);
        academy.CertificateLogoUrl = $"/academy-assets/{academyId:N}/certificates/{fileName}";
        await dbContext.SaveChangesAsync(token);
        return Ok(ToBranding(academy));
    }

    [HttpPost]
    public async Task<ActionResult<CertificateSummary>> Issue(Guid academyId, IssueCertificateRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Certificate title is required." });
        if (!TemplateKeys.Contains(request.TemplateKey ?? "classic")) return BadRequest(new { message = "Select a supported certificate template." });
        if (!DesignKeys.Contains(request.DesignKey ?? "laurels")) return BadRequest(new { message = "Select a supported certificate design." });
        if (!InRange(request.ArtworkX, 0, 100) || !InRange(request.ArtworkY, 0, 100) || !InRange(request.ArtworkSize, 36, 180)) return BadRequest(new { message = "Artwork placement is outside the certificate canvas." });
        if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The student does not belong to this academy." });
        if (request.BatchId.HasValue && !await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The class or batch does not belong to this academy." });

        var certificate = new Certificate
        {
            AcademyId = academyId, CertificateNumber = $"CERT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            StudentId = request.StudentId, BatchId = request.BatchId, Title = request.Title.Trim(),
            TemplateKey = request.TemplateKey?.Trim().ToLowerInvariant() ?? "classic", DesignKey = request.DesignKey?.Trim().ToLowerInvariant() ?? "laurels", ArtworkX = request.ArtworkX ?? 50, ArtworkY = request.ArtworkY ?? 30, ArtworkSize = request.ArtworkSize ?? 72, VerificationCode = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),
            IssuedDate = request.IssuedDate ?? DateOnly.FromDateTime(DateTime.UtcNow), Notes = Clean(request.Notes, 2000)
        };
        dbContext.Certificates.Add(certificate);
        dbContext.AuditLogs.Add(new AuditLog { AcademyId = academyId, Action = "CertificateIssued", EntityType = "Certificate", EntityId = certificate.Id });
        await dbContext.SaveChangesAsync(token);
        return Created($"/api/academies/{academyId}/certificates/{certificate.Id}", ToSummary(certificate));
    }

    [HttpPatch("{certificateId:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid academyId, Guid certificateId, UpdateCertificateStatusRequest request, CancellationToken token)
    {
        var certificate = await dbContext.Certificates.SingleOrDefaultAsync(x => x.Id == certificateId && x.AcademyId == academyId, token);
        if (certificate is null) return NotFound();
        if (!new[] { "Issued", "Revoked", "Replaced" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest();
        certificate.Status = request.Status.Trim();
        dbContext.AuditLogs.Add(new AuditLog { AcademyId = academyId, Action = "CertificateStatusChanged", EntityType = "Certificate", EntityId = certificate.Id });
        await dbContext.SaveChangesAsync(token);
        return Ok();
    }

    private static string? Clean(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];
    private static string? NormalizeAccent(string? value) => value is not null && System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$") ? value.ToUpperInvariant() : null;
    private static CertificateBranding ToBranding(Academy academy) => new(academy.Name, academy.CertificateLogoUrl, academy.CertificateAccentColor ?? "#0F6CBD", academy.CertificateSignatoryName);
    private static bool InRange(int? value, int minimum, int maximum) => !value.HasValue || (value.Value >= minimum && value.Value <= maximum);
    private static CertificateSummary ToSummary(Certificate x) => new(x.Id, x.CertificateNumber, x.StudentId, x.BatchId, x.Title, x.TemplateKey, x.DesignKey, x.ArtworkX, x.ArtworkY, x.ArtworkSize, x.VerificationCode, x.IssuedDate, x.Status, x.Notes);
}

public sealed record IssueCertificateRequest(Guid StudentId, Guid? BatchId, string Title, string? TemplateKey, string? DesignKey, int? ArtworkX, int? ArtworkY, int? ArtworkSize, DateOnly? IssuedDate, string? Notes);
public sealed record CertificateSummary(Guid Id, string CertificateNumber, Guid StudentId, Guid? BatchId, string Title, string TemplateKey, string DesignKey, int ArtworkX, int ArtworkY, int ArtworkSize, string VerificationCode, DateOnly IssuedDate, string Status, string? Notes);
public sealed record CertificateBranding(string AcademyName, string? LogoUrl, string AccentColor, string? SignatoryName);
public sealed record UpdateCertificateBrandingRequest(string? AccentColor, string? SignatoryName);
public sealed record UpdateCertificateStatusRequest(string Status);
