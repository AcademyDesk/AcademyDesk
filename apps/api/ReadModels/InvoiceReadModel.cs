using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.ReadModels;

internal static class InvoiceReadModel
{
    internal static IQueryable<InvoiceSummary> Query(AcademyDeskDbContext db, Guid academyId, Guid? invoiceId = null) =>
        WithCollected(db, db.Invoices.AsNoTracking()
            .Where(invoice => invoice.AcademyId == academyId && (invoiceId == null || invoice.Id == invoiceId)))
            .OrderByDescending(invoice => invoice.IssuedDate)
            .Select(invoice => new InvoiceSummary(invoice.Id, invoice.InvoiceNumber, invoice.StudentId, invoice.FeePlanId,
                invoice.TotalAmount, invoice.AdjustedAmount, invoice.PaidAmount, invoice.Currency, invoice.IssuedDate,
                invoice.DueDate, invoice.Status));

    internal static IQueryable<InvoiceReadEntry> WithCollected(AcademyDeskDbContext db, IQueryable<Invoice> invoices) =>
        invoices.Select(invoice => new InvoiceReadEntry
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            StudentId = invoice.StudentId,
            FeePlanId = invoice.FeePlanId,
            TotalAmount = invoice.TotalAmount,
            AdjustedAmount = invoice.AdjustedAmount,
            PaidAmount = db.Payments.Where(payment => payment.AcademyId == invoice.AcademyId && payment.InvoiceId == invoice.Id &&
                (payment.Status == "Completed" || payment.Status == "Reconciled") &&
                payment.Currency.Trim().ToUpper() == invoice.Currency.Trim().ToUpper())
                .Sum(payment => (decimal?)payment.Amount) ?? 0,
            Currency = invoice.Currency,
            IssuedDate = invoice.IssuedDate,
            DueDate = invoice.DueDate,
            Status = invoice.Status
        });
}

internal sealed class InvoiceReadEntry
{
    public Guid Id { get; init; }
    public required string InvoiceNumber { get; init; }
    public Guid StudentId { get; init; }
    public Guid? FeePlanId { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AdjustedAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public required string Currency { get; init; }
    public DateOnly IssuedDate { get; init; }
    public DateOnly DueDate { get; init; }
    public required string Status { get; init; }
}
