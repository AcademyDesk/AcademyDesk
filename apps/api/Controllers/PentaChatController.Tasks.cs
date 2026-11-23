using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaChatController
{
    [HttpGet("tasks/catalog")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<ActionResult> TaskCatalog(Guid academyId, [FromServices] PentaTaskConversations tasks, CancellationToken token) =>
        Safely(() => tasks.CatalogAsync(User, academyId, token));

    [HttpPost("tasks/conversations"), RequestSizeLimit(1024)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<ActionResult> CreateTaskConversation(Guid academyId, [FromServices] PentaTaskConversations tasks, CancellationToken token) =>
        Safely(() => tasks.CreateAsync(User, academyId, token));

    [HttpGet("tasks/conversations/{conversationId:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<ActionResult> TaskHistory(Guid academyId, Guid conversationId, long? beforeVersion,
        [FromServices] PentaTaskConversations tasks, CancellationToken token) =>
        Safely(() => tasks.HistoryAsync(User, academyId, conversationId, beforeVersion, token));

    [HttpPost("tasks/conversations/{conversationId:guid}/turns"), RequestSizeLimit(12 * 1024)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<ActionResult> TaskTurn(Guid academyId, Guid conversationId, PentaTaskTurnInput? input,
        [FromServices] PentaTaskConversations tasks, CancellationToken token) =>
        Safely(() => tasks.TurnAsync(User, academyId, conversationId, input, token));

    [HttpGet("tasks/conversations/{conversationId:guid}/turns/{requestId:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<ActionResult> RecoverTaskTurn(Guid academyId, Guid conversationId, Guid requestId,
        [FromServices] PentaTaskConversations tasks, CancellationToken token) =>
        Safely(() => tasks.ReadAsync(User, academyId, conversationId, requestId, token));
}
