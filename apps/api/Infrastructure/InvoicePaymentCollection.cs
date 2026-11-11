using System.Globalization;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;

namespace AcademyDesk.Api.Infrastructure;

internal sealed record PaymentCollectionPlan(Guid InvoiceId, string InvoiceNumber, string Currency, string InvoiceStatus,
    decimal Total, decimal Adjustments, decimal Collected, decimal Amount, string Method, string? Reference)
{
    public decimal BalanceBefore => Total - Adjustments - Collected;
    public decimal BalanceAfter => BalanceBefore - Amount;
    public string AmountExact => Amount.ToString("0.00", CultureInfo.InvariantCulture);
    public string BalanceBeforeExact => BalanceBefore.ToString("0.00", CultureInfo.InvariantCulture);
    public string BalanceAfterExact => BalanceAfter.ToString("0.00", CultureInfo.InvariantCulture);
}

internal static class InvoicePaymentCollection
{
    // The caller holds InvoiceWriteScope through review, any mutation and commit.
    internal static async Task<(PaymentCollectionPlan? Plan, string? Error)> ReviewAsync(
        AcademyDeskDbContext db, Invoice invoice, RecordPaymentRequest request, CancellationToken token)
    {
        if (string.Equals(invoice.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return (null, "A payment cannot be recorded on a cancelled invoice.");
        if (request.Amount <= 0) return (null, "Payment amount must be positive.");
        if (request.Amount > 9999999999999999.99m || decimal.Round(request.Amount, 2) != request.Amount)
            return (null, "Payment amount must fit within 16 whole digits and two decimal places.");

        var collected = await InvoiceWriteScope.ReadCollectedAsync(db, invoice, token);
        if (request.Amount > invoice.TotalAmount - invoice.AdjustedAmount - collected)
            return (null, "Payment exceeds the invoice balance.");

        return (new(invoice.Id, invoice.InvoiceNumber, invoice.Currency, invoice.Status, invoice.TotalAmount, invoice.AdjustedAmount,
            collected, request.Amount, string.IsNullOrWhiteSpace(request.Method) ? "Offline" : request.Method.Trim(),
            request.Reference?.Trim()), null);
    }

    internal static Payment Record(AcademyDeskDbContext db, Invoice invoice, PaymentCollectionPlan plan)
    {
        var payment = new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = plan.Amount,
            Currency = plan.Currency, Method = plan.Method, Reference = plan.Reference };
        invoice.Status = plan.BalanceAfter == 0 ? "Paid" : "PartiallyPaid";
        db.Payments.Add(payment);
        return payment;
    }
}
