namespace AcademyDesk.Api.Domain.Entities;

public sealed class Branch : AcademyEntity
{
    public required string Name { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public bool IsActive { get; set; } = true;

    public Academy? Academy { get; set; }
}
