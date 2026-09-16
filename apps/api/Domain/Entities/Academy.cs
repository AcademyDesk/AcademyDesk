namespace AcademyDesk.Api.Domain.Entities;

public sealed class Academy : EntityBase
{
    public required string Name { get; set; }
    public string? LegalName { get; set; }
    public string CountryCode { get; set; } = "IN";
    public string TimeZone { get; set; } = "Asia/Kolkata";
    public bool IsActive { get; set; } = true;

    public List<Branch> Branches { get; set; } = [];
}
