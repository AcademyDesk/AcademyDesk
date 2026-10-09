using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class ClassSessionUtcTests
{
    [Theory]
    [InlineData(DateTimeKind.Unspecified, "Scheduled")]
    [InlineData(DateTimeKind.Unspecified, "Cancelled")]
    [InlineData(DateTimeKind.Utc, "Scheduled")]
    [InlineData(DateTimeKind.Utc, "Cancelled")]
    public async Task List_preserves_UTC_ticks_and_history_but_explicitly_serializes_Z(DateTimeKind kind, string status)
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var academy = Guid.NewGuid();
        var start = new DateTime(2026, 9, 30, 19, 0, 0, kind);
        var owned = new ClassSession { AcademyId = academy, BatchId = Guid.NewGuid(), StartUtc = start, EndUtc = start.AddHours(1), Status = status, DeliveryMode = "Online", RoomName = "https://session.example.invalid/meeting" };
        await using (var seed = new AcademyDeskDbContext(options))
        {
            seed.AddRange(owned, new ClassSession { AcademyId = Guid.NewGuid(), BatchId = Guid.NewGuid(), StartUtc = start, EndUtc = start.AddHours(1) });
            await seed.SaveChangesAsync();
        }
        await using var db = new AcademyDeskDbContext(options);
        var before = JsonSerializer.Serialize(await db.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync());
        var result = await new ClassSessionsController(db).List(academy, start.AddMinutes(-1), start.AddMinutes(1), default);
        var row = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ClassSessionSummary>>(Assert.IsType<OkObjectResult>(result.Result).Value));
        Assert.Equal(owned.Id, row.Id); Assert.Equal(status, row.Status); Assert.Equal(owned.RoomName, row.RoomName);
        Assert.Equal(start.Ticks, row.StartUtc.Ticks); Assert.Equal(start.AddHours(1).Ticks, row.EndUtc.Ticks);
        Assert.Equal(DateTimeKind.Utc, row.StartUtc.Kind); Assert.Equal(DateTimeKind.Utc, row.EndUtc.Kind);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(row, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Equal("2026-09-30T19:00:00Z", json.RootElement.GetProperty("startUtc").GetString());
        Assert.Equal("2026-09-30T20:00:00Z", json.RootElement.GetProperty("endUtc").GetString());
        await using var fresh = new AcademyDeskDbContext(options);
        Assert.Equal(before, JsonSerializer.Serialize(await fresh.ClassSessions.AsNoTracking().OrderBy(x => x.Id).ToListAsync()));
    }
}
