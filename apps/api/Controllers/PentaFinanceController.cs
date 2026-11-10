using System.Data.Common;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Identity.Bearer")]
[Route("api/academies/{academyId:guid}/penta/finance")]
public sealed class PentaFinanceController(PentaFinanceReadService reads, ILogger<PentaFinanceController> logger) : ControllerBase
{
    [HttpGet("overview")]
    public Task<ActionResult> Overview(Guid academyId, CancellationToken token) => Read(academyId, PentaFinanceReadKind.Overview, null, token);

    [HttpGet("invoices/{invoiceId:guid}")]
    public Task<ActionResult> Invoice(Guid academyId, Guid invoiceId, CancellationToken token) => Read(academyId, PentaFinanceReadKind.Invoice, invoiceId, token);

    [HttpGet("collections")]
    public Task<ActionResult> Collections(Guid academyId, CancellationToken token) => Read(academyId, PentaFinanceReadKind.Collections, null, token);

    [HttpGet("invoices/{invoiceId:guid}/reconciliation")]
    public Task<ActionResult> Reconciliation(Guid academyId, Guid invoiceId, CancellationToken token) =>
        Read(academyId, PentaFinanceReadKind.Reconciliation, invoiceId, token);

    private async Task<ActionResult> Read(Guid academyId, PentaFinanceReadKind kind, Guid? invoiceId, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            var result = await reads.ReadAsync(User, academyId, kind, invoiceId, token);
            return result.Status switch
            {
                200 => Ok(result.Value),
                403 => Forbid(IdentityConstants.BearerScheme),
                _ => NotFound()
            };
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException or InvalidOperationException)
        {
            logger.LogWarning("PENTA finance read could not be verified ({FailureType}); trace {TraceId}",
                ex.GetType().Name, HttpContext.TraceIdentifier);
            return StatusCode(503, new { message = "The finance read could not be verified. Try again or use the Finance workspace." });
        }
    }
}
