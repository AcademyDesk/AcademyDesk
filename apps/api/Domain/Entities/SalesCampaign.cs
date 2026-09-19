namespace AcademyDesk.Api.Domain.Entities;
public sealed class SalesCampaign : AcademyEntity { public required string Name { get; set; } public string Channel { get; set; } = "WhatsApp"; public DateOnly StartDate { get; set; } public DateOnly? EndDate { get; set; } public decimal Budget { get; set; } public string Status { get; set; } = "Draft"; }
