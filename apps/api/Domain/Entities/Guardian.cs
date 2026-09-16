namespace AcademyDesk.Api.Domain.Entities;

public sealed class Guardian : AcademyEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}
