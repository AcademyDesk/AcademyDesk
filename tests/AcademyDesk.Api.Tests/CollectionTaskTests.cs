using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcademyDesk.Api.Tests;

public sealed class CollectionTaskTests
{
    private static AcademyDeskDbContext Context() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static AdminWorkItem Task(Guid academy, string type = "Collections") => new() {
        AcademyId = academy, Type = type, Title = "Synthetic", Description = null, Priority = "High",
        AssignedUserId = Guid.NewGuid(), EntityType = "Invoice", EntityId = Guid.NewGuid(),
        Status = "Completed", CompletedAtUtc = new DateTime(2026, 9, 1), EscalationStage = "Initial", PromisedPaymentDate = new DateOnly(2026, 10, 1) };

    [Fact]
    public async System.Threading.Tasks.Task List_only_projects_same_tenant_collections_without_assignments_or_entity_metadata()
    {
        using var db = Context(); var academy = Guid.NewGuid();
        var own = Task(academy); var other = Task(academy, "Operations"); var foreign = Task(Guid.NewGuid());
        db.AddRange(own, other, foreign); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await new FinanceGovernanceController(db).CollectionTasks(academy, default);
        var rows = Assert.IsType<List<CollectionTaskSummary>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Single(rows); Assert.Equal(own.Id, rows[0].Id); Assert.Null(rows[0].Description);
        Assert.Equal("Completed", rows[0].Status); Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(new[] { "Description", "DueAtUtc", "EscalationStage", "Id", "Priority", "PromisedPaymentDate", "Status", "Title", "Type" },
            typeof(CollectionTaskSummary).GetProperties().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal(new[] { "finance.manage" }, AcademyDesk.Api.Security.PermissionCatalog.RequiredFor("FinanceGovernanceController"));
        Assert.Null(AcademyDesk.Api.Security.PermissionCatalog.RequiredFor("AdminWorkItemsController"));
    }

    [Theory]
    [InlineData("Initial")][InlineData("Reminder")][InlineData("ManagerReview")][InlineData("FinalNotice")]
    public async System.Threading.Tasks.Task Update_changes_only_escalation_and_promise(string stage)
    {
        using var db = Context(); var row = Task(Guid.NewGuid()); db.Add(row); await db.SaveChangesAsync();
        var before = JsonSerializer.Serialize(row); var date = new DateOnly(2026, 11, 3);
        var result = await new FinanceGovernanceController(db).UpdateCollectionTask(row.AcademyId, row.Id, new(stage, date), default);
        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(stage, row.EscalationStage); Assert.Equal(date, row.PromisedPaymentDate);
        row.EscalationStage = "Initial"; row.PromisedPaymentDate = new DateOnly(2026, 10, 1);
        Assert.Equal(before, JsonSerializer.Serialize(row));
    }

    [Fact]
    public async System.Threading.Tasks.Task Promise_date_is_optional_and_can_be_cleared()
    {
        using var db = Context(); var row = Task(Guid.NewGuid()); db.Add(row); await db.SaveChangesAsync();
        var result = await new FinanceGovernanceController(db).UpdateCollectionTask(row.AcademyId, row.Id, new("Reminder", null), default);
        Assert.IsType<OkObjectResult>(result.Result); db.ChangeTracker.Clear();
        Assert.Null((await db.AdminWorkItems.SingleAsync()).PromisedPaymentDate);
    }

    [Theory]
    [InlineData(null)][InlineData("")][InlineData("Unknown")][InlineData("reminder")][InlineData("Reminder ")]
    public async System.Threading.Tasks.Task Invalid_stage_does_not_mutate(string? stage)
    {
        using var db = Context(); var row = Task(Guid.NewGuid()); db.Add(row); await db.SaveChangesAsync(); var before = JsonSerializer.Serialize(row);
        var result = await new FinanceGovernanceController(db).UpdateCollectionTask(row.AcademyId, row.Id, new(stage!, null), default);
        Assert.IsType<BadRequestObjectResult>(result.Result); Assert.Equal(before, JsonSerializer.Serialize(row));
    }

    [Theory]
    [InlineData("foreign")][InlineData("missing")][InlineData("non-collections")]
    public async System.Threading.Tasks.Task Wrong_target_is_not_found_and_not_mutated(string kind)
    {
        using var db = Context(); var academy = Guid.NewGuid(); var row = Task(kind == "foreign" ? Guid.NewGuid() : academy, kind == "non-collections" ? "Operations" : "Collections");
        db.Add(row); await db.SaveChangesAsync(); var before = JsonSerializer.Serialize(row);
        var result = await new FinanceGovernanceController(db).UpdateCollectionTask(academy, kind == "missing" ? Guid.NewGuid() : row.Id, new("Reminder", null), default);
        Assert.IsType<NotFoundResult>(result.Result); Assert.Equal(before, JsonSerializer.Serialize(row));
    }
}
