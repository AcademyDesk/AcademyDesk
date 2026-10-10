using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

// Controller/EF InMemory projection proof; SQL/HTTP authority is tested separately.
public sealed class TeacherAvailabilityProfileTests
{
    public static TheoryData<string?, int> Cases => new()
    {
        { null, 0 }, { "", 0 }, { "   ", 0 }, { "null", 0 }, { "[]", 0 },
        { "[null]", 0 }, { "[null,{\"day\":\"Monday\",\"from\":\"09:00\",\"to\":\"10:00\"},null]", 1 },
        { "[{\"Day\":\"Tuesday\",\"From\":null,\"To\":null}]", 1 },
        { "[{}]", 0 }, { "{}", 0 }, { "1", 0 }, { "broken", 0 }, { "[{\"day\":42}]", 0 }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Stored_optional_availability_is_readable_without_mutation(string? json, int expected)
    {
        using var db = Database();
        var teacher = new Teacher { AcademyId = Guid.NewGuid(), FirstName = "Synthetic", LastName = "Teacher", AvailabilityJson = json };
        db.Teachers.Add(teacher); await db.SaveChangesAsync();
        var result = await new ProfilesController(db).Teacher(teacher.AcademyId, teacher.Id, default);
        var profile = Assert.IsType<TeacherProfileSummary>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(expected, profile.Availability.Count);
        if (expected == 1) Assert.Contains(profile.Availability[0].Day, new[] { "Monday", "Tuesday" });
        db.ChangeTracker.Clear(); Assert.Equal(json, (await db.Teachers.SingleAsync()).AvailabilityJson);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Save_optional_metadata_returns_readable_profile_and_preserves_storage_contract(string? json, int expected)
    {
        using var db = Database();
        var teacher = new Teacher { AcademyId = Guid.NewGuid(), FirstName = "Synthetic", LastName = "Teacher" };
        db.Teachers.Add(teacher); await db.SaveChangesAsync();
        var request = new UpdateTeacherAdminProfileRequest(null, "  Saved name  ", null, null, null, null, null, null, null, null, null, null, null, null, json);
        var result = await new ProfilesController(db).UpdateTeacherProfile(teacher.AcademyId, teacher.Id, request, default);
        var profile = Assert.IsType<TeacherProfileSummary>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(expected, profile.Availability.Count); Assert.Equal("Saved name", profile.PreferredName);
        db.ChangeTracker.Clear(); var saved = await db.Teachers.SingleAsync();
        Assert.Equal(string.IsNullOrWhiteSpace(json) ? null : json.Trim(), saved.AvailabilityJson);
        Assert.Equal("Saved name", saved.PreferredName);
        Assert.Null(saved.DateOfBirth); Assert.Null(saved.JoiningDate);
        Assert.Equal("Synthetic", saved.FirstName); Assert.Equal("Teacher", saved.LastName);
    }

    private static AcademyDeskDbContext Database() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
