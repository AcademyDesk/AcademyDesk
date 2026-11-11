using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    private const string PaymentConversationRoot = "~/api/academies/{academyId:guid}/penta/payment-collection/conversations";
    private const string PaymentDraftRoute = PaymentConversationRoot + "/{conversationId:guid}/drafts/{requestId:guid}";

    [HttpPut(PaymentConversationRoot + "/{startRequestId:guid}"), RequestSizeLimit(1024)]
    public Task<ActionResult> PaymentStart(Guid academyId, Guid startRequestId,
        [FromServices] PentaPaymentCollectionWorkflow workflow, CancellationToken token) =>
        PaymentSafely(workflow, () => workflow.StartAsync(User, academyId, startRequestId, token));

    [HttpPut(PaymentDraftRoute), RequestSizeLimit(4 * 1024)]
    public Task<ActionResult> PaymentPrepare(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaPaymentCollectionWorkflow workflow, CancellationToken token) => PaymentSafely(workflow, () =>
        {
            if (!Keys(input, "contextVersion", "payload") || !Integer(input, "contextVersion", 0, 9007199254740991, out var context) ||
                input.GetProperty("payload").ValueKind != JsonValueKind.Object) return InvalidPaymentInput();
            return workflow.PrepareAsync(User, academyId, conversationId, requestId, context, input.GetProperty("payload").GetRawText(), token);
        });

    [HttpGet(PaymentDraftRoute)]
    public Task<ActionResult> PaymentRead(Guid academyId, Guid conversationId, Guid requestId,
        [FromServices] PentaPaymentCollectionWorkflow workflow, CancellationToken token) =>
        PaymentSafely(workflow, () => workflow.ReadAsync(User, academyId, conversationId, requestId, token));

    [HttpPost(PaymentDraftRoute + "/approve"), RequestSizeLimit(1024)]
    public Task<ActionResult> PaymentApprove(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaPaymentCollectionWorkflow workflow, CancellationToken token) => PaymentSafely(workflow, () =>
        {
            if (!Binding(input, true, out var revision, out var hash) || input.GetProperty("intent").ValueKind != JsonValueKind.String ||
                input.GetProperty("intent").GetString() != PentaPaymentCollectionWorkflow.ApprovalIntent) return InvalidPaymentInput();
            return workflow.ApproveAsync(User, academyId, conversationId, requestId, new(revision, hash), token);
        });

    [HttpPost(PaymentDraftRoute + "/execute"), RequestSizeLimit(1024)]
    public Task<ActionResult> PaymentExecute(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaPaymentCollectionWorkflow workflow, CancellationToken token) => PaymentSafely(workflow, () =>
        {
            var binding = FinanceExecutionBinding(input);
            return binding is null ? InvalidPaymentInput() : workflow.ExecuteAsync(User, academyId, conversationId, requestId, binding, token);
        });

    private Task<ActionResult> PaymentSafely(PentaPaymentCollectionWorkflow workflow, Func<Task<PentaConversationOutcome>> operation) =>
        FinanceWriteSafely("payment collection", workflow.Enabled, operation);

    private Task<ActionResult> FinanceWriteSafely(string subject, bool enabled, Func<Task<PentaConversationOutcome>> operation) =>
        Safely(async () =>
        {
            var outcome = await operation();
            return ToResult(outcome.HttpStatus, outcome.Value, outcome.Error ?? $"The {subject} request could not be completed.");
        }, subject, enabled);

    private static PentaFinanceDraftBinding? FinanceExecutionBinding(JsonElement input)
    {
        if (!Keys(input, "revision", "payloadHash", "approvalId") || !Integer(input, "revision", 1, 1, out var revision) ||
            input.GetProperty("payloadHash").ValueKind != JsonValueKind.String ||
            input.GetProperty("payloadHash").GetString() is not { Length: 64 } hash ||
            hash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')) ||
            input.GetProperty("approvalId").ValueKind != JsonValueKind.String ||
            !input.GetProperty("approvalId").TryGetGuid(out var approvalId) || approvalId == Guid.Empty) return null;
        return new(revision, hash, approvalId);
    }

    private static Task<PentaConversationOutcome> InvalidPaymentInput() =>
        Task.FromResult(new PentaConversationOutcome(400, Error: "Enter the expected payment draft and approval fields."));
}
