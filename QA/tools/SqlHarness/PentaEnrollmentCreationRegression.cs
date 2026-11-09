using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyEnrollmentCreationWorkflowAsync(QaApiFactory factory, QaRunManifest manifest,
        Guid academy, Guid foreign, Guid actor, string email, Func<Task<EnrollmentFixture>> seed)
    {
        var clock = new OnboardingDraftClock();
        var flags = new Dictionary<string, string?> { ["Penta:Enabled"] = "true", ["Penta:EnrollmentCreationEnabled"] = "true" };
        using var host = new QaApiFactory(manifest, flags, isolatedClock: clock);
        using var admin = await SignIn(host, email);
        using var otherAdmin = await SignIn(host, "qa-admin-a@example.invalid");
        using var teacher = await SignIn(host, "qa-teacher-a@example.invalid");
        using var disabled = await SignIn(factory, email);
        using var anonymous = host.CreateClient();
        const string prepareAudit = "PentaEnrollmentPrepared";
        const string approveAudit = "PentaEnrollmentApproved";
        const string executeAudit = "PentaEnrollmentExecuted";
        var cases = 0;

        static async Task<HttpClient> SignIn(QaApiFactory owner, string login)
        {
            var client = owner.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, login, "Synthetic!39Ab"));
            return client;
        }

        async Task<PentaMiniSession> Session()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = new PentaMiniSession { AcademyId = academy, ActorUserId = actor,
                ExpiresAtUtc = clock.Value.UtcDateTime.AddHours(1), ProtectedState = "Synthetic session fixture" };
            db.PentaMiniSessions.Add(row);
            await db.SaveChangesAsync();
            return row;
        }

        string Route(Guid session, Guid request, Guid? tenant = null) =>
            $"/api/academies/{tenant ?? academy}/penta/enrollments/conversations/{session}/drafts/{request}";

        var exactFieldNames = new JsonSerializerOptions();
        Task<HttpResponseMessage> Prepare(HttpClient client, string route, EnrollmentFixture fixture, long context = 0) =>
            client.PutAsJsonAsync(route, new { contextVersion = context, payload = new
            {
                tool_key = PentaEnrollmentFields.CreateTool, tool_revision = 1,
                fields = new { StudentId = new { kind = "reference", value = fixture.Student.ToString("D") },
                    BatchId = new { kind = "reference", value = fixture.Target.ToString("D") },
                    StartDate = new { kind = "date", value = "2026-11-01" } }
            } }, exactFieldNames);

        static Task<HttpResponseMessage> Approve(HttpClient client, string route, PentaEnrollmentDraftView draft,
            string intent = PentaEnrollmentCreationWorkflow.ApprovalIntent, string? hash = null) =>
            client.PostAsJsonAsync(route + "/approve", new { revision = draft.Revision, payloadHash = hash ?? draft.PayloadHash, intent });

        static Task<HttpResponseMessage> Execute(HttpClient client, string route, PentaEnrollmentDraftView draft, Guid? approval = null) =>
            client.PostAsJsonAsync(route + "/execute", new { revision = draft.Revision, payloadHash = draft.PayloadHash, approvalId = approval ?? draft.ApprovalId });

        async Task<PentaEnrollmentDraftView> ReadDraft(Task<HttpResponseMessage> request, HttpStatusCode expected, string label)
        {
            using var response = await request;
            RequireFinanceStatus(response, expected, label);
            if (response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Enrollment draft response must be no-store.");
            cases++;
            return (await response.Content.ReadFromJsonAsync<PentaEnrollmentDraftView>())!;
        }

        async Task<string> State()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                drafts = await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                audits = await db.AuditLogs.AsNoTracking().Where(x => x.EntityType == "PentaEnrollmentDraft").OrderBy(x => x.Id).ToListAsync() });
        }

        async Task Refuse(Func<Task<HttpResponseMessage>> request, HttpStatusCode status, string name)
        {
            var before = await State();
            using var response = await request();
            RequireFinanceStatus(response, status, name);
            if (before != await State()) throw new InvalidOperationException("Refused enrollment operation changed SQL state: " + name);
            cases++;
        }

        var fixture = await seed();
        var session = await Session();
        var request = Guid.NewGuid();
        var route = Route(session.Id, request);
        await Refuse(() => Prepare(disabled, route, fixture), HttpStatusCode.NotFound, "creation flag disabled");
        await Refuse(() => Prepare(anonymous, route, fixture), HttpStatusCode.Unauthorized, "anonymous prepare");
        await Refuse(() => Prepare(teacher, route, fixture), HttpStatusCode.Forbidden, "teacher prepare");
        await Refuse(() => Prepare(admin, Route(session.Id, request, foreign), fixture), HttpStatusCode.Forbidden, "cross-tenant prepare");
        await Refuse(() => Prepare(admin, route, fixture, 1), HttpStatusCode.Conflict, "stale context prepare");
        var draft = await ReadDraft(Prepare(admin, route, fixture), HttpStatusCode.Created, "prepare enrollment");
        if (draft.RequestId != request || draft.Status != "Prepared" || draft.ApprovalId is not null || draft.Receipt is not null ||
            draft.Admission.StudentId != fixture.Student || draft.Admission.BatchId != fixture.Target || draft.Admission.StartDate != new DateOnly(2026, 11, 1))
            throw new InvalidOperationException("Prepared enrollment view did not match the checked fields.");
        var state = await State();
        var repeated = await ReadDraft(Prepare(admin, route, fixture), HttpStatusCode.OK, "prepare retry");
        if (repeated != draft || state != await State()) throw new InvalidOperationException("Prepare retry changed the draft or audit.");
        await Refuse(() => Prepare(admin, route, fixture with { Target = fixture.Source }), HttpStatusCode.Conflict, "same key changed payload");
        await Refuse(() => otherAdmin.GetAsync(route), HttpStatusCode.NotFound, "other same-tenant administrator cannot read");
        await Refuse(() => Execute(admin, route, draft, Guid.NewGuid()), HttpStatusCode.Conflict, "unapproved execution");
        await Refuse(() => Approve(admin, route, draft, "yes"), HttpStatusCode.BadRequest, "conversational yes cannot approve");
        await Refuse(() => Approve(admin, route, draft, hash: new string('0', 64)), HttpStatusCode.Conflict, "different hash cannot approve");
        var approved = await ReadDraft(Approve(admin, route, draft), HttpStatusCode.OK, "exact enrollment approval");
        if (approved.Status != "Authorized" || approved.ApprovalId is null || approved.Receipt is not null)
            throw new InvalidOperationException("Enrollment approval was not recorded independently.");
        state = await State();
        if (await ReadDraft(Approve(admin, route, draft), HttpStatusCode.OK, "approval retry") != approved || state != await State())
            throw new InvalidOperationException("Approval retry changed SQL state.");
        await Refuse(() => Execute(admin, route, approved, Guid.NewGuid()), HttpStatusCode.Conflict, "foreign approval cannot execute");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.Enrollments.AnyAsync(x => x.BatchId == fixture.Target && x.StudentId == fixture.Student))
                throw new InvalidOperationException("Prepare or approval wrote an enrollment.");
        }

        var auditFault = new EnrollmentSharedAuditFault(executeAudit);
        using (var faultyHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: auditFault, isolatedClock: clock))
        using (var faulty = await SignIn(faultyHost, email))
            await Refuse(() => Execute(faulty, route, approved), HttpStatusCode.ServiceUnavailable, "required audit rollback");
        if (auditFault.Hits != 1) throw new InvalidOperationException("Enrollment execution did not reach the injected audit failure.");
        var attempts = await Task.WhenAll(Execute(admin, route, approved), Execute(admin, route, approved));
        using var creationResponse = attempts[0];
        using var retryResponse = attempts[1];
        if (!attempts.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 200, 201 }))
            throw new InvalidOperationException("Concurrent execute should commit once and return the original result once.");
        var committed = (await creationResponse.Content.ReadFromJsonAsync<PentaEnrollmentDraftView>())!;
        if (committed != await retryResponse.Content.ReadFromJsonAsync<PentaEnrollmentDraftView>() ||
            committed.Status != "Succeeded" || committed.Receipt is not { } receipt)
            throw new InvalidOperationException("Concurrent execute returned divergent receipts.");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var entry = await db.Enrollments.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.StudentId == fixture.Student && x.BatchId == fixture.Target);
            var audits = await db.AuditLogs.AsNoTracking().Where(x => x.ActorUserId == actor && x.EntityType == "PentaEnrollmentDraft").ToListAsync();
            if (entry.Id != receipt.Enrollment.Id || !audits.Select(x => x.Action).Order().SequenceEqual(new[] { approveAudit, executeAudit, prepareAudit }.Order()) ||
                audits.Single(x => x.Action == executeAudit).Id != receipt.AuditId)
                throw new InvalidOperationException("Enrollment, receipt and exactly one audit per transition must agree.");
            await db.PentaMiniSessions.Where(x => x.Id == session.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.Version, 1));
        }
        cases++;
        var recovered = await ReadDraft(admin.GetAsync(route), HttpStatusCode.OK, "receipt recovery after conversation advances");
        if (recovered != committed) throw new InvalidOperationException("Recovered receipt differed from the original.");

        var changedFixture = await seed();
        var changedSession = await Session();
        var changedRoute = Route(changedSession.Id, Guid.NewGuid());
        var pending = await ReadDraft(Prepare(admin, changedRoute, changedFixture), HttpStatusCode.Created, "second draft prepare");
        var confirmed = await ReadDraft(Approve(admin, changedRoute, pending), HttpStatusCode.OK, "second draft approval");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            await db.Batches.Where(x => x.Id == changedFixture.Target).ExecuteUpdateAsync(set => set.SetProperty(x => x.Capacity, 0));
        }
        await Refuse(() => Execute(admin, changedRoute, confirmed), HttpStatusCode.Conflict, "capacity changed since approval");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            await db.PentaMiniSessions.Where(x => x.Id == changedSession.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.Version, 1));
        }
        await Refuse(() => Execute(admin, changedRoute, confirmed), HttpStatusCode.Conflict, "conversation changed since approval");
        clock.Value = clock.Value.AddMinutes(11);
        await Refuse(() => Execute(admin, changedRoute, confirmed), HttpStatusCode.Gone, "approval expired");
        Console.WriteLine($"PASS: PENTA ENROLLMENT CREATION {cases} native HTTP/SQL checks; exactly one enrollment and receipt across concurrent execute.");
    }
}
