namespace AcademyDesk.Api.Domain.Entities;

public sealed class PlatformBillingInvoice : EntityBase
{
    public Guid AcademyId { get; set; }
    public required string InvoiceNumber { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = "Draft";
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}
