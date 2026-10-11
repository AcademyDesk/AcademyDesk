using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class FinanceSettingsConcurrencyTests
{
    private sealed class Fixture : IDisposable
    {
        public AcademyDeskDbContext Db { get; } = new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        private readonly IdentityDbContext identity = new(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public Guid Academy { get; } = Guid.NewGuid();
        public FinanceGovernanceController Controller { get; }
        public Fixture()
        {
            Db.Academies.Add(new Academy { Id = Academy, Name = "Synthetic settings academy" }); Db.SaveChanges();
            Controller = new(Db, identity) { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        }
        public async Task<FinanceSettingsSummary> Get(Guid? academy = null) => Assert.IsType<FinanceSettingsSummary>(Assert.IsType<OkObjectResult>((await Controller.Get(academy ?? Academy, default)).Result).Value);
        public void Dispose() { Db.Dispose(); identity.Dispose(); }
    }
    private static FinanceSettingsRequest Body(string? version, string? theme = "Compact", string label = "GST") =>
        new("Synthetic", label, 18, 7, false, null, "Synthetic signatory", "Finance", null, "Modern", theme, version);

    [Fact]
    public async Task Default_token_is_stable_tenant_bound_and_does_not_insert()
    {
        using var f = new Fixture(); var first = await f.Get(); var second = await f.Get();
        Assert.Equal(first, second); Assert.Matches("^[A-F0-9]{64}$", first.SettingsVersion!);
        Assert.NotEqual(first.SettingsVersion, (await f.Get(Guid.NewGuid())).SettingsVersion);
        Assert.Empty(await f.Db.AcademyFinanceSettings.ToArrayAsync());
        Assert.Equal("no-store", f.Controller.Response.Headers.CacheControl.ToString());
    }
    [Theory]
    [InlineData(null)][InlineData("")][InlineData(" ")]
    public async Task Missing_version_fails_closed_without_insertion(string? version)
    {
        using var f = new Fixture();
        Assert.Equal(428, Assert.IsType<ObjectResult>((await f.Controller.Save(f.Academy, Body(version), default)).Result).StatusCode);
        Assert.Empty(await f.Db.AcademyFinanceSettings.ToArrayAsync());
    }
    [Theory]
    [InlineData("*")][InlineData("wrong")][InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
    public async Task Manipulated_version_rejects_without_insertion(string version)
    {
        using var f = new Fixture(); Assert.IsType<ConflictObjectResult>((await f.Controller.Save(f.Academy, Body(version), default)).Result);
        Assert.Empty(await f.Db.AcademyFinanceSettings.ToArrayAsync());
    }
    [Theory]
    [InlineData("Standard")][InlineData("Compact")][InlineData("Professional")][InlineData(null)]
    public async Task Current_version_saves_and_rotates_including_explicit_reset(string? theme)
    {
        using var f = new Fixture(); var before = await f.Get();
        var saved = Assert.IsType<FinanceSettingsSummary>(Assert.IsType<OkObjectResult>((await f.Controller.Save(f.Academy, Body(before.SettingsVersion, theme), default)).Result).Value);
        Assert.Equal(theme ?? "Standard", saved.PayslipTemplateKey); Assert.NotEqual(before.SettingsVersion, saved.SettingsVersion);
        f.Db.ChangeTracker.Clear(); Assert.Equal(saved, await f.Get()); Assert.Single(await f.Db.AcademyFinanceSettings.ToArrayAsync());
    }
    [Fact]
    public async Task Stale_save_preserves_newer_state_and_noop_also_invalidates_token()
    {
        using var f = new Fixture(); var empty = await f.Get();
        await f.Controller.Save(f.Academy, Body(empty.SettingsVersion), default); var compact = await f.Get();
        await f.Controller.Save(f.Academy, Body(compact.SettingsVersion, "Professional"), default); var current = await f.Get();
        Assert.IsType<ConflictObjectResult>((await f.Controller.Save(f.Academy, Body(compact.SettingsVersion, "Compact", "Old draft"), default)).Result);
        Assert.Equal(current, await f.Get());
        await f.Controller.Save(f.Academy, Body(current.SettingsVersion, "Professional"), default); var noop = await f.Get();
        Assert.NotEqual(current.SettingsVersion, noop.SettingsVersion);
        Assert.IsType<ConflictObjectResult>((await f.Controller.Save(f.Academy, Body(current.SettingsVersion), default)).Result);
        Assert.Equal(noop, await f.Get()); Assert.Single(await f.Db.AcademyFinanceSettings.ToArrayAsync());
    }
    [Fact]
    public async Task ABA_does_not_revive_old_version_and_invalid_values_do_not_change_row()
    {
        using var f = new Fixture(); var empty = await f.Get(); await f.Controller.Save(f.Academy, Body(empty.SettingsVersion), default);
        var first = await f.Get(); await f.Controller.Save(f.Academy, Body(first.SettingsVersion, "Professional"), default);
        await f.Controller.Save(f.Academy, Body((await f.Get()).SettingsVersion), default); var returned = await f.Get();
        Assert.NotEqual(first.SettingsVersion, returned.SettingsVersion);
        Assert.IsType<ConflictObjectResult>((await f.Controller.Save(f.Academy, Body(first.SettingsVersion), default)).Result);
        Assert.IsType<BadRequestObjectResult>((await f.Controller.Save(f.Academy, Body(returned.SettingsVersion) with { TaxRatePercent = 101 }, default)).Result);
        Assert.Equal(returned, await f.Get());
    }
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)]
    [InlineData(6)][InlineData(7)][InlineData(8)][InlineData(9)][InlineData(10)]
    public async Task Every_settings_field_change_invalidates_snapshot_without_metadata_change(int field)
    {
        using var f = new Fixture(); var empty = await f.Get(); await f.Controller.Save(f.Academy, Body(empty.SettingsVersion), default);
        var before = await f.Get(); var row = await f.Db.AcademyFinanceSettings.SingleAsync();
        switch (field)
        {
            case 0: row.TaxRegistrationNumber = "Changed"; break; case 1: row.TaxLabel = "Changed"; break;
            case 2: row.TaxRatePercent = 19; break; case 3: row.DefaultPaymentTermsDays = 8; break;
            case 4: row.TaxInclusivePricing = true; break; case 5: row.InvoiceLogoUrl = "changed"; break;
            case 6: row.InvoiceAuthorityName = "changed"; break; case 7: row.InvoiceAuthorityTitle = "changed"; break;
            case 8: row.InvoiceSignatureUrl = "changed"; break; case 9: row.InvoiceTemplateKey = "Formal"; break;
            case 10: row.PayslipTemplateKey = "Professional"; break;
        }
        await f.Db.SaveChangesAsync(); var current = await f.Get(); Assert.NotEqual(before.SettingsVersion, current.SettingsVersion);
        Assert.IsType<ConflictObjectResult>((await f.Controller.Save(f.Academy, Body(before.SettingsVersion), default)).Result);
        Assert.Equal(current, await f.Get());
    }
}
