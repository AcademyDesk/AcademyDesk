namespace AcademyDesk.Api.Domain.Entities;

public sealed class PlatformSettings : EntityBase
{
    public string PlatformName { get; set; } = "AcademyDesk";
    public string? SupportEmail { get; set; }
    public string DefaultCurrency { get; set; } = "INR";
    public int DefaultTrialDays { get; set; } = 30;
    public int DataRetentionDays { get; set; } = 2555;
    public bool MaintenanceMode { get; set; }
    public string? StatusMessage { get; set; }
}
