using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    private const string EnrollmentDraftRoute = "~/api/academies/{academyId:guid}/penta/enrollments/conversations/{conversationId:guid}/drafts/{requestId:guid}";

    [HttpPut(EnrollmentDraftRoute), RequestSizeLimit(9 * 1024)]
    public Task<ActionResult> EnrollmentPrepare(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) =>
        EnrollmentSafely(workflow, async () =>
        {
            if (!Keys(input, "contextVersion", "payload") || !Integer(input, "contextVersion", 0, 9007199254740991, out var context) ||
                input.GetProperty("payload").ValueKind != JsonValueKind.Object) return EnrollmentBadInput();
            var outcome = await workflow.PrepareAsync(User, academyId, conversationId, requestId, context,
                input.GetProperty("payload").GetRawText(), token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    [HttpGet(EnrollmentDraftRoute)]
    public Task<ActionResult> EnrollmentRead(Guid academyId, Guid conversationId, Guid requestId,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) =>
        EnrollmentSafely(workflow, async () =>
        {
            var outcome = await workflow.ReadAsync(User, academyId, conversationId, requestId, token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    [HttpPost(EnrollmentDraftRoute + "/approve"), RequestSizeLimit(1024)]
    public Task<ActionResult> EnrollmentApprove(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) =>
        EnrollmentSafely(workflow, async () =>
        {
            if (!Binding(input, true, out var revision, out var hash) ||
                input.GetProperty("intent").ValueKind != JsonValueKind.String ||
                input.GetProperty("intent").GetString() is not (PentaEnrollmentCreationWorkflow.ApprovalIntent or
                    PentaEnrollmentCreationWorkflow.UpdateIntent or PentaEnrollmentCreationWorkflow.TransferIntent)) return EnrollmentBadInput();
            var outcome = await workflow.ApproveAsync(User, academyId, conversationId, requestId, new(revision, hash),
                input.GetProperty("intent").GetString()!, token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    [HttpPost(EnrollmentDraftRoute + "/execute"), RequestSizeLimit(1024)]
    public Task<ActionResult> EnrollmentExecute(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) =>
        EnrollmentSafely(workflow, async () =>
        {
            if (!Keys(input, "revision", "payloadHash", "approvalId") || !Integer(input, "revision", 1, 1, out var revision) ||
                input.GetProperty("payloadHash").ValueKind != JsonValueKind.String ||
                input.GetProperty("payloadHash").GetString() is not { Length: 64 } hash ||
                hash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')) ||
                input.GetProperty("approvalId").ValueKind != JsonValueKind.String ||
                !input.GetProperty("approvalId").TryGetGuid(out var approvalId) || approvalId == Guid.Empty) return EnrollmentBadInput();
            var outcome = await workflow.ExecuteAsync(User, academyId, conversationId, requestId, new(revision, hash, approvalId), token);
            return ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
        });

    private Task<ActionResult> EnrollmentSafely(PentaEnrollmentCreationWorkflow workflow, Func<Task<ActionResult>> operation) =>
        Safely(operation, "enrollment", workflow.Enabled);

    private ActionResult EnrollmentBadInput() => BadRequest(new { message = "Enter the expected enrollment draft and confirmation fields." });
}
