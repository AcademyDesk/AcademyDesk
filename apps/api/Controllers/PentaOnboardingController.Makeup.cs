using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    private const string MakeupDraftRoute = "~/api/academies/{academyId:guid}/penta/makeup/conversations/{conversationId:guid}/drafts/{requestId:guid}";

    [HttpPut(MakeupDraftRoute), RequestSizeLimit(9 * 1024)]
    public Task<ActionResult> MakeupPrepare(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaMakeupSchedulingWorkflow workflow, CancellationToken token) => MakeupSafely(workflow, async () =>
        {
            if (!Keys(input, "contextVersion", "payload") || !Integer(input, "contextVersion", 0, 9007199254740991, out var context) ||
                input.GetProperty("payload").ValueKind != JsonValueKind.Object) return MakeupBadInput();
            var outcome = await workflow.PrepareAsync(User, academyId, conversationId, requestId, context, input.GetProperty("payload").GetRawText(), token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    [HttpGet(MakeupDraftRoute)]
    public Task<ActionResult> MakeupRead(Guid academyId, Guid conversationId, Guid requestId,
        [FromServices] PentaMakeupSchedulingWorkflow workflow, CancellationToken token) => MakeupSafely(workflow, async () =>
        {
            var outcome = await workflow.ReadAsync(User, academyId, conversationId, requestId, token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    [HttpPost(MakeupDraftRoute + "/approve"), RequestSizeLimit(1024)]
    public Task<ActionResult> MakeupApprove(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaMakeupSchedulingWorkflow workflow, CancellationToken token) => MakeupSafely(workflow, async () =>
        {
            if (!Binding(input, true, out var revision, out var hash) || input.GetProperty("intent").ValueKind != JsonValueKind.String ||
                input.GetProperty("intent").GetString() != PentaMakeupSchedulingWorkflow.ApprovalIntent) return MakeupBadInput();
            var outcome = await workflow.ApproveAsync(User, academyId, conversationId, requestId, new(revision, hash), token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    [HttpPost(MakeupDraftRoute + "/execute"), RequestSizeLimit(1024)]
    public Task<ActionResult> MakeupExecute(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaMakeupSchedulingWorkflow workflow, CancellationToken token) => MakeupSafely(workflow, async () =>
        {
            if (!Keys(input, "revision", "payloadHash", "approvalId") || !Integer(input, "revision", 1, 1, out var revision) ||
                input.GetProperty("payloadHash").ValueKind != JsonValueKind.String ||
                input.GetProperty("payloadHash").GetString() is not { Length: 64 } hash ||
                hash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')) ||
                input.GetProperty("approvalId").ValueKind != JsonValueKind.String ||
                !input.GetProperty("approvalId").TryGetGuid(out var approvalId) || approvalId == Guid.Empty) return MakeupBadInput();
            var outcome = await workflow.ExecuteAsync(User, academyId, conversationId, requestId, new(revision, hash, approvalId), token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    private Task<ActionResult> MakeupSafely(PentaMakeupSchedulingWorkflow workflow, Func<Task<ActionResult>> operation) =>
        Safely(operation, "make-up scheduling", workflow.Enabled);

    private ActionResult MakeupBadInput() => BadRequest(new { message = "Enter the expected make-up draft and confirmation fields." });
}
