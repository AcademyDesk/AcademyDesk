namespace AcademyDesk.Api.Domain.Entities;
public sealed class AcademyHoliday : AcademyEntity { public required string Name { get; set; } public DateOnly HolidayDate { get; set; } public string? Notes { get; set; } public bool IsClosed { get; set; } = true; }
