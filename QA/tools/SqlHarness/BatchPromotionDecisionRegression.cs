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
    private sealed record PromotionState(string Stable, List<BatchPromotion> Promotions, List<Enrollment> Enrollments, List<AuditLog> Audits);

    private static async Task VerifyBatchPromotionDecisionsAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreign, actor, foreignActor; var count = 0; var effective = new DateOnly(2026, 10, 1);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            actor = await identity.Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            foreignActor = await identity.Users.Where(x => x.Email == "qa-admin-b@example.invalid").Select(x => x.Id).SingleAsync();
            foreach (var item in await db.Academies.Where(x => x.Id == academy || x.Id == foreign).ToListAsync())
                item.EnabledModulesJson = JsonSerializer.Serialize((JsonSerializer.Deserialize<string[]>(item.EnabledModulesJson) ?? []).Append("AcademicGovernance").Distinct());
            await db.SaveChangesAsync();
        }

        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var other = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        string Root(Guid a) => $"/api/academies/{a}/batch-promotions";
        string Decision(Guid a, Guid id) => $"{Root(a)}/{id}/decision";
        void Pass(string label) { count++; Console.WriteLine($"PROMOTION CASE {label} PASS."); }

        async Task<(BatchPromotion Promotion, Enrollment Source, Batch Target)> Seed(string state = "Pending", bool activeSource = true, bool activeTarget = false, Guid? tenant = null)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var a = tenant ?? academy; var student = Guid.NewGuid(); var sourceBatch = new Batch { AcademyId = a, Name = "Promotion source " + Guid.NewGuid().ToString("N"), CourseId = Guid.NewGuid() };
            var target = new Batch { AcademyId = a, Name = "Promotion target " + Guid.NewGuid().ToString("N"), CourseId = Guid.NewGuid() };
            var source = new Enrollment { AcademyId = a, StudentId = student, BatchId = sourceBatch.Id, StartDate = effective.AddDays(-20), Status = activeSource ? "Active" : "Completed", EndDate = activeSource ? null : effective.AddDays(-1) };
            var promotion = new BatchPromotion { AcademyId = a, StudentId = student, SourceBatchId = sourceBatch.Id, TargetBatchId = target.Id, EffectiveDate = effective, Status = state, Notes = " original " };
            db.AddRange(sourceBatch, target, source, promotion);
            if (activeTarget) db.Enrollments.Add(new Enrollment { AcademyId = a, StudentId = student, BatchId = target.Id, StartDate = effective.AddDays(-1), Status = "Active" });
            await db.SaveChangesAsync(); return (promotion, source, target);
        }

        async Task<PromotionState> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var governance = await GovernanceStateAsync(factory);
            return new PromotionState(JsonSerializer.Serialize(new { governance.Stable, governance.Tasks, Courses = await db.Courses.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }),
                await db.BatchPromotions.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), governance.Audits);
        }
        async Task<HttpResponseMessage> Send(Guid a, Guid id, string status = "Approved", string? notes = " decided ", string? token = null, bool anonymous = false, bool list = false)
        {
            using var request = new HttpRequestMessage(list ? HttpMethod.Get : HttpMethod.Patch, list ? Root(a) : Decision(a, id));
            if (!anonymous) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? admin);
            if (!list) request.Content = JsonContent.Create(new { status, notes });
            return await client.SendAsync(request);
        }
        void AssertBoundary(PromotionState before, PromotionState after, BatchPromotion expected, HttpStatusCode status, bool platform = false, Guid? expectedActor = null)
        {
            if (before.Stable != after.Stable) throw new InvalidOperationException("Unrelated promotion state changed.");
            var success = status == HttpStatusCode.OK;
            var beforePromotion = before.Promotions.Single(x => x.Id == expected.Id);
            var current = after.Promotions.Single(x => x.Id == expected.Id);
            var promotionChanged = JsonSerializer.Serialize(beforePromotion) != JsonSerializer.Serialize(current);
            if (promotionChanged != success) throw new InvalidOperationException("Promotion write boundary failed.");
            var added = after.Enrollments.Where(x => before.Enrollments.All(y => y.Id != x.Id)).ToArray();
            var changed = before.Enrollments.Where(x => after.Enrollments.Any(y => y.Id == x.Id && JsonSerializer.Serialize(y) != JsonSerializer.Serialize(x))).ToArray();
            var approval = success && current.Status == "Approved";
            var sourceBefore = before.Enrollments.SingleOrDefault(x => x.StudentId == expected.StudentId && x.BatchId == expected.SourceBatchId);
            var sourceAfter = after.Enrollments.SingleOrDefault(x => x.StudentId == expected.StudentId && x.BatchId == expected.SourceBatchId);
            var sourceChanged = sourceBefore is not null && sourceAfter is not null && JsonSerializer.Serialize(sourceBefore) != JsonSerializer.Serialize(sourceAfter);
            if (added.Length != (approval ? 1 : 0) || changed.Length != (approval ? 1 : 0) || (approval && !sourceChanged) || JsonSerializer.Serialize(before.Enrollments.Where(x => changed.All(y => y.Id != x.Id))) != JsonSerializer.Serialize(after.Enrollments.Where(x => added.All(y => y.Id != x.Id) && changed.All(y => y.Id != x.Id)))) throw new InvalidOperationException("Enrollment full-row boundary failed.");
            if (approval && (sourceAfter!.Status != "Completed" || sourceAfter.EndDate != expected.EffectiveDate || sourceAfter.LifecycleReason != "Promoted to target batch" || added[0].Status != "Active" || added[0].StudentId != expected.StudentId || added[0].BatchId != expected.TargetBatchId || added[0].StartDate != expected.EffectiveDate)) throw new InvalidOperationException("Approval enrollment projection wrong.");
            var audits = after.Audits.Where(x => before.Audits.All(y => y.Id != x.Id)).ToArray();
            if (audits.Length != (success && !platform ? 1 : 0) || JsonSerializer.Serialize(before.Audits) != JsonSerializer.Serialize(after.Audits.Where(x => audits.All(y => y.Id != x.Id)))) throw new InvalidOperationException("Promotion audit boundary failed.");
            if (audits.Length == 1 && (audits[0].ActorUserId != (expectedActor ?? actor) || audits[0].AcademyId != expected.AcademyId || audits[0].Action != "PATCH BatchPromotions" || !audits[0].MetadataJson!.Contains(Decision(expected.AcademyId, expected.Id), StringComparison.Ordinal))) throw new InvalidOperationException("Promotion audit actor/route mismatch.");
        }
        async Task Check(string label, Guid a, BatchPromotion p, HttpStatusCode expected, string status = "Approved", string? notes = " decided ", string? token = null, bool anonymous = false, bool platform = false, Guid? expectedActor = null)
        {
            await Task.Delay(650); var before = await State(); using var response = await Send(a, p.Id, status, notes, token, anonymous); RequireFinanceStatus(response, expected, label); var after = await State();
            AssertBoundary(before, after, p, expected, platform, expectedActor);
            Pass(label);
        }

        var approve = await Seed(); await Check("approve-atomic-enrollment-audit", academy, approve.Promotion, HttpStatusCode.OK, notes: " approved ");
        foreach (var status in new[] { "Approved", "Rejected" }) await Check("terminal-approved-replay-" + status, academy, approve.Promotion, HttpStatusCode.Conflict, status);
        var reject = await Seed(); await Check("reject-audit", academy, reject.Promotion, HttpStatusCode.OK, "Rejected", " rejected ");
        foreach (var status in new[] { "Approved", "Rejected" }) await Check("terminal-rejected-replay-" + status, academy, reject.Promotion, HttpStatusCode.Conflict, status);
        foreach (var input in new[] { "Pending", "", "approved" }) { var p = await Seed(); await Check("invalid-status-" + (input.Length == 0 ? "blank" : input), academy, p.Promotion, HttpStatusCode.BadRequest, input); }
        var stale = await Seed(activeSource: false); await Check("stale-source409-no-write", academy, stale.Promotion, HttpStatusCode.Conflict);
        var duplicate = await Seed(activeTarget: true); await Check("existing-target409-no-write", academy, duplicate.Promotion, HttpStatusCode.Conflict);
        var own = await Seed(); var foreignPromotion = await Seed(tenant: foreign);
        await Check("foreign-id404-no-write", academy, foreignPromotion.Promotion, HttpStatusCode.NotFound);
        await Check("foreign-route404-no-write", foreign, own.Promotion, HttpStatusCode.NotFound, token: other);
        await Check("foreign-admin403", academy, (await Seed()).Promotion, HttpStatusCode.Forbidden, token: other);
        await Check("teacher403", academy, (await Seed()).Promotion, HttpStatusCode.Forbidden, token: teacher);
        await Check("anonymous401", academy, (await Seed()).Promotion, HttpStatusCode.Unauthorized, anonymous: true);
        foreach (var (a, token, status, label) in new[] { (academy, admin, HttpStatusCode.OK, "list-own"), (academy, other, HttpStatusCode.Forbidden, "list-foreign403") })
        { var before = await State(); using var response = await Send(a, own.Promotion.Id, token: token, list: true); RequireFinanceStatus(response, status, label); var after = await State(); if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after)) throw new InvalidOperationException("List wrote state."); if (status == HttpStatusCode.OK) { using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); if (json.RootElement.EnumerateArray().Any(x => x.GetProperty("academyId").GetGuid() != academy)) throw new InvalidOperationException("List leaked foreign promotions."); } Pass(label); }
        var fault = await Seed(); await SetAuditInsertDeniedAsync(manifest, true); try { await Check("audit-fault500-rolls-back", academy, fault.Promotion, HttpStatusCode.InternalServerError); } finally { await SetAuditInsertDeniedAsync(manifest, false); }
        await Check("post-audit-fault-retry", academy, fault.Promotion, HttpStatusCode.OK);

        foreach (var (first, second, label) in new[] { ("Approved", "Rejected", "concurrent-approve-reject"), ("Rejected", "Approved", "concurrent-reject-approve"), ("Approved", "Approved", "concurrent-approve-replay") })
        {
            var item = await Seed(); var before = await State(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); await using var held = await db.Database.BeginTransactionAsync();
            var resource = $"AcademyDesk:BatchPromotion:{academy:D}:{item.Promotion.Id:D}";
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=0; IF @r<0 THROW 51000,'QA promotion lock failed',1;");
            var pending = new[] { Send(academy, item.Promotion.Id, first), Send(academy, item.Promotion.Id, second) }; await RequirePromotionWaitersAsync(manifest, 2); await held.CommitAsync(); var responses = await Task.WhenAll(pending);
            try { if (responses.Count(x => x.StatusCode == HttpStatusCode.OK) != 1 || responses.Count(x => x.StatusCode == HttpStatusCode.Conflict) != 1) throw new InvalidOperationException("Concurrent decision did not yield one terminal write and one conflict."); var after = await State(); var current = after.Promotions.Single(x => x.Id == item.Promotion.Id); var audits = after.Audits.Where(x => before.Audits.All(y => y.Id != x.Id)).ToArray(); if (audits.Length != 1 || current.Status is not ("Approved" or "Rejected")) throw new InvalidOperationException("Concurrent terminal/audit invariant failed."); Console.WriteLine($"PROMOTION concurrent {label} winner={current.Status} invariant=PASS."); Pass(label + "-SQL-WAIT2"); } finally { foreach (var response in responses) response.Dispose(); }
            await Task.Delay(650);
        }
        using (var scope = factory.Services.CreateScope())
        { var item = await Seed(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); await using var held = await db.Database.BeginTransactionAsync(); var resource = $"AcademyDesk:BatchPromotion:{academy:D}:{item.Promotion.Id:D}"; await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=0; IF @r<0 THROW 51000,'QA promotion lock failed',1;"); await Check("lock-timeout503-no-write", academy, item.Promotion, HttpStatusCode.ServiceUnavailable); await held.RollbackAsync(); }

        var platformId = await CreateAccessActorAsync(factory, "qa-promotion-platform@example.invalid", "PlatformOwner", foreign);
        using (var scope = factory.Services.CreateScope()) { var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = await users.FindByIdAsync(platformId.ToString()) ?? throw new InvalidOperationException("Platform fixture missing"); user.IsPlatformOwner = true; if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Platform flag failed"); }
        var platform = await LoginAsync(client, "qa-promotion-platform@example.invalid", "Synthetic!39Ab"); var platformItem = await Seed(); await Check("platform-owned-transaction", academy, platformItem.Promotion, HttpStatusCode.OK, token: platform, platform: true); await Check("platform-terminal409", academy, platformItem.Promotion, HttpStatusCode.Conflict, token: platform, platform: true);
        if (count != 26) throw new InvalidOperationException("Promotion case count changed: " + count);
        Console.WriteLine("PROMOTION REGRESSION PASS:26 cases; real Identity/HTTP/SQL, terminal replay rejection, stale/duplicate guards, exact enrollment/audit boundaries, deterministic application-lock concurrency, timeout, audit rollback and platform transaction; browser/device/full progression policy NOT RUN.");
    }

    private static async Task RequirePromotionWaitersAsync(QaRunManifest manifest, int expected)
    {
        await using var sql = new Microsoft.Data.SqlClient.SqlConnection(Connection(manifest.SqlServer, manifest.Database, "sa", Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD")!));
        await sql.OpenAsync();
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var waits = (int)(await ScalarAsync(sql, "SELECT COUNT(*) FROM sys.dm_tran_locks WHERE resource_type='APPLICATION' AND request_status='WAIT' AND resource_database_id=DB_ID()"))!;
            if (waits == expected) { Console.WriteLine($"PROMOTION SQL application-lock WAIT={waits} observed before release."); return; }
            await Task.Delay(100);
        }
        throw new InvalidOperationException("HTTP requests did not reach the held promotion lock; concurrency not proved.");
    }
}
