using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class InvoiceDocumentSettingsTests
{
    private static AcademyDeskDbContext Context() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Missing_own_row_returns_invoice_defaults_without_insertion_or_foreign_fallback()
    {
        using var db = Context(); db.Add(new AcademyFinanceSettings { AcademyId = Guid.NewGuid(), TaxRegistrationNumber = "FOREIGN", InvoiceTemplateKey = "Formal" });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await new InvoicesController(db).DocumentSettings(Guid.NewGuid(), default);
        var summary = Assert.IsType<InvoiceDocumentSettingsSummary>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(new InvoiceDocumentSettingsSummary("", "GST", null, null, null, null, "Classic"), summary);
        Assert.Empty(db.ChangeTracker.Entries()); Assert.Equal(1, await db.AcademyFinanceSettings.CountAsync()); Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("Classic")][InlineData("Modern")][InlineData("Minimal")][InlineData("Formal")]
    public async Task Own_invoice_branding_is_preserved_without_management_fields(string theme)
    {
        using var db = Context(); var academy = Guid.NewGuid();
        db.AddRange(new AcademyFinanceSettings { AcademyId = academy, TaxRegistrationNumber = "QA-TAX", TaxLabel = "VAT", InvoiceLogoUrl = "qa-logo",
            InvoiceAuthorityName = "Synthetic Signatory", InvoiceAuthorityTitle = "Finance", InvoiceSignatureUrl = "qa-sign", InvoiceTemplateKey = theme,
            TaxRatePercent = 20m, DefaultPaymentTermsDays = 30, TaxInclusivePricing = true, PayslipTemplateKey = "Professional" },
            new AcademyFinanceSettings { AcademyId = Guid.NewGuid(), TaxRegistrationNumber = "FOREIGN" });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await new InvoicesController(db).DocumentSettings(academy, default);
        Assert.Equal(new InvoiceDocumentSettingsSummary("QA-TAX", "VAT", "qa-logo", "Synthetic Signatory", "Finance", "qa-sign", theme),
            Assert.IsType<InvoiceDocumentSettingsSummary>(Assert.IsType<OkObjectResult>(result.Result).Value));
        Assert.Empty(db.ChangeTracker.Entries()); Assert.Equal(2, await db.AcademyFinanceSettings.CountAsync());
    }

    [Fact]
    public async Task Existing_nullable_document_fields_stay_null()
    {
        using var db = Context(); var academy = Guid.NewGuid(); db.Add(new AcademyFinanceSettings { AcademyId = academy }); await db.SaveChangesAsync();
        var result = await new InvoicesController(db).DocumentSettings(academy, default);
        Assert.Equal(new InvoiceDocumentSettingsSummary("", "GST", null, null, null, null, "Classic"), Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public void Contract_has_exact_seven_preview_fields_and_existing_Finance_gate()
    {
        Assert.Equal(new[] { "InvoiceAuthorityName", "InvoiceAuthorityTitle", "InvoiceLogoUrl", "InvoiceSignatureUrl", "InvoiceTemplateKey", "TaxLabel", "TaxRegistrationNumber" },
            typeof(InvoiceDocumentSettingsSummary).GetProperties().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal(new[] { "finance.manage" }, PermissionCatalog.RequiredFor(nameof(InvoicesController)));
        Assert.Equal("Finance", SubscriptionPlanCatalog.ModuleForController(nameof(InvoicesController)));
        Assert.Equal("FinanceControls", SubscriptionPlanCatalog.ModuleForController(nameof(FinanceGovernanceController)));
    }
}
