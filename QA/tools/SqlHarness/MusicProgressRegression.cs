using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
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
    private sealed record MusicSqlState(StudentMusicProgress[] Progress, AuditLog[] Audits);

    private static async Task<MusicSqlState> ReadMusicStateAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        return new(await db.StudentMusicProgress.AsNoTracking().OrderBy(row => row.Id).ToArrayAsync(),
            await db.AuditLogs.AsNoTracking().OrderBy(row => row.Id).ToArrayAsync());
    }

    private static async Task VerifyMusicProgressAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreign, adminId;
        StudentMusicProgress target, other, outside;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            adminId = await identity.Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            var student = await db.Students.SingleAsync(x => x.AcademyId == academy);
            var outsideStudent = await db.Students.SingleAsync(x => x.AcademyId == foreign);
            var otherStudent = new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "Unrelated learner" };
            var piece = new MusicPiece { AcademyId = academy, Title = "Synthetic practice piece" };
            var outsidePiece = new MusicPiece { AcademyId = foreign, Title = "Synthetic foreign piece" };
            target = new() { AcademyId = academy, StudentId = student.Id, MusicPieceId = piece.Id, Score = 0m };
            other = new() { AcademyId = academy, StudentId = otherStudent.Id, MusicPieceId = piece.Id, Score = 48.25m, Notes = "Unrelated learner" };
            outside = new() { AcademyId = foreign, StudentId = outsideStudent.Id, MusicPieceId = outsidePiece.Id, Score = 99m, Notes = "Foreign learner" };
            db.AddRange(otherStudent, piece, outsidePiece, target, other, outside);
            await db.SaveChangesAsync();
        }

        var ownerId = await CreateAccessActorAsync(factory, "qa-music-platform@example.invalid", "Owner", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var owner = (await users.FindByIdAsync(ownerId.ToString()))!;
            owner.IsPlatformOwner = true;
            if (!(await users.UpdateAsync(owner)).Succeeded) throw new InvalidOperationException("Synthetic music owner setup failed.");
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var platform = await LoginAsync(client, "qa-music-platform@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;

        var list = $"/api/academies/{academy}/music-progress";
        var route = $"{list}/{target.Id}";
        var foreignList = $"/api/academies/{foreign}/music-progress";
        var date = new DateOnly(2026, 12, 1);
        var checks = 0;
        void Pass(string label)
        {
            checks++;
            Console.WriteLine("PASS: MUSICPROGRESS " + label);
        }

        async Task<HttpResponseMessage> Send(HttpClient targetClient, string? access, string method, string url, UpdateMusicProgressRequest? body = null)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (access is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await targetClient.SendAsync(request);
        }

        async Task VerifyUpdate(string access, Guid actor, UpdateMusicProgressRequest body, string label)
        {
            var before = await ReadMusicStateAsync(factory);
            using var update = await Send(client, access, "PATCH", route, body);
            RequireFinanceStatus(update, HttpStatusCode.OK, label);
            var expected = new MusicProgressSummary(target.Id, target.StudentId, target.MusicPieceId, body.Status, body.TargetDate, body.Score, body.Notes?.Trim());
            if (await update.Content.ReadFromJsonAsync<MusicProgressSummary>() != expected)
                throw new InvalidOperationException("Music update response did not preserve its exact values.");

            var after = await ReadMusicStateAsync(factory);
            var stored = after.Progress.Single(x => x.Id == target.Id);
            var storedValue = new MusicProgressSummary(stored.Id, stored.StudentId, stored.MusicPieceId, stored.Status, stored.TargetDate, stored.Score, stored.Notes);
            if (stored.AcademyId != academy || storedValue != expected ||
                JsonSerializer.Serialize(before.Progress.Where(x => x.Id != target.Id)) != JsonSerializer.Serialize(after.Progress.Where(x => x.Id != target.Id)))
                throw new InvalidOperationException("Fresh SQL changed the target values or another learner's row.");
            var addedAudit = after.Audits.ExceptBy(before.Audits.Select(x => x.Id), x => x.Id).Single();
            if (after.Audits.Length != before.Audits.Length + 1 || addedAudit.ActorUserId != actor ||
                addedAudit.AcademyId != academy || addedAudit.Action != "PATCH MusicProgress" ||
                !addedAudit.MetadataJson!.Contains(route, StringComparison.Ordinal))
                throw new InvalidOperationException("Music status update lacks its attributed audit.");

            using var reload = await Send(client, access, "GET", list);
            RequireFinanceStatus(reload, HttpStatusCode.OK, "fresh music progress read");
            var rows = await reload.Content.ReadFromJsonAsync<MusicProgressSummary[]>();
            if (rows is null || rows.Length != 2 || rows.Single(x => x.Id == target.Id) != expected || rows.All(x => x.Id != other.Id) ||
                JsonSerializer.Serialize(after) != JsonSerializer.Serialize(await ReadMusicStateAsync(factory)))
                throw new InvalidOperationException("Music reload changed values, scope or SQL state.");
            Pass(label);
        }

        foreach (var status in new[] { "Assigned", "Learning", "ReadyForReview", "Mastered", "Paused" })
        foreach (var score in new decimal?[] { 0m, 37.5m, null })
            await VerifyUpdate(admin, adminId, new(status, date, score, "  Synthetic progress note  "), $"{status} score {score?.ToString() ?? "null"} roundtrip");

        var valid = new UpdateMusicProgressRequest("Learning", date, 0m, "Synthetic rejected write");
        (string? Access, string Method, string Url, UpdateMusicProgressRequest? Body, HttpStatusCode Status, string Label)[] denials = [
            (null, "PATCH", route, valid, HttpStatusCode.Unauthorized, "anonymous write"),
            (teacher, "GET", list, null, HttpStatusCode.Forbidden, "Teacher read"),
            (teacher, "PATCH", route, valid, HttpStatusCode.Forbidden, "Teacher write"),
            (admin, "GET", foreignList, null, HttpStatusCode.Forbidden, "foreign tenant read"),
            (admin, "PATCH", $"{foreignList}/{outside.Id}", valid, HttpStatusCode.Forbidden, "foreign tenant write"),
            (admin, "PATCH", $"{list}/{outside.Id}", valid, HttpStatusCode.NotFound, "foreign learner ID in own route"),
            (admin, "PATCH", route, valid with { Status = "" }, HttpStatusCode.BadRequest, "invalid status")
        ];
        foreach (var (access, method, url, body, status, label) in denials)
        {
            var before = JsonSerializer.Serialize(await ReadMusicStateAsync(factory));
            using var response = await Send(client, access, method, url, body);
            RequireFinanceStatus(response, status, label);
            if (before != JsonSerializer.Serialize(await ReadMusicStateAsync(factory)))
                throw new InvalidOperationException("Rejected music request changed SQL state.");
            Pass(label);
        }

        var failures = new List<object>();
        foreach (var actor in new[] { (Name: "Admin", Access: admin, Score: 61m), (Name: "PlatformOwner", Access: platform, Score: 72m) })
        {
            var fault = new MusicProgressAuditFault();
            using var faulted = new QaApiFactory(manifest, isolatedDomainInterceptor: fault);
            using var faultedClient = faulted.CreateClient();
            var before = JsonSerializer.Serialize(await ReadMusicStateAsync(factory));
            using var response = await Send(faultedClient, actor.Access, "PATCH", route, new("Mastered", date, actor.Score, "Synthetic audit failure"));
            var unchanged = before == JsonSerializer.Serialize(await ReadMusicStateAsync(factory));
            if (response.StatusCode != HttpStatusCode.InternalServerError || fault.Hits != 1 || !unchanged)
            {
                failures.Add(new { actor = actor.Name, status = (int)response.StatusCode, fault.Hits, unchanged });
                Console.WriteLine($"MUSICPROGRESS FAIL {actor.Name} status={(int)response.StatusCode} faultHits={fault.Hits} unchanged={unchanged}");
            }
            else Pass(actor.Name + " audit failure rolled back");
        }
        if (failures.Count == 0)
            await VerifyUpdate(platform, ownerId, new("ReadyForReview", date, 0m, "Authorized owner"), "Platform Owner exact save and audit");

        var folder = Path.Combine(Directory.GetCurrentDirectory(), "QA", "EVIDENCE", "c8-music-sql-" + manifest.RunId.ToString("N"));
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, checks, failures }, new JsonSerializerOptions { WriteIndented = true }));
        if (failures.Count != 0) throw new InvalidOperationException("Music progress audit rollback controls failed. Native responses and fresh SQL evidence were retained.");
        Console.WriteLine($"PASS: MUSICPROGRESS SUMMARY {checks} native HTTP and SQL controls.");
    }

    private sealed class MusicProgressAuditFault : SaveChangesInterceptor
    {
        public int Hits { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default)
        {
            if (data.Context is AcademyDeskDbContext db && db.ChangeTracker.Entries<AuditLog>().Any(entry => entry.State == EntityState.Added && entry.Entity.Action == "PATCH MusicProgress"))
            {
                Hits++;
                throw new InvalidOperationException("Synthetic required music audit failure.");
            }
            return base.SavingChangesAsync(data, result, token);
        }
    }
}
