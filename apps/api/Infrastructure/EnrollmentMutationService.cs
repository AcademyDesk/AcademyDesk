using System.Security.Claims;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AcademyDesk.Api.Infrastructure;

/// <summary>
/// Authoritative Owner/Admin facade for future conversational enrollment callers.
/// Manual uses the same internal operations through its existing access/audit filter.
/// Callers own scoped approval, replay, audit and commit; success here is not a
/// committed conversation result. No endpoint/model dispatch is registered.
/// </summary>
public sealed class EnrollmentMutationService(AcademyDeskDbContext db, IdentityDbContext identity)
{
    public Task<EnrollmentMutationOutcome> CreateAsync(ClaimsPrincipal user,Guid academy,CreateEnrollmentRequest input,CancellationToken token)=>
        RunAsync(user,academy,()=>new EnrollmentMutationOperations(db).CreateAsync(academy,input,token),token);
    public Task<EnrollmentMutationOutcome> UpdateAsync(ClaimsPrincipal user,Guid academy,Guid enrollment,UpdateEnrollmentRequest input,CancellationToken token)=>
        RunAsync(user,academy,()=>new EnrollmentMutationOperations(db).UpdateAsync(academy,enrollment,input,token),token);
    public Task<EnrollmentMutationOutcome> TransferAsync(ClaimsPrincipal user,Guid academy,Guid enrollment,TransferEnrollmentRequest input,CancellationToken token)=>
        RunAsync(user,academy,()=>new EnrollmentMutationOperations(db).TransferAsync(academy,enrollment,input,token),token);

    private async Task<EnrollmentMutationOutcome> RunAsync(ClaimsPrincipal principal,Guid academy,Func<Task<EnrollmentMutationOutcome>> operation,CancellationToken token)
    {
        if((db.Database.IsRelational()||identity.Database.IsRelational())&&
            (!db.Database.IsSqlServer()||!identity.Database.IsSqlServer()||db.Database.CurrentTransaction is not{} domain||
            identity.Database.CurrentTransaction is not{} auth||!ReferenceEquals(domain.GetDbTransaction(),auth.GetDbTransaction())))
            return new(503,Error:"Enrollment changes require an atomic host transaction.");
        if(principal.Identity?.IsAuthenticated!=true||academy==Guid.Empty||!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier),out var actor))return new(403);
        // Hold the academy and current identity/membership until the caller's commit.
        // Lock order is academy -> identity -> existing admission applock, matching
        // the authoritative host transaction before any domain admission changes.
        if(db.Database.IsSqlServer())await db.Database.ExecuteSqlInterpolatedAsync($"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academy}",token);
        var user=db.Database.IsSqlServer()?await identity.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={actor}").AsNoTracking().SingleOrDefaultAsync(token):
            await identity.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==actor,token);
        if(user is null||!user.IsActive||user.IsPlatformOwner||user.AcademyId!=academy)return new(403);
        var memberships=db.Database.IsSqlServer()?await identity.UserRoles.FromSqlInterpolated($"SELECT * FROM [AspNetUserRoles] WITH (UPDLOCK,HOLDLOCK) WHERE [UserId]={actor}").AsNoTracking().ToArrayAsync(token):
            await identity.UserRoles.AsNoTracking().Where(x=>x.UserId==actor).ToArrayAsync(token);
        var ids=memberships.Select(x=>x.RoleId).ToArray();
        var roles=db.Database.IsSqlServer()?identity.Roles.FromSqlInterpolated($"SELECT * FROM [AspNetRoles] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] IN (SELECT [RoleId] FROM [AspNetUserRoles] WHERE [UserId]={actor})"):
            identity.Roles.Where(x=>ids.Contains(x.Id));
        if(!await roles.AsNoTracking().AnyAsync(x=>x.Name=="Owner"||x.Name=="AcademyAdmin",token))return new(403);
        var tenant=await db.Academies.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==academy,token);
        if(tenant is null||!tenant.IsActive||!SubscriptionPlanCatalog.Allows(tenant.EnabledModulesJson,SubscriptionPlanCatalog.ModuleForController(nameof(EnrollmentsController))))return new(403);
        return await operation();
    }
}
