using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class CalendarSourceUtcTests
{
    [Theory]
    [InlineData(DateTimeKind.Unspecified, "Planned", "Scheduled")]
    [InlineData(DateTimeKind.Unspecified, "Cancelled", "Cancelled")]
    [InlineData(DateTimeKind.Utc, "Planned", "Scheduled")]
    [InlineData(DateTimeKind.Utc, "Cancelled", "Cancelled")]
    public async Task Lists_preserve_instants_tenant_history_and_rows_with_explicit_UTC(DateTimeKind kind, string eventStatus, string makeupStatus)
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var academy = Guid.NewGuid();
        var start = new DateTime(2026, 12, 31, 19, 0, 0, kind);
        var ownedEvent = new AcademyEvent { AcademyId = academy, Title = "Boundary recital", StartUtc = start, EndUtc = start.AddHours(1), Status = eventStatus };
        var ownedMakeup = new MakeupClass { AcademyId = academy, StudentId = Guid.NewGuid(), BatchId = Guid.NewGuid(), StartUtc = start, EndUtc = start.AddHours(1), Status = makeupStatus, DeliveryMode = "Hybrid", MeetingLink = "https://meeting.example.invalid/boundary" };
        await using (var seed = new AcademyDeskDbContext(options))
        {
            seed.AddRange(ownedEvent, ownedMakeup,
                new AcademyEvent { AcademyId = Guid.NewGuid(), Title = "Other academy", StartUtc = start, EndUtc = start.AddHours(1) },
                new MakeupClass { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), BatchId = Guid.NewGuid(), StartUtc = start, EndUtc = start.AddHours(1) });
            await seed.SaveChangesAsync();
        }
        async Task<string> Snapshot()
        {
            await using var db = new AcademyDeskDbContext(options);
            return JsonSerializer.Serialize(new { events = await db.AcademyEvents.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), makeups = await db.MakeupClasses.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        var before = await Snapshot();
        await using var read = new AcademyDeskDbContext(options);
        var events = await new EventsController(read).List(academy, default);
        var eventRow = Assert.Single(Assert.IsAssignableFrom<IEnumerable<EventSummary>>(Assert.IsType<OkObjectResult>(events.Result).Value));
        var makeups = await new MakeupClassesController(read).List(academy, default);
        var makeupRow = Assert.Single(Assert.IsAssignableFrom<IEnumerable<MakeupSummary>>(Assert.IsType<OkObjectResult>(makeups).Value));
        Assert.Equal(ownedEvent.Id, eventRow.Id); Assert.Equal(eventStatus, eventRow.Status);
        Assert.Equal(ownedMakeup.Id, makeupRow.Id); Assert.Equal(makeupStatus, makeupRow.Status); Assert.Equal(ownedMakeup.MeetingLink, makeupRow.MeetingLink);
        foreach (var row in new[] { JsonSerializer.SerializeToElement(eventRow, new JsonSerializerOptions(JsonSerializerDefaults.Web)), JsonSerializer.SerializeToElement(makeupRow, new JsonSerializerOptions(JsonSerializerDefaults.Web)) })
        {
            Assert.Equal("2026-12-31T19:00:00Z", row.GetProperty("startUtc").GetString());
            Assert.Equal("2026-12-31T20:00:00Z", row.GetProperty("endUtc").GetString());
            Assert.Equal(start.Ticks, row.GetProperty("startUtc").GetDateTime().Ticks);
            Assert.Equal(DateTimeKind.Utc, row.GetProperty("startUtc").GetDateTime().Kind);
        }
        Assert.Equal(before, await Snapshot());
    }
}
