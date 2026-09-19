namespace AcademyDesk.Api.Domain.Entities;

public sealed class PayrollProfile : AcademyEntity
{
    public required string WorkerType { get; set; }
    public Guid? TeacherId { get; set; }
    public string? StaffUserId { get; set; }
    public required string WorkerName { get; set; }
    public string PaymentModel { get; set; } = "Monthly";
    public decimal? MonthlyAmount { get; set; }
    public decimal? AmountPerCycle { get; set; }
    public int? SessionsPerCycle { get; set; }
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public bool IsActive { get; set; } = true;
}
