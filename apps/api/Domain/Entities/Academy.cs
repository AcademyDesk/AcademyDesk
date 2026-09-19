namespace AcademyDesk.Api.Domain.Entities;

public sealed class Academy : EntityBase
{
    public required string Name { get; set; }
    public string? LegalName { get; set; }
    public string CountryCode { get; set; } = "IN";
    public string TimeZone { get; set; } = "Asia/Kolkata";
    public bool IsActive { get; set; } = true;
    public string SubscriptionPlan { get; set; } = "Trial";
    public string SubscriptionStatus { get; set; } = "Trial";
    public DateTime? SubscriptionEndsAtUtc { get; set; }
    public int StudentLimit { get; set; } = 100;
    public int StaffLimit { get; set; } = 10;
    public string EnabledModulesJson { get; set; } = "[\"Core\"]";
    public string? CertificateLogoUrl { get; set; }
    public string? CertificateAccentColor { get; set; }
    public string? CertificateSignatoryName { get; set; }

    public List<Branch> Branches { get; set; } = [];
}
