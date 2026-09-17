namespace AcademyDesk.Api.Domain.Entities;

public sealed class CommunicationTemplate : AcademyEntity
{
    public required string Channel { get; set; }
    public required string Name { get; set; }
    public required string TemplateKey { get; set; }
    public required string Category { get; set; }
    public string TemplateGroup { get; set; } = "General";
    public required string Status { get; set; }
    public string Language { get; set; } = "en";
    public string? ProviderTemplateName { get; set; }
    public string? Subject { get; set; }
    public required string Body { get; set; }
    public bool IsActive { get; set; } = true;
}
