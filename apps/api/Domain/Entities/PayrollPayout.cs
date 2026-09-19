namespace AcademyDesk.Api.Domain.Entities;

public sealed class PayrollPayout : AcademyEntity
{
    public Guid PayrollProfileId { get; set; }
    public required string PayslipNumber { get; set; }
    public required string PeriodLabel { get; set; }
    public int? SessionsCovered { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = "Paid";
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string? Reference { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
}
