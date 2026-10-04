using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/penta")]
public sealed class PentaController(PentaPilotPolicy policy, PentaSyntheticDispatcher dispatcher) : ControllerBase
{
    [HttpPost("turns")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<ActionResult<PentaSyntheticResult>> Turn(Guid academyId, PentaTurnRequest? request, CancellationToken token)
    {
        if (!policy.IsAvailable) return NotFound();
        if (!await policy.AllowsAsync(User, academyId, token)) return Forbid();

        if (request is null || request.AdditionalProperties is { Count: > 0 } ||
            string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 4000 ||
            request.Capability is not null && !PentaCapabilities.All.Contains(request.Capability))
            return BadRequest(new { message = "Invalid PENTA diagnostic request." });

        var capability = request.Capability ?? "executor";
        var result = await dispatcher.RunAsync(capability, token);
        return result is null
            ? StatusCode(StatusCodes.Status502BadGateway, new { message = "PENTA diagnostic response was invalid." })
            : Ok(result);
    }
}
