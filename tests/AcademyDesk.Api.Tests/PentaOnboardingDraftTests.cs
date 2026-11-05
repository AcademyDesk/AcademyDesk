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
    public void Combined_migration_creates_scoped_protected_draft_store_without_changing_existing_tables()
    {
        using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=NeverConnected;Integrated Security=true;Connect Timeout=1").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations.Single(x => x.Key.EndsWith("_AddProtectedPentaHost", StringComparison.Ordinal)).Value,
            "Microsoft.EntityFrameworkCore.SqlServer");
        Assert.All(migration.UpOperations, operation => Assert.True(operation is CreateTableOperation or CreateIndexOperation));
        var tables = migration.UpOperations.OfType<CreateTableOperation>().ToArray();
        Assert.Equal(12, tables.Length);
        Assert.All(tables, table => Assert.StartsWith("Penta", table.Name));
        var table = Assert.Single(tables, table => table.Name == "PentaOnboardingDrafts");
        var body = Assert.Single(table.Columns, column => column.Name.Contains("Protected"));
        Assert.Equal("ProtectedBody", body.Name);
        Assert.False(body.IsNullable);
        Assert.Equal(16000, body.MaxLength);
        Assert.DoesNotContain(table.Columns, x => x.Name.Contains("Password") || x.Name.Contains("Email") || x.Name.Contains("Digest"));
        var foreignKey = Assert.Single(table.ForeignKeys);
        Assert.Equal("PentaMiniSessions", foreignKey.PrincipalTable);
        Assert.Equal(new[] { "AcademyId", "ActorUserId", "SessionId" }, foreignKey.Columns);
        Assert.Equal(new[] { "AcademyId", "ActorUserId", "Id" }, foreignKey.PrincipalColumns);
        Assert.Equal(ReferentialAction.Restrict, foreignKey.OnDelete);
        var indexes = migration.UpOperations.OfType<CreateIndexOperation>().Where(index => index.Table == table.Name).ToArray();
        Assert.Equal(2, indexes.Length);
        Assert.Contains(indexes, index => index.Columns.SequenceEqual(new[] { "AcademyId", "ActorUserId", "CreatedAtUtc" }));
        Assert.Contains(indexes, index => index.Columns.SequenceEqual(new[] { "AcademyId", "ActorUserId", "SessionId" }));
        var removed = migration.DownOperations.Select(operation => Assert.IsType<DropTableOperation>(operation).Name).Order().ToArray();
        Assert.Equal(tables.Select(table => table.Name).Order(), removed);
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
