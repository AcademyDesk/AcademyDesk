using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    private const string ClassBrowserRoot = "~/api/academies/{academyId:guid}/penta/class-scheduling/browser";
    private const string ClassBrowserDraft = ClassBrowserRoot + "/conversations/{conversationId:guid}/drafts/{requestId:guid}";

    [HttpGet(ClassBrowserRoot + "/readiness")]
    public Task<ActionResult> ClassBrowserReadiness(Guid academyId, [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) =>
        ClassBrowserSafely(workflow, async () => { var r = await workflow.BrowserReadinessAsync(User, academyId, token); return ToResult(r.HttpStatus, r.Value, r.Error); });

    [HttpGet(ClassBrowserRoot + "/batches")]
    public Task<ActionResult> ClassBrowserBatches(Guid academyId, [FromQuery] string? q, [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) =>
        ClassBrowserSafely(workflow, async () => { var r = await workflow.BrowserLookupAsync(User, academyId, q, false, token); return ToResult(r.HttpStatus, r.Value, r.Error); });

    [HttpGet(ClassBrowserRoot + "/sessions")]
    public Task<ActionResult> ClassBrowserSessions(Guid academyId, [FromQuery] string? q, [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) =>
        ClassBrowserSafely(workflow, async () => { var r = await workflow.BrowserLookupAsync(User, academyId, q, true, token); return ToResult(r.HttpStatus, r.Value, r.Error); });

    [HttpPut(ClassBrowserRoot + "/conversations/{startRequestId:guid}"), RequestSizeLimit(1024)]
    public Task<ActionResult> ClassBrowserStart(Guid academyId, Guid startRequestId, [FromBody] JsonElement input, [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) =>
        ClassBrowserSafely(workflow, async () => { if (!Keys(input)) return ClassSessionBadInput(); var r = await workflow.BrowserStartAsync(User, academyId, startRequestId, true, token); return ToResult(r.HttpStatus, r.Value, r.Error); });

    [HttpGet(ClassBrowserRoot + "/starts/{startRequestId:guid}")]
    public Task<ActionResult> ClassBrowserStartResult(Guid academyId, Guid startRequestId, [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) =>
        ClassBrowserSafely(workflow, async () => { var r = await workflow.BrowserStartAsync(User, academyId, startRequestId, false, token); return ToResult(r.HttpStatus, r.Value, r.Error); });

    [HttpPut(ClassBrowserDraft), RequestSizeLimit(5 * 1024)]
    public Task<ActionResult> ClassBrowserPrepare(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input, [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) =>
        ClassBrowserSafely(workflow, async () =>
        {
            if (!Keys(input, "contextVersion", "payload") || !Integer(input, "contextVersion", 0, 0, out var version) || input.GetProperty("payload").ValueKind != JsonValueKind.Object) return ClassSessionBadInput();
            var r = await workflow.BrowserPrepareAsync(User, academyId, conversationId, requestId, version, input.GetProperty("payload").GetRawText(), token);
            return ToResult(r.HttpStatus, r.Draft, r.Error);
        });

    [HttpGet(ClassBrowserDraft)]
    public Task<ActionResult> ClassBrowserRead(Guid academyId, Guid conversationId, Guid requestId, [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) =>
        ClassBrowserSafely(workflow, async () => { var r = await workflow.BrowserReadAsync(User, academyId, conversationId, requestId, token); return ToResult(r.HttpStatus, r.Draft, r.Error); });

    [HttpPost(ClassBrowserDraft + "/approve"), RequestSizeLimit(1024)]
    public Task<ActionResult> ClassBrowserApprove(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input, [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) =>
        ClassBrowserSafely(workflow, async () =>
        {
            if (!Binding(input, true, out var revision, out var hash) || input.GetProperty("intent").ValueKind != JsonValueKind.String || input.GetProperty("intent").GetString() is not (PentaClassSessionSchedulingWorkflow.ApprovalIntent or PentaClassSessionSchedulingWorkflow.RescheduleIntent)) return ClassSessionBadInput();
            var r = await workflow.BrowserApproveAsync(User, academyId, conversationId, requestId, new(revision, hash), input.GetProperty("intent").GetString()!, token);
            return ToResult(r.HttpStatus, r.Draft, r.Error);
        });

    [HttpPost(ClassBrowserDraft + "/execute"), RequestSizeLimit(1024)]
    public Task<ActionResult> ClassBrowserExecute(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input, [FromServices] PentaClassSessionSchedulingWorkflow workflow, CancellationToken token) =>
        ClassBrowserSafely(workflow, async () =>
        {
            if (!Keys(input, "revision", "payloadHash", "approvalId") || !Integer(input, "revision", 1, 1, out var revision) || input.GetProperty("payloadHash").ValueKind != JsonValueKind.String || input.GetProperty("payloadHash").GetString() is not { Length: 64 } hash || hash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')) || input.GetProperty("approvalId").ValueKind != JsonValueKind.String || !input.GetProperty("approvalId").TryGetGuid(out var approval) || approval == Guid.Empty) return ClassSessionBadInput();
            var r = await workflow.BrowserExecuteAsync(User, academyId, conversationId, requestId, new(revision, hash, approval), token);
            return ToResult(r.HttpStatus, r.Draft, r.Error);
        });

    private Task<ActionResult> ClassBrowserSafely(PentaClassSessionSchedulingWorkflow workflow, Func<Task<ActionResult>> operation) => Safely(operation, "class scheduling browser", workflow.BrowserEnabled);
}
