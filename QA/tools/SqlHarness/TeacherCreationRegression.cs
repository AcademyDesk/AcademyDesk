using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Security;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyTeacherCreationServiceAsync(QaApiFactory factory,HttpClient client,QaRunManifest manifest)
    {
        Guid academy,foreign;using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();}
        var actor=await CreateAccessActorAsync(factory,"qa-teacher-create-service@example.invalid","AcademyAdmin",academy);
        var token=await LoginAsync(client,"qa-teacher-create-service@example.invalid","Synthetic!39Ab");
        var teacher=await LoginAsync(client,"qa-teacher-a@example.invalid","Synthetic!39Ab");
        var platformId=await CreateAccessActorAsync(factory,"qa-teacher-create-platform@example.invalid","AcademyAdmin",academy);
        using(var scope=factory.Services.CreateScope()){
            var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=(await users.FindByIdAsync(platformId.ToString()))!;
            user.IsPlatformOwner=true;if(!(await users.UpdateAsync(user)).Succeeded)throw new InvalidOperationException("Synthetic platform actor setup failed.");}
        var platform=await LoginAsync(client,"qa-teacher-create-platform@example.invalid","Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization=null;
        var count=0;void Pass(string label){count++;Console.WriteLine("PASS: TEACHERCREATION "+label);}
        async Task<string> Snapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{teachers=await db.Teachers.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                users=await identity.Users.AsNoTracking().Select(x=>new{x.Id,x.AcademyId,x.TeacherId,x.IsActive,x.DisplayName,x.IsPlatformOwner}).OrderBy(x=>x.Id).ToArrayAsync(),
                roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync(),
                audit=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),invoices=await db.Invoices.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),drafts=await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync()});}
        async Task<HttpResponseMessage> Post(HttpClient host,string? bearer,Guid tenant,object body){var request=new HttpRequestMessage(HttpMethod.Post,$"/api/academies/{tenant}/teachers"){Content=JsonContent.Create(body)};
            if(bearer is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",bearer);return await host.SendAsync(request);}
        async Task Reject(string label,HttpStatusCode expected,string? bearer,Guid tenant,object request){var before=await Snapshot();using var response=await Post(client,bearer,tenant,request);
            RequireFinanceStatus(response,expected,label);if(before!=await Snapshot())throw new InvalidOperationException("Teacher creation denial changed scoped state.");Pass(label);}
        var optional=new CreateTeacherRequest(" Synthetic "," Manual ",null,null,null,null);
        await Reject("anonymous cannot create",HttpStatusCode.Unauthorized,null,academy,optional);
        await Reject("Teacher cannot create",HttpStatusCode.Forbidden,teacher,academy,optional);
        await Reject("foreign academy cannot create",HttpStatusCode.Forbidden,token,foreign,optional);
        await Reject("missing name retains400",HttpStatusCode.BadRequest,token,academy,optional with{FirstName=" "});
        await Reject("missing branch retains400",HttpStatusCode.BadRequest,token,academy,optional with{BranchId=Guid.NewGuid()});
        var principal=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,actor.ToString()),new Claim(ClaimTypes.Role,"Teacher")],"synthetic"));
        var beforeDirect=await Snapshot();using(var scope=factory.Services.CreateScope()){
            var outcome=await scope.ServiceProvider.GetRequiredService<TeacherCreationService>().CreateAsync(principal,academy,optional,default);
            if(outcome.HttpStatus!=503)throw new InvalidOperationException("Unguarded direct SQL service call did not refuse.");}
        if(beforeDirect!=await Snapshot())throw new InvalidOperationException("Unguarded direct service created data.");Pass("direct SQL service requires shared transaction before any write");

        async Task RejectDirect(string label,ClaimsPrincipal claimed){
            var before=await Snapshot();using(var scope=factory.Services.CreateScope()){
                var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var original=AcademyIdentityTransaction.Validate(db,identity);await using var transaction=await db.Database.BeginTransactionAsync();
                await using var linked=await AcademyIdentityTransaction.EnlistAsync(db,identity,transaction,original,default);
                var outcome=await scope.ServiceProvider.GetRequiredService<TeacherCreationService>().CreateAsync(claimed,academy,optional,default);
                if(outcome.HttpStatus!=403||outcome.Result is not null)throw new InvalidOperationException("Direct teacher authorization did not refuse.");
                // Commit the denial transaction to prove there was no hidden staged write.
                await db.SaveChangesAsync();await identity.SaveChangesAsync();await transaction.CommitAsync();
            }
            if(before!=await Snapshot())throw new InvalidOperationException("Direct teacher authorization denial changed scoped state.");Pass(label);
        }
        await RejectDirect("unauthenticated known admin identity cannot create through shared service",
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,actor.ToString()),new Claim(ClaimTypes.Role,"Owner")])));
        Guid teacherActor;using(var scope=factory.Services.CreateScope())teacherActor=await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
            .Where(x=>x.UserName=="qa-teacher-a@example.invalid").Select(x=>x.Id).SingleAsync();
        await RejectDirect("authenticated Teacher forged Owner claims cannot create through shared service",
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,teacherActor.ToString()),new Claim(ClaimTypes.Role,"Owner")],"synthetic")));

        var created=new List<Guid>();
        async Task VerifyCreated(HttpClient host,string bearer,Guid tenant,Guid expectedActor,CreateTeacherRequest request,string label){
            Guid[] oldAudits;int oldUsers;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();oldAudits=await db.AuditLogs.Select(x=>x.Id).ToArrayAsync();oldUsers=await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.CountAsync();}
            using var response=await Post(host,bearer,tenant,request);RequireFinanceStatus(response,HttpStatusCode.Created,label);var saved=(await response.Content.ReadFromJsonAsync<TeacherSummary>())!;
            if(saved.Id==Guid.Empty||response.Headers.Location?.ToString()!=$"/api/academies/{tenant}/teachers/{saved.Id}")throw new InvalidOperationException("Teacher creation wire response changed.");
            using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.Teachers.AsNoTracking().SingleAsync(x=>x.Id==saved.Id);
                if(row.AcademyId!=tenant||row.FirstName!=request.FirstName.Trim()||row.Email!=request.Email?.Trim()||row.CompensationJson!=request.CompensationJson||
                    row.AvailabilityJson!=request.AvailabilityJson||row.CertificationsJson!=request.CertificationsJson||row.DateOfBirth!=request.DateOfBirth||row.JoiningDate!=request.JoiningDate||!row.IsActive)
                    throw new InvalidOperationException("Teacher creation field roundtrip mismatch.");
                var audits=await db.AuditLogs.AsNoTracking().Where(x=>!oldAudits.Contains(x.Id)).ToArrayAsync();if(audits.Length!=1||audits[0].Action!="POST Teachers"||audits[0].ActorUserId!=expectedActor||audits[0].AcademyId!=tenant)
                    throw new InvalidOperationException("Teacher create and mandatory current-actor audit did not commit together.");
                if(await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.CountAsync()!=oldUsers)throw new InvalidOperationException("Teacher create unexpectedly created a portal account.");}
            created.Add(saved.Id);Pass(label);}
        await VerifyCreated(client,token,academy,actor,optional,"Manual optional fields empty create and one audit");
        await VerifyCreated(client,token,academy,actor,optional with{LastName="Metadata",DateOfBirth=new(2000,1,1),JoiningDate=new(2026,11,1),CompensationJson="{\"synthetic\":true}",AvailabilityJson="[]",CertificationsJson="[]"},"Manual metadata/null wire compatibility retained");
        await VerifyCreated(client,platform,foreign,platformId,optional with{LastName="Platform"},"platform cross-academy creation now atomic and audited");

        var fault=new TeacherCreationAuditFault();using(var failingFactory=new QaApiFactory(manifest,isolatedDomainInterceptor:fault)){using var failing=failingFactory.CreateClient();
            var before=await Snapshot();using var response=await Post(failing,token,academy,optional with{LastName="AuditFailure"});RequireFinanceStatus(response,HttpStatusCode.InternalServerError,"teacher required audit rollback");
            if(fault.Hits!=1||before!=await Snapshot())throw new InvalidOperationException("Teacher/audit survived required audit failure.");Pass("required generic audit failure rolls teacher back");}

        // Same domain service is reached by the strictly parsed inactive typed
        // candidate, under an explicit host transaction with a real domain audit.
        // This direct harness path is not a model dispatch or production tool.
        var payload=JsonSerializer.Serialize(new{tool_key=PentaTeacherCreationFields.Tool,tool_revision=1,fields=new{
            FirstName=new{kind="text",value="Synthetic"},LastName=new{kind="text",value="TypedCandidate"}}});
        if(!PentaTeacherCreationFields.TryRead(payload,out var parsed))throw new InvalidOperationException("Typed teacher fixture invalid.");
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var original=AcademyIdentityTransaction.Validate(db,identity);await using var transaction=await db.Database.BeginTransactionAsync();
            await using var linked=await AcademyIdentityTransaction.EnlistAsync(db,identity,transaction,original,default);
            var result=await scope.ServiceProvider.GetRequiredService<TeacherCreationService>().CreateAsync(principal,academy,parsed!,default);
            if(result.HttpStatus!=201||result.Result is null)throw new InvalidOperationException("Shared typed teacher create failed.");
            db.AuditLogs.Add(new AuditLog{AcademyId=academy,ActorUserId=actor,EntityType="Teacher",EntityId=result.Result.Id,Action="SyntheticTeacherSharedServiceCreated"});
            await db.SaveChangesAsync();await transaction.CommitAsync();created.Add(result.Result.Id);
        }
        Pass("strict typed candidate uses same authorized shared service with host transaction/audit, no AI dispatch");
        using(var scope=factory.Services.CreateScope()){
            var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            identity.UserRoles.RemoveRange(await identity.UserRoles.Where(x=>x.UserId==actor).ToListAsync());await identity.SaveChangesAsync();}
        await RejectDirect("removed current admin membership cannot create through shared service despite Owner claims",
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,actor.ToString()),new Claim(ClaimTypes.Role,"Owner")],"synthetic")));
        using(var scope=factory.Services.CreateScope()){
            var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();var stored=await identity.Users.SingleAsync(x=>x.Id==actor);stored.IsActive=false;await identity.SaveChangesAsync();}
        await Reject("revoked actor old bearer cannot create",HttpStatusCode.Forbidden,token,academy,optional);
        var evidence=Path.GetFullPath(Path.Combine("QA","EVIDENCE","teacher-creation-sql-"+manifest.RunId.ToString("N")));Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence,"result.json"),JsonSerializer.Serialize(new{run=manifest.RunId.ToString("N"),passed=true,checks=count,created=created.Count,syntheticDomainData=true,
            newAiToolRegistered=false,modelInference=false,accountsCreated=0,physicalDevices="NOT RUN"},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS: TEACHERCREATION all {count} real SQL/HTTP/shared-service cases; no model registration or account creation.");
    }

    private sealed class TeacherCreationAuditFault : SaveChangesInterceptor
    {
        public int Hits{get;private set;}
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,InterceptionResult<int> result,CancellationToken token=default){
            if(data.Context is AcademyDeskDbContext db&&db.ChangeTracker.Entries<AuditLog>().Any(x=>x.State==EntityState.Added&&x.Entity.Action=="POST Teachers")){
                Hits++;throw new InvalidOperationException("Synthetic required teacher audit failure.");}
            return base.SavingChangesAsync(data,result,token);
        }
    }
}
