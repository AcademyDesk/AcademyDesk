using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    private const string ClassSessionDraftRoute = "~/api/academies/{academyId:guid}/penta/class-scheduling/conversations/{conversationId:guid}/drafts/{requestId:guid}";

    [HttpPut(ClassSessionDraftRoute), RequestSizeLimit(5 * 1024)]
    public Task<ActionResult> ClassSessionPrepare(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) => ClassSessionSafely(workflow, async () =>
        {
            if (!Keys(input, "contextVersion", "payload") || !Integer(input, "contextVersion", 0, 9007199254740991, out var context) ||
                input.GetProperty("payload").ValueKind != JsonValueKind.Object) return ClassSessionBadInput();
            var outcome = await workflow.PrepareAsync(User, academyId, conversationId, requestId, context, input.GetProperty("payload").GetRawText(), token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    [HttpGet(ClassSessionDraftRoute)]
    public Task<ActionResult> ClassSessionRead(Guid academyId, Guid conversationId, Guid requestId,
        [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) => ClassSessionSafely(workflow, async () =>
        {
            var outcome = await workflow.ReadAsync(User, academyId, conversationId, requestId, token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    [HttpPost(ClassSessionDraftRoute + "/approve"), RequestSizeLimit(1024)]
    public Task<ActionResult> ClassSessionApprove(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) => ClassSessionSafely(workflow, async () =>
        {
            if (!Binding(input, true, out var revision, out var hash) || input.GetProperty("intent").ValueKind != JsonValueKind.String ||
                input.GetProperty("intent").GetString() != PentaClassSessionSchedulingWorkflow.ApprovalIntent) return ClassSessionBadInput();
            var outcome = await workflow.ApproveAsync(User, academyId, conversationId, requestId, new(revision, hash), token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    [HttpPost(ClassSessionDraftRoute + "/execute"), RequestSizeLimit(1024)]
    public Task<ActionResult> ClassSessionExecute(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) => ClassSessionSafely(workflow, async () =>
        {
            if (!Keys(input, "revision", "payloadHash", "approvalId") || !Integer(input, "revision", 1, 1, out var revision) ||
                input.GetProperty("payloadHash").ValueKind != JsonValueKind.String ||
                input.GetProperty("payloadHash").GetString() is not { Length: 64 } hash ||
                hash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')) ||
                input.GetProperty("approvalId").ValueKind != JsonValueKind.String ||
                !input.GetProperty("approvalId").TryGetGuid(out var approvalId) || approvalId == Guid.Empty) return ClassSessionBadInput();
            var outcome = await workflow.ExecuteAsync(User, academyId, conversationId, requestId, new(revision, hash, approvalId), token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    private Task<ActionResult> ClassSessionSafely(PentaClassSessionSchedulingWorkflow workflow, Func<Task<ActionResult>> operation) =>
        Safely(operation, "class scheduling", workflow.Enabled);

    private ActionResult ClassSessionBadInput() => BadRequest(new { message = "Enter the expected class scheduling draft and confirmation fields." });
}
