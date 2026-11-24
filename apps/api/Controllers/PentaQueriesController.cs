using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController, Authorize(AuthenticationSchemes = "Identity.Bearer")]
[Route("api/academies/{academyId:guid}/penta/queries")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PentaQueriesController(PentaQueryConversations conversation, ILogger<PentaQueriesController> logger) : ControllerBase
{
    [HttpGet("health")]
    public Task<ActionResult> Health(Guid academyId, CancellationToken token) => Safe(() => conversation.HealthAsync(User, academyId, token));
    [HttpPost("conversations"), RequestSizeLimit(1024)]
    public Task<ActionResult> Create(Guid academyId, CancellationToken token) => Safe(() => conversation.CreateAsync(User, academyId, token));
    [HttpPost("conversations/{conversationId:guid}/turns"), RequestSizeLimit(12 * 1024)]
    public Task<ActionResult> Turn(Guid academyId, Guid conversationId, PentaTaskTurnInput? input, CancellationToken token) =>
        Safe(() => conversation.TurnAsync(User, academyId, conversationId, input, token));
    [HttpGet("conversations/{conversationId:guid}/turns/{requestId:guid}")]
    public Task<ActionResult> Read(Guid academyId, Guid conversationId, Guid requestId, CancellationToken token) =>
        Safe(() => conversation.ReadAsync(User, academyId, conversationId, requestId, null, token));
    [HttpGet("conversations/{conversationId:guid}")]
    public Task<ActionResult> History(Guid academyId, Guid conversationId, [FromQuery] long? beforeVersion, CancellationToken token) =>
        Safe(() => conversation.ReadAsync(User, academyId, conversationId, null, beforeVersion, token));
    private async Task<ActionResult> Safe(Func<Task<PentaConversationOutcome>> action)
    {
        try {
            var result = await action();
            return result.HttpStatus == 403 ? Forbid() : StatusCode(result.HttpStatus, result.Value ?? new { message = result.Error });
        } catch (Exception ex) when (ex is DbException or DbUpdateException or CryptographicException or JsonException or InvalidOperationException) {
            logger.LogWarning("PENTA query failed ({FailureType}); trace {TraceId}", ex.GetType().Name, HttpContext.TraceIdentifier);
            return StatusCode(503, new { message = "This conversation could not be verified. Recover the pending response or use Manual; do not repeat the request." });
        }
    }
}
