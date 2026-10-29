using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Security;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed record EnrollmentFixture(Guid Student,Guid Target,Guid Source,Guid Enrollment,Guid Course);
    private static async Task VerifyEnrollmentMutationAsync(QaApiFactory factory,QaRunManifest manifest)
    {
        Guid academy,foreign,teacherActor;using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            teacherActor=await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x=>x.UserName=="qa-teacher-a@example.invalid").Select(x=>x.Id).SingleAsync();}
        const string email="qa-enrollment-shared@example.invalid";var actor=await CreateAccessActorAsync(factory,email,"AcademyAdmin",academy);
        var frontDesk=await CreateAccessActorAsync(factory,"qa-enrollment-frontdesk@example.invalid","FrontDesk",academy);
        async Task<HttpClient> Client(string login,QaApiFactory? host=null){var c=(host??factory).CreateClient();c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",await LoginAsync(c,login,"Synthetic!39Ab"));return c;}
        using var admin=await Client(email);using var desk=await Client("qa-enrollment-frontdesk@example.invalid");using var teacher=await Client("qa-teacher-a@example.invalid");using var anonymous=factory.CreateClient();
        ClaimsPrincipal Principal(Guid? who=null,bool authenticated=true)=>new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,(who??actor).ToString("D")),new Claim(ClaimTypes.Role,"Owner")],authenticated?"Synthetic service principal":null));
        var count=0;void Pass(string label){count++;Console.WriteLine("PASS: ENROLLMENTSHARED "+label);}
        async Task<EnrollmentFixture> Seed(int capacity=5,int waitlist=3){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var student=new Student{AcademyId=academy,FirstName="Synthetic",LastName="Enrollment service "+Guid.NewGuid().ToString("N")};var course=new ProgramCourse{AcademyId=academy,Name="Synthetic target "+Guid.NewGuid().ToString("N")};
            var sourceCourse=new ProgramCourse{AcademyId=academy,Name="Synthetic source "+Guid.NewGuid().ToString("N")};var target=new Batch{AcademyId=academy,Name="Synthetic target "+Guid.NewGuid().ToString("N"),CourseId=course.Id,Capacity=capacity,WaitlistCapacity=waitlist};
            var source=new Batch{AcademyId=academy,Name="Synthetic source "+Guid.NewGuid().ToString("N"),CourseId=sourceCourse.Id,Capacity=8};var enrollment=new Enrollment{AcademyId=academy,StudentId=student.Id,BatchId=source.Id,StartDate=new(2026,10,1),Status="Active"};
            db.AddRange(student,course,sourceCourse,target,source,enrollment);await db.SaveChangesAsync();return new(student.Id,target.Id,source.Id,enrollment.Id,course.Id);}
        async Task<string> Snapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{enrollments=await db.Enrollments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),batches=await db.Batches.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),teachers=await db.Teachers.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),invoices=await db.Invoices.AsNoTracking().OrderBy(x=>x.Id).ToArrayAsync(),
                users=await identity.Users.AsNoTracking().Select(x=>new{x.Id,x.AcademyId,x.IsActive,x.IsPlatformOwner,x.TeacherId}).OrderBy(x=>x.Id).ToArrayAsync(),roles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToArrayAsync()});}
        async Task<EnrollmentMutationOutcome> Direct(EnrollmentFixture f,string action,ClaimsPrincipal? p=null,Guid? tenant=null,bool addAudit=true,QaApiFactory? host=null){
            using var scope=(host??factory).Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var original=AcademyIdentityTransaction.Validate(db,identity);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await using var linked=await AcademyIdentityTransaction.EnlistAsync(db,identity,tx,original,default);var service=scope.ServiceProvider.GetRequiredService<EnrollmentMutationService>();
            var result=action switch{
                "create"=>await service.CreateAsync(p??Principal(),tenant??academy,new(f.Student,f.Target,new(2026,11,1),"Active"),default),
                "waitlist"=>await service.CreateAsync(p??Principal(),tenant??academy,new(f.Student,f.Target,null,"Waitlisted"),default),
                "update"=>await service.UpdateAsync(p??Principal(),tenant??academy,f.Enrollment,new("Paused",new(2026,11,1)," Synthetic reason "),default),
                "activate"=>await service.UpdateAsync(p??Principal(),tenant??academy,f.Enrollment,new("Active",null,null),default),
                _=>await service.TransferAsync(p??Principal(),tenant??academy,f.Enrollment,new(f.Target,new(2026,11,2)),default)};
            if(result.HttpStatus is 200 or 201&&addAudit)db.AuditLogs.Add(new(){AcademyId=tenant??academy,ActorUserId=actor,EntityType="QaEnrollment",EntityId=result.Result!.Id,Action="QaEnrollment"+action,MetadataJson="{}"});
            // Commit refusals too, proving there are no hidden staged mutations.
            await db.SaveChangesAsync();await identity.SaveChangesAsync();await tx.CommitAsync();return result;
        }
        async Task DenyDirect(string label,int expected,EnrollmentFixture f,string action,ClaimsPrincipal? p=null,Guid? tenant=null){var before=await Snapshot();var outcome=await Direct(f,action,p,tenant);if(outcome.HttpStatus!=expected||outcome.Result is not null||before!=await Snapshot())throw new InvalidOperationException("Shared enrollment refusal failed: "+label);Pass(label);}
        string Base()=> $"/api/academies/{academy}/enrollments";
        Task<HttpResponseMessage> Native(EnrollmentFixture f,string action,HttpClient? c=null)=>action switch{
            "create"=>(c??admin).PostAsJsonAsync(Base(),new CreateEnrollmentRequest(f.Student,f.Target,new(2026,11,1),"Active")),
            "update"=>(c??admin).PutAsJsonAsync(Base()+"/"+f.Enrollment,new UpdateEnrollmentRequest("Paused",new(2026,11,1)," Synthetic reason ")),
            _=>(c??admin).PostAsJsonAsync(Base()+"/"+f.Enrollment+"/transfer",new TransferEnrollmentRequest(f.Target,new(2026,11,2)))};
        var first=await Seed();
        foreach(var action in new[]{"create","update","transfer"}){
            var before=await Snapshot();using(var scope=factory.Services.CreateScope()){
                var service=scope.ServiceProvider.GetRequiredService<EnrollmentMutationService>();var result=action switch{
                    "create"=>await service.CreateAsync(Principal(),academy,new(first.Student,first.Target,null,"Active"),default),
                    "update"=>await service.UpdateAsync(Principal(),academy,first.Enrollment,new("Paused",null,"Synthetic"),default),
                    _=>await service.TransferAsync(Principal(),academy,first.Enrollment,new(first.Target,null),default)};
                if(result.HttpStatus!=503)throw new InvalidOperationException("Shared service accepted missing transaction.");}
            if(before!=await Snapshot())throw new InvalidOperationException("Unguarded enrollment service wrote data.");Pass(action+" requires shared transaction before access");
            await DenyDirect(action+" ignores forged Owner for actual Teacher",403,first,action,Principal(teacherActor));
            await DenyDirect(action+" refuses unauthenticated administrator identity",403,first,action,Principal(authenticated:false));
            await DenyDirect(action+" refuses foreign tenant",403,first,action,tenant:foreign);
        }
        // Manual's established granular FrontDesk permission remains available;
        // this admin-only PENTA facade deliberately does not widen its tool scope.
        await DenyDirect("FrontDesk does not obtain admin PENTA authority",403,first,"create",Principal(frontDesk));
        var manualDesk=await Seed();using(var response=await Native(manualDesk,"create",desk))RequireFinanceStatus(response,HttpStatusCode.Created,"retained FrontDesk native enrollment");Pass("Manual FrontDesk permission retained by original filter");
        foreach(var who in new[]{anonymous,teacher}){var before=await Snapshot();using var response=await Native(first,"create",who);RequireFinanceStatus(response,who==anonymous?HttpStatusCode.Unauthorized:HttpStatusCode.Forbidden,"native enrollment permission");if(before!=await Snapshot())throw new InvalidOperationException("Native deny wrote data.");Pass("native denied actor no mutation");}
        var created=await Direct(first,"create");if(created.HttpStatus!=201||created.Result!.StartDate!=new DateOnly(2026,11,1)||created.Result.Status!="Active")throw new InvalidOperationException("Shared create response failed.");
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();if(!await db.Enrollments.AnyAsync(x=>x.Id==created.Result.Id&&x.AcademyId==academy&&x.StudentId==first.Student&&x.BatchId==first.Target)||!await db.AuditLogs.AnyAsync(x=>x.EntityId==created.Result.Id&&x.Action=="QaEnrollmentcreate"))throw new InvalidOperationException("Shared enrollment persistence/audit mismatch.");}Pass("shared creation preserves deterministic fields and caller audit");
        await DenyDirect("duplicate active admission blocked",409,first,"create");
        var waiting=await Seed();var wait=await Direct(waiting,"waitlist");if(wait.HttpStatus!=201||wait.Result!.Status!="Waitlisted")throw new InvalidOperationException("Waitlist create failed.");Pass("Waitlisted explicit admission retained");
        await DenyDirect("existing waitlist cannot be duplicated",409,waiting,"create");var activation=await Direct(waiting with{Enrollment=wait.Result.Id},"activate");if(activation.HttpStatus!=200||activation.Result!.Id!=wait.Result.Id||activation.Result.Status!="Active")throw new InvalidOperationException("Waitlist activation did not reuse row.");Pass("activate existing waitlist row instead of duplicate");
        var moved=await Seed();var transfer=await Direct(moved,"transfer");if(transfer.HttpStatus!=200||transfer.Result!.BatchId!=moved.Target||transfer.Result.StartDate!=new DateOnly(2026,11,2))throw new InvalidOperationException("Transfer response changed.");
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var source=await db.Enrollments.AsNoTracking().SingleAsync(x=>x.Id==moved.Enrollment);if(source.Status!="Transferred"||source.EndDate!=new DateOnly(2026,11,2)||await db.Enrollments.CountAsync(x=>x.StudentId==moved.Student)!=2)throw new InvalidOperationException("Transfer source/target did not persist together.");}Pass("shared transfer preserves source history and exact effective date");
        var change=await Seed();var changed=await Direct(change,"update");if(changed.HttpStatus!=200||changed.Result!.Status!="Paused")throw new InvalidOperationException("Shared lifecycle update failed.");
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.Enrollments.SingleAsync(x=>x.Id==change.Enrollment);if(row.LifecycleReason!="Synthetic reason"||row.EndDate!=new DateOnly(2026,11,1))throw new InvalidOperationException("Lifecycle nullable/date/reason differs.");}Pass("shared lifecycle reason trimming and end date retained");
        var stale=await Seed();await DenyDirect("missing current student rejected",400,stale with{Student=Guid.NewGuid()},"create");await DenyDirect("missing current batch rejected",400,stale with{Target=Guid.NewGuid()},"create");
        await DenyDirect("missing lifecycle enrollment rejected",404,stale with{Enrollment=Guid.NewGuid()},"update");await DenyDirect("missing transfer target rejected",404,stale with{Target=Guid.NewGuid()},"transfer");
        foreach(var path in new[]{"create","transfer"}){
            var full=await Seed(0);await DenyDirect(path+" observes current target capacity",409,full,path);
            var prerequisite=await Seed();Guid required;using(var scope=factory.Services.CreateScope()){
                var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var course=new ProgramCourse{AcademyId=academy,Name="Synthetic required "+Guid.NewGuid().ToString("N")};required=course.Id;
                db.AddRange(course,new CoursePrerequisite{AcademyId=academy,CourseId=prerequisite.Course,RequiredCourseId=course.Id,MustBeCompleted=true});await db.SaveChangesAsync();}
            await DenyDirect(path+" enforces completed prerequisites",409,prerequisite,path);
            using(var scope=factory.Services.CreateScope()){
                var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var past=new Batch{AcademyId=academy,CourseId=required,Name="Synthetic completed history "+Guid.NewGuid().ToString("N"),IsActive=false};
                db.AddRange(past,new Enrollment{AcademyId=academy,StudentId=prerequisite.Student,BatchId=past.Id,StartDate=new(2026,1,1),EndDate=new(2026,9,1),Status="Completed"});await db.SaveChangesAsync();}
            var outcome=await Direct(prerequisite,path);if(outcome.HttpStatus!=(path=="create"?201:200))throw new InvalidOperationException("Completed inactive history prerequisite regressed.");Pass(path+" accepts completed prerequisite history in inactive batch");
        }
        // Future conversational caller audit failure after the service's actual save
        // must roll back create, lifecycle or both halves of a transfer.
        foreach(var action in new[]{"create","update","transfer"}){
            var fixture=await Seed();var fault=new EnrollmentSharedAuditFault("QaEnrollment"+action);using var faulty=new QaApiFactory(manifest,isolatedDomainInterceptor:fault);var before=await Snapshot();var threw=false;
            try{_=await Direct(fixture,action,host:faulty);}catch(InvalidOperationException){threw=true;}
            if(!threw||fault.Hits!=1||before!=await Snapshot())throw new InvalidOperationException("Shared enrollment audit failure left partial state.");Pass(action+" caller audit failure rolls all domain rows back");
            var nativeFault=new EnrollmentSharedAuditFault(action=="update"?"PUT Enrollments":"POST Enrollments");using var nativeFail=new QaApiFactory(manifest,isolatedDomainInterceptor:nativeFault);using var c=await Client(email,nativeFail);
            before=await Snapshot();using var response=await Native(fixture,action,c);RequireFinanceStatus(response,HttpStatusCode.InternalServerError,"native audit rollback");if(nativeFault.Hits!=1||before!=await Snapshot())throw new InvalidOperationException("Native enrollment audit rollback changed.");Pass(action+" native audit rollback retained");
        }
        var raceStatuses=new List<int[]>();for(var attempt=1;attempt<=5;attempt++){
            var f=await Seed(1);Guid second;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var s=new Student{AcademyId=academy,FirstName="Synthetic",LastName="Race"};second=s.Id;db.Add(s);await db.SaveChangesAsync();}
            var direct=Direct(f,"create");var native=Native(f with{Student=second},"create");await Task.WhenAll(direct,native);using var response=await native;
            var statuses=new[]{direct.Result.HttpStatus,(int)response.StatusCode}.Order().ToArray();if(!statuses.SequenceEqual(new[]{201,409}))throw new InvalidOperationException("Mixed admission last-seat race was not guarded.");
            using(var scope=factory.Services.CreateScope())if(await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Enrollments.CountAsync(x=>x.BatchId==f.Target&&x.Status=="Active")!=1)throw new InvalidOperationException("Mixed race exceeded capacity.");raceStatuses.Add(statuses);Pass("mixed Manual/service last-seat race "+attempt+"201/409 one active row");
        }
        for(var attempt=1;attempt<=5;attempt++){
            var f=await Seed(10);var direct=Direct(f,"create");var native=Native(f,"create");await Task.WhenAll(direct,native);using var response=await native;
            if(!new[]{direct.Result.HttpStatus,(int)response.StatusCode}.Order().SequenceEqual(new[]{201,409}))throw new InvalidOperationException("Mixed duplicate admission race was not guarded.");
            using(var scope=factory.Services.CreateScope())if(await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Enrollments.CountAsync(x=>x.BatchId==f.Target&&x.StudentId==f.Student&&x.Status=="Active")!=1)throw new InvalidOperationException("Mixed duplicate rows committed.");Pass("mixed Manual/service duplicate race "+attempt+" one admission");
        }
        for(var attempt=1;attempt<=5;attempt++){
            var f=await Seed(1);Guid second;using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var s=new Student{AcademyId=academy,FirstName="Synthetic",LastName="Transfer race"};second=s.Id;db.Add(s);await db.SaveChangesAsync();}
            var direct=Direct(f,"transfer");var native=Native(f with{Student=second},"create");await Task.WhenAll(direct,native);using var response=await native;
            var statuses=new[]{direct.Result.HttpStatus,(int)response.StatusCode}.Order().ToArray();if(!(statuses.SequenceEqual(new[]{200,409})||statuses.SequenceEqual(new[]{201,409})))throw new InvalidOperationException("Mixed transfer/create race failed.");
            using(var scope=factory.Services.CreateScope()){
                var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();if(await db.Enrollments.CountAsync(x=>x.BatchId==f.Target&&x.Status=="Active")!=1)throw new InvalidOperationException("Transfer race over capacity.");var source=await db.Enrollments.AsNoTracking().SingleAsync(x=>x.Id==f.Enrollment);
                if(source.Status!=(direct.Result.HttpStatus==200?"Transferred":"Active"))throw new InvalidOperationException("Losing transfer changed source history.");}Pass("mixed transfer/create last-seat race "+attempt+" all-or-nothing source");
        }
        var revocation=await Seed();using(var scope=factory.Services.CreateScope()){
            var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();var roleIds=await identity.Roles.Where(x=>x.Name=="AcademyAdmin").Select(x=>x.Id).ToArrayAsync();
            await identity.UserRoles.Where(x=>x.UserId==actor&&roleIds.Contains(x.RoleId)).ExecuteDeleteAsync();}
        foreach(var action in new[]{"create","update","transfer"})await DenyDirect(action+" revoked database membership defeats stale Owner claims",403,revocation,action);
        var dir=Path.GetFullPath(Path.Combine("QA","EVIDENCE","enrollment-shared-sql-"+manifest.RunId.ToString("N")));Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir,"result.json"),JsonSerializer.Serialize(new{run=manifest.RunId.ToString("N"),passed=true,checks=count,capacityRaces=5,duplicateRaces=5,transferRaces=5,raceStatuses,sqlBacked=true,modelInference=false,toolRegistered=false,productionApproved=false},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS: ENROLLMENTSHARED all {count} real SQL/HTTP/service cases;15 guarded mixed admission races, no model dispatch.");
    }
    private sealed class EnrollmentSharedAuditFault(string action):SaveChangesInterceptor{
        public int Hits{get;private set;}
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,InterceptionResult<int> result,CancellationToken token=default){
            if(data.Context is AcademyDeskDbContext db&&db.ChangeTracker.Entries<AuditLog>().Any(x=>x.State==EntityState.Added&&x.Entity.Action==action)){Hits++;throw new InvalidOperationException("Synthetic enrollment required audit fault.");}return base.SavingChangesAsync(data,result,token);
        }
    }
}
