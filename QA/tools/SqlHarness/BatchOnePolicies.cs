using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyBatchOnePoliciesAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, teacher, actor, invoiceId, foreignAcademy;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreignAcademy = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy && x.FirstName == "Synthetic").Select(x => x.Id).SingleAsync();
            actor = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            var student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).FirstAsync();
            var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-FP1-" + Guid.NewGuid().ToString("N"), TotalAmount = 1000m };
            db.Invoices.Add(invoice); await db.SaveChangesAsync(); invoiceId = invoice.Id;
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab"); client.DefaultRequestHeaders.Authorization = null;
        var root = $"/api/academies/{academy}"; var guarded = 0;
        async Task<string> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Profiles = (await db.PayrollProfiles.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(), Payouts = (await db.PayrollPayouts.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(), Invoices = (await db.Invoices.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(), Payments = (await db.Payments.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(), Adjustments = (await db.FinanceAdjustments.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(), Audits = (await db.AuditLogs.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray() });
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, string route, object? body = null, string? credential = null)
        {
            await Task.Delay(650); using var request = new HttpRequestMessage(method, route);
            if (body is not null) request.Content = JsonContent.Create(body);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential ?? token);
            var response = await client.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) throw new InvalidOperationException("Batch1 cannot pass with429.");
            return response;
        }
        async Task Denied(HttpMethod method, string route, object body, HttpStatusCode status, string stage)
        {
            var before = await State(); using var response = await Send(method, route, body);
            RequireFinanceStatus(response, status, stage);
            if (before != await State()) throw new InvalidOperationException(stage + " mutated the exact six-table financial state.");
            guarded++; Console.WriteLine($"BATCH1 POLICY PASS {stage}: {(int)status}, full six-table no-write/audit.");
        }
        foreach (var type in new[] { "Refund", "refund", "CreditNote", "creditnote" })
            await Denied(HttpMethod.Post, root + "/finance-adjustments", new CreateFinanceAdjustmentRequest(invoiceId, type, 100m, "Synthetic request"), HttpStatusCode.BadRequest, "FP1 create " + type);
        foreach (var type in new[] { "Refund", "CreditNote" })
        {
            Guid id;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var legacy = new FinanceAdjustment { AcademyId = academy, InvoiceId = invoiceId, Type = type, Amount = 100m, Reason = "Retained legacy fixture" };
                db.Add(legacy); await db.SaveChangesAsync(); id = legacy.Id;
            }
            foreach (var approve in new[] { true, false })
                await Denied(HttpMethod.Patch, root + "/finance-adjustments/" + id + "/approval", new FinanceAdjustmentDecisionRequest(approve, "Synthetic review"), HttpStatusCode.Conflict, $"FP1 legacy {type}/{approve}");
        }
        using (var create = await Send(HttpMethod.Post, root + "/finance-adjustments", new CreateFinanceAdjustmentRequest(invoiceId, "Discount", 100m, "Synthetic reduction")))
        {
            RequireFinanceStatus(create, HttpStatusCode.OK, "FP1 reduction create");
            var item = await create.Content.ReadFromJsonAsync<FinanceAdjustmentSummary>() ?? throw new InvalidOperationException("No reduction.");
            using var approve = await Send(HttpMethod.Patch, root + "/finance-adjustments/" + item.Id + "/approval", new FinanceAdjustmentDecisionRequest(true, "Explicit approval"));
            RequireFinanceStatus(approve, HttpStatusCode.OK, "FP1 reduction approval");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if ((await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoiceId)).AdjustedAmount != 100m || await db.Payments.AnyAsync(x => x.InvoiceId == invoiceId)) throw new InvalidOperationException("Reduction fabricated settlement.");
        }
        foreach (var model in new[] { "Monthly", "SessionBlock" })
        {
            var request = new SavePayrollProfileRequest("Teacher", teacher, null, "Synthetic FP2 " + model, model, model == "Monthly" ? 1000m : null, model == "SessionBlock" ? 1000m : null, model == "SessionBlock" ? 4 : null, new DateOnly(2026, 10, 1));
            using var create = await Send(HttpMethod.Post, root + "/payroll/profiles", request); RequireFinanceStatus(create, HttpStatusCode.Created, "FP2 profile");
            var profile = await create.Content.ReadFromJsonAsync<PayrollProfileSummary>() ?? throw new InvalidOperationException("No profile.");
            var pay = new CreatePayrollPayoutRequest(profile.Id, "QA-FP2-" + model, null, null, 1000m, "BankTransfer", null, null);
            await Denied(HttpMethod.Post, root + "/payroll/payouts", pay with { Reference = "Synthetic bank reference" }, HttpStatusCode.BadRequest, "FP2 reference " + model);
            await Denied(HttpMethod.Post, root + "/payroll/payouts", pay with { PaidAtUtc = DateTime.UtcNow }, HttpStatusCode.BadRequest, "FP2 paid date " + model);
            using var paid = await Send(HttpMethod.Post, root + "/payroll/payouts", pay); RequireFinanceStatus(paid, HttpStatusCode.Created, "FP2 statement");
            var summary = await paid.Content.ReadFromJsonAsync<PayrollPayoutSummary>() ?? throw new InvalidOperationException("No statement.");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var stored = await db.PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == summary.Id);
            if (stored.Status != "NoCashDue" || stored.PaymentMethod != "NoCashDue" || stored.PaidAtUtc != null || stored.Reference != null || stored.NetAmount != 0m || stored.GrossAmount != 1000m || stored.Deductions != 1000m || stored.TeacherIdSnapshot != teacher || stored.IdentityRecordedAtUtc == null || summary.NetAmountExact != "0.00" || summary.SettlementKind != "NoCashDue") throw new InvalidOperationException("Zero net masquerades as bank payout.");
            var audit = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).FirstAsync(x => x.Action == "POST Payroll" && x.AcademyId == academy);
            if (audit.ActorUserId != actor) throw new InvalidOperationException("FP2 wrong audit actor.");
            guarded++; Console.WriteLine("BATCH1 POLICY PASS FP2 " + model + ":201, exact SQL statement/null payment evidence/snapshot/actor audit.");
        }
        Guid voided;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var payment = new Payment { AcademyId = academy, InvoiceId = invoiceId, Amount = 100m, Status = "Voided", ReconciliationReference = "Retained void evidence", ReconciledAtUtc = DateTime.UtcNow };
            db.Add(payment); await db.SaveChangesAsync(); voided = payment.Id;
        }
        foreach (var status in new[] { "Completed", "Reconciled" })
            await Denied(HttpMethod.Patch, root + "/payments/" + voided + "/status", new UpdatePaymentStatusRequest(status), HttpStatusCode.BadRequest, "FP3 terminal " + status);
        await Denied(HttpMethod.Patch, root + "/payments/" + voided + "/reconcile", new ReconcilePaymentRequest("New synthetic evidence"), HttpStatusCode.BadRequest, "FP3 terminal dedicated");
        // Prove immutable payroll ownership independently of today's profile links.
        Guid ownId, borrowedId, foreignId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var otherTeacher = new Teacher { AcademyId = academy, FirstName = "Payroll", LastName = "Other" }; db.Add(otherTeacher);
            var moved = new PayrollProfile { AcademyId = academy, TeacherId = otherTeacher.Id, WorkerType = "Teacher", WorkerName = "Current other worker", MonthlyAmount = 1000m, IsActive = false };
            var current = new PayrollProfile { AcademyId = academy, TeacherId = teacher, WorkerType = "Teacher", WorkerName = "Current original", MonthlyAmount = 1000m };
            db.AddRange(moved, current); await db.SaveChangesAsync();
            PayrollPayout Row(Guid tenant, Guid profile, Guid identity, string name) => new() { AcademyId = tenant, PayrollProfileId = profile, WorkerNameSnapshot = name, WorkerTypeSnapshot = "Teacher", TeacherIdSnapshot = identity, IdentityRecordedAtUtc = DateTime.UtcNow, PayslipNumber = "QA-OWNERSHIP-" + Guid.NewGuid().ToString("N"), PeriodLabel = "QA snapshot ownership", GrossAmount = 1000m, NetAmount = 1000m };
            var own = Row(academy, moved.Id, teacher, "Original recorded teacher"); own.SessionsCovered = 7;
            var borrowed = Row(academy, current.Id, otherTeacher.Id, "Other recorded teacher");
            // Cross-tenant probe is intentionally a corrupt synthetic linkage, not production data.
            var foreign = Row(foreignAcademy, current.Id, teacher, "Foreign synthetic record");
            db.AddRange(own, borrowed, foreign); await db.SaveChangesAsync(); ownId = own.Id; borrowedId = borrowed.Id; foreignId = foreign.Id;
        }
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var beforeRead = await State();
        using (var read = await Send(HttpMethod.Get, "/api/teacher/payments?year=" + DateTime.UtcNow.Year + "&month=" + DateTime.UtcNow.Month, credential: teacherToken))
        {
            RequireFinanceStatus(read, HttpStatusCode.OK, "teacher immutable ownership");
            var response = await read.Content.ReadFromJsonAsync<TeacherPaymentSummary>() ?? throw new InvalidOperationException("No teacher response.");
            using var wire = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
            if (!response.Payslips.Any(x => x.Id == ownId && x.WorkerName == "Original recorded teacher") || response.Payslips.Any(x => x.Id == borrowedId || x.Id == foreignId) || response.Payslips.Any(x => x.IdentitySource != "PayoutSnapshot")) throw new InvalidOperationException("Mutable link/foreign/unverified payroll disclosed.");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            foreach (var item in response.Payslips)
            {
                var stored = await db.PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == item.Id);
                var json = wire.RootElement.GetProperty("payslips").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == item.Id);
                var identity = json.GetProperty("identityRecordedAtUtc").GetString();
                if (item.SessionsCovered != stored.SessionsCovered || item.IdentityRecordedAtUtc?.Ticks != stored.IdentityRecordedAtUtc?.Ticks ||
                    item.IdentityRecordedAtUtc?.Kind != DateTimeKind.Utc || identity is null || !identity.EndsWith("Z", StringComparison.Ordinal) ||
                    item.GrossAmountExact != stored.GrossAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) ||
                    item.DeductionsExact != stored.Deductions.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) ||
                    item.NetAmountExact != stored.NetAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
                    throw new InvalidOperationException("Teacher payslip lost recorded cycle, UTC identity provenance or exact stored money.");
            }
            if (response.Payslips.Single(x => x.Id == ownId).SessionsCovered != 7 || !response.Payslips.Any(x => x.SessionsCovered == null))
                throw new InvalidOperationException("Teacher cycle proof must include both recorded session and monthly rows.");
            var expected = await db.PayrollPayouts.AsNoTracking().Where(x => x.AcademyId == academy && x.TeacherIdSnapshot == teacher && x.WorkerTypeSnapshot == "Teacher" && x.IdentityRecordedAtUtc != null && x.WorkerNameSnapshot != null && ((x.Status == "Paid" && x.PaidAtUtc != null && x.PaidAtUtc.Value.Year == DateTime.UtcNow.Year && x.PaidAtUtc.Value.Month == DateTime.UtcNow.Month) || (x.Status == "NoCashDue" && x.CreatedAtUtc.Year == DateTime.UtcNow.Year && x.CreatedAtUtc.Month == DateTime.UtcNow.Month))).Select(x => x.Id).ToArrayAsync();
            if (!expected.Order().SequenceEqual(response.Payslips.Select(x => x.Id).Order())) throw new InvalidOperationException("Teacher read omitted authorized history or included another worker.");
            if (beforeRead != await State()) throw new InvalidOperationException("Teacher read wrote financial data.");
            guarded++; Console.WriteLine("BATCH1 SECURITY PASS: teacher snapshot-owned inactive-profile history included; borrowed, foreign and legacy excluded; exact authorized SQL set/no writes; recorded session/monthly cycle, unchanged UTC identity ticks/Z and exact money verified.");
        }
        if (guarded != 18) throw new InvalidOperationException("Batch1 policy count incomplete: " + guarded);
        Console.WriteLine("BATCH1 POLICIES PASS:18 native SQL/API controls, FP1 positive reduction, FP2 two statements, FP3 terminal paths, teacher snapshot privacy, no429. Qualified review/physical devices NOT RUN.");
    }
}
