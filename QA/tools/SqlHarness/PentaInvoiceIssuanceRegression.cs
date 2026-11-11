using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InvoiceDraft = AcademyDesk.Api.Intelligence.Penta.PentaFinanceDraft<AcademyDesk.Api.Infrastructure.InvoiceIssuancePlan, AcademyDesk.Api.Intelligence.Penta.PentaInvoiceReceipt>;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaInvoiceIssuanceAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy, Guid student)
    {
        const string adminEmail = "qa-penta-invoice-admin@example.invalid";
        const string intent = "ApproveIssueInvoice";
        const string audit = "PentaInvoiceIssued";
        var flags = new Dictionary<string, string?>
        {
            ["Penta:Enabled"] = "true", ["Penta:InvoiceIssuanceEnabled"] = "true", ["Penta:PaymentCollectionEnabled"] = "true"
        };
        var academyPath = $"/api/academies/{academy:D}";
        var root = academyPath + "/penta/invoice-issuance/conversations/";
        var manual = academyPath + "/invoices";
        var clock = new OnboardingDraftClock();
        var today = DateOnly.FromDateTime(clock.Value.UtcDateTime);
        var due = today.AddDays(80);
        var fee = new FeePlan { AcademyId = academy, Name = "QA invoice review", Amount = 120.01m, Currency = "USD" };
        var inactive = new FeePlan { AcademyId = academy, Name = "QA inactive", Amount = 120.01m, IsActive = false };
        var foreign = await WithDb(db => db.Students.Where(x => x.AcademyId != academy).Select(x => x.Id).FirstAsync());
        await WithDb(db => { db.AddRange(fee, inactive); return db.SaveChangesAsync(); });
        await CreateAccessActorAsync(factory, adminEmail, "AcademyAdmin", academy);
        using var host = new QaApiFactory(manifest, flags, isolatedClock: clock);
        using var admin = await SignIn(host, adminEmail);
        using var disabled = await SignIn(factory, adminEmail);
        using var foreignAdmin = await SignIn(host, "qa-admin-b@example.invalid");
        using var teacher = await SignIn(host, "qa-teacher-a@example.invalid");
        using var owner = await SignIn(host, "qa-penta-payroll-owner@example.invalid");
        using var anonymous = host.CreateClient();
        var count = 0;
        var optionsPath = academyPath + "/penta/invoice-issuance/options";
        var start = Guid.NewGuid();
        var startPath = root + start.ToString("D");

        await Expect(disabled.PutAsync(startPath, null), 404);
        Pass("default off");
        await Expect(anonymous.PutAsync(startPath, null), 401);
        Pass("authentication required");
        await Expect(foreignAdmin.PutAsync(startPath, null), 403);
        await Expect(teacher.PutAsync(startPath, null), 403);
        Pass("foreign academy and teacher refused");
        await Expect(disabled.PostAsJsonAsync(optionsPath, new { kind = "student", query = "Isolated" }), 404);
        await Expect(foreignAdmin.PostAsJsonAsync(optionsPath, new { kind = "student", query = "Isolated" }), 403);
        await Expect(teacher.PostAsJsonAsync(optionsPath, new { kind = "student", query = "Isolated" }), 403);
        await Expect(admin.PostAsJsonAsync(optionsPath, new { kind = "student", query = "I" }), 400);
        Pass("lookup enforces enablement, current tenant, role and minimum query");

        var lookupFault = new EnrollmentSharedAuditFault("PentaInvoiceOptionsRead");
        using (var broken = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: lookupFault))
        using (var caller = await SignIn(broken, adminEmail))
            await Expect(caller.PostAsJsonAsync(optionsPath, new { kind = "feePlan", query = "QA invoice" }), 503);
        if (lookupFault.Hits != 1) throw new InvalidOperationException("Required lookup audit was bypassed.");
        Pass("lookup withholds results when audit fails");
        var session = await Session(admin.PutAsync(startPath, null), 201);
        if (session != await Session(admin.PutAsync(startPath, null), 200))
            throw new InvalidOperationException("Starting again returned a different invoice conversation.");
        Pass("one conversation per start request");

        var paymentSession = await Session(admin.PutAsync(academyPath + $"/penta/payment-collection/conversations/{start:D}", null), 201);
        if (paymentSession.ConversationId == session.ConversationId) throw new InvalidOperationException("Finance conversation purposes collided.");
        var path = Draft();
        await Expect(admin.PutAsJsonAsync(Draft(paymentSession), Input(student, fee.Id, null, due)), 404);
        await Expect(owner.PutAsJsonAsync(path, Input(student, fee.Id, null, due)), 404);
        Pass("payment conversation and other actors cannot adopt the invoice draft");
        await Expect(admin.PutAsJsonAsync(path, Input(foreign, fee.Id, null, due)), 400);
        await Expect(admin.PutAsJsonAsync(path, Input(student, inactive.Id, null, due)), 400);
        Pass("foreign student and inactive plan refused");
        await Expect(admin.PutAsJsonAsync(path, Input(student, null, "0.001", due)), 400);
        await Expect(admin.PostAsJsonAsync(manual, new { studentId = student, amount = .001m, dueDate = due }), 400);
        Pass("both invoice paths refuse fractional cents");
        await Expect(admin.PutAsJsonAsync(path, Input(student, null, null, due)), 400);
        await Expect(admin.PutAsJsonAsync(path, new { contextVersion = 0, payload = Payload(student, fee.Id, null, due), role = "Owner" }), 400);
        Pass("missing terms and forged authority refused");

        var before = await Count();
        var reviewed = await Prepare(path, student, fee.Id, null, due);
        if (reviewed.Plan is not { AmountExact: "120.01", Currency: "USD", RequestedAmount: null } ||
            reviewed.Plan.StudentId != student || reviewed.Plan.FeePlanId != fee.Id || reviewed.Plan.DueDate != due || before != await Count())
            throw new InvalidOperationException("Fee-plan review differs from the intended invoice or wrote a ledger row.");
        if (reviewed != await Response(admin.PutAsJsonAsync(path, Input(student, fee.Id, null, due)), 200))
            throw new InvalidOperationException("Invoice prepare replay changed the draft.");
        await Expect(admin.PutAsJsonAsync(path, Input(student, fee.Id, "120.00", due)), 409);
        Pass("exact fee-plan review and bound prepare replay");

        await Expect(admin.PostAsJsonAsync(path + "/execute", Execute(reviewed, Guid.NewGuid())), 409);
        await Expect(admin.PostAsJsonAsync(path + "/approve", new { reviewed.Revision, payloadHash = new string('0', 64), intent }), 409);
        await Expect(admin.PostAsJsonAsync(path + "/approve", new { reviewed.Revision, reviewed.PayloadHash, intent = "ApproveRecordReceivedPayment" }), 400);
        Pass("no execution without exact invoice approval");

        var approvalFault = new EnrollmentSharedAuditFault("PentaInvoiceApproved");
        using (var broken = new QaApiFactory(manifest, flags, isolatedClock: clock, isolatedDomainInterceptor: approvalFault))
        using (var caller = await SignIn(broken, adminEmail))
            await Expect(caller.PostAsJsonAsync(path + "/approve", Approval(reviewed)), 503);
        if (approvalFault.Hits != 1 || (await Response(admin.GetAsync(path), 200)).Status != "Prepared")
            throw new InvalidOperationException("Invoice approval survived its audit failure.");
        Pass("approval audit failure rolled back");

        var approved = await Approve(path, reviewed);
        if (approved.ApprovalId is null || approved != await Approve(path, reviewed) || await Count() != before)
            throw new InvalidOperationException("Invoice approval created a ledger row or changed on retry.");
        Pass("approval is idempotent and has no ledger effect");

        var executionFault = new EnrollmentSharedAuditFault(audit);
        using (var broken = new QaApiFactory(manifest, flags, isolatedClock: clock, isolatedDomainInterceptor: executionFault))
        using (var caller = await SignIn(broken, adminEmail))
            await Expect(caller.PostAsJsonAsync(path + "/execute", Execute(approved)), 503);
        if (executionFault.Hits != 1 || await Count() != before || (await Response(admin.GetAsync(path), 200)).Status != "Authorized")
            throw new InvalidOperationException("Invoice or receipt survived its execution audit failure.");
        Pass("execution audit rolls back the invoice");

        clock.Value = clock.Value.AddSeconds(1);
        var responses = await Task.WhenAll(admin.PostAsJsonAsync(path + "/execute", Execute(approved)),
            admin.PostAsJsonAsync(path + "/execute", Execute(approved)));
        var ordered = responses.OrderBy(x => (int)x.StatusCode).ToArray();
        var replay = await Response(Task.FromResult(ordered[0]), 200);
        var issued = await Response(Task.FromResult(ordered[1]), 201);
        if (replay != issued || issued.Receipt is not { AmountExact: "120.01", Currency: "USD" } receipt ||
            receipt.DueDate != due || await Count() != (before.Invoices + 1, before.Audits + 1))
            throw new InvalidOperationException("Concurrent issuance did not commit one invoice and one receipt.");
        Pass("simultaneous execute calls share one audited receipt");

        var source = await WithDb(db => db.Invoices.AsNoTracking().SingleAsync(x => x.Id == receipt.InvoiceId && x.AcademyId == academy));
        if (source.InvoiceNumber != receipt.InvoiceNumber || source.StudentId != student || source.FeePlanId != fee.Id ||
            source.Status != "Issued" || source.TotalAmount != 120.01m || source.Currency != "USD" ||
            issued != await Response(admin.GetAsync(path), 200))
            throw new InvalidOperationException("Invoice recovery did not match the committed ledger.");
        await Expect(admin.PutAsJsonAsync(Draft(), Input(student, fee.Id, null, due)), 409, "A matching invoice already exists.");
        Pass("receipt matches the ledger and duplicate review is blocked");

        var stalePath = Draft();
        var stale = await Approve(stalePath, await Prepare(stalePath, student, fee.Id, null, due.AddDays(1)));
        await WithDb(db => db.FeePlans.Where(x => x.Id == fee.Id).ExecuteUpdateAsync(x => x.SetProperty(p => p.Amount, 121m)));
        await Expect(admin.PostAsJsonAsync(stalePath + "/execute", Execute(stale)), 409, "The invoice terms changed after review.");
        if (await Count() != (before.Invoices + 1, before.Audits + 1))
            throw new InvalidOperationException("Changing the fee plan created an invoice.");
        Pass("current fee-plan terms invalidate stale approval");

        var manualPath = Draft();
        var prior = await Approve(manualPath, await Prepare(manualPath, student, null, "27.01", due.AddDays(2)));
        using (var manualResponse = await admin.PostAsJsonAsync(manual, new { studentId = student, amount = 27.01m, dueDate = due.AddDays(2) }))
        {
            RequireFinanceStatus(manualResponse, HttpStatusCode.Created, "Manual one-time invoice");
            var actual = await manualResponse.Content.ReadFromJsonAsync<InvoiceSummary>();
            if (actual is not { Currency: "INR", TotalAmount: 27.01m } || actual.DueDate != prior.Plan.DueDate)
                throw new InvalidOperationException("Manual defaults diverged from the reviewed PENTA one-time invoice.");
        }
        await Expect(admin.PostAsJsonAsync(manualPath + "/execute", Execute(prior)), 409, "A matching invoice already exists.");
        Pass("Manual one-time invoice terms match and block a pending duplicate");

        var staleContextPath = Draft();
        await Expect(admin.PutAsJsonAsync(staleContextPath, new { contextVersion = 1, payload = Payload(student, fee.Id, null, due.AddDays(3)) }), 409);
        Pass("stale conversation version denied");

        for (var i = 0; i < 5; i++)
        {
            var invoiceDue = due.AddDays(10 + i);
            var left = Draft();
            var right = Draft();
            var leftDraft = await Approve(left, await Prepare(left, student, null, "1.01", invoiceDue));
            var rightDraft = await Approve(right, await Prepare(right, student, null, "1.01", invoiceDue));
            var snapshot = await Count();
            clock.Value = clock.Value.AddSeconds(1);
            var race = await Task.WhenAll(admin.PostAsJsonAsync(left + "/execute", Execute(leftDraft)),
                admin.PostAsJsonAsync(right + "/execute", Execute(rightDraft)));
            var byStatus = race.OrderBy(x => (int)x.StatusCode).ToArray();
            var winner = await Response(Task.FromResult(byStatus[0]), 201);
            await Expect(Task.FromResult(byStatus[1]), 409, "A matching invoice already exists.");
            if (winner.Receipt is not { AmountExact: "1.01", Currency: "INR" } ||
                await Count() != (snapshot.Invoices + 1, snapshot.Audits + 1))
                throw new InvalidOperationException("Independently approved drafts issued duplicate invoices.");
            Pass($"distinct-draft duplicate race {i + 1}");
        }

        var expiresPath = Draft();
        var expires = await Approve(expiresPath, await Prepare(expiresPath, student, null, "1.02", due.AddDays(60)));
        clock.Value = clock.Value.AddMinutes(11);
        await Expect(admin.PostAsJsonAsync(expiresPath + "/execute", Execute(expires)), 410);
        if (issued != await Response(admin.GetAsync(path), 200))
            throw new InvalidOperationException("Expiry removed an already committed receipt.");
        Pass("expired approval blocked and committed receipt recoverable");

        if (count != 26) throw new InvalidOperationException("Unexpected invoice issuance check count: " + count);
        Console.WriteLine($"FINANCE PENTA INVOICE SUMMARY {count} native HTTP/SQL checks PASS, including 5/5 duplicate invoice races.");

        string Draft(MiniSessionView? from = null) => root + (from ?? session).ConversationId.ToString("D") + "/drafts/" + Guid.NewGuid().ToString("D");
        static object Payload(Guid learner, Guid? feePlan, string? amount, DateOnly date)
        {
            var fields = new Dictionary<string, object>
            {
                ["StudentId"] = new { kind = "reference", value = learner.ToString("D") },
                ["DueDate"] = new { kind = "date", value = FormattableString.Invariant($"{date:yyyy-MM-dd}") }
            };
            if (feePlan is { } plan) fields["FeePlanId"] = new { kind = "reference", value = plan.ToString("D") };
            if (amount is not null) fields["Amount"] = new { kind = "decimal", value = amount };
            return new { tool_key = "AcademyDesk.Invoice.Issue", tool_revision = 1, fields };
        }
        static object Input(Guid learner, Guid? plan, string? amount, DateOnly date) => new { contextVersion = 0, payload = Payload(learner, plan, amount, date) };
        static object Approval(InvoiceDraft draft) => new { draft.Revision, draft.PayloadHash, intent };
        static object Execute(InvoiceDraft draft, Guid? approval = null) => new { draft.Revision, draft.PayloadHash, approvalId = approval ?? draft.ApprovalId };
        Task<InvoiceDraft> Prepare(string url, Guid learner, Guid? plan, string? amount, DateOnly date) =>
            Response(admin.PutAsJsonAsync(url, Input(learner, plan, amount, date)), 201);
        Task<InvoiceDraft> Approve(string url, InvoiceDraft draft) =>
            Response(admin.PostAsJsonAsync(url + "/approve", Approval(draft)), 200);
        static async Task<InvoiceDraft> Response(Task<HttpResponseMessage> task, int code)
        {
            using var response = await task;
            RequireFinanceStatus(response, (HttpStatusCode)code, "invoice issuance draft");
            if (response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Invoice issuance response may be cached.");
            return (await response.Content.ReadFromJsonAsync<InvoiceDraft>())!;
        }
        static async Task<MiniSessionView> Session(Task<HttpResponseMessage> task, int code)
        {
            using var response = await task;
            RequireFinanceStatus(response, (HttpStatusCode)code, "invoice issuance conversation");
            return (await response.Content.ReadFromJsonAsync<MiniSessionView>())!;
        }
        static async Task Expect(Task<HttpResponseMessage> task, int code, string? message = null)
        {
            using var response = await task;
            RequireFinanceStatus(response, (HttpStatusCode)code, "invoice issuance refusal");
            if (message is not null && !(await response.Content.ReadAsStringAsync()).Contains(message, StringComparison.Ordinal))
                throw new InvalidOperationException("Invoice issuance response used an unexpected refusal message.");
        }
        static async Task<HttpClient> SignIn(QaApiFactory api, string email)
        {
            var caller = api.CreateClient();
            caller.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(caller, email, "Synthetic!39Ab"));
            return caller;
        }
        Task<(int Invoices, int Audits)> Count() => WithDb(async db =>
            (await db.Invoices.CountAsync(x => x.AcademyId == academy), await db.AuditLogs.CountAsync(x => x.AcademyId == academy && x.Action == audit)));
        async Task<T> WithDb<T>(Func<AcademyDeskDbContext, Task<T>> action)
        {
            using var scope = factory.Services.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }
        void Pass(string label)
        {
            count++;
            Console.WriteLine($"FINANCE PENTA INVOICE CASE {label} PASS");
        }
    }
}
