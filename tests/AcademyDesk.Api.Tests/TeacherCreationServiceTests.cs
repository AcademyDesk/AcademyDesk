using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using IdentityDbContext = AcademyDesk.Api.Data.IdentityDbContext;

namespace AcademyDesk.Api.Tests;

public sealed class TeacherCreationServiceTests
{
    [Theory]
    [InlineData(true,true)]
    [InlineData(true,false)]
    [InlineData(false,true)]
    public async Task SQL_or_mixed_store_without_shared_transaction_rejects_before_database_or_identity_access(bool sqlDomain,bool sqlIdentity)
    {
        var d = new DbContextOptionsBuilder<AcademyDeskDbContext>();
        var i = new DbContextOptionsBuilder<IdentityDbContext>();
        const string never = "Server=127.0.0.1,1;Database=NeverConnected;Integrated Security=true;Connect Timeout=1";
        if(sqlDomain)d.UseSqlServer(never);else d.UseInMemoryDatabase(Guid.NewGuid().ToString());
        if(sqlIdentity)i.UseSqlServer(never);else i.UseInMemoryDatabase(Guid.NewGuid().ToString());
        await using var db=new AcademyDeskDbContext(d.Options);await using var identity=new IdentityDbContext(i.Options);
        var result=await new TeacherCreationService(db,identity,null!).CreateAsync(new ClaimsPrincipal(),Guid.NewGuid(),Request(),default);
        Assert.Equal(503,result.HttpStatus);Assert.Null(result.Result);Assert.Empty(db.ChangeTracker.Entries());Assert.Empty(identity.ChangeTracker.Entries());
        if(sqlDomain)Assert.Equal(System.Data.ConnectionState.Closed,db.Database.GetDbConnection().State);
        if(sqlIdentity)Assert.Equal(System.Data.ConnectionState.Closed,identity.Database.GetDbConnection().State);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("AcademyAdmin")]
    public async Task Current_database_administrator_can_create_with_all_optional_fields_absent(string role)
    {
        await using var f=await Fixture.Create(role);
        var result=await f.Service.CreateAsync(f.Principal,f.Academy,Request(),default);
        Assert.Equal(201,result.HttpStatus);Assert.NotNull(result.Result);
        var row=Assert.Single(await f.Db.Teachers.AsNoTracking().ToListAsync());
        Assert.Equal("Synthetic",row.FirstName);Assert.Equal("Teacher",row.LastName);Assert.Equal(f.Academy,row.AcademyId);
        Assert.Null(row.Email);Assert.Null(row.BranchId);Assert.Null(row.DateOfBirth);Assert.Null(row.CompensationJson);Assert.True(row.IsActive);
        Assert.Single(await f.Identity.Users.ToListAsync());Assert.Empty(await f.Db.AuditLogs.ToListAsync()); // caller owns commit/audit
    }

    [Fact]
    public async Task Manual_metadata_and_nullable_empty_values_are_preserved_without_account_side_effects()
    {
        await using var f=await Fixture.Create();
        var branch=new Branch{AcademyId=f.Academy,Name="Synthetic branch",IsActive=false};f.Db.Branches.Add(branch);await f.Db.SaveChangesAsync();
        var request=new CreateTeacherRequest("  Synthetic  "," Teacher "," "," 1 "," Piano ",branch.Id,
            " Qualification "," Part time ",new(2000,1,2),new(2026,11,1)," Address "," City "," State "," 123 ","[]","{\"days\":[]}","{\"synthetic\":true}");
        var result=await f.Service.CreateAsync(f.Principal,f.Academy,request,default);Assert.Equal(201,result.HttpStatus);
        var row=Assert.Single(await f.Db.Teachers.ToListAsync());Assert.Equal("",row.Email);Assert.Equal("1",row.Phone);Assert.Equal("Piano",row.Specialties);
        Assert.Equal(request.CertificationsJson,row.CertificationsJson);Assert.Equal(request.AvailabilityJson,row.AvailabilityJson);Assert.Equal(request.CompensationJson,row.CompensationJson);
        Assert.Equal(request.DateOfBirth,row.DateOfBirth);Assert.Equal(request.JoiningDate,row.JoiningDate);Assert.Equal("Address",row.AddressLine1);
        Assert.Equal("City",row.City);Assert.Equal("State",row.State);Assert.Equal("123",row.PostalCode);Assert.Equal(branch.Id,row.BranchId);
        Assert.Equal("Qualification",row.Qualifications);Assert.Equal("Part time",row.EmploymentType);Assert.Single(await f.Identity.Users.ToListAsync());
    }

    [Theory]
    [InlineData("Teacher")]
    [InlineData("Student")]
    [InlineData("Guardian")]
    [InlineData("FinanceUser")]
    public async Task Claimed_admin_role_does_not_override_current_database_role(string role)
    {
        await using var f=await Fixture.Create(role);f.Principal.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Role,"Owner")],"forged"));
        Assert.Equal(403,(await f.Service.CreateAsync(f.Principal,f.Academy,Request(),default)).HttpStatus);Assert.Empty(await f.Db.Teachers.ToListAsync());
    }

    [Theory]
    [InlineData("inactive_actor")]
    [InlineData("foreign_actor")]
    [InlineData("removed_role")]
    [InlineData("inactive_academy")]
    [InlineData("anonymous")]
    public async Task Current_authority_changes_refuse_without_domain_write(string change)
    {
        await using var f=await Fixture.Create();
        if(change=="inactive_actor")f.User.IsActive=false;
        if(change=="foreign_actor")f.User.AcademyId=Guid.NewGuid();
        if(change=="removed_role")f.Identity.UserRoles.RemoveRange(await f.Identity.UserRoles.ToListAsync());
        if(change=="inactive_academy")(await f.Db.Academies.SingleAsync()).IsActive=false;
        await f.Db.SaveChangesAsync();await f.Identity.SaveChangesAsync();
        var principal=change=="anonymous"?new ClaimsPrincipal():f.Principal;
        Assert.Equal(403,(await f.Service.CreateAsync(principal,f.Academy,Request(),default)).HttpStatus);Assert.Empty(await f.Db.Teachers.ToListAsync());
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("missing")]
    [InlineData("empty")]
    public async Task Branch_scope_validation_is_shared_before_any_creation(string kind)
    {
        await using var f=await Fixture.Create();var branch=new Branch{AcademyId=Guid.NewGuid(),Name="Foreign synthetic"};f.Db.Branches.Add(branch);await f.Db.SaveChangesAsync();
        var id=kind=="foreign"?branch.Id:kind=="empty"?Guid.Empty:Guid.NewGuid();
        var result=await f.Service.CreateAsync(f.Principal,f.Academy,Request() with{BranchId=id},default);
        Assert.Equal(400,result.HttpStatus);Assert.Equal("The selected branch does not belong to this academy.",result.Error);Assert.Empty(await f.Db.Teachers.ToListAsync());
    }

    [Fact]
    public async Task Unauthenticated_identity_cannot_impersonate_a_known_current_administrator()
    {
        await using var f=await Fixture.Create();
        var principal=new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier,f.User.Id.ToString()),new Claim(ClaimTypes.Role,"Owner")]));
        Assert.False(principal.Identity!.IsAuthenticated);
        Assert.Equal(403,(await f.Service.CreateAsync(principal,f.Academy,Request(),default)).HttpStatus);
        Assert.Empty(await f.Db.Teachers.ToListAsync());Assert.Empty(await f.Db.AuditLogs.ToListAsync());
        Assert.Single(await f.Identity.Users.ToListAsync());
    }

    [Fact]
    public async Task Missing_name_keeps_existing_400_and_actual_platform_owner_keeps_cross_academy_access()
    {
        await using var f=await Fixture.Create();
        Assert.Equal(400,(await f.Service.CreateAsync(f.Principal,f.Academy,Request() with{FirstName=" "},default)).HttpStatus);
        f.User.IsPlatformOwner=true;f.User.AcademyId=Guid.NewGuid();await f.Identity.SaveChangesAsync();
        Assert.Equal(201,(await f.Service.CreateAsync(f.Principal,f.Academy,Request(),default)).HttpStatus);
    }

    private static CreateTeacherRequest Request()=>new(" Synthetic "," Teacher ",null,null,null,null);
    private sealed class Fixture : IAsyncDisposable
    {
        public AcademyDeskDbContext Db=null!;public IdentityDbContext Identity=null!;public UserManager<ApplicationUser> Users=null!;
        public ApplicationUser User=null!;public Guid Academy;public ClaimsPrincipal Principal=null!;
        public TeacherCreationService Service=>new(Db,Identity,Users);
        public static async Task<Fixture> Create(string role="AcademyAdmin")
        {
            var f=new Fixture{Db=new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options),
                Identity=new(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)};
            var academy=new Academy{Name="Synthetic teacher creation"};f.Db.Academies.Add(academy);await f.Db.SaveChangesAsync();f.Academy=academy.Id;
            f.User=new ApplicationUser{Id=Guid.NewGuid(),AcademyId=academy.Id,UserName="synthetic",IsActive=true};var storedRole=new ApplicationRole{Id=Guid.NewGuid(),Name=role,NormalizedName=role.ToUpperInvariant()};
            f.Identity.Users.Add(f.User);f.Identity.Roles.Add(storedRole);f.Identity.UserRoles.Add(new IdentityUserRole<Guid>{UserId=f.User.Id,RoleId=storedRole.Id});await f.Identity.SaveChangesAsync();
            f.Users=new(new UserStore<ApplicationUser,ApplicationRole,IdentityDbContext,Guid>(f.Identity),null!,new PasswordHasher<ApplicationUser>(),[],[],new UpperInvariantLookupNormalizer(),new IdentityErrorDescriber(),null!,NullLogger<UserManager<ApplicationUser>>.Instance);
            f.Principal=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,f.User.Id.ToString())],"synthetic"));return f;
        }
        public async ValueTask DisposeAsync(){Users.Dispose();await Db.DisposeAsync();await Identity.DisposeAsync();}
    }
}
