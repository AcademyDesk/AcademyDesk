using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    private const string InvoiceConversationRoot = "~/api/academies/{academyId:guid}/penta/invoice-issuance/conversations";
    private const string InvoiceDraftRoute = InvoiceConversationRoot + "/{conversationId:guid}/drafts/{requestId:guid}";

    [HttpPost("~/api/academies/{academyId:guid}/penta/invoice-issuance/options"), RequestSizeLimit(1024)]
    public Task<ActionResult> InvoiceOptions(Guid academyId, [FromBody] JsonElement input,
        [FromServices] PentaInvoiceIssuanceWorkflow workflow, CancellationToken token) =>
        FinanceWriteSafely("invoice lookup", workflow.Enabled, () =>
        {
            if (!Keys(input, "kind", "query") || input.GetProperty("kind").ValueKind != JsonValueKind.String ||
                input.GetProperty("kind").GetString() is not ("student" or "feePlan") ||
                input.GetProperty("query").ValueKind != JsonValueKind.String) return InvalidInvoiceInput();
            return workflow.OptionsAsync(User, academyId, input.GetProperty("kind").GetString()!,
                input.GetProperty("query").GetString()!, token);
        });

    [HttpPut(InvoiceConversationRoot + "/{startRequestId:guid}"), RequestSizeLimit(1024)]
    public Task<ActionResult> InvoiceStart(Guid academyId, Guid startRequestId,
        [FromServices] PentaInvoiceIssuanceWorkflow workflow, CancellationToken token) =>
        FinanceWriteSafely("invoice issuance", workflow.Enabled, () => workflow.StartAsync(User, academyId, startRequestId, token));

    [HttpPut(InvoiceDraftRoute), RequestSizeLimit(4 * 1024)]
    public Task<ActionResult> InvoicePrepare(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaInvoiceIssuanceWorkflow workflow, CancellationToken token) =>
        FinanceWriteSafely("invoice issuance", workflow.Enabled, () =>
        {
            if (!Keys(input, "contextVersion", "payload") || !Integer(input, "contextVersion", 0, 9007199254740991, out var context) ||
                input.GetProperty("payload").ValueKind != JsonValueKind.Object) return InvalidInvoiceInput();
            return workflow.PrepareAsync(User, academyId, conversationId, requestId, context, input.GetProperty("payload").GetRawText(), token);
        });

    [HttpGet(InvoiceDraftRoute)]
    public Task<ActionResult> InvoiceRead(Guid academyId, Guid conversationId, Guid requestId,
        [FromServices] PentaInvoiceIssuanceWorkflow workflow, CancellationToken token) =>
        FinanceWriteSafely("invoice issuance", workflow.Enabled, () => workflow.ReadAsync(User, academyId, conversationId, requestId, token));

    [HttpPost(InvoiceDraftRoute + "/approve"), RequestSizeLimit(1024)]
    public Task<ActionResult> InvoiceApprove(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaInvoiceIssuanceWorkflow workflow, CancellationToken token) =>
        FinanceWriteSafely("invoice issuance", workflow.Enabled, () =>
        {
            if (!Binding(input, true, out var revision, out var hash) || input.GetProperty("intent").ValueKind != JsonValueKind.String ||
                input.GetProperty("intent").GetString() != PentaInvoiceIssuanceWorkflow.ApprovalIntent) return InvalidInvoiceInput();
            return workflow.ApproveAsync(User, academyId, conversationId, requestId, new(revision, hash), token);
        });

    [HttpPost(InvoiceDraftRoute + "/execute"), RequestSizeLimit(1024)]
    public Task<ActionResult> InvoiceExecute(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaInvoiceIssuanceWorkflow workflow, CancellationToken token) =>
        FinanceWriteSafely("invoice issuance", workflow.Enabled, () =>
        {
            var binding = FinanceExecutionBinding(input);
            return binding is null ? InvalidInvoiceInput() : workflow.ExecuteAsync(User, academyId, conversationId, requestId, binding, token);
        });

    private static Task<PentaConversationOutcome> InvalidInvoiceInput() =>
        Task.FromResult(new PentaConversationOutcome(400, Error: "Enter the expected invoice draft and approval fields."));
}
