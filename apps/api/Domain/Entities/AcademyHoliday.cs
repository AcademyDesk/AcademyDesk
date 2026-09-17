namespace AcademyDesk.Api.Domain.Entities;
public sealed class AcademyHoliday : AcademyEntity { public required string Name { get; set; } public DateOnly HolidayDate { get; set; } public string? Notes { get; set; } public string Scope { get; set; } = "Custom"; public string? StateOrUt { get; set; } public bool IsClosed { get; set; } = true; }
