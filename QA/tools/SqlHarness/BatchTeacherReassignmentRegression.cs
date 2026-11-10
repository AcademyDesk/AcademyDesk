using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyClassTeacherInheritanceMigrationAsync(AcademyDeskDbContext db)
    {
        // Program has already verified ownership of this generated QA database.
        const string prior = "20261030031403_AddCombinedPentaHostPersistence";
        const string current = "20261110112020_TrackClassSessionTeacherInheritance";
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(prior);
        var id = Guid.NewGuid();
        var academy = Guid.NewGuid();
        var batch = Guid.NewGuid();
        var teacher = Guid.NewGuid();
        var start = DateTime.UtcNow.Date.AddDays(1);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [ClassSessions] ([Id],[AcademyId],[BatchId],[TeacherId],[StartUtc],[EndUtc],[DeliveryMode],[RoomName],[Status],[CreatedAtUtc])
            VALUES ({id},{academy},{batch},{teacher},{start},{start.AddHours(1)},{"InPerson"},{"Legacy studio"},{"Scheduled"},SYSUTCDATETIME())
            """);
        await migrator.MigrateAsync(current);
        var legacy = await db.ClassSessions.AsNoTracking().SingleAsync(x => x.Id == id);
        if (legacy.TeacherId != teacher || legacy.BatchId != batch || legacy.AcademyId != academy ||
            legacy.StartUtc != start || legacy.EndUtc != start.AddHours(1) || legacy.RoomName != "Legacy studio" ||
            legacy.Status != "Scheduled" || legacy.FollowsBatchTeacher is not null)
            throw new InvalidOperationException("Migration changed an existing class or invented assignment provenance.");
        Console.WriteLine("BATCHPRESERVE MIGRATION PASS: legacy class survives the additive upgrade with unknown provenance.");

        await db.ClassSessions.Where(x => x.Id == id).ExecuteUpdateAsync(set => set.SetProperty(x => x.FollowsBatchTeacher, false));
        var blocked = false;
        try { await migrator.MigrateAsync(prior); }
        catch (SqlException error) when (error.Number == 51029) { blocked = true; }
        var saved = await db.ClassSessions.AsNoTracking().SingleAsync(x => x.Id == id);
        if (!blocked || !(await db.Database.GetAppliedMigrationsAsync()).Contains(current) ||
            saved.TeacherId != teacher || saved.FollowsBatchTeacher != false)
            throw new InvalidOperationException("Downgrade did not protect recorded assignment provenance.");
        await db.ClassSessions.Where(x => x.Id == id).ExecuteDeleteAsync();
        Console.WriteLine("BATCHPRESERVE MIGRATION PASS: downgrade refuses to discard recorded provenance.");
    }

    private static async Task VerifyBatchTeacherReassignmentAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy,
        Guid foreign, Guid course, Guid original, Guid replacement, Guid branch)
    {
        const string email = "qa-admin-a@example.invalid";
        using var admin = await SignIn(factory);
        var batches = $"/api/academies/{academy}/batches";
        var classes = $"/api/academies/{academy}/sessions";
        var count = 0;
        var start = DateTime.UtcNow.Date.AddDays(40).AddHours(4);
        var third = new Teacher { AcademyId = academy, FirstName = "Synthetic", LastName = "Third batch teacher" };
        var substitute = new Teacher { AcademyId = academy, FirstName = "Synthetic", LastName = "Explicit substitute" };
        var foreignTeacher = new Teacher { AcademyId = foreign, FirstName = "Synthetic", LastName = "Foreign instructor" };
        await WithDb(db => { db.AddRange(third, substitute, foreignTeacher); return db.SaveChangesAsync(); });

        static async Task<HttpClient> SignIn(QaApiFactory host)
        {
            var client = host.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, "Synthetic!39Ab"));
            return client;
        }
        async Task<T> WithDb<T>(Func<AcademyDeskDbContext, Task<T>> operation)
        {
            using var scope = factory.Services.CreateScope();
            return await operation(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }
        Task<string> State(bool audits = true) => WithDb(async db => JsonSerializer.Serialize(new
        {
            batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            classes = await db.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            attendance = await db.AttendanceRecords.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            audits = audits ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() : null
        }));
        Task<ClassSession> Get(Guid id) => WithDb(db => db.ClassSessions.AsNoTracking().SingleAsync(x => x.Id == id));
        Task<Batch> GetBatch(Guid id) => WithDb(db => db.Batches.AsNoTracking().SingleAsync(x => x.Id == id));
        Task<Batch> SeedBatch(string name, Guid? teacher) => WithDb(async db =>
        {
            var row = new Batch { AcademyId = academy, Name = "Reassignment " + name, CourseId = course, TeacherId = teacher, BranchId = branch };
            db.Batches.Add(row); await db.SaveChangesAsync(); return row;
        });
        Task<ClassSession> Seed(Batch batch, Guid? teacher, DateTime time, string status = "Scheduled", Guid? tenant = null) =>
            WithDb(async db =>
            {
                var row = new ClassSession { AcademyId = tenant ?? academy, BatchId = batch.Id, TeacherId = teacher,
                    StartUtc = time, EndUtc = time.AddHours(1), BranchId = branch, Status = status };
                db.ClassSessions.Add(row); await db.SaveChangesAsync(); return row;
            });
        Task<HttpResponseMessage> Reassign(HttpClient client, Batch batch, Guid? teacher, string? name = null, bool? active = null)
        {
            var body = BatchBody(batch);
            body["teacherId"] = teacher;
            if (name is not null) body["name"] = name;
            if (active.HasValue) body["isActive"] = active.Value;
            return client.PutAsJsonAsync($"{batches}/{batch.Id}", body);
        }
        Task<HttpResponseMessage> Create(Batch batch, DateTime time, Guid? teacher = null) =>
            admin.PostAsJsonAsync(classes, new CreateClassSessionRequest(batch.Id, teacher, null, time, time.AddHours(1), "InPerson", null));
        async Task<ClassSession> Created(Batch batch, DateTime time, Guid? teacher, bool follows)
        {
            using var response = await Create(batch, time, teacher);
            RequireFinanceStatus(response, HttpStatusCode.Created, "create reassignment fixture");
            var summary = (await response.Content.ReadFromJsonAsync<ClassSessionSummary>())!;
            var row = await Get(summary.Id);
            if (row.FollowsBatchTeacher != follows || row.TeacherId != (teacher ?? batch.TeacherId))
                throw new InvalidOperationException("Manual class did not record its assignment provenance.");
            return row;
        }
        void Pass(string label) { count++; Console.WriteLine($"BATCHPRESERVE REASSIGN CASE {label} PASS"); }
        async Task Refuse(Func<Task<HttpResponseMessage>> send, HttpStatusCode expected, string label)
        {
            var state = await State();
            using var response = await send();
            RequireFinanceStatus(response, expected, label);
            if (state != await State()) throw new InvalidOperationException(label + " changed batch, class, attendance or audit.");
            Pass(label);
        }
        async Task ExpectTeacher(Guid id, Guid? teacher, bool? follows)
        {
            var row = await Get(id);
            if (row.TeacherId != teacher || row.FollowsBatchTeacher != follows)
                throw new InvalidOperationException("Class teacher or provenance did not match the reassignment plan.");
        }
        async Task Change(Batch batch, Guid? teacher, string? name = null, bool? active = null)
        {
            using var response = await Reassign(admin, batch, teacher, name, active);
            RequireFinanceStatus(response, HttpStatusCode.OK, "reassign batch");
            if ((await GetBatch(batch.Id)).TeacherId != teacher) throw new InvalidOperationException("Batch default did not persist.");
        }

        var main = await SeedBatch("main", original);
        var fixedRow = await Created(main, start, substitute.Id, false);
        var following = await Created(main, start.AddHours(1), null, true);
        var sameAsDefault = await Created(main, start.AddHours(2), original, true);
        var inherited = await Seed(main, original, start.AddHours(3));
        var legacySubstitute = await Seed(main, replacement, start.AddHours(4));
        var legacyUnassigned = await Seed(main, null, start.AddHours(5));
        var past = await Seed(main, original, DateTime.UtcNow.Date.AddDays(-4));
        var cancelled = await Seed(main, original, start.AddHours(6), "Cancelled");
        var completed = await Seed(main, original, start.AddHours(7), "Completed");
        var outside = await Seed(main, original, start.AddHours(8), tenant: foreign);
        var otherBatch = await SeedBatch("other", null);
        await Seed(otherBatch, replacement, following.StartUtc.AddHours(-1));
        await Seed(otherBatch, replacement, inherited.StartUtc, "Cancelled");
        Pass("Manual create records defaults and explicit substitutes");

        var ids = new[] { fixedRow, following, sameAsDefault, inherited, legacySubstitute, legacyUnassigned, past, cancelled, completed, outside }
            .Select(x => x.Id).ToArray();
        var stable = await WithDb(db => db.ClassSessions.AsNoTracking().Where(x => ids.Contains(x.Id)).ToArrayAsync());
        await Change(main, original, "Reassignment renamed", false);
        foreach (var row in stable)
            if (JsonSerializer.Serialize(row) != JsonSerializer.Serialize(await Get(row.Id)))
                throw new InvalidOperationException("A batch edit with the same teacher changed a class row.");
        Pass("rename and deactivation preserve all session rows");

        await Change(await GetBatch(main.Id), replacement, active: true);
        await ExpectTeacher(following.Id, replacement, true);
        await ExpectTeacher(sameAsDefault.Id, replacement, true);
        await ExpectTeacher(inherited.Id, replacement, true);
        await ExpectTeacher(legacySubstitute.Id, replacement, false);
        await ExpectTeacher(fixedRow.Id, substitute.Id, false);
        await ExpectTeacher(legacyUnassigned.Id, null, null);
        foreach (var row in stable.Where(x => x.Id == past.Id || x.Id == cancelled.Id || x.Id == completed.Id || x.Id == outside.Id))
            if (JsonSerializer.Serialize(row) != JsonSerializer.Serialize(await Get(row.Id)))
                throw new InvalidOperationException("Reassignment touched a past, non-scheduled or foreign class.");
        Pass("eligible sessions move despite adjacency or cancelled occupancy, other classes stay unchanged");

        await Change(await GetBatch(main.Id), third.Id);
        await ExpectTeacher(following.Id, third.Id, true);
        await ExpectTeacher(legacySubstitute.Id, replacement, false);
        await ExpectTeacher(fixedRow.Id, substitute.Id, false);
        Pass("consecutive default changes preserve a substitute that matched the intervening default");

        await Change(await GetBatch(main.Id), null);
        await ExpectTeacher(following.Id, null, true);
        await ExpectTeacher(legacyUnassigned.Id, null, false);
        await Change(await GetBatch(main.Id), third.Id);
        await ExpectTeacher(following.Id, third.Id, true);
        await ExpectTeacher(legacyUnassigned.Id, null, false);
        await ExpectTeacher(legacySubstitute.Id, replacement, false);
        Pass("clearing and restoring default preserves a separately unassigned legacy class");

        var locked = await SeedBatch("conflict", original);
        var lockedSession = await Seed(locked, original, start.AddDays(1));
        await Seed(otherBatch, replacement, lockedSession.StartUtc.AddMinutes(30));
        await Refuse(() => Reassign(admin, locked, replacement, "Reassignment must roll back name"), HttpStatusCode.Conflict, "overlap in another batch");
        await Seed(locked, substitute.Id, lockedSession.StartUtc.AddMinutes(-20));
        await Refuse(() => Reassign(admin, locked, substitute.Id), HttpStatusCode.Conflict, "overlap with preserved substitute in same batch");
        await Seed(locked, original, lockedSession.StartUtc.AddMinutes(20));
        await Refuse(() => Reassign(admin, locked, third.Id), HttpStatusCode.Conflict, "overlap among sessions that would inherit the new teacher");
        await Refuse(() => Reassign(admin, main, foreignTeacher.Id), HttpStatusCode.BadRequest, "foreign teacher rejected");

        var auditBatch = await SeedBatch("audit", original);
        await Seed(auditBatch, original, start.AddDays(2));
        var fault = new EnrollmentSharedAuditFault("PUT Batches");
        using (var faultyHost = new QaApiFactory(manifest, isolatedDomainInterceptor: fault))
        using (var faulty = await SignIn(faultyHost))
            await Refuse(() => Reassign(faulty, auditBatch, replacement), HttpStatusCode.InternalServerError, "required audit rollback");
        if (fault.Hits != 1) throw new InvalidOperationException("Batch reassignment did not reach the required audit.");

        var left = await SeedBatch("race left", original);
        var right = await SeedBatch("race right", third.Id);
        var raceTime = start.AddDays(3);
        await Seed(left, original, raceTime);
        await Seed(right, third.Id, raceTime);
        var beforeAudits = await WithDb(db => db.AuditLogs.CountAsync(x => x.AcademyId == academy && x.Action == "PUT Batches"));
        var batchPair = await Task.WhenAll(Reassign(admin, left, replacement), Reassign(admin, right, replacement));
        using (batchPair[0]) using (batchPair[1])
            if (!batchPair.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 200, 409 }))
                throw new InvalidOperationException("Concurrent batch writes did not admit exactly one reassignment.");
        if (await WithDb(db => db.ClassSessions.CountAsync(x => x.AcademyId == academy && x.StartUtc == raceTime && x.TeacherId == replacement)) != 1 ||
            await WithDb(db => db.AuditLogs.CountAsync(x => x.AcademyId == academy && x.Action == "PUT Batches")) != beforeAudits + 1)
            throw new InvalidOperationException("Concurrent reassignment committed the wrong SQL state.");
        Pass("concurrent overlapping batch reassignments commit one winner");

        var mixed = await SeedBatch("mixed", original);
        var mixedTime = start.AddDays(4);
        await Seed(mixed, original, mixedTime);
        var mixedPair = await Task.WhenAll(Reassign(admin, mixed, replacement), Create(otherBatch, mixedTime, replacement));
        using (mixedPair[0]) using (mixedPair[1])
        {
            var result = mixedPair.Select(x => (int)x.StatusCode).Order().ToArray();
            if (result[0] is not (200 or 201) || result[1] != 409)
                throw new InvalidOperationException("Batch reassignment and Manual class creation must share the slot lock.");
        }
        if (await WithDb(db => db.ClassSessions.CountAsync(x => x.AcademyId == academy && x.StartUtc == mixedTime && x.TeacherId == replacement)) != 1)
            throw new InvalidOperationException("Mixed race committed multiple assignments.");
        Pass("batch reassignment and Manual class creation admit one conflicting write");

        var pentaBatch = await SeedBatch("protected", original);
        using var pentaHost = new QaApiFactory(manifest, new Dictionary<string, string?>
        {
            ["Penta:Enabled"] = "true", ["Penta:ClassSessionSchedulingEnabled"] = "true"
        });
        using var penta = await SignIn(pentaHost);
        Guid actor;
        using (var scope = factory.Services.CreateScope())
            actor = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email))!.Id;
        var conversation = new PentaMiniSession { AcademyId = academy, ActorUserId = actor, ProtectedState = "Synthetic batch assignment review",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1) };
        await WithDb(db => { db.PentaMiniSessions.Add(conversation); return db.SaveChangesAsync(); });
        var draftRoute = $"/api/academies/{academy}/penta/class-scheduling/conversations/{conversation.Id}/drafts/{Guid.NewGuid()}";
        var plannedTime = start.AddDays(5);
        var fields = new Dictionary<string, object>
        {
            ["BatchId"] = new { kind = "reference", value = pentaBatch.Id.ToString("D") },
            ["TeacherId"] = new { kind = "reference", value = replacement.ToString("D") },
            ["StartUtc"] = new { kind = "text", value = plannedTime.ToString("O") },
            ["EndUtc"] = new { kind = "text", value = plannedTime.AddHours(1).ToString("O") },
            ["DeliveryMode"] = new { kind = "text", value = "Offline" }
        };
        using var prepared = await penta.PutAsJsonAsync(draftRoute, new { contextVersion = 0,
            payload = new { tool_key = "AcademyDesk.ClassSession.Create", tool_revision = 1, fields } }, new JsonSerializerOptions());
        RequireFinanceStatus(prepared, HttpStatusCode.Created, "protected substitute preview");
        var draft = (await prepared.Content.ReadFromJsonAsync<PentaClassSessionDraftView>())!;
        if (draft.FollowsBatchTeacher != false || draft.Plan.TeacherId != replacement)
            throw new InvalidOperationException("Protected review did not bind the explicit substitute.");
        using var approval = await penta.PostAsJsonAsync(draftRoute + "/approve", new { revision = draft.Revision, payloadHash = draft.PayloadHash,
            intent = "ApproveClassSessionSchedule" });
        RequireFinanceStatus(approval, HttpStatusCode.OK, "protected substitute approval");
        var approved = (await approval.Content.ReadFromJsonAsync<PentaClassSessionDraftView>())!;
        await Change(pentaBatch, replacement);
        await Refuse(() => penta.PostAsJsonAsync(draftRoute + "/execute", new { revision = approved.Revision, payloadHash = approved.PayloadHash,
            approvalId = approved.ApprovalId }), HttpStatusCode.Conflict, "PENTA approval becomes stale when inheritance changes without changing the teacher");

        if (count != 13) throw new InvalidOperationException("Unexpected batch reassignment count: " + count);
        Console.WriteLine($"BATCHPRESERVE REASSIGN SUMMARY {count} native HTTP/SQL checks PASS.");
    }
}
