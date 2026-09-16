namespace AcademyDesk.Api.Domain.Entities;

public sealed class FeePlan : AcademyEntity
{
    public required string Name { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Frequency { get; set; } = "Monthly";
    public bool IsActive { get; set; } = true;
}
