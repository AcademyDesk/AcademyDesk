using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademyDesk.Api.Controllers;
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
    private static async Task VerifyPentaPaymentCollectionAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy, Guid student)
    {
        const string adminEmail = "qa-admin-a@example.invalid";
        const string retiringEmail = "qa-penta-payment-retiring@example.invalid";
        const string intent = "ApproveRecordReceivedPayment";
        const string executionAudit = "PentaPaymentRecorded";
        var flags = new Dictionary<string, string?> { ["Penta:Enabled"] = "true", ["Penta:PaymentCollectionEnabled"] = "true" };
        var root = $"/api/academies/{academy:D}";
        var conversations = root + "/penta/payment-collection/conversations/";
        var manual = root + "/payments";
        var clock = new OnboardingDraftClock();
        using var host = new QaApiFactory(manifest, flags, isolatedClock: clock);
        using var admin = await SignIn(host, adminEmail);
        using var disabled = await SignIn(factory, adminEmail);
        using var teacher = await SignIn(host, "qa-teacher-a@example.invalid");
        using var foreignAdmin = await SignIn(host, "qa-admin-b@example.invalid");
        using var owner = await SignIn(host, "qa-penta-payroll-owner@example.invalid");
        using var anonymous = host.CreateClient();
        var cases = 0;

        var other = await WithDb(db => db.Students.Where(x => x.AcademyId != academy)
            .Select(x => new { x.Id, x.AcademyId }).FirstAsync());
        var foreign = await Seed("FOREIGN", 0m, other.AcademyId, other.Id);
        var cancelled = await Seed("CANCELLED", 0m, status: "Cancelled");
        var target = await Seed("MAIN", 200m, addNoise: true);
        var start = Guid.NewGuid();
        var startRoute = conversations + start.ToString("D");
        await Status(disabled.PutAsync(startRoute, null), 404);
        Pass("default-off start");
        await Status(anonymous.PutAsync(startRoute, null), 401);
        Pass("anonymous start denied");
        await Status(teacher.PutAsync(startRoute, null), 403);
        Pass("teacher start denied");
        await Status(foreignAdmin.PutAsync(startRoute, null), 403);
        Pass("cross-tenant start denied");
        var session = await Start(admin, start, 201);
        if (session != await Start(admin, start, 200)) throw new InvalidOperationException("Start replay opened a different conversation.");
        Pass("scoped conversation starts once");

        var route = Draft(session);
        await Status(owner.PutAsJsonAsync(route, Input(target, "200.00")), 404);
        Pass("another actor cannot borrow the conversation");
        await Status(admin.PutAsJsonAsync(route, Input(foreign, "1.00")), 404);
        Pass("foreign invoice hidden in owned conversation");
        await Status(admin.PutAsJsonAsync(route, Input(target, "0.001")), 400);
        Pass("PENTA refuses sub-cent amount");
        await Status(admin.PostAsJsonAsync(manual, new { invoiceId = target, amount = .001m }), 400);
        Pass("Manual refuses sub-cent amount");
        await Status(admin.PutAsJsonAsync(route, Input(cancelled, "1.00")), 400);
        Pass("cancelled invoice refused");
        await Status(admin.PutAsJsonAsync(route, new { contextVersion = 0, payload = Payload(target, "1.00"), role = "Owner" }), 400);
        Pass("forged authority rejected");

        var before = await Ledger(target);
        var draft = await DraftResponse(admin.PutAsJsonAsync(route, Input(target, "200.00")), 201);
        if (draft.Status != "Prepared" || draft.Plan is not { Collected: 600m, Adjustments: 200m, Amount: 200m,
                BalanceBefore: 200m, BalanceAfter: 0m, Currency: "USD", Method: "Cash", Reference: null } ||
            before != await Ledger(target)) throw new InvalidOperationException("Reviewed adjusted balance or preview side effects differ.");
        Pass("review uses adjusted same-currency balance");
        if (draft != await DraftResponse(admin.PutAsJsonAsync(route, Input(target, "200.00")), 200))
            throw new InvalidOperationException("Prepare replay changed the draft.");
        await Status(admin.PutAsJsonAsync(route, Input(target, "199.99")), 409);
        Pass("same draft ID cannot change the exact amount");
        await Status(admin.PostAsJsonAsync(route + "/execute", Execution(draft, Guid.NewGuid())), 409);
        await Status(admin.PostAsJsonAsync(route + "/approve", new { draft.Revision, payloadHash = new string('0', 64), intent }), 409);
        await Status(admin.PostAsJsonAsync(route + "/approve", new { draft.Revision, draft.PayloadHash, intent = "ChargePayment" }), 400);
        if (before != await Ledger(target)) throw new InvalidOperationException("Invalid approval or execute changed the ledger.");
        Pass("unapproved execution, altered hash and wrong intent refused");

        var approvalFault = new EnrollmentSharedAuditFault("PentaPaymentApproved");
        using (var failing = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: approvalFault))
        using (var caller = await SignIn(failing, adminEmail))
            await Status(caller.PostAsJsonAsync(route + "/approve", Approval(draft)), 503);
        if (approvalFault.Hits != 1 || (await DraftResponse(admin.GetAsync(route), 200)).Status != "Prepared")
            throw new InvalidOperationException("Failed approval audit did not roll back.");
        Pass("approval audit failure rolls back");

        var approved = await Approve(admin, route, draft);
        if (approved.ApprovalId is null || approved.Status != "Authorized" || approved != await Approve(admin, route, draft) ||
            before != await Ledger(target)) throw new InvalidOperationException("Approval must be repeatable without recording a payment.");
        await Status(admin.PostAsJsonAsync(route + "/execute", Execution(approved, Guid.NewGuid())), 409);
        Pass("exact approval does not charge or record payment");

        var executionFault = new EnrollmentSharedAuditFault(executionAudit);
        using (var failing = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: executionFault))
        using (var caller = await SignIn(failing, adminEmail))
            await Status(caller.PostAsJsonAsync(route + "/execute", Execution(approved)), 503);
        if (executionFault.Hits != 1 || before != await Ledger(target) ||
            (await DraftResponse(admin.GetAsync(route), 200)).Status != "Authorized")
            throw new InvalidOperationException("Required audit failure did not roll back payment and receipt.");
        Pass("execution audit failure rolls back invoice and payment");

        var recorded = await DraftResponse(admin.PostAsJsonAsync(route + "/execute", Execution(approved)), 201);
        var receipt = recorded.Receipt;
        var final = await Ledger(target);
        if (recorded.Status != "Succeeded" || receipt is not { Amount: 200m, Currency: "USD", RemainingBalance: 0m } ||
            receipt.InvoiceId != target || final != ("Paid", 800m, before.Rows + 1) ||
            !await WithDb(db => db.AuditLogs.AnyAsync(x => x.Id == receipt.AuditId && x.AcademyId == academy &&
                x.Action == executionAudit && x.MetadataJson == null)))
            throw new InvalidOperationException("Receipt or audited adjusted ledger is inconsistent.");
        Pass("exact payment and audit commit together");
        if (recorded != await DraftResponse(admin.GetAsync(route), 200) ||
            recorded != await DraftResponse(admin.PostAsJsonAsync(route + "/execute", Execution(approved)), 200) ||
            final != await Ledger(target)) throw new InvalidOperationException("Receipt recovery repeated the payment.");
        Pass("GET recovery and exact execute replay return the same receipt");

        var staleInvoice = await Seed("STALE", 0m);
        var staleRoute = Draft(session);
        var stale = await Approve(admin, staleRoute, await DraftResponse(admin.PutAsJsonAsync(staleRoute, Input(staleInvoice, "200.00")), 201));
        await Status(admin.PostAsJsonAsync(manual, new { invoiceId = staleInvoice, amount = 50m }), 201);
        await Status(admin.PostAsJsonAsync(staleRoute + "/execute", Execution(stale)), 409, "The invoice changed after review.");
        if (await Ledger(staleInvoice) != ("PartiallyPaid", 650m, 2))
            throw new InvalidOperationException("Stale review changed a ledger even though enough money remained.");
        Pass("changed Manual balance requires a fresh review");

        var duplicateInvoice = await Seed("DUPLICATE", 0m);
        var duplicateRoute = Draft(session);
        var duplicate = await Approve(admin, duplicateRoute,
            await DraftResponse(admin.PutAsJsonAsync(duplicateRoute, Input(duplicateInvoice, "400.00")), 201));
        var retries = await Task.WhenAll(
            admin.PostAsJsonAsync(duplicateRoute + "/execute", Execution(duplicate)),
            admin.PostAsJsonAsync(duplicateRoute + "/execute", Execution(duplicate)));
        var ordered = retries.OrderBy(x => (int)x.StatusCode).ToArray();
        var recovered = await DraftResponse(Task.FromResult(ordered[0]), 200);
        var committed = await DraftResponse(Task.FromResult(ordered[1]), 201);
        if (recovered.Receipt is null || recovered != committed || await Ledger(duplicateInvoice) != ("Paid", 1000m, 2))
            throw new InvalidOperationException("Concurrent exact retries did not share one receipt.");
        Pass("concurrent exact retries record one payment and recover one receipt");

        var modules = await WithDb(db => db.Academies.Where(x => x.Id == academy).Select(x => x.EnabledModulesJson).SingleAsync());
        await WithDb(db => db.Academies.Where(x => x.Id == academy)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.EnabledModulesJson, """["Core"]""")));
        await Status(admin.GetAsync(route), 403);
        await WithDb(db => db.Academies.Where(x => x.Id == academy)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.EnabledModulesJson, modules)));
        Pass("loss of Finance module also blocks receipt readback");

        var expiryInvoice = await Seed("EXPIRED", 0m);
        var expiryRoute = Draft(session);
        var expiring = await Approve(admin, expiryRoute,
            await DraftResponse(admin.PutAsJsonAsync(expiryRoute, Input(expiryInvoice, "200.00")), 201));
        clock.Value = clock.Value.AddMinutes(11);
        await Status(admin.PostAsJsonAsync(expiryRoute + "/execute", Execution(expiring)), 410);
        if ((await DraftResponse(admin.GetAsync(route), 200)).Receipt != receipt)
            throw new InvalidOperationException("A committed receipt vanished when draft approval expired.");
        Pass("expired approval is blocked and committed receipt remains recoverable");

        var retiring = await CreateAccessActorAsync(factory, retiringEmail, "AcademyAdmin", academy);
        using var retiringClient = await SignIn(host, retiringEmail);
        var retiringSession = await Start(retiringClient, Guid.NewGuid(), 201);
        var retiringRoute = Draft(retiringSession);
        var retiringInvoice = await Seed("REVOKED", 0m);
        var retiringDraft = await Approve(retiringClient, retiringRoute,
            await DraftResponse(retiringClient.PutAsJsonAsync(retiringRoute, Input(retiringInvoice, "400.00")), 201));
        var revoke = new FinanceReadBeforeLock(async () =>
        {
            using var scope = factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(retiring.ToString("D")))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded)
                throw new InvalidOperationException("Could not revoke synthetic collector.");
        });
        using (var revokedHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: revoke))
        using (var revoked = await SignIn(revokedHost, retiringEmail))
            await Status(revoked.PostAsJsonAsync(retiringRoute + "/execute", Execution(retiringDraft)), 403);
        if (revoke.Hits != 1 || await Ledger(retiringInvoice) != ("PartiallyPaid", 600m, 1))
            throw new InvalidOperationException("Role revocation did not stop the payment.");
        Pass("role revoked after entry stops execution");

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var invoice = await Seed($"RACE-{attempt}", 200m);
            var race = Draft(session);
            var draftForRace = await Approve(admin, race, await DraftResponse(admin.PutAsJsonAsync(race, Input(invoice, "200.00")), 201));
            var auditCount = await WithDb(db => db.AuditLogs.CountAsync(x => x.Action == executionAudit));
            var responses = await Task.WhenAll(
                admin.PostAsJsonAsync(race + "/execute", Execution(draftForRace)),
                admin.PostAsJsonAsync(manual, new { invoiceId = invoice, amount = 200m }));
            using var pentaResponse = responses[0];
            using var manualResponse = responses[1];
            var pentaWon = pentaResponse.StatusCode == HttpStatusCode.Created;
            if (pentaWon)
                await Status(Task.FromResult(manualResponse), 400, "Payment exceeds the invoice balance.");
            else
            {
                await Status(Task.FromResult(pentaResponse), 409, "The invoice changed after review.");
                await Status(Task.FromResult(manualResponse), 201);
            }
            if (await Ledger(invoice) != ("Paid", 800m, 2) ||
                await WithDb(db => db.AuditLogs.CountAsync(x => x.Action == executionAudit)) != auditCount + (pentaWon ? 1 : 0))
                throw new InvalidOperationException("Mixed Manual and PENTA race produced an extra payment or audit.");
            Pass($"mixed collection race {attempt} preserved the exact adjusted total");
        }

        if (cases != 29) throw new InvalidOperationException("Unexpected protected payment case count: " + cases);
        Console.WriteLine($"FINANCE PENTA PAYMENT SUMMARY {cases} native HTTP/SQL checks PASS, including 5/5 mixed collection races.");

        string Draft(MiniSessionView current) => conversations + current.ConversationId.ToString("D") + "/drafts/" + Guid.NewGuid().ToString("D");
        static object Payload(Guid id, string amount) => new
        {
            tool_key = "AcademyDesk.Payment.Record",
            tool_revision = 1,
            fields = new Dictionary<string, object>
            {
                ["InvoiceId"] = new { kind = "reference", value = id.ToString("D") },
                ["Amount"] = new { kind = "decimal", value = amount },
                ["Method"] = new { kind = "text", value = "Cash" }
            }
        };
        static object Input(Guid id, string amount) => new { contextVersion = 0, payload = Payload(id, amount) };
        static object Approval(PentaPaymentDraft value) => new { value.Revision, value.PayloadHash, intent };
        static object Execution(PentaPaymentDraft value, Guid? overrideId = null) =>
            new { value.Revision, value.PayloadHash, approvalId = overrideId ?? value.ApprovalId };
        static Task<PentaPaymentDraft> Approve(HttpClient caller, string path, PentaPaymentDraft value) =>
            DraftResponse(caller.PostAsJsonAsync(path + "/approve", Approval(value)), 200);

        async Task<MiniSessionView> Start(HttpClient caller, Guid key, int expected)
        {
            using var response = await caller.PutAsync(conversations + key.ToString("D"), null);
            RequireFinanceStatus(response, (HttpStatusCode)expected, "payment collection conversation");
            return (await response.Content.ReadFromJsonAsync<MiniSessionView>())!;
        }
        static async Task<PentaPaymentDraft> DraftResponse(Task<HttpResponseMessage> pending, int expected)
        {
            using var response = await pending;
            RequireFinanceStatus(response, (HttpStatusCode)expected, "protected payment draft");
            if (response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Payment draft must be no-store.");
            return (await response.Content.ReadFromJsonAsync<PentaPaymentDraft>())!;
        }
        static async Task Status(Task<HttpResponseMessage> pending, int expected, string? message = null)
        {
            using var response = await pending;
            RequireFinanceStatus(response, (HttpStatusCode)expected, "protected payment guard");
            if (message is not null && !(await response.Content.ReadAsStringAsync()).Contains(message, StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected protected payment response message.");
        }
        static async Task<HttpClient> SignIn(QaApiFactory api, string email)
        {
            var caller = api.CreateClient();
            caller.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(caller, email, "Synthetic!39Ab"));
            return caller;
        }
        async Task<T> WithDb<T>(Func<AcademyDeskDbContext, Task<T>> action)
        {
            using var scope = factory.Services.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }
        Task<Guid> Seed(string code, decimal adjusted, Guid? tenant = null, Guid? learner = null, string status = "PartiallyPaid", bool addNoise = false) =>
            WithDb(async db =>
            {
                var invoice = new Invoice { AcademyId = tenant ?? academy, StudentId = learner ?? student, InvoiceNumber = "QA-PENTA-PAY-" + code,
                    TotalAmount = 1000m, AdjustedAmount = adjusted, Currency = "USD", Status = status };
                db.Add(invoice);
                db.Add(new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 600m, Status = "Reconciled", Currency = "USD" });
                if (addNoise)
                {
                    db.Add(new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 90m, Status = "Completed", Currency = "EUR" });
                    db.Add(new Payment { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = 50m, Status = "Voided", Currency = "USD" });
                }
                await db.SaveChangesAsync();
                return invoice.Id;
            });
        Task<(string Status, decimal Collected, int Rows)> Ledger(Guid invoice) => WithDb(async db =>
        {
            var status = await db.Invoices.Where(x => x.Id == invoice).Select(x => x.Status).SingleAsync();
            var payments = await db.Payments.Where(x => x.InvoiceId == invoice).ToArrayAsync();
            return (status, payments.Where(x => x.Currency == "USD" && x.Status is ("Completed" or "Reconciled")).Sum(x => x.Amount), payments.Length);
        });
        void Pass(string label)
        {
            cases++;
            Console.WriteLine($"FINANCE PENTA PAYMENT CASE {label} PASS");
        }
    }
}
