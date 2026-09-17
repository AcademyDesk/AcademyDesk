namespace AcademyDesk.Api.Domain.Entities;

public sealed class Guardian : AcademyEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PreferredName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? PreferredLanguage { get; set; }
    public bool IsActive { get; set; } = true;
}
