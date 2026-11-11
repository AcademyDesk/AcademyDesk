using System.Globalization;
using System.Security.Claims;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Action = AcademyDesk.Api.Intelligence.Penta.PentaFinanceDraftWorkflow.Action;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaPaymentReceipt(Guid ReceiptId, Guid AuditId, Guid PaymentId, Guid InvoiceId,
    decimal Amount, string Currency, decimal RemainingBalance, DateTime RecordedAtUtc) : IPentaFinanceReceipt
{
    public string AmountExact => Amount.ToString("0.00", CultureInfo.InvariantCulture);
    public string RemainingBalanceExact => RemainingBalance.ToString("0.00", CultureInfo.InvariantCulture);
}

public sealed class PentaPaymentCollectionWorkflow(PentaFinanceDraftWorkflow drafts, AcademyDeskDbContext db)
{
    internal const string ApprovalIntent = "ApproveRecordReceivedPayment";
    private readonly PaymentOperation operation = new(db);
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

    private sealed class PaymentOperation(AcademyDeskDbContext data)
        : PentaFinanceOperation<RecordPaymentRequest, PaymentCollectionPlan, PentaPaymentReceipt>(
            "Penta:PaymentCollectionEnabled", "academy.payment-collection.v1", "PentaPaymentCollectionDraft",
            "PentaPaymentCollectionStarted", "PentaPaymentPrepared", "PentaPaymentApproved", "PentaPaymentRecorded",
            "The invoice changed after review. Open a fresh payment draft.")
    {
        internal override RecordPaymentRequest? Parse(string payload) => PentaPaymentFields.Read(payload);
        internal override RecordPaymentRequest Restore(PaymentCollectionPlan plan) => new(plan.InvoiceId, plan.Amount, plan.Method, null);

        internal override async Task<PentaFinanceReview<PaymentCollectionPlan>> ReviewAsync(Guid academy,
            RecordPaymentRequest request, DateTime now, CancellationToken token)
        {
            await using var write = await InvoiceWriteScope.OpenAsync(data, academy, request.InvoiceId, token);
            if (write.Invoice is not { } invoice) return new(null, 404);
            var (plan, error) = await InvoicePaymentCollection.ReviewAsync(data, invoice, request, token);
            return plan is null ? new(null, 400, error) : new(plan);
        }

        internal override PentaPaymentReceipt Apply(Guid academy, PaymentCollectionPlan plan, DateTime now)
        {
            // Review loaded this invoice under InvoiceWriteScope within the same transaction.
            var invoice = data.Invoices.Local.Single(x => x.AcademyId == academy && x.Id == plan.InvoiceId);
            var payment = InvoicePaymentCollection.Record(data, invoice, plan);
            return new(Guid.NewGuid(), Guid.NewGuid(), payment.Id, invoice.Id, payment.Amount, payment.Currency,
                plan.BalanceAfter, payment.PaidAtUtc);
        }

        internal override Task<bool> ReceiptMatchesAsync(Guid academy, PaymentCollectionPlan plan, PentaPaymentReceipt receipt, CancellationToken token) =>
            data.Payments.AsNoTracking().AnyAsync(x => x.AcademyId == academy && x.Id == receipt.PaymentId &&
                x.InvoiceId == receipt.InvoiceId && x.Amount == receipt.Amount && x.Currency == receipt.Currency &&
                x.Method == plan.Method && x.Reference == null && x.PaidAtUtc == receipt.RecordedAtUtc, token);
    }
}
