using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyAssignedTeacherAttendanceAsync(QaApiFactory factory, QaRunManifest manifest,
        Guid academy, Guid foreign, Guid batch, Guid student)
    {
        const string teacherEmail = "qa-teacher-a@example.invalid";
        const string pentaAudit = "PentaTeacherAttendanceMarked";
        var flags = new Dictionary<string, string?> { ["Penta:Enabled"] = "true", ["Penta:TeacherAttendanceEnabled"] = "true" };
        using var host = new QaApiFactory(manifest, flags);
        using var teacherClient = await SignIn(host, teacherEmail);
        using var manual = await SignIn(factory, teacherEmail);
        using var admin = await SignIn(host, "qa-admin-a@example.invalid");
        using var anonymous = host.CreateClient();

        var teacher = await WithDb(db => db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync());
        var other = new Teacher { AcademyId = academy, FirstName = "Synthetic", LastName = "Substitute" };
        var enrolled = new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "Second roster learner" };
        var outsider = new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "Not on roster" };
        var foreignStudent = await WithDb(db => db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync());
        var extra = new Enrollment { AcademyId = academy, BatchId = batch, StudentId = enrolled.Id };
        var session = NewSession(academy, teacher);
        var differentSession = NewSession(academy, other.Id);
        var foreignSession = NewSession(foreign, teacher);
        await WithDb(db => { db.AddRange(other, enrolled, outsider, extra, session, differentSession, foreignSession); return db.SaveChangesAsync(); });

        var count = 0;
        var route = $"/api/teacher/sessions/{session.Id}/attendance";
        string Draft(Guid? classId = null) => $"/api/teacher/penta/attendance/sessions/{classId ?? session.Id}/drafts/{Guid.NewGuid()}";

        static ClassSession NewSession(Guid tenant, Guid assignedTeacher)
        {
            var start = DateTime.UtcNow.Date.AddHours(-2);
            return new() { AcademyId = tenant, TeacherId = assignedTeacher, StartUtc = start, EndUtc = start.AddHours(1) };
        }

        // Session assignments are explicit, including where the batch default differs.
        await WithDb(db => db.ClassSessions.Where(x => x.Id == session.Id || x.Id == differentSession.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.BatchId, batch)));

        static async Task<HttpClient> SignIn(QaApiFactory owner, string email)
        {
            var client = owner.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, "Synthetic!39Ab"));
            return client;
        }
        async Task<T> WithDb<T>(Func<AcademyDeskDbContext, Task<T>> operation)
        {
            using var scope = factory.Services.CreateScope();
            return await operation(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }
        Task<string> State(bool full = true) => WithDb(async db => JsonSerializer.Serialize(new
        {
            attendance = await db.AttendanceRecords.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            notices = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            audits = full ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null,
            drafts = full ? await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null,
            anchors = full ? await db.PentaMiniSessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null
        }));
        void Pass(string name) { count++; Console.WriteLine($"ATTENDANCENOTES TEACHER CASE {name} PASS"); }
        async Task Refuse(Func<Task<HttpResponseMessage>> send, HttpStatusCode status, string label)
        {
            var before = await State();
            using var response = await send();
            RequireFinanceStatus(response, status, label);
            if (before != await State()) throw new InvalidOperationException(label + " modified the captured records.");
            Pass(label);
        }
        Task<HttpResponseMessage> Single(HttpClient client, Guid learner, string status = "Present", string? notes = null, Guid? id = null) =>
            client.PostAsJsonAsync(id.HasValue ? $"/api/teacher/sessions/{id}/attendance" : route, new TeacherMarkAttendanceRequest(learner, status, notes));
        Task<HttpResponseMessage> Bulk(params TeacherBulkAttendanceItem[] rows) =>
            manual.PostAsJsonAsync(route + "/bulk", new TeacherBulkAttendanceRequest(rows));
        Task<HttpResponseMessage> Prepare(HttpClient client, string draft, Guid learner, string status = "Present", string? notes = null, bool injected = false)
        {
            var fields = new Dictionary<string, object>
            {
                ["StudentId"] = new { kind = "reference", value = learner.ToString("D") },
                ["Status"] = new { kind = "text", value = status }
            };
            if (notes is not null) fields["Notes"] = new { kind = "text", value = notes };
            if (injected) fields["TeacherId"] = new { kind = "reference", value = teacher.ToString("D") };
            return client.PutAsJsonAsync(draft, new { payload = new { tool_key = "AcademyDesk.TeacherAttendance.Mark", tool_revision = 1, fields } },
                new JsonSerializerOptions());
        }
        static Task<HttpResponseMessage> Confirm(HttpClient client, string draft, PentaTeacherAttendanceDraft view,
            string intent = "ConfirmTeacherAttendanceAndQueueInApp", string? hash = null) =>
            client.PostAsJsonAsync(draft + "/confirm", new { payloadHash = hash ?? view.PayloadHash, intent });
        static async Task<PentaTeacherAttendanceDraft> Read(Task<HttpResponseMessage> send, HttpStatusCode status, string label)
        {
            using var response = await send;
            RequireFinanceStatus(response, status, label);
            if (response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Attendance draft must be no-store.");
            return (await response.Content.ReadFromJsonAsync<PentaTeacherAttendanceDraft>())!;
        }

        await Refuse(() => Single(anonymous, student), HttpStatusCode.Unauthorized, "Manual anonymous");
        await Refuse(() => Single(admin, student), HttpStatusCode.Forbidden, "Manual admin cannot use teacher route");
        await Refuse(() => Single(manual, student, id: differentSession.Id), HttpStatusCode.Forbidden, "Manual unassigned session");
        await Refuse(() => Single(manual, foreignStudent), HttpStatusCode.NotFound, "Manual foreign learner");
        await Refuse(() => Single(manual, outsider.Id), HttpStatusCode.NotFound, "Manual learner not enrolled");
        await Refuse(() => Single(manual, student, "Late"), HttpStatusCode.BadRequest, "Manual invalid status");
        await Refuse(() => Single(manual, student, notes: new string('x', 501)), HttpStatusCode.BadRequest, "Manual note limit");
        await Refuse(() => Bulk(new(student, "Present", null), new(student, "Absent", null)), HttpStatusCode.BadRequest, "Manual duplicate bulk rows");
        await Refuse(() => Bulk(new(student, "Present", null), new(outsider.Id, "Absent", null)), HttpStatusCode.BadRequest, "Manual bulk rejects entire mixed roster");

        var initialNotices = await WithDb(db => db.Notifications.CountAsync());
        using (var reply = await Single(manual, student, "Present", " Original note "))
        {
            RequireFinanceStatus(reply, HttpStatusCode.OK, "Manual save");
            var value = await reply.Content.ReadFromJsonAsync<TeacherAttendanceSummary>();
            if (value != new TeacherAttendanceSummary(student, "Present", "Original note"))
                throw new InvalidOperationException("Manual receipt differs.");
        }
        using (var reply = await Bulk(new(student, "Absent", null), new(enrolled.Id, "Present", " Second learner ")))
            RequireFinanceStatus(reply, HttpStatusCode.OK, "Manual bulk save");
        await WithDb(async db =>
        {
            var saved = await db.AttendanceRecords.Where(x => x.ClassSessionId == session.Id).ToDictionaryAsync(x => x.StudentId);
            if (saved.Count != 2 || saved[student].Status != "Absent" || saved[student].Notes is not null ||
                saved[enrolled.Id].Status != "Present" || saved[enrolled.Id].Notes != "Second learner" ||
                await db.Notifications.CountAsync() != initialNotices + 3 ||
                await db.AuditLogs.CountAsync(x => x.EntityId == session.Id && x.Action == "TeacherAttendanceMarked") != 2)
                throw new InvalidOperationException("Manual bulk, single, notices or audits differ.");
            return true;
        });
        Pass("Manual single and bulk persist notes, notices and audits");

        var manualState = await State();
        using (var reply = await manual.GetAsync(route))
        {
            RequireFinanceStatus(reply, HttpStatusCode.OK, "Manual read");
            var rows = await reply.Content.ReadFromJsonAsync<TeacherAttendanceSummary[]>();
            if (rows?.Length != 2 || !rows.Contains(new TeacherAttendanceSummary(student, "Absent", null)) || manualState != await State())
                throw new InvalidOperationException("Manual read does not match committed attendance.");
        }
        Pass("Manual assigned attendance read");

        var manualFault = new EnrollmentSharedAuditFault("TeacherAttendanceMarked");
        using (var faultyHost = new QaApiFactory(manifest, isolatedDomainInterceptor: manualFault))
        using (var faulty = await SignIn(faultyHost, teacherEmail))
            await Refuse(() => Single(faulty, student, "Present"), HttpStatusCode.InternalServerError, "Manual required audit rollback");
        if (manualFault.Hits != 1) throw new InvalidOperationException("Manual audit fault was not reached.");

        await Refuse(() => Prepare(manual, Draft(), student), HttpStatusCode.NotFound, "PENTA default disabled");
        await Refuse(() => Prepare(anonymous, Draft(), student), HttpStatusCode.Unauthorized, "PENTA anonymous");
        await Refuse(() => Prepare(admin, Draft(), student), HttpStatusCode.Forbidden, "PENTA admin cannot use teacher route");
        await Refuse(() => Prepare(teacherClient, Draft(differentSession.Id), student), HttpStatusCode.Forbidden, "PENTA unassigned session");
        await Refuse(() => Prepare(teacherClient, Draft(foreignSession.Id), student), HttpStatusCode.Forbidden, "PENTA foreign class");
        await Refuse(() => Prepare(teacherClient, Draft(), foreignStudent), HttpStatusCode.NotFound, "PENTA foreign learner");
        await Refuse(() => Prepare(teacherClient, Draft(), student, injected: true), HttpStatusCode.BadRequest, "PENTA rejects supplied authority field");
        await Refuse(() => Prepare(teacherClient, Draft(), student, "Late"), HttpStatusCode.BadRequest, "PENTA invalid status");
        await Refuse(() => Prepare(teacherClient, Draft(), student, notes: new string('x', 501)), HttpStatusCode.BadRequest, "PENTA note limit");

        var operation = Draft();
        var before = await State(false);
        var draft = await Read(Prepare(teacherClient, operation, student, notes: "  Approved note  "), HttpStatusCode.Created, "preview");
        if (draft.ClassSessionId != session.Id || draft.BatchId != batch || draft.Status != "Prepared" || draft.Receipt is not null ||
            draft.Change != new TeacherAttendanceChange(student, "Present", "Approved note", "Your attendance for the class has been marked Present.") ||
            before != await State(false))
            throw new InvalidOperationException("Preview differs from the review or writes business rows.");
        Pass("PENTA exact preview with no attendance or notice change");
        await Refuse(() => Confirm(teacherClient, operation, draft, "yes"), HttpStatusCode.BadRequest, "PENTA conversation yes cannot confirm");
        await Refuse(() => Confirm(teacherClient, operation, draft, hash: new string('0', 64)), HttpStatusCode.Conflict, "PENTA wrong hash");
        await Refuse(() => Prepare(teacherClient, operation, student, "Absent"), HttpStatusCode.Conflict, "PENTA same key different payload");

        var fault = new EnrollmentSharedAuditFault(pentaAudit);
        using (var faultyHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault))
        using (var faulty = await SignIn(faultyHost, teacherEmail))
            await Refuse(() => Confirm(faulty, operation, draft), HttpStatusCode.ServiceUnavailable, "PENTA required audit rollback");
        if (fault.Hits != 1) throw new InvalidOperationException("PENTA audit fault was not reached.");

        var notices = await WithDb(db => db.Notifications.CountAsync());
        var pair = await Task.WhenAll(Confirm(teacherClient, operation, draft), Confirm(teacherClient, operation, draft));
        using (pair[0]) using (pair[1])
        {
            if (!pair.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 200, 201 }))
                throw new InvalidOperationException("Expected one attendance commit and one replay.");
            var saved = (await pair[0].Content.ReadFromJsonAsync<PentaTeacherAttendanceDraft>())!;
            if (saved.Status != "Succeeded" || saved.Receipt is not { InAppQueued: 1 } receipt ||
                receipt != (await pair[1].Content.ReadFromJsonAsync<PentaTeacherAttendanceDraft>())?.Receipt)
                throw new InvalidOperationException("Attendance receipts differ.");
            await WithDb(async db =>
            {
                var row = await db.AttendanceRecords.SingleAsync(x => x.ClassSessionId == session.Id && x.StudentId == student);
                if (row.Id != receipt.AttendanceId || row.Notes != "Approved note" || row.Status != "Present" ||
                    await db.Notifications.CountAsync() != notices + 1 ||
                    !await db.Notifications.AnyAsync(x => x.AcademyId == academy && x.RecipientId == student && x.RecipientType == "Student" &&
                        x.Message == draft.Change.InAppMessage && x.Channel == "InApp" && x.Status == "Queued" && x.SentAtUtc == null) ||
                    !await db.AuditLogs.AnyAsync(x => x.Id == receipt.AuditId && x.AcademyId == academy && x.EntityId == session.Id && x.Action == pentaAudit))
                    throw new InvalidOperationException("SQL attendance, one queued notice or attributed audit differs.");
                return true;
            });
            var after = await State();
            if ((await Read(teacherClient.GetAsync(operation), HttpStatusCode.OK, "read receipt")).Receipt != receipt || after != await State())
                throw new InvalidOperationException("Readback changed the saved outcome.");
        }
        Pass("PENTA simultaneous confirmation commits once with one notice and durable readback");

        var staleRoute = Draft();
        var stale = await Read(Prepare(teacherClient, staleRoute, student, "Absent"), HttpStatusCode.Created, "preview before manual correction");
        using (var reply = await Single(manual, student, notes: "Manual correction"))
            RequireFinanceStatus(reply, HttpStatusCode.OK, "manual correction");
        await Refuse(() => Confirm(teacherClient, staleRoute, stale), HttpStatusCode.Conflict, "PENTA later Manual edit makes draft stale");

        var rosterRoute = Draft();
        var roster = await Read(Prepare(teacherClient, rosterRoute, enrolled.Id, "Absent"), HttpStatusCode.Created, "preview before withdrawal");
        await WithDb(db => db.Enrollments.Where(x => x.Id == extra.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.Status, "Withdrawn")));
        await Refuse(() => Confirm(teacherClient, rosterRoute, roster), HttpStatusCode.Conflict, "PENTA roster withdrawal stops confirmation");

        var transferRoute = Draft();
        var transfer = await Read(Prepare(teacherClient, transferRoute, student, "Absent"), HttpStatusCode.Created, "preview before reassignment");
        await WithDb(db => db.ClassSessions.Where(x => x.Id == session.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.TeacherId, other.Id)));
        await Refuse(() => Confirm(teacherClient, transferRoute, transfer), HttpStatusCode.Forbidden, "PENTA assignment removal stops confirmation");
        await Refuse(() => teacherClient.GetAsync(operation), HttpStatusCode.Forbidden, "PENTA assignment removal stops receipt access");
        await WithDb(db => db.ClassSessions.Where(x => x.Id == session.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.TeacherId, teacher)));
        await WithDb(db => db.Teachers.Where(x => x.Id == teacher).ExecuteUpdateAsync(set => set.SetProperty(x => x.IsActive, false)));
        await Refuse(() => Single(manual, student), HttpStatusCode.Forbidden, "Manual inactive teacher");
        await Refuse(() => manual.GetAsync(route), HttpStatusCode.Forbidden, "Manual inactive teacher cannot read attendance");
        await Refuse(() => Prepare(teacherClient, Draft(), student), HttpStatusCode.Forbidden, "PENTA inactive teacher");

        if (count != 34) throw new InvalidOperationException("Unexpected assigned teacher check count: " + count);
        Console.WriteLine($"ATTENDANCENOTES TEACHER SUMMARY {count} native HTTP/SQL checks PASS.");
    }
}
