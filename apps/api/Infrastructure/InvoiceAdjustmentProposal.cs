using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;

namespace AcademyDesk.Api.Infrastructure;

internal static class InvoiceAdjustmentProposal
{
    private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase)
        { "Discount", "Scholarship", "Concession", "Refund", "CreditNote" };

    internal static bool IsKnownType(string? type) => type is not null && Types.Contains(type);
    internal static bool IsLegacyDocumentType(string? type) =>
        string.Equals(type, "Refund", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(type, "CreditNote", StringComparison.OrdinalIgnoreCase);
    internal static bool ValidAmount(decimal amount) =>
        amount > 0m && amount <= 9999999999999999.99m && decimal.Round(amount, 2) == amount;

    internal static string? Validate(CreateFinanceAdjustmentRequest request)
    {
        if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Reason) || !IsKnownType(request.Type))
            return "Enter a valid type, positive amount, and reason.";
        if (IsLegacyDocumentType(request.Type))
            return "Refunds and credit notes require dedicated reviewed workflows; submit only invoice reductions here.";
        if (!ValidAmount(request.Amount)) return "Enter an amount with at most two decimal places within the supported range.";
        if (request.Reason.Trim().Length > 2000) return "Enter a reason of at most 2,000 characters.";
        return null;
    }

    internal static FinanceAdjustment Create(AcademyDeskDbContext db, Guid academy, Guid invoice, string currency,
        CreateFinanceAdjustmentRequest request)
    {
        var adjustment = new FinanceAdjustment { AcademyId = academy, InvoiceId = invoice, Currency = currency,
            Type = request.Type!, Amount = request.Amount, Reason = request.Reason!.Trim() };
        db.FinanceAdjustments.Add(adjustment);
        return adjustment;
    }
}
