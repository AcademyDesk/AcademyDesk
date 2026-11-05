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
    PentaDraftApprovalService drafts, PentaAcademyContextService academyContext,
    PentaStudentSearchService studentSearch, PentaBatchSearchService batchSearch,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet("batch-search")]
    public async Task<ActionResult<PentaBatchSearchResult>> BatchSearch(Guid academyId,
        [FromQuery] string? q, CancellationToken token)
    {
        if (!policy.IsAvailable) return NotFound();
        if (!await policy.AllowsAsync(User, academyId, token)) return Forbid();
        if (!PentaBatchSearchService.TryNormalizeQuery(q, out var query))
            return BadRequest(new { message = "Enter 2 to 80 searchable characters." });
        var actorId = (await users.GetUserAsync(User))!.Id;
        var result = await batchSearch.ReadAsync(academyId, actorId, query, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("student-search")]
    public async Task<ActionResult<PentaStudentSearchResult>> StudentSearch(Guid academyId,
        [FromQuery] string? q, CancellationToken token)
    {
        if (!policy.IsAvailable) return NotFound();
        if (!await policy.AllowsAsync(User, academyId, token)) return Forbid();
        if (!PentaStudentSearchService.TryNormalizeQuery(q, out var query))
            return BadRequest(new { message = "Enter 2 to 80 searchable characters." });
        var actorId = (await users.GetUserAsync(User))!.Id;
        var result = await studentSearch.ReadAsync(academyId, actorId, query, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("academy-context")]
    public async Task<ActionResult<PentaAcademyContext>> AcademyContext(Guid academyId, CancellationToken token)
    {
        if (!policy.IsAvailable) return NotFound();
        if (!await policy.AllowsAsync(User, academyId, token)) return Forbid();
        var actorId = (await users.GetUserAsync(User))!.Id;
        var result = await academyContext.ReadAsync(academyId, actorId, token);
        return result is null ? NotFound() : Ok(result);
    }

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
        if (outcome.HttpStatus is 403 or 429 or 503)
            return StatusCode(outcome.HttpStatus, new { message = outcome.Error });
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

    [HttpPost("draft-previews")]
    [RequestSizeLimit(4 * 1024)]
    public async Task<ActionResult<PentaDraftPreviewState>> PrepareDraft(Guid academyId,
        PentaDraftPreviewRequest? request, CancellationToken token)
    {
        if (!policy.IsAvailable) return NotFound();
        if (request is null || request.AdditionalProperties is { Count: > 0 })
            return BadRequest(new { message = "Invalid draft preview request." });
        return DraftOutcome(await drafts.PrepareAsync(User, academyId, request.Title,
            Request.Headers["Idempotency-Key"].ToString(), token));
    }

    [HttpGet("draft-previews/{approvalId:guid}")]
    public async Task<ActionResult<PentaDraftPreviewState>> DraftPreview(Guid academyId,
        Guid approvalId, CancellationToken token)
    {
        if (!policy.IsAvailable) return NotFound();
        return DraftOutcome(await drafts.GetAsync(User, academyId, approvalId, token));
    }

    [HttpPost("draft-previews/{approvalId:guid}/confirm")]
    [RequestSizeLimit(4 * 1024)]
    public async Task<ActionResult<PentaDraftPreviewState>> ConfirmDraft(Guid academyId,
        Guid approvalId, PentaDraftConfirmRequest? request, CancellationToken token)
    {
        if (!policy.IsAvailable) return NotFound();
        if (request is null || request.AdditionalProperties is { Count: > 0 })
            return BadRequest(new { message = "Invalid draft confirmation request." });
        return DraftOutcome(await drafts.ConfirmAsync(User, academyId, approvalId, request.Digest, token));
    }

    private ActionResult<PentaDraftPreviewState> DraftOutcome(PentaDraftApprovalOutcome outcome) =>
        outcome.HttpStatus switch
        {
            403 => Forbid(),
            404 => NotFound(),
            409 => Conflict(new { message = outcome.Error }),
            400 => BadRequest(new { message = outcome.Error }),
            _ => StatusCode(outcome.HttpStatus, outcome.State is null
                ? new { message = outcome.Error } : outcome.State)
        };
}

public sealed record PentaDraftPreviewRequest(string? Title)
{
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? AdditionalProperties { get; init; }
}

public sealed record PentaDraftConfirmRequest(string? Digest)
{
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? AdditionalProperties { get; init; }
}
