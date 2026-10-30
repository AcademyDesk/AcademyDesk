using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class StudentOnboardingServiceTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Direct_call_without_shared_SQL_transaction_is_denied_before_any_identity_or_domain_work(bool domainSql, bool identitySql)
    {
        var domainOptions = new DbContextOptionsBuilder<AcademyDeskDbContext>();
        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>();
        if (domainSql) domainOptions.UseSqlServer("Server=127.0.0.1,1;Database=NeverConnected;Integrated Security=true;Connect Timeout=1");
        else domainOptions.UseInMemoryDatabase(Guid.NewGuid().ToString());
        if (identitySql) identityOptions.UseSqlServer("Server=127.0.0.1,1;Database=NeverConnected;Integrated Security=true;Connect Timeout=1");
        else identityOptions.UseInMemoryDatabase(Guid.NewGuid().ToString());
        using var db = new AcademyDeskDbContext(domainOptions.Options);
        using var identity = new IdentityDbContext(identityOptions.Options);
        // Missing identity services intentionally prove the transaction guard runs first.
        var service = new StudentOnboardingService(db, identity, null!, null!);
        var request = JsonSerializer.Deserialize<StudentOnboardingRequest>("""{"StudentFirstName":"Synthetic","StudentLastName":"Student","DateOfBirth":"2000-01-01"}""")!;
        var result = await service.CreateAsync(new ClaimsPrincipal(), Guid.NewGuid(), request, CancellationToken.None);
        Assert.Equal(503, result.HttpStatus);
        Assert.Null(result.Result);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(identity.ChangeTracker.Entries());
        if (domainSql) Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
        if (identitySql) Assert.Equal(System.Data.ConnectionState.Closed, identity.Database.GetDbConnection().State);
    }
}
