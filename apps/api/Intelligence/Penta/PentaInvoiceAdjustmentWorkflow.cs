using System.Globalization;
using System.Security.Claims;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Action = AcademyDesk.Api.Intelligence.Penta.PentaFinanceDraftWorkflow.Action;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaAdjustmentPlan(Guid InvoiceId, string InvoiceNumber, string InvoiceStatus, string Type,
    decimal Amount, string Currency, string Reason, decimal Total, decimal Adjusted, decimal Collected)
{
    public string AmountExact => Amount.ToString("0.00", CultureInfo.InvariantCulture);
    public string CurrentBalanceExact => (Total - Adjusted - Collected).ToString("0.00", CultureInfo.InvariantCulture);
    public string BalanceIfApprovedExact => (Total - Adjusted - Collected - Amount).ToString("0.00", CultureInfo.InvariantCulture);
}

internal sealed record PentaAdjustmentReceipt(Guid ReceiptId, Guid AuditId, Guid AdjustmentId, Guid InvoiceId,
    decimal Amount, string Currency, string StatusAtSubmission) : IPentaFinanceReceipt
{
    public string AmountExact => Amount.ToString("0.00", CultureInfo.InvariantCulture);
}

public sealed class PentaInvoiceAdjustmentWorkflow(PentaFinanceDraftWorkflow drafts, AcademyDeskDbContext db)
{
    internal const string ApprovalIntent = "ApproveSubmitInvoiceAdjustment";
    private readonly ProposalOperation operation = new(db);
    public bool Enabled => drafts.IsEnabled(operation.Flag);

    internal Task<PentaConversationOutcome> StartAsync(ClaimsPrincipal user, Guid academy, Guid start, CancellationToken token) =>
        drafts.RunAsync(operation, Action.Start, user, academy, Guid.Empty, start, null, null, null, token);
    internal Task<PentaConversationOutcome> PrepareAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        long context, string payload, CancellationToken token) =>
        drafts.RunAsync(operation, Action.Prepare, user, academy, session, request, context, payload, null, token);
    internal Task<PentaConversationOutcome> ReadAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request, CancellationToken token) =>
        drafts.RunAsync(operation, Action.Read, user, academy, session, request, null, null, null, token);
    internal Task<PentaConversationOutcome> ApproveAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaFinanceDraftBinding binding, CancellationToken token) =>
        drafts.RunAsync(operation, Action.Approve, user, academy, session, request, null, null, binding, token);
    internal Task<PentaConversationOutcome> ExecuteAsync(ClaimsPrincipal user, Guid academy, Guid session, Guid request,
        PentaFinanceDraftBinding binding, CancellationToken token) =>
        drafts.RunAsync(operation, Action.Execute, user, academy, session, request, null, null, binding, token);

    private sealed class ProposalOperation(AcademyDeskDbContext data)
        : PentaFinanceOperation<CreateFinanceAdjustmentRequest, PentaAdjustmentPlan, PentaAdjustmentReceipt>(
            "Penta:InvoiceAdjustmentEnabled", "academy.invoice-adjustment-proposal.v1", "PentaInvoiceAdjustmentDraft",
            "PentaInvoiceAdjustmentStarted", "PentaInvoiceAdjustmentPrepared", "PentaInvoiceAdjustmentAuthorized",
            "PentaInvoiceAdjustmentSubmitted", "The invoice balance changed after review. Open a fresh adjustment draft.")
    {
        internal override string AdditionalModule => "FinanceControls";
        internal override CreateFinanceAdjustmentRequest? Parse(string payload) => PentaAdjustmentFields.Read(payload);
        internal override CreateFinanceAdjustmentRequest Restore(PentaAdjustmentPlan plan) =>
            new(plan.InvoiceId, plan.Type, plan.Amount, plan.Reason);

        internal override async Task<PentaFinanceReview<PentaAdjustmentPlan>> ReviewAsync(Guid academy,
            CreateFinanceAdjustmentRequest request, DateTime now, CancellationToken token)
        {
            if (InvoiceAdjustmentProposal.Validate(request) is { } error) return new(null, 400, error);
            await using var write = await InvoiceWriteScope.OpenAsync(data, academy, request.InvoiceId, token);
            if (write.Invoice is not { } invoice) return new(null, 404);
            if (string.Equals(invoice.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                return new(null, 400, "A reduction cannot be proposed on a cancelled invoice.");
            var collected = await InvoiceWriteScope.ReadCollectedAsync(data, invoice, token);
            if (request.Amount > invoice.TotalAmount - invoice.AdjustedAmount - collected)
                return new(null, 400, "Adjustment exceeds the remaining invoice balance.");

            var type = request.Type!.ToUpperInvariant();
            var currency = invoice.Currency.Trim().ToUpperInvariant();
            if (await data.FinanceAdjustments.AsNoTracking().AnyAsync(x => x.AcademyId == academy && x.InvoiceId == invoice.Id &&
                x.Status == "PendingApproval" && x.Type.ToUpper() == type && x.Amount == request.Amount &&
                x.Currency.Trim().ToUpper() == currency, token))
                return new(null, 409, "A matching adjustment is already pending. Review it in Manual Finance Adjustments.");

            return new(new(invoice.Id, invoice.InvoiceNumber, invoice.Status, request.Type!, request.Amount, invoice.Currency,
                request.Reason!.Trim(), invoice.TotalAmount, invoice.AdjustedAmount, collected));
        }

        internal override PentaAdjustmentReceipt Apply(Guid academy, PentaAdjustmentPlan plan, DateTime now)
        {
            var proposal = InvoiceAdjustmentProposal.Create(data, academy, plan.InvoiceId, plan.Currency, Restore(plan));
            return new(Guid.NewGuid(), Guid.NewGuid(), proposal.Id, proposal.InvoiceId, proposal.Amount, proposal.Currency, proposal.Status);
        }

        internal override Task<bool> ReceiptMatchesAsync(Guid academy, PentaAdjustmentPlan plan, PentaAdjustmentReceipt receipt, CancellationToken token) =>
            data.FinanceAdjustments.AsNoTracking().AnyAsync(x => x.Id == receipt.AdjustmentId && x.AcademyId == academy &&
                x.InvoiceId == receipt.InvoiceId && x.Type == plan.Type && x.Amount == receipt.Amount &&
                x.Currency == receipt.Currency && x.Reason == plan.Reason, token);
    }
}
