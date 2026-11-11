using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    private const string AdjustmentConversationRoot = "~/api/academies/{academyId:guid}/penta/invoice-adjustments/conversations";
    private const string AdjustmentDraftRoute = AdjustmentConversationRoot + "/{conversationId:guid}/drafts/{requestId:guid}";

    [HttpPut(AdjustmentConversationRoot + "/{startRequestId:guid}"), RequestSizeLimit(1024)]
    public Task<ActionResult> AdjustmentStart(Guid academyId, Guid startRequestId,
        [FromServices] PentaInvoiceAdjustmentWorkflow workflow, CancellationToken token) =>
        FinanceWriteSafely("invoice adjustment proposal", workflow.Enabled, () => workflow.StartAsync(User, academyId, startRequestId, token));

    [HttpPut(AdjustmentDraftRoute), RequestSizeLimit(4 * 1024)]
    public Task<ActionResult> AdjustmentPrepare(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaInvoiceAdjustmentWorkflow workflow, CancellationToken token) =>
        FinanceWriteSafely("invoice adjustment proposal", workflow.Enabled, () =>
        {
            if (!Keys(input, "contextVersion", "payload") || !Integer(input, "contextVersion", 0, 9007199254740991, out var context) ||
                input.GetProperty("payload").ValueKind != JsonValueKind.Object) return InvalidAdjustmentInput();
            return workflow.PrepareAsync(User, academyId, conversationId, requestId, context, input.GetProperty("payload").GetRawText(), token);
        });

    [HttpGet(AdjustmentDraftRoute)]
    public Task<ActionResult> AdjustmentRead(Guid academyId, Guid conversationId, Guid requestId,
        [FromServices] PentaInvoiceAdjustmentWorkflow workflow, CancellationToken token) =>
        FinanceWriteSafely("invoice adjustment proposal", workflow.Enabled, () => workflow.ReadAsync(User, academyId, conversationId, requestId, token));

    [HttpPost(AdjustmentDraftRoute + "/approve"), RequestSizeLimit(1024)]
    public Task<ActionResult> AdjustmentAuthorize(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaInvoiceAdjustmentWorkflow workflow, CancellationToken token) =>
        FinanceWriteSafely("invoice adjustment proposal", workflow.Enabled, () =>
        {
            if (!Binding(input, true, out var revision, out var hash) || input.GetProperty("intent").ValueKind != JsonValueKind.String ||
                input.GetProperty("intent").GetString() != PentaInvoiceAdjustmentWorkflow.ApprovalIntent) return InvalidAdjustmentInput();
            return workflow.ApproveAsync(User, academyId, conversationId, requestId, new(revision, hash), token);
        });

    [HttpPost(AdjustmentDraftRoute + "/execute"), RequestSizeLimit(1024)]
    public Task<ActionResult> AdjustmentSubmit(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaInvoiceAdjustmentWorkflow workflow, CancellationToken token) =>
        FinanceWriteSafely("invoice adjustment proposal", workflow.Enabled, () =>
        {
            var binding = FinanceExecutionBinding(input);
            return binding is null ? InvalidAdjustmentInput() : workflow.ExecuteAsync(User, academyId, conversationId, requestId, binding, token);
        });

    private static Task<PentaConversationOutcome> InvalidAdjustmentInput() =>
        Task.FromResult(new PentaConversationOutcome(400, Error: "Enter the expected adjustment draft and authorization fields."));
}
