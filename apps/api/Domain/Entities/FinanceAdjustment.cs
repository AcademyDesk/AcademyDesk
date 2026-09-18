namespace AcademyDesk.Api.Domain.Entities;

public sealed class FinanceAdjustment : AcademyEntity
{
    public Guid InvoiceId { get; set; }
    public string Type { get; set; } = "Discount";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public required string Reason { get; set; }
    public string Status { get; set; } = "PendingApproval";
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovalNotes { get; set; }
    public DateTime? AppliedAtUtc { get; set; }
}
