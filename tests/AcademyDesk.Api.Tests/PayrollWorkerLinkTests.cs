using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class PayrollWorkerLinkTests
{
    [Theory]
    [InlineData("Teacher", "missing")]
    [InlineData("Teacher", "foreign")]
    [InlineData("Teacher", "both")]
    [InlineData("Staff", "missing")]
    [InlineData("Staff", "foreign")]
    [InlineData("Staff", "malformed")]
    [InlineData("Staff", "both")]
    [InlineData("Staff", "Teacher")]
    [InlineData("Staff", "Student")]
    [InlineData("Staff", "Guardian")]
    [InlineData("Staff", "linked-teacher")]
    public async Task Invalid_worker_links_refuse_create_update_and_payout_without_mutation(string worker, string mode)
    {
        await using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseInMemoryDatabase("qa-worker-link-" + Guid.NewGuid()).Options);
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase("qa-worker-link-identity-" + Guid.NewGuid()).Options);
        var academy = Guid.NewGuid(); var teacher = new Teacher { AcademyId = academy, FirstName = "Synthetic", LastName = "Worker" };
        var member = new ApplicationUser { Id = Guid.NewGuid(), AcademyId = academy, DisplayName = "Synthetic worker" };
        db.Add(teacher); identity.Add(member);
        if (mode == "foreign") { teacher.AcademyId = Guid.NewGuid(); member.AcademyId = teacher.AcademyId; }
        if (mode == "linked-teacher") member.TeacherId = teacher.Id;
        if (mode is "Teacher" or "Student" or "Guardian")
        {
            var role = new ApplicationRole { Id = Guid.NewGuid(), Name = mode, NormalizedName = mode.ToUpperInvariant() };
            identity.Add(role); identity.Add(new IdentityUserRole<Guid> { UserId = member.Id, RoleId = role.Id });
        }
        await identity.SaveChangesAsync();
        Guid? teacherId = worker == "Teacher" || mode == "both" ? teacher.Id : null;
        var staffId = worker == "Staff" || mode == "both" ? member.Id.ToString() : null;
        if (mode == "missing") { if (worker == "Teacher") teacherId = Guid.NewGuid(); else staffId = Guid.NewGuid().ToString(); }
        if (mode == "malformed") staffId = "invalid-worker";
        var profile = new PayrollProfile { AcademyId = academy, WorkerType = worker, TeacherId = teacherId,
            StaffUserId = staffId, WorkerName = "Stored synthetic", MonthlyAmount = 1000m };
        db.Add(profile); await db.SaveChangesAsync();
        var controller = new PayrollController(db, identity);
        var before = JsonSerializer.Serialize(new { Profiles = await db.PayrollProfiles.AsNoTracking().ToArrayAsync(), Payouts = await db.PayrollPayouts.AsNoTracking().ToArrayAsync() });
        Assert.IsType<BadRequestObjectResult>((await controller.CreateProfile(academy,
            new(worker, teacherId, staffId, "Rejected create", "Monthly", 1000m, null, null, null), default)).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.UpdateProfile(academy, profile.Id,
            new(worker, teacherId, staffId, "Rejected update", "Monthly", 2000m, null, null, null), default)).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.Pay(academy,
            new(profile.Id, "2026-10", null, null, 0m, null, null, null), default)).Result);
        db.ChangeTracker.Clear();
        Assert.Equal(before, JsonSerializer.Serialize(new { Profiles = await db.PayrollProfiles.AsNoTracking().ToArrayAsync(), Payouts = await db.PayrollPayouts.AsNoTracking().ToArrayAsync() }));
    }
}
