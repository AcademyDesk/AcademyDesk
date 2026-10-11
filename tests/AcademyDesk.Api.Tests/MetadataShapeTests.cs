using System.Reflection;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MetadataShapeTests
{
    public static IEnumerable<object?[]> InvalidRoots() => new string?[]
        { null, "", "  ", "null", "[]", "[{}]", "\"text\"", "12", "true", "false", "{" }.Select(x => new object?[] { x });

    [Theory, MemberData(nameof(InvalidRoots))]
    public async Task Compensation_invalid_roots_return_empty_summary_without_rewriting_storage(string? metadata)
    {
        await using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var row = new Teacher { AcademyId = Guid.NewGuid(), FirstName = "Synthetic", LastName = "Metadata", CompensationJson = metadata };
        db.Add(row); await db.SaveChangesAsync();
        var summary = Assert.IsType<TeacherCompensationSummary>(Assert.IsType<OkObjectResult>(
            (await new TeacherCompensationController(db).Get(row.AcademyId, row.Id, default)).Result).Value);
        Assert.Equal(new TeacherCompensationSummary(row.Id, "", null, null, null, null, null, null), summary);
        Assert.Equal(metadata, (await db.Teachers.AsNoTracking().SingleAsync()).CompensationJson);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("{\"Model\":\"Monthly\",\"MonthlySalary\":0,\"EffectiveFrom\":\"2026-10-01\"}", "Monthly", 0)]
    [InlineData("{\"model\":\"Monthly\",\"baseMonthlySalary\":\"1200.50\",\"effectiveFrom\":\"2026-10-01\"}", "Monthly", 1200.50)]
    public async Task Compensation_object_aliases_zero_and_date_remain_compatible(string metadata, string model, decimal monthly)
    {
        await using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var row = new Teacher { AcademyId = Guid.NewGuid(), FirstName = "Synthetic", LastName = "Metadata", CompensationJson = metadata };
        db.Add(row); await db.SaveChangesAsync();
        var summary = Assert.IsType<TeacherCompensationSummary>(Assert.IsType<OkObjectResult>(
            (await new TeacherCompensationController(db).Get(row.AcademyId, row.Id, default)).Result).Value);
        Assert.Equal(model, summary.Model); Assert.Equal(monthly, summary.MonthlySalary);
        Assert.Equal(new DateOnly(2026, 10, 1), summary.EffectiveFrom);
        Assert.Equal(metadata, (await db.Teachers.AsNoTracking().SingleAsync()).CompensationJson);
    }

    // Pure existing private-reader tests; real Identity/HTTP/SQL is a separate gate.
    private static T Read<T>(Type controller, string method, params object?[] args) =>
        (T)controller.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args)!;
    private static readonly DateTime Now = new(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc);
    private const string Active = "\"important\":\"true\",\"expiresAtUtc\":\"2026-10-12T00:00:00Z\"";

    [Theory, MemberData(nameof(InvalidRoots))]
    public void Announcement_invalid_roots_fail_closed_without_throwing(string? metadata)
    {
        Assert.False(Read<bool>(typeof(PortalController), "IsActiveImportantAnnouncement", metadata, Now));
        Assert.False(Read<bool>(typeof(PortalController), "IsAnnouncementForAudience", metadata, "Admin"));
        Assert.False(Read<bool>(typeof(PlatformControlController), "HasAdminAudience", metadata));
        Assert.Null(Read<string?>(typeof(PlatformControlController), "ReadAnnouncementExpiry", metadata));
    }

    [Theory]
    [InlineData("{\"important\":true,\"expiresAtUtc\":\"2026-10-12T00:00:00Z\"}")]
    [InlineData("{\"important\":[],\"expiresAtUtc\":\"2026-10-12T00:00:00Z\"}")]
    [InlineData("{\"important\":\"true\",\"expiresAtUtc\":12}")]
    [InlineData("{\"important\":\"true\",\"expiresAtUtc\":{}}")]
    [InlineData("{" + Active + ",\"startsAtUtc\":[]}")]
    [InlineData("{" + Active + ",\"startsAtUtc\":true}")]
    public void Portal_wrong_property_types_are_not_active(string metadata) =>
        Assert.False(Read<bool>(typeof(PortalController), "IsActiveImportantAnnouncement", metadata, Now));

    [Theory]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("12")]
    [InlineData("true")]
    public void Announcement_audience_and_expiry_wrong_types_fail_closed(string value)
    {
        var metadata = "{\"audiences\":" + value + ",\"expiresAtUtc\":" + value + "}";
        Assert.False(Read<bool>(typeof(PortalController), "IsAnnouncementForAudience", metadata, "Admin"));
        Assert.False(Read<bool>(typeof(PlatformControlController), "HasAdminAudience", metadata));
        Assert.Null(Read<string?>(typeof(PlatformControlController), "ReadAnnouncementExpiry", metadata));
    }

    [Fact]
    public void Valid_objects_retain_legacy_audience_and_time_behaviour()
    {
        var admin = "{" + Active + ",\"audiences\":\"Admin\"}";
        Assert.True(Read<bool>(typeof(PortalController), "IsActiveImportantAnnouncement", admin, Now));
        Assert.True(Read<bool>(typeof(PortalController), "IsAnnouncementForAudience", admin, "Admin"));
        Assert.False(Read<bool>(typeof(PortalController), "IsAnnouncementForAudience", admin, "Teacher"));
        Assert.True(Read<bool>(typeof(PlatformControlController), "HasAdminAudience", admin));
        Assert.Equal("2026-10-12T00:00:00Z", Read<string?>(typeof(PlatformControlController), "ReadAnnouncementExpiry", admin));
        Assert.True(Read<bool>(typeof(PortalController), "IsAnnouncementForAudience", "{" + Active + "}", "Teacher"));
        Assert.True(Read<bool>(typeof(PortalController), "IsAnnouncementForAudience", "{\"audiences\":\"Both\"}", "Student"));
        Assert.False(Read<bool>(typeof(PortalController), "IsActiveImportantAnnouncement", "{" + Active + ",\"startsAtUtc\":\"2026-10-13T00:00:00Z\"}", Now));
        Assert.False(Read<bool>(typeof(PortalController), "IsActiveImportantAnnouncement", admin, Now.AddDays(2)));
    }
}
