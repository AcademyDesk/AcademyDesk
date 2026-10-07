using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Cryptography;

namespace AcademyDesk.Api.Controllers;

[ApiController, Authorize]
[Route("api/academies/{academyId:guid}/penta/chat")]
public sealed class PentaChatController(PentaMiniOrchestrator chat, ILogger<PentaChatController> logger) : ControllerBase
{
    [HttpGet("health")]
    public async Task<ActionResult> Health(Guid academyId, CancellationToken token) => Result(await chat.HealthAsync(User, academyId, token));
    [HttpPost("conversations"), RequestSizeLimit(1024)]
    public Task<ActionResult> Create(Guid academyId, CancellationToken token) => Safely(() => chat.CreateAsync(User, academyId, token));
    [HttpPost("conversations/{conversationId:guid}/turns"), RequestSizeLimit(12 * 1024)]
    public Task<ActionResult> Turn(Guid academyId, Guid conversationId, MiniTurnInput? input, CancellationToken token) =>
        Safely(() => chat.TurnAsync(User, academyId, conversationId, input, token));
    private async Task<ActionResult> Safely(Func<Task<PentaConversationOutcome>> operation)
    {
        try { return Result(await operation()); }
        catch (Exception ex) when (ex is DbException or DbUpdateException or CryptographicException or InvalidOperationException)
        {
            // No SQL text, prompts, credentials or record values in failure logs.
            // A failed audit/store transaction must never become a success receipt.
            logger.LogWarning("PENTA store/read failed ({FailureType}); trace {TraceId}", ex.GetType().Name, HttpContext.TraceIdentifier);
            return StatusCode(503, new { message = "PENTA could not verify or record this read. Do not repeat a pending request. Start a new conversation or use the manual workspace." });
        }
    }
    private ActionResult Result(PentaConversationOutcome result) => result.HttpStatus switch
    { 403 => Forbid(), 404 => NotFound(), _ => StatusCode(result.HttpStatus, result.Value ?? new { message = result.Error }) };
}
