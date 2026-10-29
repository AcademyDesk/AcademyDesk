namespace AcademyDesk.Api.Domain.Entities;

public sealed class PayrollPayout : AcademyEntity
{
    public Guid PayrollProfileId { get; set; }
    // Nullable only for legacy rows: never infer past identity from today's profile.
    public string? WorkerNameSnapshot { get; set; }
    public string? WorkerTypeSnapshot { get; set; }
    public Guid? TeacherIdSnapshot { get; set; }
    public string? StaffUserIdSnapshot { get; set; }
    public DateTime? IdentityRecordedAtUtc { get; set; }
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
    // NULL for new NoCashDue earnings statements; legacy Paid rows are preserved.
    public DateTime? PaidAtUtc { get; set; } = DateTime.UtcNow;
}
