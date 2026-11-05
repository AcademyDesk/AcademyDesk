using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaOnboardingExecutionAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid academy, foreign, actor, teacher, session, branch, foreignBranch, inactiveBranch;
        var clock = new OnboardingDraftClock();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            actor = await identity.Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            teacher = await identity.Users.Where(x => x.Email == "qa-teacher-a@example.invalid").Select(x => x.Id).SingleAsync();
            var owned = new Branch { AcademyId = academy, Name = "Execution fixture" };
            var other = new Branch { AcademyId = foreign, Name = "Foreign execution fixture" };
            var inactive = new Branch { AcademyId = academy, Name = "Inactive execution fixture", IsActive = false };
            db.Branches.AddRange(owned, other, inactive); branch=owned.Id; foreignBranch=other.Id; inactiveBranch=inactive.Id;
            var row = new PentaMiniSession { AcademyId = academy, ActorUserId = actor, Version = 1,
                ExpiresAtUtc = clock.Value.UtcDateTime.AddHours(1), ProtectedState = "Synthetic execution fixture; no inference" };
            db.PentaMiniSessions.Add(row); await db.SaveChangesAsync(); session = row.Id;
        }
        ClaimsPrincipal Principal(Guid id) => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "SyntheticHost"));
        var principal = Principal(actor);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Penta:OperationalDraftContractsEnabled"]="true", ["Penta:OperationalOnboardingExecutionContractsEnabled"]="true" }).Build();
        PentaOnboardingDraftService Service(IServiceProvider services) => new(
            services.GetRequiredService<AcademyDeskDbContext>(), services.GetRequiredService<IdentityDbContext>(),
            services.GetRequiredService<IDataProtectionProvider>(), clock, config, services.GetRequiredService<IHostEnvironment>(),
            services.GetRequiredService<StudentOnboardingService>());
        async Task<PentaOnboardingDraftOutcome> Draft(string action, Guid id, string? payload=null, long revision=1,
            string? hash=null, string? intent=null)
        {
            using var scope=factory.Services.CreateScope(); var service=Service(scope.ServiceProvider);
            return action switch {
                "save"=>await service.SaveAsync(principal,academy,session,id,revision-1,1,payload!,default),
                "review"=>await service.ConfirmAsync(principal,academy,session,id,revision,hash!,default),
                "get"=>await service.GetAsync(principal,academy,session,id,default),
                _=>await service.ConfirmCreationAsync(principal,academy,session,id,revision,hash!,intent ?? PentaOnboardingDraftService.CreationIntent,default) };
        }
        async Task<PentaOnboardingExecutionOutcome> Execute(Guid id, string hash, long revision=1, ClaimsPrincipal? user=null,
            Guid? tenant=null, QaApiFactory? host=null)
        {
            using var scope=(host ?? factory).Services.CreateScope();
            return await Service(scope.ServiceProvider).ExecuteAsync(user ?? principal,tenant ?? academy,session,id,revision,hash,default);
        }
        string Payload(string suffix, string? number=null, Guid? branchId=null, bool minor=false)
        {
            var fields=new Dictionary<string,object> { ["StudentFirstName"]=new {kind="text",value="Synthetic"},
                ["StudentLastName"]=new {kind="text",value=suffix}, ["DateOfBirth"]=new {kind="date",value=minor?"2015-01-01":"2000-01-01"},
                ["PreferredName"]=new {kind="text",value="Nickname"}, ["StudentAddressLine1"]=new {kind="text",value="Synthetic street"},
                ["StudentState"]=new {kind="text",value="Synthetic state"}, ["StudentPostalCode"]=new {kind="text",value="560001"},
                ["EmergencyContactName"]=new {kind="text",value="Emergency contact"}, ["MedicalOrAccessibilityNotes"]=new {kind="text",value="Synthetic note only"} };
            if(number is not null) fields["StudentNumber"]=new {kind="text",value=number};
            if(branchId is not null) fields["BranchId"]=new {kind="reference",value=branchId.Value.ToString("D")};
            if(minor) {
                fields["ParentFirstName"]=new {kind="text",value="Synthetic"}; fields["ParentLastName"]=new {kind="text",value="Guardian"};
                fields["ParentEmail"]=new {kind="text",value="synthetic-parent@example.invalid"};
                fields["AllowFinance"]=new {kind="boolean",value=false}; fields["AllowDocuments"]=new {kind="boolean",value=false};
            }
            return JsonSerializer.Serialize(new {tool_key=PentaOnboardingDraftService.Tool,tool_revision=1,fields});
        }
        async Task<string> Snapshot()
        {
            using var scope=factory.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new {students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                drafts=await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.AcademyId,x.UserName}).ToArrayAsync(),
                roles=await identity.Roles.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Name}).ToArrayAsync(),
                memberships=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync() });
        }
        var count=0;
        void Pass(string label) { count++; Console.WriteLine("PASS: PENTAONBOARDINGEXECUTION "+label); }
        async Task Deny(string label, int expected, Func<Task<PentaOnboardingExecutionOutcome>> call)
        {
            var before=await Snapshot(); var result=await call();
            if(result.HttpStatus!=expected || result.Receipt is not null || before!=await Snapshot())
                throw new InvalidOperationException("Execution denial failed: "+label+" expected="+expected+" actual="+result.HttpStatus);
            Pass(label+" status="+expected+" no state changes");
        }
        async Task<PentaOnboardingDraftView> Prepare(string payload)
        {
            var id=Guid.NewGuid(); var saved=await Draft("save",id,payload);
            if(saved.HttpStatus!=201 || saved.Draft is null) throw new InvalidOperationException("Execution draft setup failed.");
            var approved=await Draft("authorize",id,hash:saved.Draft.PayloadHash);
            if(approved.HttpStatus!=200 || approved.Draft?.Status!="Authorized")
                throw new InvalidOperationException("Creation authorization setup failed: "+approved.HttpStatus);
            return approved.Draft;
        }
        var id=Guid.NewGuid(); var saved=await Draft("save",id,Payload("Execution one",branchId:branch));
        var hash=saved.Draft!.PayloadHash;
        await Deny("unconfirmed draft",409,()=>Execute(id,hash));
        if((await Draft("review",id,hash:hash)).HttpStatus!=200) throw new InvalidOperationException("Review setup failed.");
        await Deny("ordinary review is not creation permission",409,()=>Execute(id,hash));
        var before=await Snapshot();
        if((await Draft("authorize",id,hash:hash,intent:"yes")).HttpStatus!=400 || before!=await Snapshot())
            throw new InvalidOperationException("Unscoped yes authorized creation.");
        Pass("explicit intent required, generic yes denied");
        if((await Draft("authorize",id,hash:hash)).Draft?.Status!="Authorized") throw new InvalidOperationException("Creation confirmation missing.");
        await Deny("teacher",403,()=>Execute(id,hash,user:Principal(teacher)));
        await Deny("foreign tenant",403,()=>Execute(id,hash,tenant:foreign));
        await Deny("wrong revision",409,()=>Execute(id,hash,revision:2));
        await Deny("wrong digest",409,()=>Execute(id,new string('b',64)));
        var racing=await Task.WhenAll(Execute(id,hash),Execute(id,hash));
        if(!racing.Select(x=>x.HttpStatus).Order().SequenceEqual(new[]{200,201}) || racing[0].Receipt!=racing[1].Receipt)
            throw new InvalidOperationException("One-time execution race failed.");
        var receipt=racing[0].Receipt!;
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var student=await db.Students.SingleAsync(x=>x.Id==receipt.StudentId && x.AcademyId==academy);
            if(student.BranchId!=branch || student.PreferredName!="Nickname" || student.State!="Synthetic state" ||
                await db.AuditLogs.CountAsync(x=>x.EntityId==id && x.Action=="PentaOnboardingExecuted")!=1 ||
                await db.Students.CountAsync(x=>x.AcademyId==academy && x.LastName=="Execution one")!=1)
                throw new InvalidOperationException("Atomic result/source/audit mismatch.");
        }
        Pass("simultaneous exact request 201/200, same committed receipt, one student and one execution audit");
        before=await Snapshot(); var replay=await Execute(id,hash);
        if(replay.HttpStatus!=200 || replay.Receipt!=receipt || before!=await Snapshot()) throw new InvalidOperationException("Replay dispatched again.");
        Pass("fresh-scope receipt replay without redispatch");
        if((await Draft("get",id)).Draft?.Status!="Succeeded") throw new InvalidOperationException("Status not authoritative.");
        Pass("fresh status reads committed Succeeded");
        before=await Snapshot();
        if((await Draft("save",id,Payload("Edited after success"),revision:2)).HttpStatus!=409 || before!=await Snapshot())
            throw new InvalidOperationException("Succeeded draft could be reused.");
        Pass("committed draft cannot be edited into another operation");

        var minor=await Prepare(Payload("Execution minor",minor:true));
        var minorResult=await Execute(minor.Id,minor.PayloadHash);
        if(minorResult.HttpStatus!=201 || minorResult.Receipt?.GuardianId is null) throw new InvalidOperationException("Minor execution failed.");
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var link=await db.StudentGuardians.SingleAsync(x=>x.StudentId==minorResult.Receipt.StudentId);
            if(!link.CanAccessPortal || link.CanViewFinance || link.CanViewDocuments || !link.CanViewAcademicProgress ||
                await identity.Users.AnyAsync(x=>x.StudentId==minorResult.Receipt.StudentId || x.GuardianId==minorResult.Receipt.GuardianId))
                throw new InvalidOperationException("Parent grants or no-credential account rule changed.");
        }
        Pass("minor guardian linkage/grants preserved; zero portal accounts created");

        foreach(var (label,branchId) in new[]{("foreign branch",foreignBranch),("inactive branch",inactiveBranch),("missing branch",Guid.NewGuid())}) {
            var invalidId=Guid.NewGuid(); var invalid=await Draft("save",invalidId,Payload(label,branchId:branchId));
            before=await Snapshot();
            if((await Draft("authorize",invalidId,hash:invalid.Draft!.PayloadHash)).HttpStatus!=400 || before!=await Snapshot())
                throw new InvalidOperationException("Invalid branch confirmed: "+label);
            Pass(label+" rejected at confirmation without state changes");
        }
        var number= "QA-EXEC-"+Guid.NewGuid().ToString("N")[..12];
        var numberDraft=await Prepare(Payload("Unique number",number));
        if((await Execute(numberDraft.Id,numberDraft.PayloadHash)).HttpStatus!=201) throw new InvalidOperationException("Unique number setup failed.");
        var duplicateNumberId=Guid.NewGuid(); var duplicateNumber=await Draft("save",duplicateNumberId,Payload("Different name",number));
        before=await Snapshot();
        if((await Draft("authorize",duplicateNumberId,hash:duplicateNumber.Draft!.PayloadHash)).HttpStatus!=400 || before!=await Snapshot())
            throw new InvalidOperationException("Duplicate number was confirmed.");
        Pass("current duplicate student number denied before authorization");
        var duplicateId=Guid.NewGuid(); var duplicate=await Draft("save",duplicateId,Payload("Execution one")); before=await Snapshot();
        if((await Draft("authorize",duplicateId,hash:duplicate.Draft!.PayloadHash)).HttpStatus!=409 || before!=await Snapshot())
            throw new InvalidOperationException("Possible existing student silently duplicated.");
        Pass("possible same-name/DOB student requires Manual resolution");
        var sameA=await Prepare(Payload("Duplicate race")); var sameB=await Prepare(Payload("Duplicate race"));
        var duplicates=await Task.WhenAll(Execute(sameA.Id,sameA.PayloadHash),Execute(sameB.Id,sameB.PayloadHash));
        if(!duplicates.Select(x=>x.HttpStatus).Order().SequenceEqual(new[]{201,409})) throw new InvalidOperationException("Independent duplicate drafts both executed.");
        Pass("two separately authorized same-person drafts serialize 201/409");

        foreach(var mode in new[]{"audit","link"}) {
            var draft=await Prepare(Payload("Rollback "+mode,minor:mode=="link"));
            var fault=new OnboardingExecutionFault(mode);
            using var faulty=new QaApiFactory(manifest,isolatedDomainInterceptor:fault);
            before=await Snapshot(); var threw=false; PentaOnboardingExecutionOutcome? result=null;
            try { result=await Execute(draft.Id,draft.PayloadHash,host:faulty); }
            catch(DbUpdateException) { threw=true; }
            if((mode=="audit" ? !threw : result?.HttpStatus!=400) || fault.Faults!=1 || before!=await Snapshot())
                throw new InvalidOperationException("Execution fault did not roll back: "+mode);
            Pass("required "+mode+" failure rolls back student/guardian/review/receipt/audit");
            if((await Execute(draft.Id,draft.PayloadHash)).HttpStatus!=201) throw new InvalidOperationException("Explicit retry after known rollback failed.");
            Pass("explicit retry after known "+mode+" rollback creates once");
        }
        // Shared Manual adapter uses the same academy lock and unique-number validation.
        var sharedNumber="QA-BOTH-"+Guid.NewGuid().ToString("N")[..12];
        var combined=await Prepare(Payload("AI Manual race",sharedNumber));
        using(var client=factory.CreateClient()) {
            var login=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");
            client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",login);
            var manual=client.PostAsJsonAsync($"/api/academies/{academy}/student-onboarding",new {
                studentFirstName="Synthetic",studentLastName="Manual race",dateOfBirth="2001-01-01",studentNumber=sharedNumber });
            var ai=Execute(combined.Id,combined.PayloadHash);
            using var response=await manual; var aiOutcome=await ai;
            if(!((int)response.StatusCode==200 && aiOutcome.HttpStatus==400 || (int)response.StatusCode==400 && aiOutcome.HttpStatus==201))
                throw new InvalidOperationException("Manual/AI unique-number overlap failed: "+(int)response.StatusCode+"/"+aiOutcome.HttpStatus);
        }
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if(await db.Students.CountAsync(x=>x.AcademyId==academy && x.StudentNumber==sharedNumber)!=1) throw new InvalidOperationException("Manual/AI race stored duplicate number.");
        }
        Pass("Manual HTTP and direct candidate share number/transaction guard, exactly one saved");

        var expired=await Prepare(Payload("Expired execution")); clock.Value=clock.Value.AddMinutes(11);
        await Deny("expired authorization",410,()=>Execute(expired.Id,expired.PayloadHash));
        before=await Snapshot(); replay=await Execute(id,hash);
        if(replay.HttpStatus!=200 || replay.Receipt!=receipt || before!=await Snapshot()) throw new InvalidOperationException("Committed replay after approval expiry failed.");
        Pass("expiry never redispatches already committed operation");
        using(var scope=factory.Services.CreateScope()) {
            var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var user=await identity.Users.SingleAsync(x=>x.Id==actor); user.IsActive=false; await identity.SaveChangesAsync();
        }
        await Deny("revoked actor cannot replay receipt",403,()=>Execute(id,hash));
        using(var scope=factory.Services.CreateScope()) {
            var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var user=await identity.Users.SingleAsync(x=>x.Id==actor); user.IsActive=true; await identity.SaveChangesAsync();
        }
        if(count!=26) throw new InvalidOperationException("Incomplete execution SQL matrix: "+count);
        Console.WriteLine("PASS: PENTAONBOARDINGEXECUTION all "+count+" real SQL cases; explicit scoped authorization, shared domain transaction, one-time execution/result/audit; no model or public route activation.");
    }
    private sealed class OnboardingExecutionFault(string mode) : DbCommandInterceptor
    {
        public int Faults {get;private set;}
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData eventData,
            InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default)
        {
            if(Faults==0 && command.CommandText.Contains(mode=="audit"?"INSERT INTO [AuditLogs]":"INSERT INTO [StudentGuardians]",StringComparison.Ordinal)) {
                Faults++; command.CommandText="THROW 51009, 'Synthetic execution fault', 1;\n"+command.CommandText; }
            return ValueTask.FromResult(result);
        }
    }
}
