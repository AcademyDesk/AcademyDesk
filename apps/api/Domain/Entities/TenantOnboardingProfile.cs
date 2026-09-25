namespace AcademyDesk.Api.Domain.Entities;

/// <summary>Platform-owner intake record describing how an academy operates.</summary>
public sealed class TenantOnboardingProfile : AcademyEntity
{
    public string Status { get; set; } = "NotStarted";
    public string CurrentSection { get; set; } = "Personal";
    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactRole { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactPhone { get; set; }
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? BusinessType { get; set; }
    public string? OperatingSince { get; set; }
    public string? Website { get; set; }
    public string? BranchSummary { get; set; }
    public string? FinanceModel { get; set; }
    public string? BillingFrequency { get; set; }
    public string? PaymentCollectionMethods { get; set; }
    public string? TeacherPaymentModels { get; set; }
    public int? TeacherCount { get; set; }
    public int? StudentCount { get; set; }
    public int? SubjectCount { get; set; }
    public string? SubjectTypes { get; set; }
    public string? DeliveryModes { get; set; }
    public string? ClassRatios { get; set; }
    public string? BatchAndClassSetup { get; set; }
    public string? OperationalNotes { get; set; }
    public string? DocumentsJson { get; set; } = "[]";
}
