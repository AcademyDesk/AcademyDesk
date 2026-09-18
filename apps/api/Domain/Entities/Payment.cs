namespace AcademyDesk.Api.Domain.Entities;

public sealed class Payment : AcademyEntity
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Method { get; set; } = "Offline";
    public string Status { get; set; } = "Completed";
    public string? Reference { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReconciledAtUtc { get; set; }
    public string? ReconciliationReference { get; set; }
}
