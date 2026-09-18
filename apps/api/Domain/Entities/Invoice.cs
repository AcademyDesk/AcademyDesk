namespace AcademyDesk.Api.Domain.Entities;

public sealed class Invoice : AcademyEntity
{
    public required string InvoiceNumber { get; set; }
    public Guid StudentId { get; set; }
    public Guid? FeePlanId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AdjustedAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public DateOnly IssuedDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
    public string Status { get; set; } = "Issued";
}
