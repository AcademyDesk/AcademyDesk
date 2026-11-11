using System.Globalization;
using System.Text.Json.Serialization;

namespace AcademyDesk.Api.Intelligence.Penta;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PentaFinanceChatInput(string? Text);

internal sealed record PentaFinanceChatRow(string[] Values, Guid? InvoiceId = null);
internal sealed record PentaFinanceChatReply(string Kind, string Title, string[] Columns, PentaFinanceChatRow[] Rows,
    bool HasMore, DateTimeOffset AsOfUtc);

internal static class PentaFinanceChat
{
    internal static (PentaFinanceReadKind Kind, Guid? Invoice)? Parse(string? text)
    {
        if (text is not { Length: > 0 and <= 180 } || text.Any(char.IsControl)) return null;
        var prompt = string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('?', '.').ToLowerInvariant();
        return prompt switch
        {
            "finance overview" or "show finance overview" or "show the finance overview" =>
                (PentaFinanceReadKind.Overview, null),
            "collections" or "show collections" or "show overdue invoices" =>
                (PentaFinanceReadKind.Collections, null),
            "expenses" or "show expenses" or "show recent expenses" =>
                (PentaFinanceReadKind.Expenses, null),
            "payroll" or "show payroll" or "show recent payroll" =>
                (PentaFinanceReadKind.Payroll, null),
            _ => ParseInvoice(prompt)
        };
    }

    private static (PentaFinanceReadKind, Guid?)? ParseInvoice(string prompt)
    {
        var parts = prompt.Split(' ');
        if (parts.Length != 3 || parts[0] != "show" ||
            !Guid.TryParseExact(parts[2], "D", out var invoice) || invoice == Guid.Empty) return null;
        return parts[1] switch
        {
            "invoice" => (PentaFinanceReadKind.Invoice, invoice),
            "reconciliation" => (PentaFinanceReadKind.Reconciliation, invoice),
            _ => null
        };
    }

    internal static PentaFinanceChatReply Present(object result) => result switch
    {
        PentaFinanceOverview overview => new("overview", "Finance overview",
            ["Currency", "Billed", "Adjustments", "Collected", "Outstanding", "Reconciled"],
            overview.ByCurrency.Take(PentaFinanceReadService.PreviewSize).Select(row => Row(row.Currency, Money(row.GrossBilled), Money(row.ApprovedAdjustments),
                Money(row.Collected), Money(row.Outstanding), Money(row.Reconciled))).ToArray(), overview.ByCurrency.Length > PentaFinanceReadService.PreviewSize, overview.AsOfUtc),
        PentaFinanceCollections collections => new("collections", "Overdue invoices",
            ["Invoice", "Due date", "Currency", "Outstanding"],
            collections.Invoices.Select(row => new PentaFinanceChatRow(
                [row.InvoiceNumber, Date(row.DueDate), row.Currency, Money(row.Balance)], row.Id)).ToArray(),
            collections.HasMore, collections.AsOfUtc),
        PentaFinanceInvoice invoice => new("invoice", "Invoice " + invoice.Invoice.InvoiceNumber,
            ["Currency", "Billed", "Adjustments", "Collected", "Outstanding", "Status"],
            [new([invoice.Invoice.Currency, Money(invoice.Invoice.TotalAmount), Money(invoice.Invoice.AdjustedAmount),
                Money(invoice.Invoice.PaidAmount), Money(invoice.Invoice.Balance), invoice.Invoice.Status], invoice.Invoice.Id)],
            false, invoice.AsOfUtc),
        PentaFinanceReconciliation reconciliation => new("reconciliation",
            "Reconciliation for " + reconciliation.Invoice.InvoiceNumber,
            ["Paid at (UTC)", "Currency", "Amount", "Status", "Counts toward balance"],
            reconciliation.Payments.Select(row => Row(Utc(row.PaidAtUtc), row.Currency, Money(row.Amount),
                row.Status, row.CountsTowardInvoiceBalance ? "Yes" : "No")).ToArray(), reconciliation.HasMore, reconciliation.AsOfUtc),
        PentaFinanceExpenses expenses => new("expenses", "Recent expenses",
            ["Date", "Currency", "Amount", "Status"],
            expenses.Expenses.Select(row => Row(Date(row.ExpenseDate), row.Currency, row.AmountExact, row.Status)).ToArray(),
            expenses.HasMore, expenses.AsOfUtc),
        PentaFinancePayroll payroll => new("payroll", "Recent payroll",
            ["Payslip", "Paid at (UTC)", "Currency", "Gross", "Deductions", "Net", "Status", "Identity source"],
            payroll.Payouts.Select(row => Row(row.PayslipNumber, Utc(row.PaidAtUtc), row.Currency, row.GrossAmountExact,
                row.DeductionsExact, row.NetAmountExact, row.Status,
                row.IdentitySource == "PayoutSnapshot" ? "Recorded snapshot" : "Legacy unverified")).ToArray(),
            payroll.HasMore, payroll.AsOfUtc),
        _ => throw new InvalidOperationException("Unexpected finance read result.")
    };

    private static PentaFinanceChatRow Row(params string[] values) => new(values);
    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string Utc(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "—";
}
