namespace AcademyDesk.Api.Domain.Entities;

public sealed class Expense : AcademyEntity
{
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Category { get; set; } = "General";
    public Guid? BranchId { get; set; }
    public DateOnly ExpenseDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string Status { get; set; } = "Recorded";
}
