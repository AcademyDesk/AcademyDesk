using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademyDesk.Api.Tests;

public sealed class AccessReviewProvenanceTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public AcademyDeskDbContext Db { get; } = new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public IdentityDbContext Identity { get; } = new(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public Guid Academy { get; } = Guid.NewGuid();
        public Guid Actor { get; } = Guid.NewGuid();
        public AccessReviewsController Controller(string? claim)
        {
            var registration = new ServiceCollection().AddLogging().AddSingleton(Db).AddSingleton(Identity);
            registration.AddIdentityCore<ApplicationUser>().AddRoles<ApplicationRole>().AddEntityFrameworkStores<IdentityDbContext>();
            var services = registration.BuildServiceProvider();
            var controller = ActivatorUtilities.CreateInstance<AccessReviewsController>(services);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claim is null ? [] : [new Claim(ClaimTypes.NameIdentifier, claim)], "Synthetic"));
            return controller;
        }
        public async Task Seed(string mode = "valid")
        {
            Db.Academies.Add(new Academy { Id = Academy, Name = "Synthetic provenance academy" });
            if (mode != "missing") Identity.Users.Add(new ApplicationUser { Id = Actor, DisplayName = "Synthetic reviewer", AcademyId = mode is "foreign" or "platform" ? Guid.NewGuid() : Academy, IsActive = mode != "inactive", IsPlatformOwner = mode == "platform", LockoutEnabled = mode == "locked", LockoutEnd = mode == "locked" ? DateTimeOffset.UtcNow.AddHours(1) : null });
            await Db.SaveChangesAsync(); await Identity.SaveChangesAsync();
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await Identity.DisposeAsync(); }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Sign_off_records_server_reviewer_and_exact_review_task_audit_receipt(bool followUp)
    {
        await using var f = new Fixture(); await f.Seed();
        var result = await f.Controller(f.Actor.ToString()).SignOff(f.Academy, new("  Synthetic reviewed scope  ", followUp), default);
        var value = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(result).Value);
        var review = Assert.Single(await f.Db.AccessReviews.AsNoTracking().ToListAsync());
        Assert.Equal(f.Actor, review.ReviewerUserId); Assert.Equal(f.Academy, review.AcademyId);
        Assert.Equal("Synthetic reviewed scope", review.Notes); Assert.Equal(review.Id, value.GetProperty("Id").GetGuid());
        Assert.Equal(f.Actor, value.GetProperty("ReviewerUserId").GetGuid());
        var audit = Assert.Single(await f.Db.AuditLogs.AsNoTracking().ToListAsync());
        Assert.Equal("AccessReviewSignedOff", audit.Action); Assert.Equal("AccessReview", audit.EntityType);
        Assert.Equal(review.Id, audit.EntityId); Assert.Equal(f.Actor, audit.ActorUserId); Assert.Equal(f.Academy, audit.AcademyId);
        using var metadata = JsonDocument.Parse(audit.MetadataJson!);
        Assert.Equal(review.Id, metadata.RootElement.GetProperty("ReviewId").GetGuid());
        if (followUp)
        {
            var task = Assert.Single(await f.Db.AdminWorkItems.AsNoTracking().ToListAsync());
            Assert.Equal(f.Academy, task.AcademyId); Assert.Equal("AccessReview", task.Type);
            Assert.Equal("AccessReview", task.EntityType); Assert.Equal(review.Id, task.EntityId);
            Assert.Equal("High", task.Priority); Assert.Null(task.AssignedUserId);
            Assert.Equal(task.Id, value.GetProperty("FollowUpId").GetGuid());
            Assert.Equal(task.Id, metadata.RootElement.GetProperty("FollowUpId").GetGuid());
        }
        else { Assert.Empty(await f.Db.AdminWorkItems.ToListAsync()); Assert.Equal(JsonValueKind.Null, value.GetProperty("FollowUpId").ValueKind); }
    }

    [Theory]
    [InlineData("anonymous")] [InlineData("malformed")] [InlineData("missing")]
    [InlineData("inactive")] [InlineData("locked")] [InlineData("foreign")]
    public async Task Unresolved_or_unavailable_actor_cannot_create_attestation(string mode)
    {
        await using var f = new Fixture(); await f.Seed(mode);
        var claim = mode == "anonymous" ? null : mode == "malformed" ? "invalid" : f.Actor.ToString();
        var result = await f.Controller(claim).SignOff(f.Academy, new("Synthetic", true), default);
        Assert.True(result is UnauthorizedResult or ForbidResult);
        Assert.Empty(await f.Db.AccessReviews.ToListAsync()); Assert.Empty(await f.Db.AdminWorkItems.ToListAsync()); Assert.Empty(await f.Db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Existing_platform_route_records_actual_actor_without_assigning_foreign_staff(bool followUp)
    {
        await using var f = new Fixture(); await f.Seed("platform");
        Assert.IsType<OkObjectResult>(await f.Controller(f.Actor.ToString()).SignOff(f.Academy, new(null, followUp), default));
        Assert.Equal(f.Actor, (await f.Db.AccessReviews.SingleAsync()).ReviewerUserId);
        Assert.All(await f.Db.AdminWorkItems.ToListAsync(), task => { Assert.Null(task.AssignedUserId); Assert.Equal(f.Academy, task.AcademyId); });
    }

    [Fact]
    public async Task Overlong_follow_up_notes_are_rejected_without_partial_attestation()
    {
        await using var f = new Fixture(); await f.Seed();
        Assert.IsType<BadRequestObjectResult>(await f.Controller(f.Actor.ToString()).SignOff(f.Academy, new(new string('x', 4001), true), default));
        Assert.Empty(await f.Db.AccessReviews.ToListAsync()); Assert.Empty(await f.Db.AdminWorkItems.ToListAsync()); Assert.Empty(await f.Db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Separate_sign_offs_keep_separate_review_and_task_correlations()
    {
        await using var f = new Fixture(); await f.Seed();
        var controller = f.Controller(f.Actor.ToString());
        for (var i = 0; i < 2; i++) Assert.IsType<OkObjectResult>(await controller.SignOff(f.Academy, new("Synthetic " + i, true), default));
        var reviews = await f.Db.AccessReviews.ToListAsync(); Assert.Equal(2, reviews.Count);
        var tasks = await f.Db.AdminWorkItems.ToListAsync(); Assert.Equal(2, tasks.Count);
        Assert.All(reviews, review => Assert.Single(tasks, task => task.EntityId == review.Id));
    }

    [Fact]
    public void Sign_off_opts_into_domain_audit_transaction_including_existing_platform_path_only()
    {
        var descriptor = new ControllerActionDescriptor { MethodInfo = typeof(AccessReviewsController).GetMethod("SignOff")! };
        Assert.True(AcademyAccessFilter.UsesAtomicBoundary(nameof(AccessReviewsController), descriptor));
        Assert.True(AcademyAccessFilter.IncludesPlatformOwner(descriptor)); Assert.False(AcademyAccessFilter.IncludesIdentity(descriptor));
        Assert.False(AcademyAccessFilter.UsesAtomicBoundary(nameof(AccessReviewsController), new ControllerActionDescriptor { MethodInfo = typeof(AccessReviewsController).GetMethod("List")! }));
    }
}
