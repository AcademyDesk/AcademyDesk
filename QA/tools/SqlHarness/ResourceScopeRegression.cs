using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyResourceScopeAsync(QaApiFactory factory,HttpClient client)
    {
        Guid academy,foreign,adminActor;ProgramCourse subjectA,subjectB,foreignSubject;Batch batchA,batchB,foreignBatch;Student studentA,studentB,dual,none,foreignStudent;StudentGuardian guardianLink;LearningResource foreignResource;
        string webRoot;
        using(var scope=factory.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            (await db.Academies.SingleAsync(x=>x.Id==academy)).EnabledModulesJson="[\"Core\",\"Certificates\",\"AcademicGovernance\"]";
            subjectA=new(){AcademyId=academy,Name="Resource subject A"};subjectB=new(){AcademyId=academy,Name="Resource subject B"};foreignSubject=new(){AcademyId=foreign,Name="Foreign resource subject"};
            batchA=new(){AcademyId=academy,Name="Resource batch A",CourseId=subjectA.Id};batchB=new(){AcademyId=academy,Name="Resource batch B",CourseId=subjectB.Id};foreignBatch=new(){AcademyId=foreign,Name="Foreign resource batch",CourseId=foreignSubject.Id};
            studentA=new(){AcademyId=academy,FirstName="Resource",LastName="A"};studentB=new(){AcademyId=academy,FirstName="Resource",LastName="B"};dual=new(){AcademyId=academy,FirstName="Resource",LastName="Dual"};none=new(){AcademyId=academy,FirstName="Resource",LastName="None"};foreignStudent=new(){AcademyId=foreign,FirstName="Resource",LastName="Foreign"};
            var guardian=new Guardian{AcademyId=academy,FirstName="Resource",LastName="Guardian"};
            guardianLink=new(){AcademyId=academy,GuardianId=guardian.Id,StudentId=studentA.Id,CanAccessPortal=true};
            foreignResource=new(){AcademyId=foreign,Title="Foreign resource sentinel",Type="Link",Url="https://example.invalid/foreign",IsPublished=true};
            db.AddRange(subjectA,subjectB,foreignSubject,batchA,batchB,foreignBatch,studentA,studentB,dual,none,foreignStudent,guardian,guardianLink,foreignResource,
                new LearningResource{AcademyId=academy,Title="Unpublished sentinel",Url="https://example.invalid/prior",IsPublished=false});
            foreach(var pair in new[]{(studentA.Id,batchA.Id),(studentB.Id,batchB.Id),(dual.Id,batchA.Id),(dual.Id,batchB.Id)})
                db.Add(new Enrollment{AcademyId=academy,StudentId=pair.Item1,BatchId=pair.Item2,Status="Active"});
            await db.SaveChangesAsync();
            var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            adminActor=(await users.FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
            webRoot=scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().WebRootPath;
        }
        var actors=new Dictionary<string,string>();
        foreach(var item in new[]{("a",studentA,academy),("b",studentB,academy),("dual",dual,academy),("none",none,academy),("foreign",foreignStudent,foreign)}){
            var email=$"qa-resource-{item.Item1}@example.invalid";await CreateAccessActorAsync(factory,email,"Student",item.Item3,item.Item2.Id);
            actors[item.Item1]=await LoginAsync(client,email,"Synthetic!39Ab");
        }
        var guardianUser=await CreateAccessActorAsync(factory,"qa-resource-guardian@example.invalid","Guardian",academy);
        using(var scope=factory.Services.CreateScope()){
            var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=(await users.FindByIdAsync(guardianUser.ToString()))!;user.GuardianId=guardianLink.GuardianId;
            if(!(await users.UpdateAsync(user)).Succeeded)throw new InvalidOperationException("Resource guardian fixture failed");
        }
        actors["guardian"]=await LoginAsync(client,"qa-resource-guardian@example.invalid","Synthetic!39Ab");
        var admin=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");
        var foreignAdmin=await LoginAsync(client,"qa-admin-b@example.invalid","Synthetic!39Ab");
        var teacher=await LoginAsync(client,"qa-teacher-a@example.invalid","Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization=null;var route=$"/api/academies/{academy}/resources";var count=0;
        var created=new List<(LearningResource Row,string Audience)>();var bytes=new byte[]{1,2,3,4,5};
        void Pass(string label){count++;Console.WriteLine($"RESOURCESCOPE CASE {label} PASS.");}
        async Task<JsonElement> Snapshot(){
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var folder=Path.Combine(webRoot,"uploads","learning-resources");
            var files=Directory.Exists(folder)?Directory.GetFiles(folder).OrderBy(x=>x,StringComparer.Ordinal).Select(x=>new{Path=Path.GetFileName(x),Hash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(x)))}).ToArray():[];
            return JsonSerializer.SerializeToElement(new{resources=await db.LearningResources.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),courses=await db.Courses.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),batches=await db.Batches.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),enrollments=await db.Enrollments.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),files});
        }
        static void Preserve(JsonElement before,JsonElement after,int add,Guid? changed=null){
            foreach(var key in new[]{"courses","batches","students","guardians","links","enrollments"})if(!JsonElement.DeepEquals(before.GetProperty(key),after.GetProperty(key)))throw new InvalidOperationException("Resource request changed source "+key);
            var old=before.GetProperty("resources").EnumerateArray().ToArray();var current=after.GetProperty("resources").EnumerateArray().ToArray();
            if(current.Length!=old.Length+add||old.Where(x=>x.GetProperty("Id").GetGuid()!=changed).Any(x=>!current.Any(y=>JsonElement.DeepEquals(x,y))))throw new InvalidOperationException("Unrelated/foreign resources changed");
        }
        void Audit(JsonElement before,JsonElement after,string method,string path){
            var old=before.GetProperty("audits").EnumerateArray().ToArray();var current=after.GetProperty("audits").EnumerateArray().ToArray();
            if(current.Length!=old.Length+1||old.Any(x=>!current.Any(y=>JsonElement.DeepEquals(x,y))))throw new InvalidOperationException("Prior audits changed");
            var added=current.Single(x=>!old.Any(y=>JsonElement.DeepEquals(x,y)));
            if(added.GetProperty("AcademyId").GetGuid()!=academy||added.GetProperty("ActorUserId").GetGuid()!=adminActor||added.GetProperty("Action").GetString()!=method+" LearningResources"||JsonDocument.Parse(added.GetProperty("MetadataJson").GetString()!).RootElement.GetProperty("Route").GetString()!=path)throw new InvalidOperationException("Resource actor/route audit mismatch");
        }
        async Task<HttpResponseMessage> Send(HttpRequestMessage request,string? auth){
            await Task.Delay(650);if(auth is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",auth);return await client.SendAsync(request);
        }
        async Task Reject(string label,HttpRequestMessage request,string? auth,HttpStatusCode expected,string? message=null){
            using(request){var before=await Snapshot();using var response=await Send(request,auth);
                if(response.StatusCode!=expected)Console.WriteLine("RESOURCESCOPE FAILURE "+label+" "+await response.Content.ReadAsStringAsync());
                RequireFinanceStatus(response,expected,label);
                if(message is not null){using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());if(json.RootElement.GetProperty("message").GetString()!=message)throw new InvalidOperationException("Scope error mismatch");}
                if(!JsonElement.DeepEquals(before,await Snapshot()))throw new InvalidOperationException("Denied/read request wrote SQL/audit/file: "+label);Pass(label);
            }
        }
        HttpRequestMessage CreateRequest(bool upload,Guid? batch,Guid? course,string title,bool published=true,string? description=null,string? type=null,string fileName="fixture.pdf",byte[]? content=null){
            var request=new HttpRequestMessage(HttpMethod.Post,upload?route+"/upload":route);
            if(!upload){request.Content=JsonContent.Create(new CreateResourceRequest(title,description,type,"https://example.invalid/"+Uri.EscapeDataString(title.Trim()),batch,course,published));return request;}
            var form=new MultipartFormDataContent();form.Add(new StringContent(title),"Title");form.Add(new StringContent(published?"true":"false"),"IsPublished");
            if(description is not null)form.Add(new StringContent(description),"Description");if(type is not null)form.Add(new StringContent(type),"Type");if(batch.HasValue)form.Add(new StringContent(batch.Value.ToString()),"BatchId");if(course.HasValue)form.Add(new StringContent(course.Value.ToString()),"CourseId");
            form.Add(new ByteArrayContent(content??bytes),"File",fileName);request.Content=form;return request;
        }
        async Task<LearningResource> Create(string label,bool upload,Guid? batch,Guid? course,string audience){
            var title="Resource "+label;var before=await Snapshot();var start=DateTime.UtcNow;
            using var request=CreateRequest(upload,batch,course,"  "+title+"  ");using var response=await Send(request,admin);var end=DateTime.UtcNow;
            if(response.StatusCode!=HttpStatusCode.OK)Console.WriteLine("RESOURCESCOPE FAILURE "+label+" "+await response.Content.ReadAsStringAsync());
            RequireFinanceStatus(response,HttpStatusCode.OK,label);if((await response.Content.ReadAsByteArrayAsync()).Length!=0)throw new InvalidOperationException("Intentional empty200 contract changed");
            var after=await Snapshot();Preserve(before,after,1);Audit(before,after,"POST",upload?route+"/upload":route);
            using var scope=factory.Services.CreateScope();var row=await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().LearningResources.AsNoTracking().SingleAsync(x=>x.Title==title);
            if(row.Id==Guid.Empty||row.AcademyId!=academy||row.BatchId!=batch||row.CourseId!=course||row.StudentId is not null||row.ClassSessionId is not null||row.Description is not null||row.Type!=(upload?"Document":"Link")||!row.IsPublished||row.UpdatedAtUtc is not null||row.CreatedAtUtc<start||row.CreatedAtUtc>end)throw new InvalidOperationException("Full stored input/server fields differ");
            var oldFiles=before.GetProperty("files").EnumerateArray().ToArray();var newFiles=after.GetProperty("files").EnumerateArray().ToArray();
            if(oldFiles.Any(x=>!newFiles.Any(y=>JsonElement.DeepEquals(x,y)))||newFiles.Length!=oldFiles.Length+(upload?1:0))throw new InvalidOperationException("File preservation/count differs");
            if(upload){if(!row.Url.StartsWith("/uploads/learning-resources/",StringComparison.Ordinal)||Path.GetExtension(row.Url)!=".pdf")throw new InvalidOperationException("Upload URL differs");var file=Path.Combine(webRoot,"uploads","learning-resources",Path.GetFileName(row.Url));var storedBytes=await File.ReadAllBytesAsync(file);if(!bytes.SequenceEqual(storedBytes))throw new InvalidOperationException("Upload bytes differ");}
            else if(row.Url!="https://example.invalid/"+Uri.EscapeDataString(title))throw new InvalidOperationException("Link URL differs");
            created.Add((row,audience));Pass(label);return row;
        }
        async Task Active(bool active){
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Batches.SingleAsync(x=>x.Id==batchA.Id)).IsActive=active;
            foreach(var id in new[]{subjectA.Id,subjectB.Id})(await db.Courses.SingleAsync(x=>x.Id==id)).IsActive=active;
            await db.SaveChangesAsync();
        }
        foreach(var upload in new[]{false,true})foreach(var mode in new[]{"Match","Mismatch","BatchOnly","SubjectOnly","AcademyWide","MissingBatch","ForeignBatch","EmptyBatch","MissingCourse","ForeignCourse","EmptyCourse","InactiveMatch","InactiveBatchOnly","InactiveSubjectOnly","InactiveMismatch"}){
            await Active(!mode.StartsWith("Inactive"));Guid? batch=batchA.Id,course=subjectA.Id;
            switch(mode){case "Mismatch":case "InactiveMismatch":course=subjectB.Id;break;case "BatchOnly":case "InactiveBatchOnly":course=null;break;case "SubjectOnly":case "InactiveSubjectOnly":batch=null;break;case "AcademyWide":batch=null;course=null;break;case "MissingBatch":batch=Guid.NewGuid();break;case "ForeignBatch":batch=foreignBatch.Id;break;case "EmptyBatch":batch=Guid.Empty;break;case "MissingCourse":course=Guid.NewGuid();break;case "ForeignCourse":course=foreignSubject.Id;break;case "EmptyCourse":course=Guid.Empty;break;}
            var label=(upload?"upload-":"link-")+mode;
            if(mode.Contains("Mismatch")||mode.StartsWith("Missing")||mode.StartsWith("Foreign")||mode.StartsWith("Empty"))
                await Reject(label,CreateRequest(upload,batch,course,"Rejected resource"),admin,HttpStatusCode.BadRequest,mode.Contains("Mismatch")?"The selected subject does not belong to the selected batch.":mode.EndsWith("Batch")?"Invalid batch.":"Invalid subject.");
            else await Create(label,upload,batch,course,mode=="AcademyWide"?"Global":"A");
        }
        await Active(true);
        await Create("link-subject-B",false,null,subjectB.Id,"B");await Create("upload-matched-B",true,batchB.Id,subjectB.Id,"B");
        foreach(var upload in new[]{false,true})foreach(var actor in new[]{(Label:"anonymous",Token:(string?)null,Status:HttpStatusCode.Unauthorized),(Label:"foreign-actor",Token:(string?)foreignAdmin,Status:HttpStatusCode.Forbidden),(Label:"teacher",Token:(string?)teacher,Status:HttpStatusCode.Forbidden)})
            await Reject((upload?"upload-":"link-")+actor.Label+"-denied",CreateRequest(upload,batchA.Id,subjectA.Id,"Denied"),actor.Token,actor.Status);
        foreach(var (label,content,status) in new[]{("no-body",(HttpContent?)null,HttpStatusCode.UnsupportedMediaType),("null-body",(HttpContent?)JsonContent.Create(JsonSerializer.SerializeToElement<object?>(null)),HttpStatusCode.BadRequest),("invalid-guid",(HttpContent?)JsonContent.Create(new{title="Invalid",url="https://example.invalid",batchId="invalid"}),HttpStatusCode.BadRequest)})
            await Reject("link-binding-"+label,new(HttpMethod.Post,route){Content=content},admin,status);
        foreach(var field in new[]{"BatchId","CourseId"}){
            var form=new MultipartFormDataContent();form.Add(new StringContent("invalid"),field);form.Add(new ByteArrayContent(bytes),"File","fixture.pdf");
            await Reject("upload-binding-"+field,new(HttpMethod.Post,route+"/upload"){Content=form},admin,HttpStatusCode.BadRequest);
        }
        await Reject("upload-missing-file",new(HttpMethod.Post,route+"/upload"){Content=new MultipartFormDataContent()},admin,HttpStatusCode.BadRequest);
        await Reject("upload-empty-file",CreateRequest(true,batchA.Id,subjectA.Id,"Empty file",content:[]),admin,HttpStatusCode.BadRequest);
        await Reject("upload-disallowed-extension",CreateRequest(true,batchA.Id,subjectA.Id,"Extension",fileName:"fixture.exe"),admin,HttpStatusCode.BadRequest);
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.Academies.SingleAsync(x=>x.Id==academy)).EnabledModulesJson="[\"Core\"]";await db.SaveChangesAsync();}
        foreach(var upload in new[]{false,true})await Reject((upload?"upload":"link")+"-module-denied",CreateRequest(upload,batchA.Id,subjectA.Id,"Module denied"),admin,HttpStatusCode.Forbidden);
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.Academies.SingleAsync(x=>x.Id==academy)).EnabledModulesJson="[\"Core\",\"Certificates\",\"AcademicGovernance\"]";await db.SaveChangesAsync();}
        var beforeList=await Snapshot();
        using(var request=new HttpRequestMessage(HttpMethod.Get,route))using(var response=await Send(request,admin)){
            RequireFinanceStatus(response,HttpStatusCode.OK,"scoped-list");var actual=await response.Content.ReadFromJsonAsync<List<ResourceSummary>>()??throw new InvalidOperationException("Missing list");
            using var scope=factory.Services.CreateScope();var expected=await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().LearningResources.AsNoTracking().Where(x=>x.AcademyId==academy).OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new ResourceSummary(x.Id,x.Title,x.Description,x.Type,x.Url,x.BatchId,x.CourseId,x.IsPublished)).ToListAsync();
            if(actual.Count!=17||!actual.SequenceEqual(expected))throw new InvalidOperationException("Full scoped ordered projection differs");
        }
        if(!JsonElement.DeepEquals(beforeList,await Snapshot()))throw new InvalidOperationException("List wrote");Pass("full-scoped-ordered-list-no-write");
        foreach(var actor in new[]{(Label:"anonymous",Token:(string?)null,Status:HttpStatusCode.Unauthorized),(Label:"foreign-actor",Token:(string?)foreignAdmin,Status:HttpStatusCode.Forbidden),(Label:"teacher",Token:(string?)teacher,Status:HttpStatusCode.Forbidden)})
            await Reject("list-"+actor.Label+"-denied",new(HttpMethod.Get,route),actor.Token,actor.Status);

        async Task Family(string label,Student student,string? auth,string audience,HttpStatusCode expected=HttpStatusCode.OK,bool documents=true){
            var before=await Snapshot();using var request=new HttpRequestMessage(HttpMethod.Get,$"/api/portal/students/{student.Id}");using var response=await Send(request,auth);
            RequireFinanceStatus(response,expected,label);
            if(expected==HttpStatusCode.OK){
                using var payload=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var actual=payload.RootElement.GetProperty("resources").EnumerateArray().Select(x=>new PortalResource(x.GetProperty("title").GetString()!,x.GetProperty("type").GetString()!,x.GetProperty("url").GetString()!)).OrderBy(x=>x.Title,StringComparer.Ordinal).ToArray();
                var expectedRows=documents?created.Where(x=>x.Row.IsPublished&&(x.Audience=="Global"||audience=="Dual"||x.Audience==audience)).Select(x=>new PortalResource(x.Row.Title,x.Row.Type,x.Row.Url)).OrderBy(x=>x.Title,StringComparer.Ordinal).ToArray():[];
                if(!actual.SequenceEqual(expectedRows))throw new InvalidOperationException($"Family audience differs {label}: actual={string.Join(",",actual.Select(x=>x.Title))}; expected={string.Join(",",expectedRows.Select(x=>x.Title))}");
            }
            if(!JsonElement.DeepEquals(before,await Snapshot()))throw new InvalidOperationException("Family read wrote");Pass(label);
        }
        await Family("student-A-exact-audience",studentA,actors["a"],"A");
        await Family("student-B-exact-audience",studentB,actors["b"],"B");
        await Family("student-dual-exact-audience",dual,actors["dual"],"Dual");
        await Family("student-no-enrollment-global-only",none,actors["none"],"None");
        await Family("guardian-A-exact-audience",studentA,actors["guardian"],"A");
        await Family("student-other-denied",studentB,actors["a"],"B",HttpStatusCode.Forbidden);
        await Family("foreign-student-denied",studentA,actors["foreign"],"A",HttpStatusCode.Forbidden);
        await Family("family-anonymous-denied",studentA,null,"A",HttpStatusCode.Unauthorized);
        async Task Publish(string label,LearningResource row,bool published){
            var before=await Snapshot();using var request=new HttpRequestMessage(HttpMethod.Patch,route+$"/{row.Id}/publish"){Content=JsonContent.Create(new PublishResourceRequest(published))};using var response=await Send(request,admin);
            RequireFinanceStatus(response,HttpStatusCode.OK,label);if((await response.Content.ReadAsByteArrayAsync()).Length!=0)throw new InvalidOperationException("Publish empty200 changed");
            var after=await Snapshot();Preserve(before,after,0,row.Id);Audit(before,after,"PATCH",route+$"/{row.Id}/publish");
            if(!JsonElement.DeepEquals(before.GetProperty("files"),after.GetProperty("files")))throw new InvalidOperationException("Publish modified files");
            var old=before.GetProperty("resources").EnumerateArray().Single(x=>x.GetProperty("Id").GetGuid()==row.Id);var current=after.GetProperty("resources").EnumerateArray().Single(x=>x.GetProperty("Id").GetGuid()==row.Id);
            foreach(var field in old.EnumerateObject().Where(x=>x.Name!="IsPublished"))if(!JsonElement.DeepEquals(field.Value,current.GetProperty(field.Name)))throw new InvalidOperationException("Publish changed unrelated field");
            if(current.GetProperty("IsPublished").GetBoolean()!=published)throw new InvalidOperationException("Publication not persisted");row.IsPublished=published;Pass(label);
        }
        var target=created[0].Row;await Publish("unpublish-exact-row",target,false);await Family("student-unpublished-hidden",studentA,actors["a"],"A");await Family("guardian-unpublished-hidden",studentA,actors["guardian"],"A");await Publish("republish-exact-row",target,true);await Family("student-republished-visible",studentA,actors["a"],"A");
        foreach(var item in new[]{(Label:"foreign-row",Id:foreignResource.Id),(Label:"missing-row",Id:Guid.NewGuid())})
            await Reject("publish-"+item.Label+"-404",new(HttpMethod.Patch,route+$"/{item.Id}/publish"){Content=JsonContent.Create(new PublishResourceRequest(false))},admin,HttpStatusCode.NotFound);
        foreach(var actor in new[]{(Label:"anonymous",Token:(string?)null,Status:HttpStatusCode.Unauthorized),(Label:"foreign-actor",Token:(string?)foreignAdmin,Status:HttpStatusCode.Forbidden),(Label:"teacher",Token:(string?)teacher,Status:HttpStatusCode.Forbidden)})
            await Reject("publish-"+actor.Label+"-denied",new(HttpMethod.Patch,route+$"/{target.Id}/publish"){Content=JsonContent.Create(new PublishResourceRequest(false))},actor.Token,actor.Status);
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.StudentGuardians.SingleAsync(x=>x.Id==guardianLink.Id)).CanViewDocuments=false;await db.SaveChangesAsync();}
        await Family("guardian-document-permission-empty",studentA,actors["guardian"],"A",documents:false);
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var link=await db.StudentGuardians.SingleAsync(x=>x.Id==guardianLink.Id);link.CanViewDocuments=true;link.AccessRevokedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();}
        await Family("guardian-revoked-denied",studentA,actors["guardian"],"A",HttpStatusCode.Forbidden);
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.Enrollments.SingleAsync(x=>x.StudentId==studentA.Id&&x.BatchId==batchA.Id)).Status="Completed";await db.SaveChangesAsync();}
        await Family("completed-enrollment-global-only",studentA,actors["a"],"None");
        Console.WriteLine($"RESOURCESCOPE REGRESSION PASS:{count} cases; real Identity/JSON/multipart/SQL/file/audit and independently enumerated Student/Guardian audiences; no Azure/device claim.");
    }
}
