using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Explicit synthetic interpreter exercises real HTTP/Identity/SQL, not model
    // semantics. Production DI never registers this fixture or grants tools to it.
    private sealed class QueryQaPlanner : IPentaQueryPlanner {
        internal int Calls;
        internal PentaQueryPlan? Plan;
        internal TaskCompletionSource<bool>? Entered, Release;
        public Task<bool> ReadyAsync(CancellationToken token) => Task.FromResult(Plan is not null);
        public async Task<PentaQueryPlannerOutcome> PlanAsync(JsonElement request, CancellationToken token) {
            Calls++; Entered?.TrySetResult(true);
            if (Release is { } release) await release.Task.WaitAsync(token);
            PentaRequire(request.GetProperty("contract").GetString() == PentaQueryContract.Version &&
                request.GetProperty("tools").GetArrayLength() == 6, "Query host catalog was not supplied.");
            return new(Plan, Plan is null ? "Unavailable" : null);
        }
    }
    private static async Task VerifyPentaQueriesAsync(QaRunManifest manifest, Guid academy, Guid foreignAcademy,
        string otherActorToken, string foreignToken, string teacherToken)
    {
        var planner = new QueryQaPlanner(); var fault = new TaskAuditFailure();
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> {
            ["Penta:Enabled"] = "true", ["Penta:Queries:Enabled"] = "true" }, isolatedQueryPlanner: planner, isolatedDomainInterceptor: fault);
        using var client = factory.CreateClient();
        PentaRequire(factory.PreflightPassed, "Query fixture missed owned SQL guard.");
        var email = $"query-owner-{Guid.NewGuid():N}@example.invalid";
        var password = "Qa!Query7" + Guid.NewGuid().ToString("N");
        Guid actorId, teacherId, studentId, batchId;
        await using (var scope = factory.Services.CreateAsyncScope()) {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var owner = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "Synthetic query owner", AcademyId = academy, IsActive = true };
            PentaRequire((await users.CreateAsync(owner, password)).Succeeded && (await users.AddToRoleAsync(owner, "AcademyAdmin")).Succeeded, "Query owner setup failed."); actorId = owner.Id;
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var course = new ProgramCourse { AcademyId = academy, Name = "Synthetic query course" }; db.Courses.Add(course);
            var teacher = new Teacher { AcademyId = academy, FirstName = "Synthetic Query", LastName = "Teacher", EmployeeCode = "QA-QUERY-T", Specialties = "Piano", Email = "private-query@example.invalid" };
            var student = new Student { AcademyId = academy, FirstName = "Synthetic Query", LastName = "Student", StudentNumber = "QA-QUERY-S" };
            var batch = new Batch { AcademyId = academy, CourseId = course.Id, Name = "Synthetic Query Batch", TeacherId = teacher.Id };
            db.Teachers.AddRange(teacher, new Teacher { AcademyId = foreignAcademy, FirstName = "Synthetic Query", LastName = "Teacher", Specialties = "Piano" });
            db.Students.Add(student); db.Batches.Add(batch); await db.SaveChangesAsync();
            teacherId = teacher.Id; studentId = student.Id; batchId = batch.Id;
        }
        client.DefaultRequestHeaders.Authorization = new("Bearer", await LoginAsync(client, email, password));
        var root = $"/api/academies/{academy:D}/penta/queries";
        var checks = 0;
        void Check(bool passed, string message) { PentaRequire(passed, message); checks++; }
        async Task<MiniSessionView> Create() {
            using var response = await client.PostAsync(root + "/conversations", null);
            RequireFinanceStatus(response, HttpStatusCode.Created, "query conversation create");
            return (await response.Content.ReadFromJsonAsync<MiniSessionView>())!;
        }
        string Turns(MiniSessionView s) => root + $"/conversations/{s.ConversationId:D}/turns";
        async Task<PentaQueryTurnReceipt> Turn(MiniSessionView s, long version, string text, string entity, string? kind = "search", int? position = null) {
            planner.Plan = new(PentaQueryContract.Version, kind!, entity, kind == "search" ? new() { ["name"] = "Synthetic Query" } : [], position);
            using var response = await client.PostAsJsonAsync(Turns(s), new PentaTaskTurnInput(Guid.NewGuid(), version, text));
            RequireFinanceStatus(response, HttpStatusCode.Created, "query turn");
            return (await response.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>())!;
        }
        async Task<string> Snapshot() {
            await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { students = await db.Students.CountAsync(), teachers = await db.Teachers.CountAsync(), batches = await db.Batches.CountAsync(),
                payments = await db.Payments.CountAsync(), total = await db.Payments.SumAsync(x => x.Amount) });
        }
        async Task<(long Version, Guid? Pending, string? Receipt)> Stored(MiniSessionView s, Guid request) {
            await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = await db.PentaMiniSessions.SingleAsync(x => x.Id == s.ConversationId);
            var turn = await db.PentaMiniTurns.SingleAsync(x => x.SessionId == s.ConversationId && x.RequestId == request);
            return (row.Version, row.PendingRequestId, turn.ProtectedReceipt);
        }
        var before = await Snapshot();
        var session = await Create();
        var teachers = await Turn(session, 0, "Show teachers", "teacher");
        Check(teachers.Result?.Entity == "teacher" && teachers.Result.Records.Length == 1 && teachers.Result.Records[0].Id == teacherId &&
            !JsonSerializer.Serialize(teachers).Contains("private-query"), "Teacher query used wrong source, leaked private fields or crossed tenant.");
        var selected = await Turn(session, 1, "Open result 1", "teacher", "open", 1);
        Check(selected.Result?.Records.Single().Id == teacherId, "Teacher ordinal did not bind to current host IDs.");
        var students = await Turn(session, 2, "I said students", "student");
        Check(students.Result?.Entity == "student" && students.Result.Records.Single().Id == studentId, "Domain correction retained teacher data.");
        var batches = await Turn(session, 3, "Find batch Synthetic Query", "batch");
        Check(batches.Result?.Entity == "batch" && batches.Result.Records.Single().Id == batchId, "Batch query used wrong entity.");
        var calls = planner.Calls;
        using (var get = await client.GetAsync(Turns(session) + $"/{batches.RequestId:D}")) {
            RequireFinanceStatus(get, HttpStatusCode.OK, "query GET recovery");
            var value = await get.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>();
            Check(value?.RequestId == batches.RequestId && planner.Calls == calls, "Recovery redispatched inference or returned wrong receipt.");
        }
        using (var history = await client.GetAsync(root + $"/conversations/{session.ConversationId:D}")) {
            RequireFinanceStatus(history, HttpStatusCode.OK, "query protected history");
            var value = (await history.Content.ReadFromJsonAsync<PentaQueryHistory>())!;
            Check(value.Turns.Select(x => x.Version).SequenceEqual(new long[] { 1, 2, 3, 4 }) && planner.Calls == calls, "Query history lost chronology or called model.");
        }
        using var visitor = factory.CreateClient();
        foreach (var bearer in new[] { otherActorToken, foreignToken, teacherToken }) {
            visitor.DefaultRequestHeaders.Authorization = new("Bearer", bearer);
            using var denied = await visitor.GetAsync(Turns(session) + $"/{batches.RequestId:D}");
            Check(denied.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound, "Query receipt leaked to another actor/role/tenant.");
        }
        var wrong = await Turn(session, 4, "I said teachers", "student");
        Check(wrong.Kind == "ERROR" && wrong.Result is null, "Wrong-domain model proposal became a successful student read.");
        var compound = await Turn(session, 5, "Create student and batch", "student");
        Check(compound.Kind == "ERROR" && compound.Result is null, "Unsupported compound write became a successful directory read.");
        planner.Plan = null;
        using (var unavailable = await client.PostAsJsonAsync(Turns(session), new PentaTaskTurnInput(Guid.NewGuid(), 6, "Show teachers"))) {
            RequireFinanceStatus(unavailable, HttpStatusCode.Created, "query provider outage receipt");
            Check((await unavailable.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>())?.Kind == "ERROR", "Outage became false success.");
        }
        // Claims must commit before generation; pending concurrency/recovery may
        // never dispatch another planner call or return incomplete private facts.
        var pending = await Create(); planner.Plan = new(PentaQueryContract.Version, "search", "teacher", [], null);
        planner.Entered = new(TaskCreationOptions.RunContinuationsAsynchronously); planner.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var input = new PentaTaskTurnInput(Guid.NewGuid(), 0, "Show teachers");
        var posting = client.PostAsJsonAsync(Turns(pending), input);
        try {
            await planner.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10)); calls = planner.Calls;
            using (var duplicate = await client.PostAsJsonAsync(Turns(pending), input)) RequireFinanceStatus(duplicate, HttpStatusCode.Conflict, "duplicate pending query");
            using (var recovering = await client.GetAsync(Turns(pending) + $"/{input.RequestId:D}")) RequireFinanceStatus(recovering, HttpStatusCode.Conflict, "pending query recovery");
            Check(planner.Calls == calls, "Pending query was dispatched twice.");
            fault.Enabled = true; planner.Release.TrySetResult(true);
            using var failed = await posting; RequireFinanceStatus(failed, HttpStatusCode.ServiceUnavailable, "query mandatory-audit failure");
        } finally { fault.Enabled = false; planner.Release?.TrySetResult(true); planner.Entered = null; planner.Release = null; }
        var rolled = await Stored(pending, input.RequestId);
        Check(rolled.Version == 0 && rolled.Pending == input.RequestId && rolled.Receipt is null, "Audit failure committed an unaudited result/context.");
        var revoked = await Create(); planner.Entered = new(TaskCreationOptions.RunContinuationsAsynchronously); planner.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var revokeInput = new PentaTaskTurnInput(Guid.NewGuid(), 0, "Show teachers");
        var revoking = client.PostAsJsonAsync(Turns(revoked), revokeInput);
        try {
            await planner.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await using (var scope = factory.Services.CreateAsyncScope()) {
                var identities = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                (await identities.Users.SingleAsync(x => x.Id == actorId)).IsActive = false; await identities.SaveChangesAsync();
            }
            planner.Release.TrySetResult(true); using var denied = await revoking;
            RequireFinanceStatus(denied, HttpStatusCode.Forbidden, "query current authority revocation after planning");
            var unchanged = await Stored(revoked, revokeInput.RequestId);
            Check(unchanged.Version == 0 && unchanged.Pending == revokeInput.RequestId && unchanged.Receipt is null, "Revoked actor obtained committed result.");
        } finally { planner.Release?.TrySetResult(true); }
        Check(await Snapshot() == before, "Read conversation changed domain records or financial rows/totals.");
        Console.WriteLine($"PENTA QUERY NATIVE {checks} SQL/Identity/API checks PASS; synthetic planner, real typed domain reads, no model acceptance.");
    }
}
