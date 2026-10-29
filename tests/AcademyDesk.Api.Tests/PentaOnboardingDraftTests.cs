using System.Security.Claims;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOnboardingDraftTests
{
    [Fact]
    public void Migration_only_creates_draft_table_and_indexes_and_model_matches_snapshot()
    {
        using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=NeverConnected;Integrated Security=true;Connect Timeout=1").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations.Single(x => x.Key.EndsWith("_AddPentaOnboardingDrafts", StringComparison.Ordinal)).Value,
            "Microsoft.EntityFrameworkCore.SqlServer");
        Assert.Equal(3, migration.UpOperations.Count);
        var table = Assert.IsType<CreateTableOperation>(migration.UpOperations[0]);
        Assert.Equal("PentaOnboardingDrafts", table.Name);
        Assert.Equal("ProtectedBody", Assert.Single(table.Columns, x => x.Name.Contains("Protected")).Name);
        Assert.DoesNotContain(table.Columns, x => x.Name.Contains("Password") || x.Name.Contains("Email") || x.Name.Contains("Digest"));
        Assert.All(migration.UpOperations.Skip(1), x => Assert.IsType<CreateIndexOperation>(x));
    }
    [Theory]
    [InlineData("Production", true)]
    [InlineData("Development", true)]
    [InlineData("Testing", false)]
    [InlineData("Testing", true)]
    public async Task Candidate_requires_explicit_test_gate_and_SQL_before_any_work(string environment, bool enabled)
    {
        using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Penta:OperationalDraftContractsEnabled"] = enabled.ToString() }).Build();
        var service = new PentaOnboardingDraftService(db, identity, new EphemeralDataProtectionProvider(), TimeProvider.System, config, new Host(environment));
        var expected = environment == "Testing" && enabled ? 503 : 404;
        Assert.Equal(expected, (await service.SaveAsync(new ClaimsPrincipal(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, 0, "{}", default)).HttpStatus);
        Assert.Equal(expected, (await service.GetAsync(new ClaimsPrincipal(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), default)).HttpStatus);
        Assert.Equal(expected, (await service.ConfirmAsync(new ClaimsPrincipal(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "", default)).HttpStatus);
        Assert.Equal(404, (await service.ConfirmCreationAsync(new ClaimsPrincipal(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "", PentaOnboardingDraftService.CreationIntent, default)).HttpStatus);
        Assert.Equal(404, (await service.ExecuteAsync(new ClaimsPrincipal(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "", default)).HttpStatus);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(identity.ChangeTracker.Entries());
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Different_configured_SQL_stores_are_denied_without_connecting(bool pending)
    {
        using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=DomainNeverConnected;Integrated Security=true;Connect Timeout=1").Options);
        using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=IdentityNeverConnected;Integrated Security=true;Connect Timeout=1").Options);
        if (pending) db.Academies.Add(new() { Name = "Unrelated pending work" });
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Penta:OperationalDraftContractsEnabled"] = "true" }).Build();
        var service = new PentaOnboardingDraftService(db, identity, new EphemeralDataProtectionProvider(), TimeProvider.System, config, new Host("Testing"));
        Assert.Equal(503, (await service.GetAsync(new ClaimsPrincipal(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), default)).HttpStatus);
        Assert.Equal(pending ? 1 : 0, db.ChangeTracker.Entries().Count());
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
        Assert.Equal(System.Data.ConnectionState.Closed, identity.Database.GetDbConnection().State);
    }
    private sealed class Host(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "DraftGateTest";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
