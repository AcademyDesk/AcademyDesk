using System.Globalization;
using System.Security.Claims;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Action = AcademyDesk.Api.Intelligence.Penta.PentaFinanceDraftWorkflow.Action;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaInvoiceReceipt(Guid ReceiptId, Guid AuditId, Guid InvoiceId, string InvoiceNumber,
    decimal Amount, string Currency, DateOnly IssuedDate, DateOnly DueDate) : IPentaFinanceReceipt
{
    public string AmountExact => Amount.ToString("0.00", CultureInfo.InvariantCulture);
}

public sealed class PentaInvoiceIssuanceWorkflow(PentaFinanceDraftWorkflow drafts, AcademyDeskDbContext db)
{
    internal const string ApprovalIntent = "ApproveIssueInvoice";
    private readonly InvoiceOperation operation = new(db);
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

    private sealed class InvoiceOperation(AcademyDeskDbContext data)
        : PentaFinanceOperation<CreateInvoiceRequest, InvoiceIssuancePlan, PentaInvoiceReceipt>(
            "Penta:InvoiceIssuanceEnabled", "academy.invoice-issuance.v1", "PentaInvoiceIssuanceDraft",
            "PentaInvoiceIssuanceStarted", "PentaInvoicePrepared", "PentaInvoiceApproved", "PentaInvoiceIssued",
            "The invoice terms changed after review. Open a fresh invoice draft.")
    {
        internal override CreateInvoiceRequest? Parse(string payload) => PentaInvoiceFields.Read(payload);
        internal override CreateInvoiceRequest Restore(InvoiceIssuancePlan plan) =>
            new(plan.StudentId, plan.FeePlanId, plan.RequestedAmount, plan.DueDate);

        internal override async Task<PentaFinanceReview<InvoiceIssuancePlan>> ReviewAsync(Guid academy,
            CreateInvoiceRequest request, DateTime now, CancellationToken token)
        {
            var (plan, error) = await InvoiceIssuance.ReviewAsync(data, academy, request, now, token);
            if (plan is null) return new(null, 400, error);
            if (!plan.StudentActive) return new(null, 400, "Choose an active student for this invoice.");
            var currency = plan.Currency.Trim().ToUpperInvariant();
            var duplicate = await data.Invoices.AsNoTracking().AnyAsync(x => x.AcademyId == academy &&
                x.StudentId == plan.StudentId && x.FeePlanId == plan.FeePlanId && x.DueDate == plan.DueDate &&
                x.TotalAmount == plan.Amount && x.Currency.Trim().ToUpper() == currency &&
                x.Status.ToUpper() != "CANCELLED", token);
            return duplicate ? new(null, 409, "A matching invoice already exists. Review it in Manual Invoices.") : new(plan);
        }

        internal override PentaInvoiceReceipt Apply(Guid academy, InvoiceIssuancePlan plan, DateTime now)
        {
            var invoice = InvoiceIssuance.Create(data, academy, plan, now);
            return new(Guid.NewGuid(), Guid.NewGuid(), invoice.Id, invoice.InvoiceNumber,
                invoice.TotalAmount, invoice.Currency, invoice.IssuedDate, invoice.DueDate);
        }

        internal override Task<bool> ReceiptMatchesAsync(Guid academy, InvoiceIssuancePlan plan, PentaInvoiceReceipt receipt, CancellationToken token) =>
            data.Invoices.AsNoTracking().AnyAsync(x => x.Id == receipt.InvoiceId && x.AcademyId == academy &&
                x.InvoiceNumber == receipt.InvoiceNumber && x.StudentId == plan.StudentId && x.FeePlanId == plan.FeePlanId &&
                x.TotalAmount == receipt.Amount && x.Currency == receipt.Currency && x.IssuedDate == receipt.IssuedDate &&
                x.DueDate == receipt.DueDate, token);
    }
}
