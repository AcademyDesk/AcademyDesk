using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class EnrollmentMutationServiceTests
{
    [Theory]
    [InlineData("create",true,true)] [InlineData("update",true,true)] [InlineData("transfer",true,true)]
    [InlineData("create",true,false)] [InlineData("update",true,false)] [InlineData("transfer",true,false)]
    [InlineData("create",false,true)] [InlineData("update",false,true)] [InlineData("transfer",false,true)]
    public async Task Every_relational_mutation_requires_one_shared_SQL_Identity_transaction_before_access(string action,bool sqlDomain,bool sqlIdentity)
    {
        var d=new DbContextOptionsBuilder<AcademyDeskDbContext>();var i=new DbContextOptionsBuilder<IdentityDbContext>();
        const string never="Server=127.0.0.1,1;Database=NeverConnected;Integrated Security=true;Connect Timeout=1";
        if(sqlDomain)d.UseSqlServer(never);else d.UseInMemoryDatabase(Guid.NewGuid().ToString());if(sqlIdentity)i.UseSqlServer(never);else i.UseInMemoryDatabase(Guid.NewGuid().ToString());
        await using var db=new AcademyDeskDbContext(d.Options);await using var identity=new IdentityDbContext(i.Options);
        var service=new EnrollmentMutationService(db,identity);var result=await Call(service,new(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),action);
        Assert.Equal(503,result.HttpStatus);Assert.Empty(db.ChangeTracker.Entries());Assert.Empty(identity.ChangeTracker.Entries());
        if(sqlDomain)Assert.Equal(System.Data.ConnectionState.Closed,db.Database.GetDbConnection().State);if(sqlIdentity)Assert.Equal(System.Data.ConnectionState.Closed,identity.Database.GetDbConnection().State);
    }
    public static IEnumerable<object[]> Refusals()=>from action in new[]{"create","update","transfer"}
        from change in new[]{"Teacher","Student","FrontDesk","inactive","foreign","removed_role","inactive_academy","missing_academy","platform","anonymous"}select new object[]{action,change};
    [Theory] [MemberData(nameof(Refusals))]
    public async Task Current_authority_denies_despite_forged_Owner_claims_without_side_effects(string action,string change)
    {
        await using var f=await Fixture.Create(change is "Teacher" or "Student" or "FrontDesk"?change:"AcademyAdmin");
        if(change=="inactive")f.User.IsActive=false;if(change=="foreign")f.User.AcademyId=Guid.NewGuid();if(change=="platform")f.User.IsPlatformOwner=true;
        if(change=="removed_role")f.Identity.UserRoles.RemoveRange(await f.Identity.UserRoles.ToArrayAsync());if(change=="inactive_academy")f.Tenant.IsActive=false;if(change=="missing_academy")f.Db.Academies.Remove(f.Tenant);
        await f.Identity.SaveChangesAsync();await f.Db.SaveChangesAsync();var before=await f.State();
        var principal=change=="anonymous"?new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,f.User.Id.ToString()),new Claim(ClaimTypes.Role,"Owner")])):f.Principal;
        var result=await Call(f.Service,principal,f.Tenant.Id,f.Student.Id,f.Target.Id,f.Enrollment.Id,action);Assert.Equal(403,result.HttpStatus);Assert.Null(result.Result);Assert.Equal(before,await f.State());
    }
    [Theory] [InlineData("Owner")] [InlineData("AcademyAdmin")]
    public async Task Current_administrator_uses_same_create_lifecycle_and_transfer_rules(string role)
    {
        await using var f=await Fixture.Create(role);
        var created=await f.Service.CreateAsync(f.Principal,f.Tenant.Id,new(f.Student.Id,f.Target.Id,null,"Waitlisted"),default);
        Assert.Equal(201,created.HttpStatus);Assert.Equal("Waitlisted",created.Result!.Status);
        var duplicate=await f.Service.CreateAsync(f.Principal,f.Tenant.Id,new(f.Student.Id,f.Target.Id,null,"Active"),default);Assert.Equal(409,duplicate.HttpStatus);
        var active=await f.Service.UpdateAsync(f.Principal,f.Tenant.Id,created.Result.Id,new("Active",null,null),default);Assert.Equal(200,active.HttpStatus);
        var blocked=await f.Service.UpdateAsync(f.Principal,f.Tenant.Id,created.Result.Id,new("Paused",null,null),default);Assert.Equal(400,blocked.HttpStatus);
        var paused=await f.Service.UpdateAsync(f.Principal,f.Tenant.Id,created.Result.Id,new("Paused",new(2026,10,28)," Synthetic reason "),default);Assert.Equal(200,paused.HttpStatus);
        var moved=await f.Service.TransferAsync(f.Principal,f.Tenant.Id,f.Enrollment.Id,new(f.Target.Id,new(2026,11,1)),default);Assert.Equal(200,moved.HttpStatus);
        Assert.Equal(new DateOnly(2026,11,1),moved.Result!.StartDate);Assert.Equal("Transferred",(await f.Db.Enrollments.SingleAsync(x=>x.Id==f.Enrollment.Id)).Status);
        Assert.Empty(await f.Db.AuditLogs.ToListAsync());Assert.Single(await f.Identity.Users.ToListAsync()); // caller owns audit/commit
    }
    [Fact]
    public async Task Manual_create_unknown_status_still_defaults_Active_and_omitted_date_uses_UTC_today()
    {
        await using var f=await Fixture.Create();f.Tenant.EnabledModulesJson="[]";await f.Db.SaveChangesAsync(); // Enrollment is an existing Core capability, not a paid-module gate.
        var today=DateOnly.FromDateTime(DateTime.UtcNow);var result=await f.Service.CreateAsync(f.Principal,f.Tenant.Id,new(f.Student.Id,f.Target.Id,null,"UnknownLegacy"),default);
        Assert.Equal(201,result.HttpStatus);Assert.Equal("Active",result.Result!.Status);Assert.InRange(result.Result.StartDate,today,DateOnly.FromDateTime(DateTime.UtcNow));
    }
    private static Task<EnrollmentMutationOutcome> Call(EnrollmentMutationService s,ClaimsPrincipal p,Guid academy,Guid student,Guid batch,Guid enrollment,string action)=>action switch{
        "create"=>s.CreateAsync(p,academy,new(student,batch,null,"Active"),default),"update"=>s.UpdateAsync(p,academy,enrollment,new("Completed",null,"Synthetic reason"),default),
        _=>s.TransferAsync(p,academy,enrollment,new(batch,null),default)};
    private sealed class Fixture:IAsyncDisposable
    {
        public AcademyDeskDbContext Db=null!;public IdentityDbContext Identity=null!;public Academy Tenant=null!;public ApplicationUser User=null!;public Student Student=null!;public Batch Target=null!;public Enrollment Enrollment=null!;
        public ClaimsPrincipal Principal=>new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,User.Id.ToString()),new Claim(ClaimTypes.Role,"Owner")],"Synthetic"));
        public EnrollmentMutationService Service=>new(Db,Identity);
        public static async Task<Fixture> Create(string role="AcademyAdmin"){
            var f=new Fixture{Db=new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options),Identity=new(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)};
            f.Tenant=new(){Name="Synthetic enrollment service"};f.Student=new(){AcademyId=f.Tenant.Id,FirstName="Synthetic",LastName="Learner"};
            var source=new Batch{AcademyId=f.Tenant.Id,Name="Source",CourseId=Guid.NewGuid(),Capacity=5};f.Target=new(){AcademyId=f.Tenant.Id,Name="Target",CourseId=Guid.NewGuid(),Capacity=5,WaitlistCapacity=5};
            f.Enrollment=new(){AcademyId=f.Tenant.Id,StudentId=f.Student.Id,BatchId=source.Id,Status="Active",StartDate=new(2026,10,1)};
            f.Db.AddRange(f.Tenant,f.Student,source,f.Target,f.Enrollment);await f.Db.SaveChangesAsync();
            f.User=new(){Id=Guid.NewGuid(),AcademyId=f.Tenant.Id,UserName="synthetic",IsActive=true};var r=new ApplicationRole{Id=Guid.NewGuid(),Name=role,NormalizedName=role.ToUpperInvariant()};
            f.Identity.AddRange(f.User,r,new IdentityUserRole<Guid>{UserId=f.User.Id,RoleId=r.Id});await f.Identity.SaveChangesAsync();return f;
        }
        public async Task<string> State()=>JsonSerializer.Serialize(new{enrollments=await Db.Enrollments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),audits=await Db.AuditLogs.AsNoTracking().ToArrayAsync(),users=await Identity.Users.AsNoTracking().Select(x=>new{x.Id,x.IsActive,x.IsPlatformOwner,x.AcademyId}).ToArrayAsync()});
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await Identity.DisposeAsync();}
    }
}
