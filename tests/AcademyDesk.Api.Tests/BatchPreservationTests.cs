using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class BatchPreservationTests
{
    private static AcademyDeskDbContext Store() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static void Check(JsonElement row, string? days)
    {
        Assert.Equal("OneToOne", row.GetProperty("ClassType").GetString());
        Assert.Equal(45, row.GetProperty("SessionMinutes").GetInt32());
        Assert.Equal(2, row.GetProperty("SessionsPerWeek").GetInt32());
        Assert.Equal(days, row.GetProperty("MeetingDaysJson").GetString());
    }

    [Theory]
    [InlineData("InPerson")]
    [InlineData("Online")]
    [InlineData("Hybrid")]
    public async Task List_returns_extended_settings_without_mutating_rows(string mode)
    {
        using var db = Store(); var academy = Guid.NewGuid();
        var row = new Batch { AcademyId = academy, Name = "Synthetic", CourseId = Guid.NewGuid(), DeliveryMode = mode,
            ClassType = "OneToOne", SessionMinutes = 45, SessionsPerWeek = 2, MeetingDaysJson = "[{\"Day\":\"Mon\",\"StartTime\":\"10:30\"}]", MeetingLink = "https://example.invalid/qa" };
        db.Add(row); await db.SaveChangesAsync(); var before = JsonSerializer.Serialize(row);
        var response = await new BatchesController(db).List(academy, default);
        var list = Assert.IsAssignableFrom<IReadOnlyList<BatchSummary>>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Check(JsonSerializer.SerializeToElement(Assert.Single(list)), row.MeetingDaysJson);
        db.ChangeTracker.Clear(); Assert.Equal(before, JsonSerializer.Serialize(await db.Batches.SingleAsync()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_returns_extended_nullable_settings(bool populated)
    {
        using var db = Store(); var academy = new Academy { Name = "Synthetic" };
        var course = new ProgramCourse { AcademyId = academy.Id, Name = "Synthetic" }; db.AddRange(academy, course); await db.SaveChangesAsync();
        var days = populated ? "[{\"Day\":\"Mon\",\"StartTime\":\"10:30\"}]" : null;
        var request = new CreateBatchRequest("Synthetic", null, course.Id, null, null, 12, 7, "InPerson", null, null, "Closed", null, null, null,
            "OneToOne", 45, 2, days, null);
        var result = await new BatchesController(db).Create(academy.Id, request, default);
        Check(JsonSerializer.SerializeToElement(Assert.IsType<BatchSummary>(Assert.IsType<CreatedResult>(result.Result).Value)), days);
    }

    [Fact]
    public async Task Inherited_optional_properties_bind_and_are_applied_by_update()
    {
        using var db = Store(); var academy = Guid.NewGuid(); var course = new ProgramCourse { AcademyId = academy, Name = "Synthetic" };
        var row = new Batch { AcademyId = academy, Name = "Original", CourseId = course.Id }; db.AddRange(course, row); await db.SaveChangesAsync();
        var days = "[{\"Day\":\"Mon\",\"StartTime\":\"10:30\"}]";
        var body = JsonSerializer.Serialize(new { name = "Renamed", courseId = course.Id, capacity = 12, waitlistCapacity = 7, deliveryMode = "Hybrid",
            classType = "OneToOne", sessionMinutes = 45, sessionsPerWeek = 2, meetingDaysJson = days,
            meetingLink = "https://example.invalid/qa", enrollmentStatus = "Closed", isActive = true });
        var request = JsonSerializer.Deserialize<UpdateBatchRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(days, request.MeetingDaysJson); Assert.Equal("OneToOne", request.ClassType);
        var result = await new BatchesController(db).Update(academy, row.Id, request, default);
        Check(JsonSerializer.SerializeToElement(Assert.IsType<BatchSummary>(Assert.IsType<OkObjectResult>(result.Result).Value)), days);
        db.ChangeTracker.Clear(); var saved = await db.Batches.SingleAsync(); Assert.Equal(days, saved.MeetingDaysJson); Assert.Equal("Hybrid", saved.DeliveryMode);
    }
}
