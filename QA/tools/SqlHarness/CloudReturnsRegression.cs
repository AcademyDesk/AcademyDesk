using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyCloudReturnsAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreign, student, admin, malformedTeacher, legacyTeacher;
        Invoice cancelled, mixedCase, adjusted, dollars, future, outside;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        const string studentEmail = "qa-cloud-return-student@example.invalid";
        const string fixturePassword = "Synthetic!39Ab";
        const string legacyCompensation = """{"Model":"Monthly","baseMonthlySalary":0,"effectiveFrom":"2026-11-01"}""";
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            var foreignStudent = await db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            admin = (await users.FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            if (!(await roles.CreateAsync(new ApplicationRole { Name = "Student", IsSystemRole = true })).Succeeded)
                throw new InvalidOperationException("Student fixture role setup failed.");
            var learner = new ApplicationUser { UserName = studentEmail, Email = studentEmail, EmailConfirmed = true,
                IsActive = true, AcademyId = academy, StudentId = student, DisplayName = "Synthetic reminder student" };
            if (!(await users.CreateAsync(learner, fixturePassword)).Succeeded || !(await users.AddToRoleAsync(learner, "Student")).Succeeded)
                throw new InvalidOperationException("Student fixture identity setup failed.");

            Invoice AddInvoice(string number, decimal amount, string status = "Issued", string currency = "INR", bool upcoming = false)
            {
                var row = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = number,
                    TotalAmount = amount, Status = status, Currency = currency, DueDate = today.AddDays(upcoming ? 7 : -2) };
                db.Invoices.Add(row);
                return row;
            }
            cancelled = AddInvoice("C14-CANCELLED", 800m, "Cancelled");
            mixedCase = AddInvoice("C14-MIXED-CASE", 700m, "cAnCeLlEd");
            adjusted = AddInvoice("C14-ADJUSTED", 1000m, "PartiallyPaid");
            adjusted.AdjustedAmount = 200m;
            var secondStudent = new Student { AcademyId = academy, FirstName = "C14", LastName = "Dollar recipient" };
            db.Students.Add(secondStudent);
            dollars = AddInvoice("C14-USD", 50m, currency: "USD");
            dollars.StudentId = secondStudent.Id;
            var settled = AddInvoice("C14-SETTLED", 400m, "Paid");
            future = AddInvoice("C14-FUTURE", 75m, upcoming: true);
            outside = AddInvoice("C14-FOREIGN", 999m);
            outside.AcademyId = foreign;
            outside.StudentId = foreignStudent;
            db.Payments.AddRange(
                new Payment { AcademyId = academy, InvoiceId = adjusted.Id, Amount = 500m, Status = "Reconciled",
                    ReconciledAtUtc = DateTime.UtcNow, ReconciliationReference = "C14 fixture" },
                new Payment { AcademyId = academy, InvoiceId = adjusted.Id, Amount = 100m, Status = "Completed" },
                new Payment { AcademyId = academy, InvoiceId = adjusted.Id, Amount = 75m, Status = "Voided" },
                new Payment { AcademyId = academy, InvoiceId = settled.Id, Amount = 400m, Status = "Completed" });

            var teacher = await db.Teachers.SingleAsync(x => x.AcademyId == academy);
            teacher.CompensationJson = "[]";
            malformedTeacher = teacher.Id;
            var legacy = new Teacher { AcademyId = academy, FirstName = "Synthetic", LastName = "Legacy", CompensationJson = legacyCompensation };
            legacyTeacher = legacy.Id;
            db.Add(legacy);
            db.Notifications.AddRange(
                Announcement(academy, "C13 shared", """{"important":"true","expiresAtUtc":"2099-01-01T00:00:00Z"}"""),
                Announcement(academy, "C13 student", """{"important":"true","expiresAtUtc":"2099-01-01T00:00:00Z","audiences":"Student"}"""),
                Announcement(academy, "C13 teacher", """{"important":"true","expiresAtUtc":"2099-01-01T00:00:00Z","audiences":"Teacher"}"""),
                Announcement(academy, "C13 invalid root", "[]"),
                Announcement(academy, "C13 invalid kind", """{"important":"true","expiresAtUtc":"2099-01-01T00:00:00Z","audiences":["Student"]}"""),
                Announcement(academy, "C13 invalid start", """{"important":"true","expiresAtUtc":"2099-01-01T00:00:00Z","startsAtUtc":"not-a-date"}"""),
                Announcement(academy, "C13 future", """{"important":"true","startsAtUtc":"2099-01-01T00:00:00Z","expiresAtUtc":"2100-01-01T00:00:00Z"}"""),
                Announcement(academy, "C13 expired", """{"important":"true","expiresAtUtc":"2000-01-01T00:00:00Z"}"""),
                Announcement(foreign, "C13 foreign", """{"important":"true","expiresAtUtc":"2099-01-01T00:00:00Z"}"""));
            await db.SaveChangesAsync();
        }

        var token = await LoginAsync(client, "qa-admin-a@example.invalid", fixturePassword);
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", fixturePassword);
        var studentToken = await LoginAsync(client, studentEmail, fixturePassword);
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", fixturePassword);
        client.DefaultRequestHeaders.Authorization = null;
        var root = $"/api/academies/{academy}";
        var route = root + "/fee-reminders";
        var count = 0;
        void Pass(string label) { count++; Console.WriteLine($"CLOUDRETURN CASE {label} PASS"); }
        async Task<JsonElement> Read(string path, string auth)
        {
            using var response = await ManualSendAsync(client, auth, "GET", path);
            RequireFinanceStatus(response, HttpStatusCode.OK, path);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        async Task CheckQueue(string label, string? auth, Guid? invoiceId, HttpStatusCode status, params (Invoice Invoice, string Amount)[] expected)
        {
            var before = await ManualStateAsync(factory);
            using var response = await ManualSendAsync(client, auth, "POST", route, new JsonObject { ["invoiceId"] = JsonValue.Create(invoiceId), ["channel"] = "InApp" });
            RequireFinanceStatus(response, status, label);
            var after = await ManualStateAsync(factory);
            if (status == HttpStatusCode.OK)
            {
                var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (payload.GetProperty("queuedCount").GetInt32() != expected.Length)
                    throw new InvalidOperationException("Unexpected reminder response count.");
            }
            if (expected.Length > 0)
            {
                var notifications = ManualAdded(before, after, "notifications", expected.Length);
                foreach (var (invoice, amount) in expected)
                {
                    var row = notifications.Single(x => x["Message"]!.GetValue<string>().Contains($"Invoice: {invoice.InvoiceNumber}.", StringComparison.Ordinal));
                    FinanceRecoveryFields(row, new { AcademyId = academy, RecipientId = invoice.StudentId, RecipientType = "Student", Channel = "InApp",
                        Status = "Queued", AttemptCount = 0, SentAtUtc = (DateTime?)null });
                    if (!row["Message"]!.GetValue<string>().Contains($"outstanding balance is {amount}.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Queued reminder currency or balance differs.");
                }
                var audits = ManualAdded(before, after, "audits", 2);
                var action = audits.Single(x => x["Action"]!.GetValue<string>() == "POST FeeReminders");
                FinanceRecoveryFields(action, new { ActorUserId = admin, AcademyId = academy });
                var summary = audits.Single(x => x["Action"]!.GetValue<string>() == "FeeRemindersQueued");
                using var metadata = JsonDocument.Parse(summary["MetadataJson"]!.GetValue<string>());
                if (metadata.RootElement.GetProperty("count").GetInt32() != expected.Length)
                    throw new InvalidOperationException("Reminder audit summary count differs.");
            }
            else if (status == HttpStatusCode.OK)
            {
                var audit = ManualAdded(before, after, "audits", 1).Single();
                FinanceRecoveryFields(audit, new { ActorUserId = admin, AcademyId = academy, Action = "POST FeeReminders" });
            }
            ManualSame(before, after, label);
            Pass(label);
        }

        var rows = await Read(root + "/invoices", token);
        decimal Balance(Guid id) => rows.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id).GetProperty("balance").GetDecimal();
        if (Balance(adjusted.Id) != 200m || Balance(dollars.Id) != 50m || rows.EnumerateArray().Any(x => x.GetProperty("id").GetGuid() == outside.Id))
            throw new InvalidOperationException("Authoritative invoice projection differs.");
        Pass("authoritative-invoice-balances");
        await CheckQueue("cancelled-target-no-notification", token, cancelled.Id, HttpStatusCode.OK);
        await CheckQueue("mixed-case-cancelled-target-no-notification", token, mixedCase.Id, HttpStatusCode.OK);
        await CheckQueue("foreign-invoice-id-no-notification", token, outside.Id, HttpStatusCode.OK);
        await CheckQueue("mixed-bulk-exact-recipients-and-balances", token, null, HttpStatusCode.OK, (adjusted, "INR 200.00"), (dollars, "USD 50.00"));
        await CheckQueue("explicit-future-target-policy", token, future.Id, HttpStatusCode.OK, (future, "INR 75.00"));
        await CheckQueue("foreign-actor-denied", foreignToken, adjusted.Id, HttpStatusCode.Forbidden);
        await CheckQueue("teacher-denied", teacherToken, adjusted.Id, HttpStatusCode.Forbidden);
        await CheckQueue("student-denied", studentToken, adjusted.Id, HttpStatusCode.Forbidden);
        await CheckQueue("anonymous-denied", null, adjusted.Id, HttpStatusCode.Unauthorized);

        var fault = new ReminderAuditFault();
        var snapshot = await ManualStateAsync(factory);
        using (var failing = new QaApiFactory(manifest, isolatedDomainInterceptor: fault))
        using (var failingClient = failing.CreateClient(new() { AllowAutoRedirect = false }))
        {
            using var response = await ManualSendAsync(failingClient, token, "POST", route, new JsonObject { ["invoiceId"] = adjusted.Id, ["channel"] = "InApp" });
            RequireFinanceStatus(response, HttpStatusCode.InternalServerError, "required actor audit fault");
            if (fault.Hits != 1) throw new InvalidOperationException("Required reminder audit fault was not reached.");
        }
        ManualSame(snapshot, await ManualStateAsync(factory), "reminder and summary audit rollback");
        Pass("required-actor-audit-rolls-back-queued-reminder");

        foreach (var (role, auth, expected) in new[]
        {
            ("student", studentToken, new[] { "C13 shared", "C13 student" }),
            ("teacher", teacherToken, new[] { "C13 shared", "C13 teacher" }),
            ("admin", token, new[] { "C13 shared" }),
            ("foreign-admin", foreignToken, new[] { "C13 foreign" }),
        })
        {
            var announcements = await Read("/api/portal/announcements", auth);
            var titles = announcements.EnumerateArray().Select(x => x.GetProperty("title").GetString()).Order(StringComparer.Ordinal);
            if (!titles.SequenceEqual(expected.Order(StringComparer.Ordinal))) throw new InvalidOperationException("Unexpected announcement visibility: " + role);
            Pass("announcement-metadata-" + role);
        }
        var malformed = await Read(root + $"/teachers/{malformedTeacher}/compensation", token);
        var legacyRead = await Read(root + $"/teachers/{legacyTeacher}/compensation", token);
        if (malformed.GetProperty("model").GetString() != "" || malformed.GetProperty("monthlySalary").ValueKind != JsonValueKind.Null ||
            legacyRead.GetProperty("model").GetString() != "Monthly" || legacyRead.GetProperty("monthlySalary").GetDecimal() != 0 ||
            legacyRead.GetProperty("effectiveFrom").GetString() != "2026-11-01")
            throw new InvalidOperationException("Compensation fallback or legacy zero differs.");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var metadata = await db.Teachers.AsNoTracking().Where(x => x.Id == malformedTeacher || x.Id == legacyTeacher).ToDictionaryAsync(x => x.Id, x => x.CompensationJson);
            if (metadata[malformedTeacher] != "[]" || metadata[legacyTeacher] != legacyCompensation || await db.NotificationReadReceipts.AnyAsync())
                throw new InvalidOperationException("Read-only metadata or receipts changed.");
        }
        ManualSame(snapshot, await ManualStateAsync(factory), "metadata reads leave notifications, ledgers and audits unchanged");
        Pass("compensation-malformed-and-legacy-sql-bytes");
        Console.WriteLine($"CLOUDRETURN SUMMARY {count} native HTTP/SQL checks PASS");
    }

    private static Notification Announcement(Guid academy, string title, string metadata) =>
        new() { AcademyId = academy, RecipientType = "Academy", Title = title, Message = "Synthetic announcement", VariablesJson = metadata };

    private sealed class ReminderAuditFault : SaveChangesInterceptor
    {
        public int Hits { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default)
        {
            if (data.Context is AcademyDeskDbContext db && db.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && x.Entity.Action == "POST FeeReminders"))
            {
                Hits++;
                throw new InvalidOperationException("Synthetic required reminder audit failure.");
            }
            return base.SavingChangesAsync(data, result, token);
        }
    }
}
