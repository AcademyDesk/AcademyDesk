namespace AcademyDesk.Api.Domain.Entities;
public sealed class AcademyFinanceSettings : AcademyEntity
{
    public string TaxRegistrationNumber { get; set; } = "";
    public string TaxLabel { get; set; } = "GST";
    public decimal TaxRatePercent { get; set; }
    public int DefaultPaymentTermsDays { get; set; } = 7;
    public bool TaxInclusivePricing { get; set; }
}
