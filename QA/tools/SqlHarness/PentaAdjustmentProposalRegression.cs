using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AdjustmentDraft = AcademyDesk.Api.Intelligence.Penta.PentaFinanceDraft<AcademyDesk.Api.Intelligence.Penta.PentaAdjustmentPlan, AcademyDesk.Api.Intelligence.Penta.PentaAdjustmentReceipt>;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaAdjustmentProposalAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy, Guid student)
    {
        const string email = "qa-penta-adjustment-admin@example.invalid";
        const string audit = "PentaInvoiceAdjustmentSubmitted";
        const string intent = "ApproveSubmitInvoiceAdjustment";
        const string reason = "QA private adjustment reason";
        var flags = new Dictionary<string, string?> {
            ["Penta:Enabled"] = "true", ["Penta:FinanceReadEnabled"] = "true", ["Penta:InvoiceAdjustmentEnabled"] = "true" };
        var root = $"/api/academies/{academy:D}";
        var conversations = root + "/penta/invoice-adjustments/conversations/";
        var key = Guid.NewGuid();
        var start = conversations + key.ToString("D");
        var count = 0;
        var chat = root + "/penta/chat/finance";
        await CreateAccessActorAsync(factory, email, "AcademyAdmin", academy);
        using var host = new QaApiFactory(manifest, flags);
        using var admin = await SignIn(host, email);
        using var disabled = await SignIn(factory, email);
        using var foreign = await SignIn(host, "qa-admin-b@example.invalid");
        using var teacher = await SignIn(host, "qa-teacher-a@example.invalid");
        using var anonymous = host.CreateClient();

        await Expect(disabled.PutAsync(start, null), 404);
        await Expect(anonymous.PutAsync(start, null), 401);
        Pass("feature and authentication gates");
        await Expect(teacher.PutAsync(start, null), 403);
        await Expect(foreign.PutAsync(start, null), 403);
        await Expect(teacher.GetAsync(chat), 403);
        await Expect(foreign.GetAsync(chat), 403);
        Pass("teacher and cross-tenant access denied");

        var modules = await WithDb(db => db.Academies.Where(x => x.Id == academy).Select(x => x.EnabledModulesJson).SingleAsync());
        await Modules("""["Core","Finance"]""");
        await Expect(admin.PutAsync(start, null), 403);
        using var basic = await admin.GetAsync(chat);
        if ((await basic.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("invoiceAdjustment").GetBoolean())
            throw new InvalidOperationException("Finance chat exposed adjustment without FinanceControls.");
        await Modules(modules);
        using var entitled = await admin.GetAsync(chat);
        if (!(await entitled.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("invoiceAdjustment").GetBoolean())
            throw new InvalidOperationException("Finance chat did not expose the enabled adjustment action.");
        Pass("FinanceControls is required");

        using var started = await admin.PutAsync(start, null);
        RequireFinanceStatus(started, HttpStatusCode.Created, "adjustment proposal start");
        var session = (await started.Content.ReadFromJsonAsync<MiniSessionView>())!;
        var target = await Seed("MAIN", includeOtherPayments: true);
        var cancelled = await Seed("CANCELLED", status: "Cancelled");
        var path = Draft();
        await Expect(admin.PutAsJsonAsync(path, Input(Guid.NewGuid(), "Discount", "100.00")), 404);
        await Expect(admin.PutAsJsonAsync(path, Input(cancelled, "Discount", "100.00")), 400);
        Pass("missing and cancelled invoices refused");
        await Expect(admin.PutAsJsonAsync(path, Input(target, "Refund", "100.00")), 400, "dedicated reviewed workflows");
        await Expect(admin.PutAsJsonAsync(path, Input(target, "CreditNote", "100.00")), 400, "dedicated reviewed workflows");
        await Expect(admin.PutAsJsonAsync(path, Input(target, "Discount", "0.001")), 400);
        await Expect(admin.PutAsJsonAsync(path, Input(target, "Discount", "200.01")), 400, "exceeds the remaining invoice balance");
        Pass("legacy documents, fractional cents and excess reductions refused");

        var original = await Snapshot(target);
        var preview = await Prepare(path, target, "Discount", "200.00");
        if (preview.Plan is not { Currency: "USD", AmountExact: "200.00", CurrentBalanceExact: "200.00",
                BalanceIfApprovedExact: "0.00", Reason: reason } || original != await Snapshot(target))
            throw new InvalidOperationException("Review changed the invoice or did not show the same-currency balance.");
        Pass("exact projected balance leaves the ledger untouched");
        await Expect(admin.PutAsJsonAsync(path, Input(target, "Discount", "199.99")), 409);
        await Expect(admin.PostAsJsonAsync(path + "/execute", Execution(preview, Guid.NewGuid())), 409);
        await Expect(admin.PostAsJsonAsync(path + "/approve", new { preview.Revision, preview.PayloadHash, intent = "ApproveIssueInvoice" }), 400);
        Pass("changed draft and unrelated authorization refused");

        var authorized = await Authorize(path, preview);
        if (authorized.Status != "Authorized" || authorized.ApprovalId is null || original != await Snapshot(target))
            throw new InvalidOperationException("Submission authorization altered the ledger.");
        Pass("authorization has no financial effect");

        var fault = new EnrollmentSharedAuditFault(audit);
        using (var failing = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault))
        using (var caller = await SignIn(failing, email))
            await Expect(caller.PostAsJsonAsync(path + "/execute", Execution(authorized)), 503);
        if (fault.Hits != 1 || original != await Snapshot(target) || (await Result(admin.GetAsync(path), 200)).Status != "Authorized")
            throw new InvalidOperationException("Proposal survived a required audit failure.");
        Pass("required execution audit rolls back the proposal");

        var submitted = await Result(admin.PostAsJsonAsync(path + "/execute", Execution(authorized)), 201);
        if (submitted.Receipt is not { StatusAtSubmission: "PendingApproval", AmountExact: "200.00" } receipt ||
            await Snapshot(target) != (original.Adjusted, original.Status, 1) ||
            !await WithDb(db => db.AuditLogs.AnyAsync(x => x.Id == receipt.AuditId && x.Action == audit && x.MetadataJson == null)))
            throw new InvalidOperationException("Proposal receipt was not pending or changed financial balances.");
        Pass("submission stores one pending proposal and a payload-free audit");
        if (submitted != await Result(admin.PostAsJsonAsync(path + "/execute", Execution(authorized)), 200) ||
            submitted != await Result(admin.GetAsync(path), 200))
            throw new InvalidOperationException("Retry did not recover the original proposal receipt.");
        await Expect(admin.PutAsJsonAsync(Draft(), Input(target, "discount", "200.00")), 409, "matching adjustment is already pending");
        Pass("receipt retry and equivalent pending duplicate refused");

        await Expect(admin.PatchAsJsonAsync(root + $"/finance-adjustments/{receipt.AdjustmentId:D}/approval", new { approve = true }), 200);
        if (await Snapshot(target) != (400m, "Paid", 1) || submitted != await Result(admin.GetAsync(path), 200))
            throw new InvalidOperationException("Manual approval failed to apply the proposal or changed its historical receipt.");
        Pass("Manual financial approval applies the reduction separately");

        var changed = await Seed("CHANGED");
        var stale = Draft();
        var staleApproval = await Authorize(stale, await Prepare(stale, changed, "Scholarship", "100.00"));
        await Expect(admin.PostAsJsonAsync(root + "/payments", new { invoiceId = changed, amount = 50m }), 201);
        await Expect(admin.PostAsJsonAsync(stale + "/execute", Execution(staleApproval)), 409, "balance changed after review");
        if (await Snapshot(changed) != (200m, "PartiallyPaid", 0)) throw new InvalidOperationException("Stale proposal was saved.");
        Pass("new Manual payment invalidates the reviewed balance");

        await Modules("""["Core","Finance"]""");
        await Expect(admin.GetAsync(path), 403);
        await Modules(modules);
        Pass("module removal also blocks receipt recovery");

        for (var i = 1; i <= 5; i++)
        {
            var invoice = await Seed("RACE-" + i);
            var left = Draft();
            var right = Draft();
            var one = await Authorize(left, await Prepare(left, invoice, "Concession", "50.00"));
            var two = await Authorize(right, await Prepare(right, invoice, "Concession", "50.00"));
            var replies = await Task.WhenAll(admin.PostAsJsonAsync(left + "/execute", Execution(one)),
                admin.PostAsJsonAsync(right + "/execute", Execution(two)));
            var ordered = replies.OrderBy(x => (int)x.StatusCode).ToArray();
            var winner = await Result(Task.FromResult(ordered[0]), 201);
            await Expect(Task.FromResult(ordered[1]), 409, "matching adjustment is already pending");
            if (winner.Receipt is not { StatusAtSubmission: "PendingApproval" } ||
                await Snapshot(invoice) != (200m, "PartiallyPaid", 1)) throw new InvalidOperationException("Pending proposals duplicated or applied.");
            Pass($"duplicate proposal race {i}");
        }

        if (count != 19) throw new InvalidOperationException("Unexpected adjustment proposal case count: " + count);
        Console.WriteLine($"FINANCE PENTA ADJUSTMENT SUMMARY {count} native HTTP/SQL checks PASS, including 5/5 duplicate proposal races.");

        string Draft() => conversations + session.ConversationId.ToString("D") + "/drafts/" + Guid.NewGuid().ToString("D");
        static object Input(Guid invoice, string type, string amount) => new { contextVersion = 0, payload = new {
            tool_key = "AcademyDesk.InvoiceAdjustment.Propose", tool_revision = 1,
            fields = new Dictionary<string, object> {
                ["InvoiceId"] = new { kind = "reference", value = invoice.ToString("D") },
                ["Type"] = new { kind = "text", value = type },
                ["Amount"] = new { kind = "decimal", value = amount },
                ["Reason"] = new { kind = "text", value = reason }
            }
        } };
        static object Execution(AdjustmentDraft draft, Guid? id = null) =>
            new { draft.Revision, draft.PayloadHash, approvalId = id ?? draft.ApprovalId };
        Task<AdjustmentDraft> Prepare(string url, Guid invoice, string type, string amount) =>
            Result(admin.PutAsJsonAsync(url, Input(invoice, type, amount)), 201);
        Task<AdjustmentDraft> Authorize(string url, AdjustmentDraft draft) =>
            Result(admin.PostAsJsonAsync(url + "/approve", new { draft.Revision, draft.PayloadHash, intent }), 200);
        static async Task<AdjustmentDraft> Result(Task<HttpResponseMessage> task, int code)
        {
            using var response = await task;
            RequireFinanceStatus(response, (HttpStatusCode)code, "protected adjustment draft");
            if (response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Adjustment draft may be cached.");
            return (await response.Content.ReadFromJsonAsync<AdjustmentDraft>())!;
        }
        static async Task Expect(Task<HttpResponseMessage> task, int code, string? message = null)
        {
            using var response = await task;
            RequireFinanceStatus(response, (HttpStatusCode)code, "protected adjustment guard");
            if (message is not null && !(await response.Content.ReadAsStringAsync()).Contains(message, StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected adjustment guard response.");
        }
        static async Task<HttpClient> SignIn(QaApiFactory api, string account)
        {
            var caller = api.CreateClient();
            caller.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(caller, account, "Synthetic!39Ab"));
            return caller;
        }
        Task<Guid> Seed(string code, string status = "PartiallyPaid", bool includeOtherPayments = false) => WithDb(async db =>
        {
            var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-ADJUSTMENT-" + code,
                TotalAmount = 1000m, AdjustedAmount = 200m, Currency = "USD", Status = status };
            db.Add(invoice);
            db.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 600m, Currency = "USD", Status = "Reconciled" });
            if (includeOtherPayments)
            {
                db.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 20m, Currency = "EUR", Status = "Completed" });
                db.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 40m, Currency = "USD", Status = "Voided" });
            }
            await db.SaveChangesAsync();
            return invoice.Id;
        });
        Task<int> Modules(string value) => WithDb(db => db.Academies.Where(x => x.Id == academy)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.EnabledModulesJson, value)));
        Task<(decimal Adjusted, string Status, int Count)> Snapshot(Guid id) => WithDb(async db =>
        {
            var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == id);
            return (invoice.AdjustedAmount, invoice.Status, await db.FinanceAdjustments.CountAsync(x => x.InvoiceId == id));
        });
        async Task<T> WithDb<T>(Func<AcademyDeskDbContext, Task<T>> action)
        {
            using var scope = factory.Services.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }
        void Pass(string label)
        {
            count++;
            Console.WriteLine($"FINANCE PENTA ADJUSTMENT CASE {label} PASS");
        }
    }
}
