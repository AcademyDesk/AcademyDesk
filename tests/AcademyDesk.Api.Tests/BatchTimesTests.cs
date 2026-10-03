using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class BatchTimesTests
{
    public static IEnumerable<object?[]> Schedules()
    {
        foreach (var update in new[] { false, true })
        foreach (var (days, valid) in new (string?, bool)[] {
            ("[{\"Day\":\"Mon\",\"StartTime\":\"09:00\"}]", true),
            ("[{\"day\":\"Mon\",\"startTime\":\"09:00\"}]", true),
            ("[{\"dAY\":\"Tuesday\",\"sTARTtIME\":\"17:45\"}]", true),
            (null, true), ("  ", true), ("[null]", false),
            ("[{\"Day\":\"Mon\",\"StartTime\":\"09:00\"},null]", false),
            ("[]", false), ("null", false), ("not-json", false), ("{}", false), ("1", false), ("\"Mon\"", false),
            ("[{}]", false), ("[{\"Day\":\"Mon\"}]", false), ("[{\"Day\":null,\"StartTime\":\"09:00\"}]", false),
            ("[{\"Day\":\"Mon\",\"StartTime\":null}]", false), ("[{\"Day\":\"Mon\",\"StartTime\":9}]", false),
            ("[{\"Day\":\"Funday\",\"StartTime\":\"09:00\"}]", false), ("[{\"Day\":\"Mon\",\"StartTime\":\"25:61\"}]", false) })
            yield return new object?[] { update, days, valid };
    }

    [Theory]
    [MemberData(nameof(Schedules))]
    public async Task Schedule_contract_accepts_casing_and_optional_values_rejects_invalid_before_writes(bool update, string? days, bool valid)
    {
        using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var academy = new Academy { Name = "Synthetic" }; var course = new ProgramCourse { AcademyId = academy.Id, Name = "Synthetic" };
        var batch = new Batch { AcademyId = academy.Id, CourseId = course.Id, Name = "Original" }; db.AddRange(academy, course, batch); await db.SaveChangesAsync();
        var before = JsonSerializer.Serialize(batch); var controller = new BatchesController(db);
        var body = JsonSerializer.Serialize(new { name = "Synthetic times", courseId = course.Id, capacity = 10,
            deliveryMode = "InPerson", enrollmentStatus = "Open", meetingDaysJson = days, isActive = true });
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var result = update ? await controller.Update(academy.Id, batch.Id, JsonSerializer.Deserialize<UpdateBatchRequest>(body, options)!, default)
            : await controller.Create(academy.Id, JsonSerializer.Deserialize<CreateBatchRequest>(body, options)!, default);
        if (!valid)
        {
            var error = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.False(string.IsNullOrWhiteSpace(JsonSerializer.SerializeToElement(error.Value).GetProperty("message").GetString()));
            db.ChangeTracker.Clear(); Assert.Equal(before, JsonSerializer.Serialize(await db.Batches.SingleAsync()));
        }
        else
        {
            var response = update ? Assert.IsType<OkObjectResult>(result.Result).Value : Assert.IsType<CreatedResult>(result.Result).Value;
            var summary = Assert.IsType<BatchSummary>(response); var expected = string.IsNullOrWhiteSpace(days) ? null : days;
            Assert.Equal(expected, summary.MeetingDaysJson); db.ChangeTracker.Clear(); Assert.Equal(expected, (await db.Batches.SingleAsync(x => x.Id == summary.Id)).MeetingDaysJson);
        }
    }
}
