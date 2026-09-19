using AcademyDesk.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/certificates/verify")]
public sealed class CertificateVerificationController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet("{verificationCode}")]
    public async Task<ActionResult<VerifiedCertificate>> Verify(string verificationCode, CancellationToken token)
    {
        var certificate = await dbContext.Certificates.AsNoTracking().Where(x => x.VerificationCode == verificationCode.Trim().ToUpperInvariant())
            .Join(dbContext.Academies.AsNoTracking(), certificate => certificate.AcademyId, academy => academy.Id,
                (certificate, academy) => new VerifiedCertificate(academy.Name, certificate.CertificateNumber, certificate.Title, certificate.IssuedDate, certificate.Status))
            .SingleOrDefaultAsync(token);
        return certificate is null ? NotFound(new { message = "Certificate verification code was not found." }) : Ok(certificate);
    }
}

public sealed record VerifiedCertificate(string AcademyName, string CertificateNumber, string Title, DateOnly IssuedDate, string Status);
