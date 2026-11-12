using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    private const string EnrollmentLifecycleBrowserRoot = "~/api/academies/{academyId:guid}/penta/enrollments/lifecycle-browser";
    private const string EnrollmentLifecycleBrowserDraft = EnrollmentLifecycleBrowserRoot + "/conversations/{conversationId:guid}/drafts/{requestId:guid}";

    [HttpGet(EnrollmentLifecycleBrowserRoot + "/readiness")]
    public Task<ActionResult> EnrollmentLifecycleBrowserReadiness(Guid academyId, [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) =>
        LifecycleBrowserEnrollmentSafely(workflow, async () => { var result = await workflow.LifecycleBrowserReadinessAsync(User, academyId, token); return ToResult(result.HttpStatus, result.Value, result.Error); });

    [HttpGet(EnrollmentLifecycleBrowserRoot + "/search")]
    public Task<ActionResult> EnrollmentLifecycleBrowserSearch(Guid academyId, [FromQuery] string? q,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => LifecycleBrowserEnrollmentSafely(workflow, async () =>
        {
            var result = await workflow.LifecycleBrowserSearchAsync(User, academyId, q, token);
            return ToResult(result.HttpStatus, result.Value, result.Error);
        });

    [HttpPut(EnrollmentLifecycleBrowserRoot + "/conversations/{startRequestId:guid}"), RequestSizeLimit(1024)]
    public Task<ActionResult> EnrollmentLifecycleBrowserStart(Guid academyId, Guid startRequestId, [FromBody] JsonElement input,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => LifecycleBrowserEnrollmentSafely(workflow, async () =>
        {
            if (!Keys(input)) return EnrollmentBadInput();
            var result = await workflow.LifecycleBrowserStartAsync(User, academyId, startRequestId, true, token);
            return ToResult(result.HttpStatus, result.Value, result.Error);
        });

    [HttpGet(EnrollmentLifecycleBrowserRoot + "/starts/{startRequestId:guid}")]
    public Task<ActionResult> EnrollmentLifecycleBrowserStartResult(Guid academyId, Guid startRequestId,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => LifecycleBrowserEnrollmentSafely(workflow, async () =>
        {
            var result = await workflow.LifecycleBrowserStartAsync(User, academyId, startRequestId, false, token);
            return ToResult(result.HttpStatus, result.Value, result.Error);
        });

    [HttpPut(EnrollmentLifecycleBrowserDraft), RequestSizeLimit(9 * 1024)]
    public Task<ActionResult> EnrollmentLifecycleBrowserPrepare(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => LifecycleBrowserEnrollmentSafely(workflow, async () =>
        {
            if (!Keys(input, "contextVersion", "payload") || !Integer(input, "contextVersion", 0, 0, out var context) ||
                input.GetProperty("payload").ValueKind != JsonValueKind.Object) return EnrollmentBadInput();
            var result = await workflow.LifecycleBrowserPrepareAsync(User, academyId, conversationId, requestId, context, input.GetProperty("payload").GetRawText(), token);
            return ToResult(result.HttpStatus, result.Draft, result.Error);
        });

    [HttpGet(EnrollmentLifecycleBrowserDraft)]
    public Task<ActionResult> EnrollmentLifecycleBrowserRead(Guid academyId, Guid conversationId, Guid requestId,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => LifecycleBrowserEnrollmentSafely(workflow, async () =>
        {
            var result = await workflow.LifecycleBrowserReadAsync(User, academyId, conversationId, requestId, token);
            return ToResult(result.HttpStatus, result.Draft, result.Error);
        });

    [HttpPost(EnrollmentLifecycleBrowserDraft + "/approve"), RequestSizeLimit(1024)]
    public Task<ActionResult> EnrollmentLifecycleBrowserApprove(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => LifecycleBrowserEnrollmentSafely(workflow, async () =>
        {
            if (!Binding(input, true, out var revision, out var hash) || input.GetProperty("intent").ValueKind != JsonValueKind.String ||
                input.GetProperty("intent").GetString() is not (PentaEnrollmentCreationWorkflow.UpdateIntent or PentaEnrollmentCreationWorkflow.TransferIntent)) return EnrollmentBadInput();
            var result = await workflow.LifecycleBrowserApproveAsync(User, academyId, conversationId, requestId, new(revision, hash), input.GetProperty("intent").GetString()!, token);
            return ToResult(result.HttpStatus, result.Draft, result.Error);
        });

    [HttpPost(EnrollmentLifecycleBrowserDraft + "/execute"), RequestSizeLimit(1024)]
    public Task<ActionResult> EnrollmentLifecycleBrowserExecute(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => LifecycleBrowserEnrollmentSafely(workflow, async () =>
        {
            if (!Keys(input, "revision", "payloadHash", "approvalId") || !Integer(input, "revision", 1, 1, out var revision) ||
                input.GetProperty("payloadHash").ValueKind != JsonValueKind.String || input.GetProperty("payloadHash").GetString() is not { Length: 64 } hash ||
                hash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')) || input.GetProperty("approvalId").ValueKind != JsonValueKind.String ||
                !input.GetProperty("approvalId").TryGetGuid(out var approval) || approval == Guid.Empty) return EnrollmentBadInput();
            var result = await workflow.LifecycleBrowserExecuteAsync(User, academyId, conversationId, requestId, new(revision, hash, approval), token);
            return ToResult(result.HttpStatus, result.Draft, result.Error);
        });

    private Task<ActionResult> LifecycleBrowserEnrollmentSafely(PentaEnrollmentCreationWorkflow workflow, Func<Task<ActionResult>> operation) =>
        Safely(operation, "enrollment lifecycle browser", workflow.LifecycleBrowserEnabled);
}
