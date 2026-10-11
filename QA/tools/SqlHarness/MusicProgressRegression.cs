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
    private static async Task VerifyMusicProgressAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign; StudentMusicProgress own, sentinel;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            foreach (var id in new[] { academy, foreign }) (await db.Academies.SingleAsync(x => x.Id == id)).EnabledModulesJson = "[\"Core\",\"AcademicGovernance\"]";
            var piece = new MusicPiece { AcademyId = academy, Title = "Synthetic zero-score piece" };
            var otherPiece = new MusicPiece { AcademyId = foreign, Title = "Foreign sentinel piece" };
            own = new StudentMusicProgress { AcademyId = academy, StudentId = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync(), MusicPieceId = piece.Id, Score = 0, Notes = "Retain synthetic notes", TargetDate = new DateOnly(2026, 10, 20) };
            sentinel = new StudentMusicProgress { AcademyId = foreign, StudentId = await db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync(), MusicPieceId = otherPiece.Id, Score = 17, Notes = "Foreign sentinel" };
            db.AddRange(piece, otherPiece, own, sentinel); await db.SaveChangesAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var otherAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null; var route = $"/api/academies/{academy}/music-progress/{own.Id}"; var checks = 0;
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Progress = await db.StudentMusicProgress.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        async Task Check(object body, HttpStatusCode expected, string? actor, string? path = null)
        {
            var before = await Snapshot(); await Task.Delay(750);
            using var request = new HttpRequestMessage(HttpMethod.Patch, path ?? route) { Content = JsonContent.Create(body) };
            if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actor);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, expected, "music progress");
            if (!response.IsSuccessStatusCode) { if (before != await Snapshot()) throw new InvalidOperationException("Denied music request changed progress/audit rows."); }
            else
            {
                var result = (await response.Content.ReadFromJsonAsync<MusicProgressSummary>())!;
                var requested = JsonSerializer.Deserialize<UpdateMusicProgressRequest>(JsonSerializer.Serialize(body), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var stored = await db.StudentMusicProgress.AsNoTracking().SingleAsync(x => x.Id == own.Id);
                var actual = new MusicProgressSummary(stored.Id, stored.StudentId, stored.MusicPieceId, stored.Status, stored.TargetDate, stored.Score, stored.Notes);
                if (actual != result || stored.Score != requested.Score || stored.Status != requested.Status || stored.TargetDate != requested.TargetDate || stored.Notes != requested.Notes) throw new InvalidOperationException("Fresh SQL score/status/date/notes mismatch.");
                if (await db.StudentMusicProgress.CountAsync() != 2 || (await db.StudentMusicProgress.AsNoTracking().SingleAsync(x => x.Id == sentinel.Id)).Score != 17) throw new InvalidOperationException("Foreign sentinel/row count changed.");
            }
            Console.WriteLine($"PASS: MUSICSQL case={++checks} HTTP={(int)response.StatusCode} freshSql=true no429=true");
        }
        object Body(decimal? score, string status = "Learning") => new { status, score, targetDate = "2026-10-20", notes = "Retain synthetic notes" };
        foreach (var score in new decimal?[] { 0, 85.5m, null }) foreach (var status in new[] { "Learning", "Mastered" }) await Check(Body(score, status), HttpStatusCode.OK, admin);
        await Check(Body(0), HttpStatusCode.OK, admin);
        await Check(Body(0, "invalid"), HttpStatusCode.BadRequest, admin);
        await Check(new { status = "Learning", score = "not-a-number", targetDate = "2026-10-20", notes = "Retain synthetic notes" }, HttpStatusCode.BadRequest, admin);
        await Check(Body(0), HttpStatusCode.Unauthorized, null);
        await Check(Body(0), HttpStatusCode.Forbidden, teacher);
        await Check(Body(0), HttpStatusCode.Forbidden, otherAdmin);
        await Check(Body(0), HttpStatusCode.Forbidden, admin, $"/api/academies/{foreign}/music-progress/{sentinel.Id}");
        await Check(Body(0), HttpStatusCode.NotFound, admin, $"/api/academies/{academy}/music-progress/{sentinel.Id}");
        await Check(Body(0), HttpStatusCode.NotFound, admin, $"/api/academies/{academy}/music-progress/{Guid.NewGuid()}");
        await Check(new { status = "Paused", score = (decimal?)null, targetDate = (string?)null, notes = (string?)null }, HttpStatusCode.OK, admin);
        await Check(Body(0), HttpStatusCode.OK, admin);
        Console.WriteLine($"PASS: MUSICSQL {checks} real Identity/HTTP/SQL controls; zero/positive/null preserved, denied no-write, foreign sentinel retained.");
        await VerifyMusicProgressBrowserAsync(factory, client, academy, own.Id);
    }
}
