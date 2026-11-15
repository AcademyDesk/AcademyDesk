using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyEnrollmentLifecycleBrowserAsync(QaApiFactory factory, QaRunManifest manifest,
        Guid academy, Guid foreign, Func<Task<EnrollmentFixture>> seed)
    {
        const string email = "qa-enrollment-lifecycle-browser@example.invalid";
        const string reason = "Synthetic lifecycle browser review";
        var actor = await CreateAccessActorAsync(factory, email, "AcademyAdmin", academy);
        using var host = new QaApiFactory(manifest, new Dictionary<string, string?>
        {
            ["Penta:Enabled"] = "true",
            ["Penta:EnrollmentCreationEnabled"] = "true",
            ["Penta:EnrollmentBrowserContractsEnabled"] = "true",
            ["Penta:EnrollmentLifecycleEnabled"] = "true",
            ["Penta:EnrollmentLifecycleBrowserContractsEnabled"] = "true"
        });
        using var admin = await PentaBrowserSignIn(host, email);
        using var teacher = await PentaBrowserSignIn(host, "qa-teacher-a@example.invalid");
        var enrollmentRoot = $"/api/academies/{academy:D}/penta/enrollments";
        var root = enrollmentRoot + "/lifecycle-browser";
        var checks = 0;

        var key = Guid.NewGuid();
        var recovery = root + $"/starts/{key:D}";
        using (var readiness = await PentaBrowserRead<JsonDocument>(admin.GetAsync(root + "/readiness"), HttpStatusCode.OK, "lifecycle readiness"))
            PentaRequire(readiness.RootElement.GetProperty("protocol").GetString() == "enrollment-lifecycle-browser.v1" &&
                readiness.RootElement.GetProperty("requiresApproval").GetBoolean(), "Wrong lifecycle readiness contract.");
        await PentaBrowserStatus(admin.GetAsync(recovery), HttpStatusCode.NotFound, "readiness does not start a conversation");
        await PentaBrowserStatus(teacher.GetAsync(root + "/search?q=Synthetic"), HttpStatusCode.Forbidden, "teacher lifecycle search");
        await PentaBrowserStatus(admin.GetAsync($"/api/academies/{foreign:D}/penta/enrollments/lifecycle-browser/readiness"),
            HttpStatusCode.Forbidden, "foreign lifecycle readiness");
        Pass("private readiness and tenant/role checks");

        var session = await PentaBrowserRead<MiniSessionView>(Start(root + $"/conversations/{key:D}"), HttpStatusCode.Created, "lifecycle start");
        var create = await PentaBrowserRead<MiniSessionView>(Start(enrollmentRoot + $"/browser-conversations/{key:D}"),
            HttpStatusCode.Created, "admission start with same key");
        PentaRequire(session.ConversationId != create.ConversationId &&
            session == await PentaBrowserRead<MiniSessionView>(admin.GetAsync(recovery), HttpStatusCode.OK, "recover lifecycle start"),
            "Lifecycle start did not bind its own purpose.");
        await PentaBrowserStatus(admin.GetAsync(Draft(create.ConversationId)), HttpStatusCode.NotFound, "admission session cannot read lifecycle");
        Pass("same-key admission and lifecycle sessions are distinct");

        foreach (var status in new[] { "Active", "Waitlisted", "Completed", "Withdrawn", "Cancelled", "Paused" })
        {
            var fixture = await seed();
            if (status == "Active")
                await Db(db => db.Enrollments.Where(x => x.Id == fixture.Enrollment)
                    .ExecuteUpdateAsync(set => set.SetProperty(x => x.Status, "Paused")));
            if (status == "Waitlisted")
                await Db(db => db.Batches.Where(x => x.Id == fixture.Source)
                    .ExecuteUpdateAsync(set => set.SetProperty(x => x.WaitlistCapacity, 3)));

            var initial = status == "Active" ? "Paused" : "Active";
            DateOnly? end = status == "Active" ? null : new DateOnly(2026, 11, 13);
            var fields = new Dictionary<string, object>
            {
                ["EnrollmentId"] = new { kind = "reference", value = fixture.Enrollment.ToString("D") },
                ["Status"] = new { kind = "text", value = status }
            };
            if (end.HasValue)
            {
                fields["EndDate"] = new { kind = "date", value = "2026-11-13" };
                fields["LifecycleReason"] = new { kind = "text", value = reason };
            }

            var route = Draft(session.ConversationId);
            var reviewed = await PentaBrowserRead<PentaEnrollmentDraftView>(
                PentaBrowserPrepare(admin, route, PentaEnrollmentFields.UpdateTool, fields), HttpStatusCode.Created, "review " + status);
            var expected = new PentaEnrollmentChangePreview(PentaEnrollmentFields.UpdateTool, fixture.Enrollment,
                fixture.Student, fixture.Source, initial, status, end, end.HasValue ? reason : null, null, null);
            PentaRequire(reviewed.Status == "Prepared" && reviewed.Change == expected && reviewed.Receipt is null,
                "Lifecycle review returned different terms.");
            await PentaBrowserStatus(PentaBrowserExecute(admin, route, reviewed.Revision, reviewed.PayloadHash, Guid.NewGuid()),
                HttpStatusCode.Conflict, "lifecycle execution before approval");
            var approved = await PentaBrowserRead<PentaEnrollmentDraftView>(
                PentaBrowserApprove(admin, route, reviewed.Revision, reviewed.PayloadHash, "ApproveEnrollmentUpdate"),
                HttpStatusCode.OK, "authorize " + status);
            PentaRequire(approved.Status == "Authorized" && approved.Change == expected && approved.Receipt is null &&
                approved.ApprovalId.HasValue && (await Stored()).Status == initial, "Lifecycle approval altered domain state.");

            var saved = await PentaBrowserRead<PentaEnrollmentDraftView>(Execute(), HttpStatusCode.Created, "execute " + status);
            var replay = await PentaBrowserRead<PentaEnrollmentDraftView>(Execute(), HttpStatusCode.OK, "replay " + status);
            var row = await Stored();
            var receipt = saved.Receipt ?? throw new InvalidOperationException("Missing lifecycle receipt.");
            PentaRequire(saved == replay && saved.Status == "Succeeded" && receipt.Enrollment.Id == row.Id &&
                receipt.Enrollment.Status == row.Status && receipt.Enrollment.EndDate == row.EndDate &&
                row.Status == status && row.EndDate == end && row.LifecycleReason == expected.LifecycleReason &&
                await Db(db => db.Enrollments.CountAsync(x => x.AcademyId == academy && x.StudentId == fixture.Student)) == 1 &&
                await Db(db => db.AuditLogs.CountAsync(x => x.Action == "PentaEnrollmentExecuted" && x.AcademyId == academy &&
                    x.ActorUserId == actor && x.Id == receipt.AuditId)) == 1, "Lifecycle receipt differs from the single committed enrollment.");
            Pass("approved and replayed " + status);

            Task<AcademyDesk.Api.Domain.Entities.Enrollment> Stored() =>
                Db(db => db.Enrollments.AsNoTracking().SingleAsync(x => x.Id == fixture.Enrollment && x.AcademyId == academy));
            Task<HttpResponseMessage> Execute() => PentaBrowserExecute(admin, route, approved.Revision, approved.PayloadHash, approved.ApprovalId);
        }

        Console.WriteLine($"PASS: PENTA ENROLLMENT LIFECYCLE BROWSER {checks} native HTTP/SQL checks");

        string Draft(Guid id) => root + $"/conversations/{id:D}/drafts/{Guid.NewGuid():D}";
        Task<HttpResponseMessage> Start(string route) => admin.PutAsJsonAsync(route, new Dictionary<string, string>());
        async Task<T> Db<T>(Func<AcademyDeskDbContext, Task<T>> action)
        {
            using var scope = factory.Services.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }
        void Pass(string label)
        {
            checks++;
            Console.WriteLine("PASS: PENTA ENROLLMENT LIFECYCLE BROWSER " + label);
        }
    }
}
