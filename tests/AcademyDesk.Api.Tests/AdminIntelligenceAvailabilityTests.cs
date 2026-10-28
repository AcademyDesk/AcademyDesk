using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class AdminIntelligenceAvailabilityTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("foreign")]
    [InlineData("active")]
    [InlineData("inactive")]
    public async Task Workload_serializes_without_leaking_unavailable_teacher_name(string kind)
    {
        await using var db = CreateDb(); var academy = Guid.NewGuid(); var teacherId = Guid.NewGuid();
        if (kind != "missing") db.Teachers.Add(new Teacher { Id = teacherId, AcademyId = kind == "foreign" ? Guid.NewGuid() : academy,
            FirstName = kind == "foreign" ? "Foreign private" : "Synthetic", LastName = "Teacher", IsActive = kind != "inactive" });
        var start = new DateTime(2026, 10, 28, 10, 0, 0, DateTimeKind.Utc);
        db.ClassSessions.AddRange(new ClassSession { AcademyId = academy, BatchId = Guid.NewGuid(), TeacherId = teacherId, StartUtc = start, EndUtc = start.AddMinutes(75) },
            new ClassSession { AcademyId = academy, BatchId = Guid.NewGuid(), TeacherId = teacherId, StartUtc = start, EndUtc = start.AddMinutes(45) },
            new ClassSession { AcademyId = Guid.NewGuid(), BatchId = Guid.NewGuid(), TeacherId = teacherId, StartUtc = start, EndUtc = start.AddHours(10) });
        await db.SaveChangesAsync();
        var payload = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await new AdminIntelligenceController(db).Get(academy, default)).Value);
        var row = Assert.Single(payload.GetProperty("workload").EnumerateArray());
        Assert.Equal(teacherId, row.GetProperty("TeacherId").GetGuid());
        Assert.Equal(kind is "active" or "inactive" ? "Synthetic Teacher" : "Unavailable teacher", row.GetProperty("TeacherName").GetString());
        Assert.Equal(2m, row.GetProperty("ScheduledHours").GetDecimal()); Assert.Equal(2, row.GetProperty("Sessions").GetInt32());
        Assert.DoesNotContain("Foreign private", payload.GetRawText()); Assert.Equal(3, await db.ClassSessions.CountAsync());
    }

    [Fact]
    public async Task Unassigned_and_cancelled_sessions_are_excluded_from_workload()
    {
        await using var db = CreateDb(); var academy = Guid.NewGuid();
        db.ClassSessions.AddRange(new ClassSession { AcademyId = academy, BatchId = Guid.NewGuid(), TeacherId = null },
            new ClassSession { AcademyId = academy, BatchId = Guid.NewGuid(), TeacherId = Guid.NewGuid(), Status = "Cancelled" });
        await db.SaveChangesAsync();
        var payload = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await new AdminIntelligenceController(db).Get(academy, default)).Value);
        Assert.Empty(payload.GetProperty("workload").EnumerateArray()); Assert.Equal(2, await db.ClassSessions.CountAsync());
    }

    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("qa-intelligence-" + Guid.NewGuid()).Options);
}
