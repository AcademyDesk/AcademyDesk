using System.Data.Common;
using System.Security.Cryptography;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController,AllowAnonymous,Route("internal/penta/onboarding/current")]
public sealed class PentaPrivateContextController(IHostEnvironment environment,IConfiguration configuration,
    PentaPrivateContextLeases? leases=null):ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Current(CancellationToken token) {
        Response.Headers.CacheControl="no-store";Response.Headers.Pragma="no-cache";
        if(!environment.IsEnvironment("Testing")||leases is null||
            !new[]{"OperationalDraftContractsEnabled","OperationalOnboardingExecutionContractsEnabled","OperationalOnboardingHttpContractsEnabled",
                "OperationalOnboardingGatewayContractsEnabled","OperationalOnboardingPrivateBridgeContractsEnabled"}.All(x=>configuration.GetValue<bool>("Penta:"+x)))return NotFound();
        if(Request.QueryString.HasValue||Request.Headers.Authorization.Count!=1||Request.Headers["X-Penta-Request"].Count!=1||
            Request.Headers[PentaPrivateContextLeases.BodyHeader].Count!=1||
            Request.Headers[PentaPrivateContextLeases.VersionHeader].Count!=1||Request.Headers[PentaPrivateContextLeases.VersionHeader]!=PentaPrivateContextLeases.Version)
            return NotFound();
        var auth=Request.Headers.Authorization.ToString();
        if(!auth.StartsWith("Bearer ",StringComparison.Ordinal)||!Guid.TryParseExact(Request.Headers["X-Penta-Request"],"D",out var request))return NotFound();
        try {
            var snapshot=await leases.ReadAsync(auth[7..],request,Request.Headers[PentaPrivateContextLeases.BodyHeader].ToString(),token);
            if(snapshot is null)return NotFound();
            Response.Headers[PentaPrivateContextLeases.VersionHeader]=PentaPrivateContextLeases.Version;
            return Ok(snapshot);
        }catch(Exception ex)when(ex is HttpRequestException or DbException or DbUpdateException or CryptographicException or InvalidOperationException) {
            return StatusCode(503,new{code="unavailable",retry_write=false});
        }
    }
}
