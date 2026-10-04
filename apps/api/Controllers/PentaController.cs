using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AcademyDesk.Api.Domain.Identity;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/penta")]
public sealed class PentaController(PentaPilotPolicy policy, PentaExecutionService executions,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpPost("turns")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<ActionResult<PentaTurnState>> Turn(Guid academyId, PentaTurnRequest? request, CancellationToken token)
    {
        if (!policy.IsAvailable) return NotFound();
        if (!await policy.AllowsAsync(User, academyId, token)) return Forbid();

        if (request is null || request.AdditionalProperties is { Count: > 0 } ||
            string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 4000 ||
            request.Capability is not null && !PentaCapabilities.All.Contains(request.Capability))
            return BadRequest(new { message = "Invalid PENTA diagnostic request." });

        var key = Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrEmpty(key)) key = Guid.NewGuid().ToString("N");
        if (key.Length > 128 || key.Any(ch => ch < 33 || ch > 126))
            return BadRequest(new { message = "Invalid Idempotency-Key." });
        var actorId = (await users.GetUserAsync(User))!.Id;
        var outcome = await executions.RunAsync(academyId, actorId, request.Capability ?? "executor",
            request.Text!, key, token);
        if (outcome.HttpStatus == 409) return Conflict(new { message = outcome.Error });
        if (outcome.HttpStatus == 502) return StatusCode(502, new { message = outcome.Error, outcome.State });
        return StatusCode(outcome.HttpStatus, outcome.State);
    }

    [HttpGet("tasks/{taskId:guid}")]
    public async Task<ActionResult<PentaTurnState>> TaskStatus(Guid academyId, Guid taskId, CancellationToken token)
    {
        if (!policy.IsAvailable) return NotFound();
        if (!await policy.AllowsAsync(User, academyId, token)) return Forbid();
        var actorId = (await users.GetUserAsync(User))!.Id;
        var state = await executions.GetAsync(academyId, actorId, taskId, token);
        return state is null ? NotFound() : Ok(state);
    }
}
