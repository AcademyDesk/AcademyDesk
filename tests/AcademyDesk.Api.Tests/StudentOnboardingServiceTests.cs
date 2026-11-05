using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using IdentityDbContext = AcademyDesk.Api.Data.IdentityDbContext;

namespace AcademyDesk.Api.Tests;

public sealed class StudentOnboardingServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Credential_free_PENTA_candidate_never_bootstraps_roles_or_writes_Identity(bool minor)
    {
        await using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var academy = new Academy { Name = "Synthetic no-account academy" }; db.Academies.Add(academy); await db.SaveChangesAsync();
        var actor = new ApplicationUser { Id = Guid.NewGuid(), AcademyId = academy.Id, UserName = "synthetic-admin", IsActive = true };
        var role = new ApplicationRole { Id = Guid.NewGuid(), Name = "AcademyAdmin", NormalizedName = "ACADEMYADMIN" };
        identity.Users.Add(actor); identity.Roles.Add(role); identity.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actor.Id, RoleId = role.Id }); await identity.SaveChangesAsync();
        using var users = new UserManager<ApplicationUser>(new UserStore<ApplicationUser, ApplicationRole, IdentityDbContext, Guid>(identity),
            null!, new PasswordHasher<ApplicationUser>(), [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actor.Id.ToString())], "SyntheticHost"));
        var fields = new Dictionary<string, object> { ["StudentFirstName"] = new { kind = "text", value = "Synthetic" },
            ["StudentLastName"] = new { kind = "text", value = "No account" }, ["DateOfBirth"] = new { kind = "date", value = minor ? "2015-01-01" : "2000-01-01" } };
        if (minor) { fields["ParentFirstName"] = new { kind = "text", value = "Synthetic" }; fields["ParentLastName"] = new { kind = "text", value = "Guardian" }; fields["ParentEmail"] = new { kind = "text", value = "synthetic-parent@example.invalid" }; }
        Assert.True(PentaOnboardingFields.TryRead(JsonSerializer.Serialize(new { tool_key = PentaOnboardingFields.Tool, tool_revision = 1, fields }), out var request));
        // A null RoleManager intentionally makes any role-bootstrap call fail.
        var outcome = await new StudentOnboardingService(db, identity, users, null!).CreateAsync(principal, academy.Id, request!, default);
        Assert.Equal(200, outcome.HttpStatus); Assert.NotNull(outcome.Result);
        Assert.False(outcome.Result.StudentAccountCreated); Assert.False(outcome.Result.ParentAccountCreated);
        Assert.Single(await db.Students.ToListAsync()); Assert.Equal(minor ? 1 : 0, await db.Guardians.CountAsync());
        Assert.Single(await identity.Users.ToListAsync()); Assert.Equal("AcademyAdmin", Assert.Single(await identity.Roles.ToListAsync()).Name);
        Assert.Single(await identity.UserRoles.ToListAsync());
        Assert.DoesNotContain(identity.ChangeTracker.Entries(), x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
    }

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
