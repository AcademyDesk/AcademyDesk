namespace AcademyDesk.Api.Domain.Entities;

public sealed class Teacher : AcademyEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? PreferredName { get; set; }
    public string? EmploymentType { get; set; }
    public DateOnly? JoiningDate { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Specialties { get; set; }
    public string? Qualifications { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? AdminNotes { get; set; }
    public Guid? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
}
