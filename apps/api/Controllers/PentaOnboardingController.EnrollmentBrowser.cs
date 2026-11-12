using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    private const string EnrollmentBrowserRoot = "~/api/academies/{academyId:guid}/penta/enrollments";
    private const string EnrollmentBrowserDraft = EnrollmentBrowserRoot + "/browser-conversations/{conversationId:guid}/drafts/{requestId:guid}";

    [HttpGet(EnrollmentBrowserRoot + "/browser-readiness")]
    public Task<ActionResult> EnrollmentBrowserReadiness(Guid academyId, [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) =>
        BrowserEnrollmentSafely(workflow, async () => { var result = await workflow.BrowserReadinessAsync(User, academyId, token); return ToResult(result.HttpStatus, result.Value, result.Error); });

    [HttpPut(EnrollmentBrowserRoot + "/browser-conversations/{startRequestId:guid}"), RequestSizeLimit(1024)]
    public Task<ActionResult> EnrollmentBrowserStart(Guid academyId, Guid startRequestId, [FromBody] JsonElement input,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => BrowserEnrollmentSafely(workflow, async () =>
        {
            if (!Keys(input)) return EnrollmentBadInput();
            var result = await workflow.BrowserStartAsync(User, academyId, startRequestId, true, token);
            return ToResult(result.HttpStatus, result.Value, result.Error);
        });

    [HttpGet(EnrollmentBrowserRoot + "/browser-starts/{startRequestId:guid}")]
    public Task<ActionResult> EnrollmentBrowserStartResult(Guid academyId, Guid startRequestId,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => BrowserEnrollmentSafely(workflow, async () =>
        {
            var result = await workflow.BrowserStartAsync(User, academyId, startRequestId, false, token);
            return ToResult(result.HttpStatus, result.Value, result.Error);
        });

    [HttpPut(EnrollmentBrowserDraft), RequestSizeLimit(9 * 1024)]
    public Task<ActionResult> EnrollmentBrowserPrepare(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => BrowserEnrollmentSafely(workflow, async () =>
        {
            if (!Keys(input, "contextVersion", "payload") || !Integer(input, "contextVersion", 0, 0, out var context) ||
                input.GetProperty("payload").ValueKind != JsonValueKind.Object) return EnrollmentBadInput();
            var result = await workflow.BrowserPrepareAsync(User, academyId, conversationId, requestId, context, input.GetProperty("payload").GetRawText(), token);
            return ToResult(result.HttpStatus, result.Draft, result.Error);
        });

    [HttpGet(EnrollmentBrowserDraft)]
    public Task<ActionResult> EnrollmentBrowserRead(Guid academyId, Guid conversationId, Guid requestId,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => BrowserEnrollmentSafely(workflow, async () =>
        {
            var result = await workflow.BrowserReadAsync(User, academyId, conversationId, requestId, token);
            return ToResult(result.HttpStatus, result.Draft, result.Error);
        });

    [HttpPost(EnrollmentBrowserDraft + "/approve"), RequestSizeLimit(1024)]
    public Task<ActionResult> EnrollmentBrowserApprove(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => BrowserEnrollmentSafely(workflow, async () =>
        {
            if (!Binding(input, true, out var revision, out var hash) || input.GetProperty("intent").ValueKind != JsonValueKind.String ||
                input.GetProperty("intent").GetString() != PentaEnrollmentCreationWorkflow.ApprovalIntent) return EnrollmentBadInput();
            var result = await workflow.BrowserApproveAsync(User, academyId, conversationId, requestId, new(revision, hash), token);
            return ToResult(result.HttpStatus, result.Draft, result.Error);
        });

    [HttpPost(EnrollmentBrowserDraft + "/execute"), RequestSizeLimit(1024)]
    public Task<ActionResult> EnrollmentBrowserExecute(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaEnrollmentCreationWorkflow workflow, CancellationToken token) => BrowserEnrollmentSafely(workflow, async () =>
        {
            if (!Keys(input, "revision", "payloadHash", "approvalId") || !Integer(input, "revision", 1, 1, out var revision) ||
                input.GetProperty("payloadHash").ValueKind != JsonValueKind.String || input.GetProperty("payloadHash").GetString() is not { Length: 64 } hash ||
                hash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')) || input.GetProperty("approvalId").ValueKind != JsonValueKind.String ||
                !input.GetProperty("approvalId").TryGetGuid(out var approval) || approval == Guid.Empty) return EnrollmentBadInput();
            var result = await workflow.BrowserExecuteAsync(User, academyId, conversationId, requestId, new(revision, hash, approval), token);
            return ToResult(result.HttpStatus, result.Draft, result.Error);
        });

    private Task<ActionResult> BrowserEnrollmentSafely(PentaEnrollmentCreationWorkflow workflow, Func<Task<ActionResult>> operation) =>
        Safely(operation, "enrollment browser", workflow.BrowserEnabled);
}
