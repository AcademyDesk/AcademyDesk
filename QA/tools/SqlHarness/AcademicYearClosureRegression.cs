using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed record YearClosureState(string Stable,List<AcademicYear> Years,List<AcademicTerm> Terms,List<AuditLog> Audits);
    private static async Task VerifyAcademicYearClosureAsync(QaApiFactory factory,HttpClient client,QaRunManifest manifest)
    {
        Guid academy,foreign,actor;var count=0;var start=new DateOnly(2026,1,1);var end=new DateOnly(2027,1,1);
        using(var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            actor=await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x=>x.Email=="qa-admin-a@example.invalid").Select(x=>x.Id).SingleAsync();
            foreach(var a in await db.Academies.Where(x=>x.Id==academy||x.Id==foreign).ToListAsync())
                a.EnabledModulesJson=JsonSerializer.Serialize((JsonSerializer.Deserialize<string[]>(a.EnabledModulesJson)??[]).Append("AcademicGovernance").Distinct());
            await db.SaveChangesAsync();
        }
        var admin=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");
        var other=await LoginAsync(client,"qa-admin-b@example.invalid","Synthetic!39Ab");
        var teacher=await LoginAsync(client,"qa-teacher-a@example.invalid","Synthetic!39Ab");client.DefaultRequestHeaders.Authorization=null;
        string Root(Guid a)=>$"/api/academies/{a}/academic-periods";
        string Route(Guid a,Guid year,bool close)=>Root(a)+(close?$"/years/{year}/close":"/terms");
        void Pass(string label){count++;Console.WriteLine($"YEARCLOSURE CASE {label} PASS.");}
        async Task<AcademicYear> Seed(bool closed=false,bool current=true,string? term=null,Guid? tenant=null)
        {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var y=new AcademicYear{AcademyId=tenant??academy,Name="Synthetic year "+Guid.NewGuid().ToString("N"),StartDate=start,EndDate=end,IsClosed=closed,IsCurrent=current};
            db.Add(y);if(term is not null)db.Add(new AcademicTerm{AcademyId=y.AcademyId,AcademicYearId=y.Id,Name="Existing term",StartDate=start,EndDate=end,IsClosed=term=="closed"});
            await db.SaveChangesAsync();return y;
        }
        async Task<YearClosureState> State()
        {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var g=await GovernanceStateAsync(factory);
            return new(JsonSerializer.Serialize(new{g.Stable,g.Tasks,Courses=await db.Courses.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
                Batches=await db.Batches.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Enrollments=await db.Enrollments.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
                Prerequisites=await db.CoursePrerequisites.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()}),
                await db.AcademicYears.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),await db.AcademicTerms.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),g.Audits);
        }
        async Task<HttpResponseMessage> Send(Guid a,Guid year,bool close=false,string name=" Term ",int first=0,int last=365,string? token=null,bool anonymous=false,bool list=false)
        {
            using var request=new HttpRequestMessage(list?HttpMethod.Get:close?HttpMethod.Patch:HttpMethod.Post,list?Root(a):Route(a,year,close));
            if(!anonymous)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token??admin);
            if(!list&&!close)request.Content=JsonContent.Create(new{academicYearId=year,name,startDate=start.AddDays(first),endDate=start.AddDays(last)});
            return await client.SendAsync(request);
        }
        void AssertDelta(YearClosureState before,YearClosureState after,Guid year,bool create,bool close,bool platform,string name=" Term ",int first=0,int last=365)
        {
            if(before.Stable!=after.Stable)throw new InvalidOperationException("Year mutation changed unrelated snapshot.");
            if(close){var expected=before.Years.Single(x=>x.Id==year);expected.IsClosed=true;expected.IsCurrent=false;}
            if(JsonSerializer.Serialize(before.Years)!=JsonSerializer.Serialize(after.Years))throw new InvalidOperationException("Academic year full-row/flags changed unexpectedly.");
            var added=after.Terms.Where(x=>before.Terms.All(y=>y.Id!=x.Id)).ToArray();
            if(added.Length!=(create?1:0)||JsonSerializer.Serialize(before.Terms)!=JsonSerializer.Serialize(after.Terms.Where(x=>added.All(y=>y.Id!=x.Id))))throw new InvalidOperationException("Term full-row boundary failed.");
            if(create&&(added[0].AcademicYearId!=year||added[0].AcademyId!=academy||added[0].Name!=name.Trim()||added[0].StartDate!=start.AddDays(first)||added[0].EndDate!=start.AddDays(last)||added[0].IsClosed||added[0].UpdatedAtUtc is not null))throw new InvalidOperationException("Stored term fields/defaults wrong.");
            var audits=after.Audits.Where(x=>before.Audits.All(y=>y.Id!=x.Id)).ToArray();
            if(audits.Length!=((create||close)&&!platform?1:0)||JsonSerializer.Serialize(before.Audits)!=JsonSerializer.Serialize(after.Audits.Where(x=>audits.All(y=>y.Id!=x.Id))))throw new InvalidOperationException("Academic period audit boundary failed.");
            if(audits.Length==1&&(audits[0].ActorUserId!=actor||audits[0].AcademyId!=academy||audits[0].Action!=(close?"PATCH":"POST")+" AcademicPeriods"||!audits[0].MetadataJson!.Contains(Route(academy,year,close),StringComparison.Ordinal)))throw new InvalidOperationException("Period actor/route audit wrong.");
        }
        async Task Check(string label,Guid a,Guid year,HttpStatusCode expected,bool close=false,string name=" Term ",int first=0,int last=365,string? token=null,bool anonymous=false,bool list=false,bool platform=false)
        {
            await Task.Delay(650);var before=await State();using var response=await Send(a,year,close,name,first,last,token,anonymous,list);RequireFinanceStatus(response,expected,label);
            var after=await State();var success=expected==HttpStatusCode.OK&&!list;AssertDelta(before,after,year,success&&!close,success&&close,platform,name,first,last);
            if(success&&!close){using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var r=json.RootElement;var saved=after.Terms.Single(x=>x.Id==r.GetProperty("id").GetGuid());
                if(r.GetProperty("academicYearId").GetGuid()!=year||r.GetProperty("name").GetString()!=saved.Name||r.GetProperty("startDate").GetString()!=saved.StartDate.ToString("yyyy-MM-dd")||r.GetProperty("endDate").GetString()!=saved.EndDate.ToString("yyyy-MM-dd")||r.GetProperty("isClosed").GetBoolean())throw new InvalidOperationException("Term response projection wrong.");}
            if(list&&expected==HttpStatusCode.OK){using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var years=json.RootElement.GetProperty("years").EnumerateArray().ToArray();var terms=json.RootElement.GetProperty("terms").EnumerateArray().ToArray();
                if(years.Length!=after.Years.Count(x=>x.AcademyId==a)||terms.Length!=after.Terms.Count(x=>x.AcademyId==a)||years.Any(x=>after.Years.Single(y=>y.Id==x.GetProperty("id").GetGuid()).AcademyId!=a)||terms.Any(x=>after.Terms.Single(y=>y.Id==x.GetProperty("id").GetGuid()).AcademyId!=a))throw new InvalidOperationException("Periods list leaked foreign rows.");}
            if(label.Contains("closed-parent",StringComparison.Ordinal)&&expected==HttpStatusCode.Conflict&&!(await response.Content.ReadAsStringAsync()).Contains("academic year is closed",StringComparison.Ordinal))throw new InvalidOperationException("Closed-year guidance absent.");
            Pass(label);
        }
        foreach(var (closed,current,first,last,name,status,label) in new(bool,bool,int,int,string,HttpStatusCode,string)[]{
            (false,false,0,365," Term ",HttpStatusCode.OK,"open-full-boundary-trim"),(false,true,0,365,"Term",HttpStatusCode.OK,"open-current-preserved"),
            (false,false,0,0,"First day",HttpStatusCode.OK,"first-day-boundary"),(false,false,365,365,"Last day",HttpStatusCode.OK,"last-day-boundary"),
            (true,false,0,365,"Term",HttpStatusCode.Conflict,"closed-parent"),(true,true,0,365,"Term",HttpStatusCode.Conflict,"closed-parent-current-legacy"),
            (false,true,0,365," ",HttpStatusCode.BadRequest,"blank-name"),(false,true,10,9,"Term",HttpStatusCode.BadRequest,"reversed-dates"),
            (false,true,-1,365,"Term",HttpStatusCode.BadRequest,"before-year"),(false,true,0,366,"Term",HttpStatusCode.BadRequest,"after-year")})
        {var y=await Seed(closed,current);await Check(label,academy,y.Id,status,name:name,first:first,last:last);}
        var own=await Seed();var f=await Seed(tenant:foreign);
        await Check("missing-parent400",academy,Guid.NewGuid(),HttpStatusCode.BadRequest);
        await Check("foreign-parent400",academy,f.Id,HttpStatusCode.BadRequest);
        await Check("foreign-route-owned-parent400",foreign,own.Id,HttpStatusCode.BadRequest,token:other);
        foreach(var (mode,status) in new[]{("empty",HttpStatusCode.OK),("all-closed",HttpStatusCode.OK),("open-term",HttpStatusCode.Conflict),("already-closed",HttpStatusCode.OK),("missing",HttpStatusCode.NotFound),("foreign",HttpStatusCode.NotFound)})
        {var y=await Seed(mode=="already-closed",mode!="already-closed",mode=="all-closed"?"closed":mode=="open-term"?"open":null);
            await Check("close-"+mode,mode=="foreign"?foreign:academy,mode=="missing"?Guid.NewGuid():y.Id,status,close:true,token:mode=="foreign"?other:admin);}
        var stale=await Seed(term:"closed");await Check("stale-prepare-close200",academy,stale.Id,HttpStatusCode.OK,close:true);await Check("stale-closed-parent409",academy,stale.Id,HttpStatusCode.Conflict);
        foreach(var close in new[]{false,true})
        {await Check("foreign-admin403-"+close,academy,own.Id,HttpStatusCode.Forbidden,close:close,token:other);await Check("teacher403-"+close,academy,own.Id,HttpStatusCode.Forbidden,close:close,token:teacher);await Check("anonymous401-"+close,academy,own.Id,HttpStatusCode.Unauthorized,close:close,anonymous:true);}
        await Check("own-list-no-write",academy,own.Id,HttpStatusCode.OK,list:true);await Check("foreign-list403",academy,own.Id,HttpStatusCode.Forbidden,token:other,list:true);
        var faultCreate=await Seed();var faultClose=await Seed(term:"closed");await SetAuditInsertDeniedAsync(manifest,true);
        try{await Check("term-audit-fault500-rollback",academy,faultCreate.Id,HttpStatusCode.InternalServerError);await Check("close-audit-fault500-rollback",academy,faultClose.Id,HttpStatusCode.InternalServerError,close:true);}
        finally{await SetAuditInsertDeniedAsync(manifest,false);}
        await Check("term-after-fault-lock-released",academy,faultCreate.Id,HttpStatusCode.OK);await Check("close-after-fault-flags-recovered",academy,faultClose.Id,HttpStatusCode.OK,close:true);

        foreach(var launchCloseFirst in new[]{false,true})
        {
            var y=await Seed(term:"closed");var before=await State();using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();await using var held=await db.Database.BeginTransactionAsync();
            await db.AcademicYears.FromSqlInterpolated($"SELECT * FROM [AcademicYears] WITH (UPDLOCK, HOLDLOCK) WHERE [Id]={y.Id} AND [AcademyId]={academy}").SingleAsync();
            var parentSession=await db.Database.SqlQueryRaw<int>("SELECT CAST(@@SPID AS int) AS [Value]").SingleAsync();
            var first=Send(academy,y.Id,close:launchCloseFirst);await RequireYearWaitersAsync(manifest,parentSession,1);
            var second=Send(academy,y.Id,close:!launchCloseFirst);await RequireYearWaitersAsync(manifest,parentSession,2);
            await held.CommitAsync();var responses=await Task.WhenAll(first,second);
            try
            {
                if(responses.Count(x=>x.StatusCode==HttpStatusCode.OK)!=1||responses.Count(x=>x.StatusCode==HttpStatusCode.Conflict)!=1)throw new InvalidOperationException("Close/create race did not produce one success plus one conflict.");
                var closeWon=responses[launchCloseFirst?0:1].StatusCode==HttpStatusCode.OK;var after=await State();AssertDelta(before,after,y.Id,!closeWon,closeWon,false);
                if(after.Years.Single(x=>x.Id==y.Id).IsClosed&&after.Terms.Any(x=>x.AcademicYearId==y.Id&&!x.IsClosed))throw new InvalidOperationException("Concurrent create invalidated year closure.");
                Console.WriteLine($"YEARCLOSURE concurrent launchCloseFirst={launchCloseFirst} winner={(closeWon?"close":"create")} invariant=PASS.");Pass("concurrent-close-create-"+launchCloseFirst+"-SQL-WAIT2");
            }
            finally{foreach(var response in responses)response.Dispose();}
            await Task.Delay(650);
        }
        var platformId=await CreateAccessActorAsync(factory,"qa-year-platform@example.invalid","PlatformOwner",foreign);
        using(var scope=factory.Services.CreateScope()){var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var u=await users.FindByIdAsync(platformId.ToString())??throw new InvalidOperationException("Platform fixture missing");u.IsPlatformOwner=true;if(!(await users.UpdateAsync(u)).Succeeded)throw new InvalidOperationException("Platform fixture flag failed");}
        var platform=await LoginAsync(client,"qa-year-platform@example.invalid","Synthetic!39Ab");
        var pCreate=await Seed();var pClose=await Seed(term:"closed");await Check("platform-term-owned-transaction",academy,pCreate.Id,HttpStatusCode.OK,token:platform,platform:true);
        await Check("platform-close-owned-transaction",academy,pClose.Id,HttpStatusCode.OK,close:true,token:platform,platform:true);await Check("platform-closed-parent409",academy,pClose.Id,HttpStatusCode.Conflict,token:platform,platform:true);
        if(count!=38)throw new InvalidOperationException("Year closure case count changed: "+count);
        Console.WriteLine("YEARCLOSURE REGRESSION PASS:38 cases; real Identity/HTTP/SQL, parent closure/stale boundaries, full captured no-write/flags, deterministic blocked competing requests, audit rollback and platform fallback; browser/device/all-linked NOT RUN.");
    }

    private static async Task RequireYearWaitersAsync(QaRunManifest manifest,int parentSession,int expected)
    {
        await using var sql=new SqlConnection(Connection(manifest.SqlServer,manifest.Database,"sa",Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD")!));await sql.OpenAsync();
        for(var attempt=0;attempt<50;attempt++)
        {
            // SQL queues later U-lock requests behind earlier waiting requests;
            // follow the blocking chain, not only direct blockers of the owner.
            await using var command=new SqlCommand("WITH waiters AS (SELECT session_id FROM sys.dm_exec_requests WHERE database_id=DB_ID() AND blocking_session_id=@parent AND wait_type LIKE 'LCK_M_%' UNION ALL SELECT r.session_id FROM sys.dm_exec_requests r JOIN waiters w ON r.blocking_session_id=w.session_id WHERE r.database_id=DB_ID() AND r.wait_type LIKE 'LCK_M_%') SELECT COUNT(DISTINCT session_id) FROM waiters OPTION (MAXRECURSION 16)",sql);command.Parameters.AddWithValue("@parent",parentSession);
            if((int)(await command.ExecuteScalarAsync())! == expected){Console.WriteLine($"YEARCLOSURE SQL parent-lock WAIT={expected} observed before release.");return;}await Task.Delay(100);
        }
        throw new InvalidOperationException("Requests did not wait on the held SQL parent; concurrency not proved.");
    }
}
