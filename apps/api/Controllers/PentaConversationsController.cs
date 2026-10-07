using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/penta/conversations")]
public sealed class PentaConversationsController(PentaConversationService conversations) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(4 * 1024)]
    public async Task<ActionResult> Create(Guid academyId, PentaConversationCreateRequest? request, CancellationToken token) =>
        Result(await conversations.CreateAsync(User, academyId, request, token));

    [HttpGet("{conversationId:guid}")]
    public async Task<ActionResult> Read(Guid academyId, Guid conversationId, CancellationToken token) =>
        Result(await conversations.ReadAsync(User, academyId, conversationId, token));

    [HttpPost("{conversationId:guid}/turns")]
    [RequestSizeLimit(4 * 1024)]
    public async Task<ActionResult> Turn(Guid academyId, Guid conversationId,
        PentaConversationTurnRequest? request, CancellationToken token) =>
        Result(await conversations.AppendAsync(User, academyId, conversationId, request, token));

    private ActionResult Result(PentaConversationOutcome outcome) => outcome.HttpStatus switch
    {
        403 => Forbid(),
        404 => NotFound(),
        _ => StatusCode(outcome.HttpStatus, outcome.Value ?? new { message = outcome.Error })
    };
}
