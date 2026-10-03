using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed record AccessFixture(Guid Academy, Guid Student, Guid Teacher, Guid Invoice, Guid Payment, Guid Adjustment, Guid Profile);
    private sealed record AccessAction(string Name, string Method, string Path, object? Body, HttpStatusCode Success, bool Payroll = false);

    private static async Task VerifyFinanceAccessAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academyA, academyB, studentA, studentB, teacher;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academyA = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            academyB = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            studentA = await db.Students.Where(x => x.AcademyId == academyA).Select(x => x.Id).SingleAsync();
            studentB = await db.Students.Where(x => x.AcademyId == academyB).Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academyA).Select(x => x.Id).SingleAsync();
        }
        var foreign = await SeedAccessFixtureAsync(factory, academyB, studentB, null);
        (string Name, string Email, bool Finance, bool Payroll)[] actors = [
            ("AcademyAdmin", "qa-admin-a@example.invalid", true, true),
            ("Owner", "qa-access-owner@example.invalid", true, true),
            ("FinanceUser", "qa-access-finance@example.invalid", true, false),
            ("QA-FinanceOnly", "qa-access-custom@example.invalid", true, false),
            ("Teacher", "qa-teacher-a@example.invalid", false, false),
            ("Student", "qa-access-student@example.invalid", false, false)
        ];
        foreach (var actor in actors)
        {
            if (actor.Name is not ("AcademyAdmin" or "Teacher"))
                await CreateAccessActorAsync(factory, actor.Email, actor.Name, academyA,
                    actor.Name == "Student" ? studentA : null, permissions: actor.Name == "QA-FinanceOnly" ? "[\"finance.manage\"]" : "[]");
            var fixture = await SeedAccessFixtureAsync(factory, academyA, studentA, teacher);
            var token = await LoginAsync(client, actor.Email, "Synthetic!39Ab");
            foreach (var action in FinanceAccessActions(fixture))
            {
                var allowed = action.Payroll ? actor.Payroll : actor.Finance;
                await CheckAccessActionAsync(factory, client, token, action, allowed ? action.Success : HttpStatusCode.Forbidden,
                    fixture, foreign, actor.Name + "/" + action.Name);
            }
            Console.WriteLine($"ACCESS MATRIX {actor.Name} PASS: 16 actions; finance={actor.Finance}/payroll={actor.Payroll}; denied snapshots unchanged, allowed results persisted/scoped.");
        }

        var baseline = await SeedAccessFixtureAsync(factory, academyA, studentA, teacher);
        var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        foreach (var action in FinanceAccessActions(baseline).Where(x => x.Method != "GET"))
        {
            var routeB = action with { Path = action.Path.Replace(academyA.ToString(), academyB.ToString()) };
            await CheckAccessActionAsync(factory, client, adminToken, routeB, HttpStatusCode.Forbidden, baseline, foreign, "foreign-route/" + action.Name);
        }
        var foreignRows = new[] {
            new AccessAction("foreign-invoice-payment", "POST", $"/api/academies/{academyA}/payments", new { invoiceId = foreign.Invoice, amount = 1m }, HttpStatusCode.NotFound),
            new AccessAction("foreign-payment-void", "PATCH", $"/api/academies/{academyA}/payments/{foreign.Payment}/status", new { status = "Voided" }, HttpStatusCode.NotFound),
            new AccessAction("foreign-payment-reconcile", "PATCH", $"/api/academies/{academyA}/payments/{foreign.Payment}/reconcile", new { reference = "FORBIDDEN" }, HttpStatusCode.NotFound),
            new AccessAction("foreign-adjustment-decision", "PATCH", $"/api/academies/{academyA}/finance-adjustments/{foreign.Adjustment}/approval", new { approve = true }, HttpStatusCode.NotFound),
            new AccessAction("foreign-profile-update", "PUT", $"/api/academies/{academyA}/payroll/profiles/{foreign.Profile}", AccessProfileBody(baseline), HttpStatusCode.NotFound, true),
            new AccessAction("foreign-profile-payout", "POST", $"/api/academies/{academyA}/payroll/payouts", new { payrollProfileId = foreign.Profile, periodLabel = "FORBIDDEN", deductions = 10m }, HttpStatusCode.BadRequest, true),
            new AccessAction("foreign-student-invoice", "POST", $"/api/academies/{academyA}/invoices", new { studentId = foreign.Student, amount = 1000m }, HttpStatusCode.BadRequest)
        };
        foreach (var action in foreignRows)
            await CheckAccessActionAsync(factory, client, adminToken, action, action.Success, baseline, foreign, action.Name, forceNoWrite: true);

        var paymentCreate = FinanceAccessActions(baseline).Single(x => x.Name == "payment-create");
        var adjustmentCreate = FinanceAccessActions(baseline).Single(x => x.Name == "adjustment-create");
        var payout = FinanceAccessActions(baseline).Single(x => x.Name == "payout-create");
        await CheckAccessActionAsync(factory, client, null, paymentCreate, HttpStatusCode.Unauthorized, baseline, foreign, "anonymous-payment");

        var grantUser = await CreateAccessActorAsync(factory, "qa-access-grant@example.invalid", "QA-NoFinance", academyA);
        var grantToken = await LoginAsync(client, "qa-access-grant@example.invalid", "Synthetic!39Ab");
        await CheckAccessActionAsync(factory, client, grantToken, paymentCreate, HttpStatusCode.Forbidden, baseline, foreign, "grant-absent");
        Guid grantId;
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var grant = new AccessGrant { AcademyId = academyA, UserId = grantUser, GrantedByUserId = grantUser,
                PermissionsJson = "[\"finance.manage\"]", IsPermanent = false, ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1), Reason = "QA-only fixture" };
            identity.AccessGrants.Add(grant); await identity.SaveChangesAsync(); grantId = grant.Id;
        }
        await CheckAccessActionAsync(factory, client, grantToken, paymentCreate, HttpStatusCode.Created, baseline, foreign, "grant-valid-same-token");
        await CheckAccessActionAsync(factory, client, grantToken, payout, HttpStatusCode.Forbidden, baseline, foreign, "grant-does-not-map-payroll");
        foreach (var expired in new[] { true, false })
        {
            using (var scope = factory.Services.CreateScope())
            {
                var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var grant = await identity.AccessGrants.SingleAsync(x => x.Id == grantId);
                grant.ExpiresAtUtc = expired ? DateTimeOffset.UtcNow.AddHours(-1) : DateTimeOffset.UtcNow.AddHours(1);
                grant.RevokedAtUtc = expired ? null : DateTimeOffset.UtcNow;
                await identity.SaveChangesAsync();
            }
            await CheckAccessActionAsync(factory, client, grantToken, paymentCreate, HttpStatusCode.Forbidden, baseline, foreign, expired ? "grant-expired" : "grant-revoked");
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Id == academyA);
            academy.EnabledModulesJson = "[\"Finance\"]"; await db.SaveChangesAsync();
        }
        await CheckAccessActionAsync(factory, client, adminToken, adjustmentCreate, HttpStatusCode.Forbidden, baseline, foreign, "FinanceControls-disabled");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Id == academyA);
            academy.EnabledModulesJson = "[\"FinanceControls\"]"; await db.SaveChangesAsync();
        }
        await CheckAccessActionAsync(factory, client, adminToken, paymentCreate, HttpStatusCode.Forbidden, baseline, foreign, "Finance-disabled-payment");
        await CheckAccessActionAsync(factory, client, adminToken, payout, HttpStatusCode.Forbidden, baseline, foreign, "Finance-disabled-payroll");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Id == academyA);
            academy.EnabledModulesJson = "[\"Finance\",\"FinanceControls\"]"; academy.IsActive = false; await db.SaveChangesAsync();
        }
        await CheckAccessActionAsync(factory, client, adminToken, paymentCreate, HttpStatusCode.Forbidden, baseline, foreign, "inactive-academy");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Academies.SingleAsync(x => x.Id == academyA)).IsActive = true; await db.SaveChangesAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await users.FindByEmailAsync("qa-admin-a@example.invalid") ?? throw new InvalidOperationException("Admin missing");
            admin.IsActive = false; if (!(await users.UpdateAsync(admin)).Succeeded) throw new InvalidOperationException("Inactive fixture failed");
        }
        await CheckAccessActionAsync(factory, client, adminToken, paymentCreate, HttpStatusCode.Forbidden, baseline, foreign, "inactive-user-existing-token");
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await users.FindByEmailAsync("qa-admin-a@example.invalid") ?? throw new InvalidOperationException("Admin missing");
            admin.IsActive = true; if (!(await users.UpdateAsync(admin)).Succeeded) throw new InvalidOperationException("Restore fixture failed");
        }
        var platform = await CreateAccessActorAsync(factory, "qa-access-platform@example.invalid", "PlatformOwner", academyB);
        var platformToken = await LoginAsync(client, "qa-access-platform@example.invalid", "Synthetic!39Ab");
        await CheckAccessActionAsync(factory, client, platformToken, paymentCreate, HttpStatusCode.Forbidden, baseline, foreign, "platform-role-without-flag");
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(platform.ToString()) ?? throw new InvalidOperationException("Platform fixture missing");
            user.IsPlatformOwner = true; if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Platform flag fixture failed");
        }
        await CheckAccessActionAsync(factory, client, platformToken, paymentCreate, HttpStatusCode.Created, baseline, foreign, "platform-flag-cross-tenant-current-db");

        var financeToken = await LoginAsync(client, "qa-access-finance@example.invalid", "Synthetic!39Ab");
        foreach (var lookup in new[] { "students", "admin-work-items" })
            await CheckAccessActionAsync(factory, client, financeToken, new AccessAction(lookup, "GET", $"/api/academies/{academyA}/{lookup}", null, HttpStatusCode.Forbidden),
                HttpStatusCode.Forbidden, baseline, foreign, "known-lookup-gap/" + lookup);
        Console.WriteLine("ACCESS KNOWN-LOOKUP-GAP: FinanceUser direct finance allowed but students/admin-work-items GET 403; existing BUG-FUNC-0006 API dependency reproduced, browser NOT RUN, no broad permissions granted.");
        Console.WriteLine("ACCESS REGRESSION PASS: six actor classes x16 actions, 11 foreign-route writes, seven foreign-row guards, anonymous, custom grant absent/valid/expired/revoked and unmapped payroll, module/inactive/user and platform role-vs-flag controls. Current mapped rules only; policy/critical/all-roles/browser gaps remain open.");
    }

    private static AccessAction[] FinanceAccessActions(AccessFixture f)
    {
        var root = $"/api/academies/{f.Academy}";
        return [
            new("invoice-list", "GET", root + "/invoices", null, HttpStatusCode.OK),
            new("invoice-create", "POST", root + "/invoices", new { studentId = f.Student, amount = 1000m }, HttpStatusCode.Created),
            new("invoice-status", "PATCH", root + $"/invoices/{f.Invoice}/status", new { status = "PartiallyPaid" }, HttpStatusCode.OK),
            new("payment-list", "GET", root + "/payments", null, HttpStatusCode.OK),
            new("payment-create", "POST", root + "/payments", new { invoiceId = f.Invoice, amount = 1m }, HttpStatusCode.Created),
            new("payment-reconcile", "PATCH", root + $"/payments/{f.Payment}/reconcile", new { reference = "QA-ACCESS-RECON" }, HttpStatusCode.OK),
            new("payment-void", "PATCH", root + $"/payments/{f.Payment}/status", new { status = "Voided" }, HttpStatusCode.OK),
            new("adjustment-list", "GET", root + "/finance-adjustments", null, HttpStatusCode.OK),
            new("adjustment-create", "POST", root + "/finance-adjustments", new { invoiceId = f.Invoice, type = "Discount", amount = 10m, reason = "Synthetic access test" }, HttpStatusCode.OK),
            new("adjustment-decide", "PATCH", root + $"/finance-adjustments/{f.Adjustment}/approval", new { approve = true, notes = "QA-ACCESS-APPROVED" }, HttpStatusCode.OK),
            new("reminder-queue", "POST", root + "/fee-reminders", new { invoiceId = f.Invoice, channel = "InApp" }, HttpStatusCode.OK),
            new("profile-list", "GET", root + "/payroll/profiles", null, HttpStatusCode.OK, true),
            new("profile-create", "POST", root + "/payroll/profiles", AccessProfileBody(f), HttpStatusCode.Created, true),
            new("profile-update", "PUT", root + $"/payroll/profiles/{f.Profile}", AccessProfileBody(f), HttpStatusCode.OK, true),
            new("payout-list", "GET", root + "/payroll/payouts", null, HttpStatusCode.OK, true),
            new("payout-create", "POST", root + "/payroll/payouts", new { payrollProfileId = f.Profile, periodLabel = "QA-ACCESS", deductions = 10m }, HttpStatusCode.Created, true)
        ];
    }
    private static object AccessProfileBody(AccessFixture f) => new { workerType = "Teacher", teacherId = f.Teacher,
        workerName = "Synthetic Access Teacher", paymentModel = "Monthly", monthlyAmount = 1000m };

    private static async Task<Guid> CreateAccessActorAsync(QaApiFactory factory, string email, string role,
        Guid academy, Guid? student = null, string permissions = "[]")
    {
        using var scope = factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        if (!await roles.RoleExistsAsync(role) && !(await roles.CreateAsync(new ApplicationRole { Name = role,
            IsSystemRole = !role.StartsWith("QA-"), AcademyId = role.StartsWith("QA-") ? academy : null, PermissionsJson = permissions })).Succeeded)
            throw new InvalidOperationException("Access role fixture failed.");
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true,
            DisplayName = "Synthetic Access Actor", AcademyId = academy, StudentId = student, IsActive = true };
        if (!(await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded || !(await users.AddToRoleAsync(user, role)).Succeeded)
            throw new InvalidOperationException("Access identity fixture failed.");
        return user.Id;
    }

    private static async Task<AccessFixture> SeedAccessFixtureAsync(QaApiFactory factory, Guid academy, Guid student, Guid? teacher)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-ACL-" + Guid.NewGuid().ToString("N"), TotalAmount = 1000m, Status = "PartiallyPaid" };
        var payment = new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 100m };
        var adjustment = new FinanceAdjustment { AcademyId = academy, InvoiceId = invoice.Id, Type = "Discount", Amount = 20m, Reason = "Synthetic access fixture" };
        var profile = new PayrollProfile { AcademyId = academy, TeacherId = teacher, WorkerType = "Teacher", WorkerName = "Synthetic Access Teacher", MonthlyAmount = 1000m };
        var priorPayout = new PayrollPayout { AcademyId = academy, PayrollProfileId = profile.Id,
            PayslipNumber = "QA-ACL-" + Guid.NewGuid().ToString("N"), PeriodLabel = "QA-SEED",
            GrossAmount = 1000m, Deductions = 10m, NetAmount = 990m };
        db.AddRange(invoice, payment, adjustment, profile, priorPayout); await db.SaveChangesAsync();
        return new(academy, student, teacher ?? Guid.Empty, invoice.Id, payment.Id, adjustment.Id, profile.Id);
    }

    private static async Task CheckAccessActionAsync(QaApiFactory factory, HttpClient client, string? token,
        AccessAction action, HttpStatusCode expected, AccessFixture own, AccessFixture foreign, string label, bool forceNoWrite = false)
    {
        // Natural pacing keeps the unchanged 120/min limiter; no auth/audit/limit override.
        await Task.Delay(650);
        client.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
        var before = await AccessFinancialSnapshotAsync(factory);
        var auditsBefore = await AccessAuditCountAsync(factory);
        using var request = new HttpRequestMessage(new HttpMethod(action.Method), action.Path);
        if (action.Body is not null) request.Content = JsonContent.Create(action.Body);
        using var response = await client.SendAsync(request);
        RequireFinanceStatus(response, expected, "access " + label);
        var body = await response.Content.ReadAsStringAsync();
        var after = await AccessFinancialSnapshotAsync(factory);
        var auditDelta = await AccessAuditCountAsync(factory) - auditsBefore;
        var noWrite = forceNoWrite || action.Method == "GET" || (int)expected >= 400;
        if (noWrite && before != after) throw new InvalidOperationException("Access rejection/read changed financial snapshot: " + label);
        if (noWrite && auditDelta != 0) throw new InvalidOperationException("Access rejection/read wrote a success audit: " + label);
        if (!noWrite) await VerifyAllowedAccessWriteAsync(factory, action, body, own);
        if ((int)expected < 400 && body.Contains(foreign.Invoice.ToString(), StringComparison.OrdinalIgnoreCase) ||
            (int)expected < 400 && body.Contains(foreign.Profile.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Foreign row leaked in access response.");
        Console.WriteLine($"ACCESS CASE {label} PASS: HTTP={(int)expected}; {(noWrite ? "captured financial/notification snapshot unchanged" : "fresh SQL persistence verified")}; audit delta={auditDelta}.");
    }

    private static async Task<string> AccessFinancialSnapshotAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        return JsonSerializer.Serialize(new { Invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Adjustments = await db.FinanceAdjustments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Profiles = await db.PayrollProfiles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Payouts = await db.PayrollPayouts.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
    }

    private static async Task<int> AccessAuditCountAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AuditLogs.CountAsync();
    }

    private static async Task VerifyAllowedAccessWriteAsync(QaApiFactory factory, AccessAction action, string body, AccessFixture f)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var responseId = Guid.Empty;
        if (action.Name is "invoice-create" or "payment-create" or "adjustment-create" or "profile-create" or "payout-create")
        {
            using var json = JsonDocument.Parse(body);
            responseId = json.RootElement.GetProperty("id").GetGuid();
        }
        var valid = action.Name switch
        {
            "invoice-create" => await db.Invoices.AsNoTracking().AnyAsync(x => x.Id == responseId && x.AcademyId == f.Academy && x.StudentId == f.Student && x.TotalAmount == 1000m),
            "invoice-status" => await db.Invoices.AsNoTracking().AnyAsync(x => x.Id == f.Invoice && x.Status == "PartiallyPaid"),
            "payment-create" => await db.Payments.AsNoTracking().AnyAsync(x => x.Id == responseId && x.AcademyId == f.Academy && x.InvoiceId == f.Invoice && x.Amount == 1m && x.Status == "Completed"),
            "payment-reconcile" => await db.Payments.AsNoTracking().AnyAsync(x => x.Id == f.Payment && x.Status == "Reconciled" && x.ReconciliationReference == "QA-ACCESS-RECON" && x.ReconciledAtUtc != null),
            "payment-void" => await db.Payments.AsNoTracking().AnyAsync(x => x.Id == f.Payment && x.Status == "Voided"),
            "adjustment-create" => await db.FinanceAdjustments.AsNoTracking().AnyAsync(x => x.Id == responseId && x.AcademyId == f.Academy && x.InvoiceId == f.Invoice && x.Amount == 10m && x.Status == "PendingApproval"),
            "adjustment-decide" => await db.FinanceAdjustments.AsNoTracking().AnyAsync(x => x.Id == f.Adjustment && x.Status == "Approved" && x.AppliedAtUtc != null) && await db.Invoices.AsNoTracking().AnyAsync(x => x.Id == f.Invoice && x.AdjustedAmount == 20m),
            "reminder-queue" => await db.Notifications.AsNoTracking().AnyAsync(x => x.AcademyId == f.Academy && x.RecipientId == f.Student && x.Title == "Fee payment reminder"),
            "profile-create" => await db.PayrollProfiles.AsNoTracking().AnyAsync(x => x.Id == responseId && x.AcademyId == f.Academy && x.TeacherId == f.Teacher && x.MonthlyAmount == 1000m),
            "profile-update" => await db.PayrollProfiles.AsNoTracking().AnyAsync(x => x.Id == f.Profile && x.TeacherId == f.Teacher && x.MonthlyAmount == 1000m),
            "payout-create" => await db.PayrollPayouts.AsNoTracking().AnyAsync(x => x.Id == responseId && x.AcademyId == f.Academy && x.PayrollProfileId == f.Profile && x.GrossAmount == 1000m && x.Deductions == 10m && x.NetAmount == 990m),
            _ => false
        };
        if (!valid) throw new InvalidOperationException("Allowed access response not persisted correctly: " + action.Name);
    }
}
