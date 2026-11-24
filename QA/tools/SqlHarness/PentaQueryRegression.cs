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
        internal int ExpectedTools = 10;
        internal PentaQueryPlan? Plan;
        internal TaskCompletionSource<bool>? Entered, Release;
        public Task<bool> ReadyAsync(CancellationToken token) => Task.FromResult(Plan is not null);
        public async Task<PentaQueryPlannerOutcome> PlanAsync(JsonElement request, CancellationToken token) {
            Calls++; Entered?.TrySetResult(true);
            if (Release is { } release) await release.Task.WaitAsync(token);
            PentaRequire(request.GetProperty("contract").GetString() == PentaQueryContract.Version &&
                request.GetProperty("tools").GetArrayLength() == ExpectedTools, "Query host catalog was not supplied.");
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
        Guid actorId, teacherId, studentId, batchId, calendarId, attendanceId;
        string originalModules, originalZone;
        await using (var scope = factory.Services.CreateAsyncScope()) {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var owner = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "Synthetic query owner", AcademyId = academy, IsActive = true };
            PentaRequire((await users.CreateAsync(owner, password)).Succeeded && (await users.AddToRoleAsync(owner, "AcademyAdmin")).Succeeded, "Query owner setup failed."); actorId = owner.Id;
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var tenant = await db.Academies.SingleAsync(x => x.Id == academy);
            originalModules = tenant.EnabledModulesJson; originalZone = tenant.TimeZone; tenant.EnabledModulesJson = "[]"; tenant.TimeZone = "Asia/Kolkata";
            var course = new ProgramCourse { AcademyId = academy, Name = "Synthetic query course" }; db.Courses.Add(course);
            var teacher = new Teacher { AcademyId = academy, FirstName = "Synthetic Query", LastName = "Teacher", EmployeeCode = "QA-QUERY-T", Specialties = "Piano", Email = "private-query@example.invalid" };
            var student = new Student { AcademyId = academy, FirstName = "Synthetic Query", LastName = "Student", StudentNumber = "QA-QUERY-S" };
            var batch = new Batch { AcademyId = academy, CourseId = course.Id, Name = "Synthetic Query Batch", TeacherId = teacher.Id };
            var foreignTeacher=new Teacher { AcademyId = foreignAcademy, FirstName = "Synthetic Query", LastName = "Teacher", Specialties = "Piano" };
            db.Teachers.AddRange(teacher, foreignTeacher);
            var foreignStudent = new Student { AcademyId = foreignAcademy, FirstName = "Synthetic Query", LastName = "Foreign Student" };
            var foreignCourse=new ProgramCourse { AcademyId=foreignAcademy,Name="Foreign Query Course" };
            var foreignBatch=new Batch { AcademyId=foreignAcademy,CourseId=foreignCourse.Id,Name="Synthetic Query Foreign" };
            db.Courses.Add(foreignCourse); db.Students.AddRange(student, foreignStudent); db.Batches.AddRange(batch,foreignBatch);
            var start = new DateTime(2026,11,23,18,30,0,DateTimeKind.Utc);
            var calendar = new ClassSession { AcademyId=academy,BatchId=batch.Id,TeacherId=teacher.Id,StartUtc=start,EndUtc=start.AddHours(1),DeliveryMode="Hybrid",RoomName="private-query-room" };
            calendarId=calendar.Id;
            var foreignCalendar=new ClassSession { AcademyId=foreignAcademy,BatchId=foreignBatch.Id,TeacherId=foreignTeacher.Id,StartUtc=start,EndUtc=start.AddHours(1) };
            db.ClassSessions.AddRange(calendar,
                foreignCalendar,
                new ClassSession { AcademyId=academy,BatchId=foreignBatch.Id,TeacherId=teacher.Id,StartUtc=start,EndUtc=start.AddHours(1) },
                new ClassSession { AcademyId=academy,BatchId=batch.Id,TeacherId=foreignTeacher.Id,StartUtc=start,EndUtc=start.AddHours(1) },
                new ClassSession { AcademyId=academy,BatchId=batch.Id,TeacherId=teacher.Id,StartUtc=start.AddDays(1),EndUtc=start.AddDays(1).AddHours(1) },
                new ClassSession { AcademyId=academy,BatchId=batch.Id,TeacherId=teacher.Id,StartUtc=start.AddMinutes(-1),EndUtc=start.AddMinutes(59) });
            var storedAttendance=new AttendanceRecord { AcademyId=academy,ClassSessionId=calendar.Id,StudentId=student.Id,Status="absent",MarkedAtUtc=start.AddDays(10),Notes="private-query-attendance" };
            attendanceId=storedAttendance.Id;
            db.AttendanceRecords.AddRange(storedAttendance,
                new AttendanceRecord { AcademyId=academy,ClassSessionId=calendar.Id,StudentId=foreignStudent.Id,Status="Absent" },
                new AttendanceRecord { AcademyId=academy,ClassSessionId=foreignCalendar.Id,StudentId=student.Id,Status="Absent" },
                new AttendanceRecord { AcademyId=foreignAcademy,ClassSessionId=foreignCalendar.Id,StudentId=foreignStudent.Id,Status="Absent" });
            // More than ten recorded marks, plus an active enrollee without a mark.
            // Missing marks must never be synthesized as Absent.
            for(var index=0;index<12;index++){
                var pupil=new Student { AcademyId=academy,FirstName="PageMark",LastName=index.ToString("D2") };db.Students.Add(pupil);
                db.Enrollments.Add(new Enrollment {AcademyId=academy,StudentId=pupil.Id,BatchId=batch.Id,Status="Active"});
                db.AttendanceRecords.Add(new AttendanceRecord { AcademyId=academy,ClassSessionId=calendar.Id,StudentId=pupil.Id,Status="Present" });
            }
            var unmarkedPupil=new Student {AcademyId=academy,FirstName="UnmarkedMark",LastName="Roster"};db.Students.Add(unmarkedPupil);
            db.Enrollments.Add(new Enrollment {AcademyId=academy,StudentId=unmarkedPupil.Id,BatchId=batch.Id,Status="Active"});
            for(var index=0;index<12;index++) db.ClassSessions.Add(new ClassSession { AcademyId=academy,BatchId=batch.Id,TeacherId=teacher.Id,
                StartUtc=start.AddDays(1).AddMinutes(index+1),EndUtc=start.AddDays(1).AddMinutes(index+61) });
            var invoice = new Invoice { AcademyId = academy, StudentId = student.Id, InvoiceNumber = "QA-Q-INR", TotalAmount = 1000m };
            db.Invoices.AddRange(invoice,
                new Invoice { AcademyId = academy, StudentId = student.Id, InvoiceNumber = "QA-Q-USD", TotalAmount = 0.01m, Currency = "USD" },
                new Invoice { AcademyId = academy, StudentId = student.Id, InvoiceNumber = "QA-Q-EUR", TotalAmount = 90071992547409.91m, Currency = "EUR" },
                new Invoice { AcademyId = foreignAcademy, StudentId = foreignStudent.Id, InvoiceNumber = "QA-Q-FOREIGN", TotalAmount = 999m, Currency = "GBP" });
            db.Payments.AddRange(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 600m, Status = "Reconciled" },
                new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 500m, Status = "Pending" });
            for (var index = 0; index < 12; index++) {
                var pageStudent = new Student { AcademyId = academy, FirstName = "PageFee", LastName = index.ToString("D2") };
                db.Students.Add(pageStudent);
                db.Invoices.Add(new Invoice { AcademyId = academy, StudentId = pageStudent.Id, InvoiceNumber = "QA-Q-PAGE-" + index, TotalAmount = 1m });
            }
            await db.SaveChangesAsync();
            teacherId = teacher.Id; studentId = student.Id; batchId = batch.Id;
        }
        try {
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
                sessions=await db.ClassSessions.OrderBy(x=>x.Id).Select(x=>new { x.Id,x.StartUtc,x.EndUtc,x.Status,x.TeacherId }).ToArrayAsync(),
                attendance=await db.AttendanceRecords.OrderBy(x=>x.Id).Select(x=>new { x.Id,x.ClassSessionId,x.StudentId,x.Status,x.MarkedAtUtc,x.Notes }).ToArrayAsync(),
                payments = await db.Payments.CountAsync(), total = await db.Payments.SumAsync(x => x.Amount),
                invoices = await db.Invoices.CountAsync(), billed = await db.Invoices.SumAsync(x => x.TotalAmount), adjusted = await db.Invoices.SumAsync(x => x.AdjustedAmount) });
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
        // Finance is a separate current-host permission, never implied by a
        // selected student or a model's choice. Preserve the typed directory.
        using (var health = await client.GetAsync(root + "/health")) {
            RequireFinanceStatus(health, HttpStatusCode.OK, "directory-only query catalog");
            var value = await health.Content.ReadFromJsonAsync<JsonElement>();
            Check(value.GetProperty("tools").GetArrayLength() == 10 && !value.GetProperty("tools").EnumerateArray().Any(x => x.GetProperty("entity").GetString() == "student-fees"), "Finance tool appeared without Finance module.");
        }
        await using (var scope = factory.Services.CreateAsyncScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = "[\"Finance\"]"; await db.SaveChangesAsync();
        }
        planner.ExpectedTools = 12;
        var moneySession = await Create();
        var money = await Turn(moneySession, 0, "Show outstanding student fees", "student-fees");
        var balances = money.Result?.Records.Single().Balances;
        Check(money.Result?.Entity == "student-fees" && money.Result.Records[0].Id == studentId && balances?.Length == 3 &&
            balances.Single(x => x.Currency == "INR").OutstandingExact == "400.00" &&
            balances.Single(x => x.Currency == "USD").OutstandingExact == "0.01" &&
            balances.Single(x => x.Currency == "EUR").OutstandingExact == "90071992547409.91" &&
            money.Result.BalanceScope == PentaQueryContract.BalanceScope, "Exact ledger balances were rounded, netted, recalculated or leaked across tenants.");
        var directory = await Turn(moneySession, 1, "Open student result 1", "student", "open", 1);
        Check(directory.Result?.Records.Single().Balances is null && directory.Result?.BalanceScope is null, "Financial fields leaked into ordinary directory view.");
        var followup = await Turn(moneySession, 2, "What does she owe?", "student-fees", "open", 1);
        Check(followup.Result?.Records.Single().Id == studentId && followup.Result.Records[0].Balances?.Length == 3, "Finance follow-up lost the selected host student reference.");
        calls = planner.Calls;
        using (var recovered = await client.GetAsync(Turns(moneySession) + $"/{followup.RequestId:D}")) {
            RequireFinanceStatus(recovered, HttpStatusCode.OK, "exact finance GET recovery");
            var value = await recovered.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>();
            Check(value?.Result?.Records[0].Balances?.Single(x => x.Currency == "USD").OutstandingExact == "0.01" && planner.Calls == calls, "Finance recovery lost exact strings or dispatched the model.");
        }
        var pageSession = await Create();
        planner.Plan = new(PentaQueryContract.Version, "search", "student-fees", new() { ["name"] = "PageFee" }, null);
        using (var paged = await client.PostAsJsonAsync(Turns(pageSession), new PentaTaskTurnInput(Guid.NewGuid(), 0, "Show outstanding fees for PageFee"))) {
            RequireFinanceStatus(paged, HttpStatusCode.Created, "bounded fee search");
            var value = await paged.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>();
            Check(value?.Result is { HasMore: true, Records.Length: 10 } && value.Message.Contains("first 10") && value.Message.Contains("More matches"), "Truncated fee search appeared complete.");
        }
        calls = planner.Calls;
        await using (var scope = factory.Services.CreateAsyncScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = "[]"; await db.SaveChangesAsync();
        }
        planner.ExpectedTools = 10;
        using (var denied = await client.GetAsync(Turns(moneySession) + $"/{followup.RequestId:D}")) {
            RequireFinanceStatus(denied, HttpStatusCode.Forbidden, "revoked finance receipt");
            Check(planner.Calls == calls, "Finance revocation invoked the model.");
        }
        var teacherAfterRevocation = await Create();
        var teacherRead = await Turn(teacherAfterRevocation, 0, "Show teachers", "teacher");
        Check(teacherRead.Result?.Entity == "teacher" && teacherRead.Result.Records.Single().Balances is null, "Revoking Finance broke or polluted unrelated teacher reads.");
        var calendarSession=await Create();
        planner.Plan=new(PentaQueryContract.Version,"search","session",new() { ["from"]="2026-11-24",["name"]="Synthetic Query",["teacher"]="Synthetic Query" },null);
        PentaQueryTurnReceipt calendarReceipt;
        using (var response=await client.PostAsJsonAsync(Turns(calendarSession),new PentaTaskTurnInput(Guid.NewGuid(),0,"Show teacher Synthetic Query's classes on 2026-11-24"))) {
            RequireFinanceStatus(response,HttpStatusCode.Created,"calendar protected read");
            calendarReceipt=(await response.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>())!;
            var row=calendarReceipt.Result?.Records.Single();
            Check(row?.Id==calendarId && row.Schedule?.StartUtc==new DateTime(2026,11,23,18,30,0,DateTimeKind.Utc) && row.Schedule.Teacher=="Synthetic Query Teacher" &&
                row.Schedule.DeliveryMode=="Hybrid" && row.Schedule.Status=="Scheduled", "Calendar boundary, relational teacher or delivery mode was wrong.");
            Check(calendarReceipt.Context.Filters["from"]=="2026-11-24" && calendarReceipt.Context.Filters["through"]=="2026-11-24" &&
                calendarReceipt.Result!.CalendarScope?.TimeZone=="Asia/Kolkata" && !JsonSerializer.Serialize(calendarReceipt).Contains("private-query"), "Calendar scope drifted or private room/contact leaked.");
        }
        calls=planner.Calls;
        using (var recovered=await client.GetAsync(Turns(calendarSession)+$"/{calendarReceipt.RequestId:D}")) {
            RequireFinanceStatus(recovered,HttpStatusCode.OK,"calendar GET-only recovery");
            Check((await recovered.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>())?.Result?.Records.Single().Id==calendarId && planner.Calls==calls,"Calendar recovery redispatched planner or changed source.");
        }
        var calendarOpen=await Turn(calendarSession,1,"Open result 1","session","open",1);
        Check(calendarOpen.Result?.Records.Single().Id==calendarId && calendarOpen.Context.Filters["from"]=="2026-11-24","Calendar ordinal lost current host IDs or pinned dates.");
        var calendarPage=await Create();
        planner.Plan=new(PentaQueryContract.Version,"search","session",new() { ["from"]="2026-11-25",["name"]="Synthetic Query" },null);
        using(var response=await client.PostAsJsonAsync(Turns(calendarPage),new PentaTaskTurnInput(Guid.NewGuid(),0,"Show classes on 2026-11-25"))) {
            RequireFinanceStatus(response,HttpStatusCode.Created,"bounded calendar page");
            var result=await response.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>();
            Check(result?.Result is { HasMore:true,Records.Length:10 } && result.Message.Contains("first 10") && result.Message.Contains("more exist"),"Truncated calendar appeared complete.");
        }
        var wrongCalendar=await Turn(calendarSession,2,"Show the teacher calendar","teacher");
        Check(wrongCalendar.Kind=="ERROR" && wrongCalendar.Result is null,"Calendar request became a teacher directory answer.");
        var invalidRangeSession=await Create();
        planner.Plan=new(PentaQueryContract.Version,"search","session",new() { ["from"]="2026-11-01",["through"]="2026-12-02" },null);
        using(var response=await client.PostAsJsonAsync(Turns(invalidRangeSession),new PentaTaskTurnInput(Guid.NewGuid(),0,"Show classes"))) {
            RequireFinanceStatus(response,HttpStatusCode.Created,"invalid calendar range receipt");
            Check((await response.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>())?.Kind=="ERROR","Unbounded calendar dates executed.");
        }
        var attendanceSession=await Create();
        planner.Plan=new(PentaQueryContract.Version,"search","attendance",new() { ["from"]="2026-11-24",["name"]="Synthetic Query",["status"]="Absent",["teacher"]="Synthetic Query" },null);
        PentaQueryTurnReceipt storedMark;
        using(var response=await client.PostAsJsonAsync(Turns(attendanceSession),new PentaTaskTurnInput(Guid.NewGuid(),0,"Show student attendance for teacher Synthetic Query's classes on 2026-11-24"))){
            RequireFinanceStatus(response,HttpStatusCode.Created,"protected recorded attendance read");storedMark=(await response.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>())!;
            var row=storedMark.Result?.Records.Single();
            Check(row?.Id==attendanceId && row.Attendance?.Status=="Absent" && row.Attendance.MarkedAtUtc==new DateTime(2026,12,3,18,30,0,DateTimeKind.Utc),"Attendance used mark date instead of class date, lost legacy case, or crossed tenant relations.");
            Check(storedMark.Result?.AttendanceScope==PentaQueryContract.AttendanceScope && storedMark.Result.CalendarScope?.From=="2026-11-24" && !JsonSerializer.Serialize(storedMark).Contains("private-query"),"Attendance leaked notes/contact or confused time/status semantics.");
        }
        calls=planner.Calls;
        using(var response=await client.GetAsync(Turns(attendanceSession)+$"/{storedMark.RequestId:D}")){
            RequireFinanceStatus(response,HttpStatusCode.OK,"recorded attendance GET recovery");
            Check((await response.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>())?.Result?.Records.Single().Id==attendanceId && planner.Calls==calls,"Attendance recovery redispatched interpretation or changed saved IDs.");
        }
        var openMark=await Turn(attendanceSession,1,"Open result 1","attendance","open",1);
        Check(openMark.Result?.Records.Single().Id==attendanceId,"Attendance ordinal did not keep its own stored mark reference.");
        var wrongMark=await Turn(attendanceSession,2,"Show teacher attendance","attendance");
        Check(wrongMark.Kind=="ERROR" && wrongMark.Result is null,"Teacher attendance became student marks.");
        var markPage=await Create();planner.Plan=new(PentaQueryContract.Version,"search","attendance",new() { ["from"]="2026-11-24",["name"]="PageMark" },null);
        using(var response=await client.PostAsJsonAsync(Turns(markPage),new PentaTaskTurnInput(Guid.NewGuid(),0,"Show recorded student attendance on 2026-11-24"))){
            RequireFinanceStatus(response,HttpStatusCode.Created,"bounded recorded attendance page");
            var value=await response.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>();
            Check(value?.Result is {HasMore:true,Records.Length:10} && value.Message.Contains("first 10") && value.Message.Contains("more exist"),"Truncated attendance appeared complete.");
        }
        var writeMark=await Turn(attendanceSession,3,"Mark attendance for students","attendance");
        Check(writeMark.Kind=="ERROR" && writeMark.Result is null,"Read conversation accepted attendance mutation.");
        var missingMark=await Create();planner.Plan=new(PentaQueryContract.Version,"search","attendance",new() { ["from"]="2026-11-24",["name"]="UnmarkedMark",["status"]="Absent" },null);
        using(var response=await client.PostAsJsonAsync(Turns(missingMark),new PentaTaskTurnInput(Guid.NewGuid(),0,"Show stored attendance on 2026-11-24"))){
            RequireFinanceStatus(response,HttpStatusCode.Created,"missing mark semantics");
            Check((await response.Content.ReadFromJsonAsync<PentaQueryTurnReceipt>())?.Result is {Records.Length:0},"Missing enrolled student's mark became an inferred absence.");
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
        } finally {
            fault.Enabled = false; planner.Release?.TrySetResult(true);
            await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var tenant=await db.Academies.SingleAsync(x => x.Id == academy); tenant.EnabledModulesJson = originalModules; tenant.TimeZone=originalZone; await db.SaveChangesAsync();
        }
    }
}
