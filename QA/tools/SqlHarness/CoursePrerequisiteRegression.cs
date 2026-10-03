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
    private static async Task VerifyCoursePrerequisitesAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreign, actor, foreignActor; var count = 0;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            actor = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            foreignActor = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x => x.Email == "qa-admin-b@example.invalid").Select(x => x.Id).SingleAsync();
            foreach (var a in await db.Academies.Where(x => x.Id == academy || x.Id == foreign).ToListAsync())
                a.EnabledModulesJson = JsonSerializer.Serialize((JsonSerializer.Deserialize<string[]>(a.EnabledModulesJson) ?? []).Append("AcademicGovernance").Distinct());
            await db.SaveChangesAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var other = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        string Route(Guid a) => $"/api/academies/{a}/academic-governance/prerequisites";
        void Pass(string label) { count++; Console.WriteLine($"PREREQUISITE CASE {label} PASS."); }
        async Task<HttpResponseMessage> Send(Guid a, Guid course, Guid required, string? token = null, bool anonymous = false, string method = "POST")
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), Route(a));
            if (!anonymous) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? admin);
            if (method == "POST") request.Content = JsonContent.Create(new { courseId = course, requiredCourseId = required });
            return await client.SendAsync(request);
        }
        async Task<(string Stable, List<CoursePrerequisite> Edges, List<AuditLog> Audits)> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var governance = await GovernanceStateAsync(factory);
            return (JsonSerializer.Serialize(new { governance.Stable, governance.Tasks,
                Courses = await db.Courses.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Modules = await db.CourseModules.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                Schemes = await db.GradingSchemes.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }),
                await db.CoursePrerequisites.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), governance.Audits);
        }
        async Task<Guid[]> Seed(string graph = "", Guid? tenant = null)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var a = tenant ?? academy; var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
            db.Courses.AddRange(ids.Select((id, i) => new ProgramCourse { Id = id, AcademyId = a, Name = "Synthetic graph " + id.ToString("N") }));
            foreach (var pair in graph.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var e = pair.Split(':').Select(int.Parse).ToArray();
                db.CoursePrerequisites.Add(new CoursePrerequisite { AcademyId = a, CourseId = ids[e[0]], RequiredCourseId = ids[e[1]] });
            }
            await db.SaveChangesAsync(); return ids;
        }
        async Task Check(string label, Guid a, Guid course, Guid required, HttpStatusCode status, string? token = null,
            bool anonymous = false, string method = "POST", bool platform = false, Guid? expectedActor = null)
        {
            await Task.Delay(650); var before = await State();
            using var response = await Send(a, course, required, token, anonymous, method); RequireFinanceStatus(response, status, label);
            var after = await State(); if (before.Stable != after.Stable) throw new InvalidOperationException("Unrelated prerequisite state changed: " + label);
            var success = method == "POST" && status == HttpStatusCode.OK;
            var added = after.Edges.Where(x => before.Edges.All(y => y.Id != x.Id)).ToArray();
            if (JsonSerializer.Serialize(before.Edges) != JsonSerializer.Serialize(after.Edges.Where(x => added.All(y => y.Id != x.Id))) || added.Length != (success ? 1 : 0))
                throw new InvalidOperationException("Prerequisite full-row boundary failed: " + label);
            if (success && (added[0].AcademyId != a || added[0].CourseId != course || added[0].RequiredCourseId != required || !added[0].MustBeCompleted))
                throw new InvalidOperationException("Wrong persisted prerequisite.");
            var addedAudits = after.Audits.Where(x => before.Audits.All(y => y.Id != x.Id)).ToArray();
            if (JsonSerializer.Serialize(before.Audits) != JsonSerializer.Serialize(after.Audits.Where(x => addedAudits.All(y => y.Id != x.Id))) || addedAudits.Length != (success && !platform ? 1 : 0))
                throw new InvalidOperationException("Prerequisite audit boundary failed: " + label);
            if (addedAudits.Length == 1 && (addedAudits[0].ActorUserId != (expectedActor ?? actor) || addedAudits[0].AcademyId != a ||
                addedAudits[0].Action != "POST AcademicGovernance" || !addedAudits[0].MetadataJson!.Contains(Route(a), StringComparison.Ordinal)))
                throw new InvalidOperationException("Prerequisite actor/route audit mismatch.");
            if (method == "GET" && status == HttpStatusCode.OK)
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.GetArrayLength() != after.Edges.Count(x => x.AcademyId == a) ||
                    json.RootElement.EnumerateArray().Any(x => x.GetProperty("academyId").GetGuid() != a)) throw new InvalidOperationException("Prerequisite GET tenant projection mismatch.");
            }
            if (label.Contains("cycle", StringComparison.Ordinal) && status == HttpStatusCode.BadRequest &&
                !(await response.Content.ReadAsStringAsync()).Contains("circular course dependency", StringComparison.Ordinal)) throw new InvalidOperationException("Cycle message missing.");
            Pass(label);
        }
        foreach (var (graph, c, r, status, label) in new (string, int, int, HttpStatusCode, string)[] {
            ("",0,1,HttpStatusCode.OK,"first-edge"), ("0:1",1,0,HttpStatusCode.BadRequest,"two-cycle"),
            ("0:1,1:2",2,0,HttpStatusCode.BadRequest,"three-cycle"), ("0:1,1:2,2:3,3:4",4,0,HttpStatusCode.BadRequest,"long-cycle"),
            ("0:1,1:2",0,2,HttpStatusCode.OK,"transitive-DAG"), ("0:1,0:2,1:3",2,3,HttpStatusCode.OK,"diamond-DAG"),
            ("0:1,0:2,1:3,2:3",3,0,HttpStatusCode.BadRequest,"diamond-cycle"), ("0:1",2,3,HttpStatusCode.OK,"disconnected-DAG"),
            ("0:1",0,1,HttpStatusCode.Conflict,"duplicate"), ("",0,0,HttpStatusCode.BadRequest,"self"),
            ("0:1,1:0",2,0,HttpStatusCode.OK,"legacy-cycle-terminates"), ("0:1,1:0,1:2",2,0,HttpStatusCode.BadRequest,"legacy-new-cycle"),
            ("0:1,1:0",2,3,HttpStatusCode.OK,"legacy-disconnected") })
        {
            var ids = await Seed(graph);
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                db.CoursePrerequisites.Add(new CoursePrerequisite { AcademyId = foreign, CourseId = ids[r], RequiredCourseId = ids[c] }); await db.SaveChangesAsync(); }
            await Check(label, academy, ids[c], ids[r], status);
        }
        var own = await Seed(); var f = await Seed(tenant: foreign);
        foreach (var (label, a, c, r) in new[] {
            ("missing-course",academy,Guid.NewGuid(),own[1]), ("missing-required",academy,own[0],Guid.NewGuid()),
            ("foreign-course",academy,f[0],own[1]), ("foreign-required",academy,own[0],f[1]), ("foreign-route-owned-ids",foreign,own[0],own[1]) })
            await Check(label,a,c,r,HttpStatusCode.BadRequest, a == foreign ? other : admin);
        await Check("foreign-admin403",academy,own[0],own[1],HttpStatusCode.Forbidden,other);
        await Check("teacher403",academy,own[0],own[1],HttpStatusCode.Forbidden,teacher);
        await Check("anonymous401",academy,own[0],own[1],HttpStatusCode.Unauthorized,anonymous:true);
        await Check("own-GET",academy,own[0],own[1],HttpStatusCode.OK,method:"GET");
        await Check("foreign-GET403",academy,own[0],own[1],HttpStatusCode.Forbidden,other,method:"GET");

        // Hold the exact application lock and require real SQL WAIT evidence,
        // not merely Task.WhenAll, before releasing concurrent HTTP requests.
        foreach (var mode in new[] { "reciprocal", "duplicate", "three-cycle" })
        {
            var ids = await Seed(mode == "three-cycle" ? "0:1" : ""); var before = await State();
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            await using var held = await db.Database.BeginTransactionAsync();
            var resource = $"AcademyDesk:CoursePrerequisites:{academy:D}";
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=0; IF @r<0 THROW 51000,'QA graph lock failed',1;");
            var requests = mode == "three-cycle" ? new[] { (ids[1],ids[2]),(ids[2],ids[0]) } :
                mode == "duplicate" ? new[] { (ids[0],ids[1]),(ids[0],ids[1]) } : new[] { (ids[0],ids[1]),(ids[1],ids[0]) };
            var pending = requests.Select(x => Send(academy,x.Item1,x.Item2)).ToArray();
            await RequireGraphWaitersAsync(manifest, 2);
            await held.CommitAsync(); var responses = await Task.WhenAll(pending);
            try
            {
                var expected = mode == "duplicate" ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest;
                if (responses.Count(x => x.StatusCode == HttpStatusCode.OK) != 1 || responses.Count(x => x.StatusCode == expected) != 1)
                    throw new InvalidOperationException("Concurrent graph statuses unexpected: " + mode);
                var after = await State(); var added = after.Edges.Where(x => before.Edges.All(y => y.Id != x.Id)).ToArray();
                var audits = after.Audits.Where(x => before.Audits.All(y => y.Id != x.Id)).ToArray();
                if (before.Stable != after.Stable || added.Length != 1 || audits.Length != 1 ||
                    JsonSerializer.Serialize(before.Edges) != JsonSerializer.Serialize(after.Edges.Where(x => x.Id != added[0].Id)) ||
                    JsonSerializer.Serialize(before.Audits) != JsonSerializer.Serialize(after.Audits.Where(x => x.Id != audits[0].Id))) throw new InvalidOperationException("Concurrent graph no-write/audit boundary failed.");
                var winner = Array.FindIndex(responses,x => x.StatusCode == HttpStatusCode.OK);
                if (added[0].CourseId != requests[winner].Item1 || added[0].RequiredCourseId != requests[winner].Item2 || added[0].AcademyId != academy ||
                    audits[0].ActorUserId != actor || audits[0].Action != "POST AcademicGovernance") throw new InvalidOperationException("Concurrent winner mismatch.");
                Pass("concurrent-" + mode + "-SQL-WAIT2-one-edge-one-audit");
            }
            finally { foreach (var response in responses) response.Dispose(); }
            await Task.Delay(650);
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); await using var held = await db.Database.BeginTransactionAsync();
            var resource = $"AcademyDesk:CoursePrerequisites:{academy:D}";
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=0; IF @r<0 THROW 51000,'QA graph lock failed',1;");
            // Different academy lock must be independent while A remains held.
            await Check("other-academy-lock-independent",foreign,f[0],f[1],HttpStatusCode.OK,other,expectedActor:foreignActor);
            await Check("lock-timeout503-no-write",academy,own[0],own[1],HttpStatusCode.ServiceUnavailable);
            await held.RollbackAsync();
        }
        // Fault only this run's runtime AuditLogs INSERT; reject/roll back edge.
        await SetAuditInsertDeniedAsync(manifest,true);
        try { await Check("audit-fault500-rollback",academy,own[0],own[1],HttpStatusCode.InternalServerError); }
        finally { await SetAuditInsertDeniedAsync(manifest,false); }
        await Check("post-fault-lock-released",academy,own[0],own[1],HttpStatusCode.OK);
        // Existing platform-owner bypass must still acquire an owned SQL transaction.
        var platformId = await CreateAccessActorAsync(factory,"qa-graph-platform@example.invalid","PlatformOwner",foreign);
        using (var scope = factory.Services.CreateScope()) { var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(platformId.ToString()) ?? throw new InvalidOperationException("Platform fixture missing");
            user.IsPlatformOwner = true; if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Platform flag failed"); }
        var platformToken = await LoginAsync(client,"qa-graph-platform@example.invalid","Synthetic!39Ab");
        var p = await Seed(); await Check("platform-own-transaction",academy,p[0],p[1],HttpStatusCode.OK,platformToken,platform:true);
        await Check("platform-cycle-rejected",academy,p[1],p[0],HttpStatusCode.BadRequest,platformToken,platform:true);
        if (count != 32) throw new InvalidOperationException("Prerequisite case count changed: " + count);
        Console.WriteLine("PREREQUISITE REGRESSION PASS:32 cases; real Identity/HTTP/SQL, graph rejection no-write, three deterministic concurrent pairs, scoped lock/timeout, audit rollback, platform bypass; browser/device/legacy repair NOT RUN.");
    }

    private static async Task RequireGraphWaitersAsync(QaRunManifest manifest, int expected)
    {
        await using var sql = new SqlConnection(Connection(manifest.SqlServer,manifest.Database,"sa",Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD")!));
        await sql.OpenAsync();
        for (var attempt=0; attempt<50; attempt++)
        {
            var waits = (int)(await ScalarAsync(sql,"SELECT COUNT(*) FROM sys.dm_tran_locks WHERE resource_type='APPLICATION' AND request_status='WAIT' AND resource_database_id=DB_ID()"))!;
            if (waits == expected) { Console.WriteLine($"PREREQUISITE SQL application-lock WAIT={waits} observed before release."); return; }
            await Task.Delay(100);
        }
        throw new InvalidOperationException("HTTP requests did not reach the held SQL graph lock; concurrency not proved.");
    }
}
