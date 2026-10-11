using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademyDesk.Api.Tests;

public sealed class WorkItemIntegrityTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public AcademyDeskDbContext Db { get; } = new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public IdentityDbContext Identity { get; } = new(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public Guid Academy { get; } = Guid.NewGuid();
        public Guid Foreign { get; } = Guid.NewGuid();
        public Invoice Invoice { get; private set; } = null!;
        public PersonDocument Document { get; private set; } = null!;
        public async Task Seed()
        {
            Db.AddRange(new Academy { Id = Academy, Name = "Work fixture" }, new Academy { Id = Foreign, Name = "Foreign sentinel" });
            Invoice = new() { AcademyId = Academy, InvoiceNumber = "QA-WORK", StudentId = Guid.NewGuid(), TotalAmount = 1000 };
            Document = new() { AcademyId = Academy, DocumentType = "Identity", FileName = "synthetic.txt" };
            Db.AddRange(Invoice, Document, new AdminWorkItem { AcademyId = Foreign, Type = "Operations", Title = "Sentinel" });
            await Db.SaveChangesAsync();
        }
        public T Controller<T>() where T : class
        {
            var services = new ServiceCollection().AddSingleton(Db).AddSingleton(Identity).BuildServiceProvider();
            return ActivatorUtilities.CreateInstance<T>(services);
        }
        public async Task<Guid> Actor(string mode)
        {
            var actor = new ApplicationUser { Id = Guid.NewGuid(), AcademyId = mode == "foreign" ? Foreign : Academy, DisplayName = "Synthetic assignee", IsActive = mode != "inactive", IsPlatformOwner = mode == "platform" };
            if (mode == "locked") { actor.LockoutEnabled = true; actor.LockoutEnd = DateTimeOffset.UtcNow.AddDays(1); }
            Identity.Users.Add(actor);
            var role = new ApplicationRole { Id = Guid.NewGuid(), Name = mode == "mixed" ? "Teacher" : mode, NormalizedName = mode.ToUpperInvariant() };
            Identity.Roles.Add(role); Identity.UserRoles.Add(new() { UserId = actor.Id, RoleId = role.Id });
            if (mode == "mixed") { var staff = new ApplicationRole { Id = Guid.NewGuid(), Name = "Operations", NormalizedName = "OPERATIONS" }; Identity.Roles.Add(staff); Identity.UserRoles.Add(new() { UserId = actor.Id, RoleId = staff.Id }); }
            await Identity.SaveChangesAsync(); return actor.Id;
        }
        public async Task<string> Snapshot() => JsonSerializer.Serialize(new { Work = await Db.AdminWorkItems.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            Invoices = await Db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Documents = await Db.PersonDocuments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            Audits = await Db.AuditLogs.AsNoTracking().ToArrayAsync(), Users = await Identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.IsActive, x.IsPlatformOwner }).ToArrayAsync() });
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await Identity.DisposeAsync(); }
    }
    private static CreateWorkItem Valid() => new("Operations", " Synthetic work ", null, null, null, null, null, null);

    [Theory]
    [InlineData("foreign")][InlineData("missing")][InlineData("empty")][InlineData("inactive")][InlineData("platform")][InlineData("locked")]
    [InlineData("Teacher")][InlineData("Student")][InlineData("Guardian")][InlineData("mixed")]
    public async Task Invalid_assignee_is_rejected_by_both_creation_paths_without_write(string mode)
    {
        await using var f = new Fixture(); await f.Seed();
        var id = mode == "empty" ? Guid.Empty : mode == "missing" ? Guid.NewGuid() : await f.Actor(mode);
        var before = await f.Snapshot();
        Assert.IsType<BadRequestObjectResult>(await f.Controller<AdminWorkItemsController>().Create(f.Academy, Valid() with { AssignedUserId = id }, default));
        Assert.Equal(before, await f.Snapshot());
        Assert.IsType<BadRequestObjectResult>(await f.Controller<FinanceGovernanceController>().CreateFollowUp(f.Academy, f.Invoice.Id, new(null, null, id, null), default));
        Assert.Equal(before, await f.Snapshot());
    }

    [Theory]
    [InlineData("Owner")][InlineData("AcademyAdmin")][InlineData("Manager")][InlineData("Operations")][InlineData("Sales")][InlineData("Marketing")]
    [InlineData("FinanceUser")][InlineData("FrontDesk")][InlineData("CustomStaff")][InlineData(null)]
    public async Task Active_same_academy_staff_and_unassigned_remain_valid(string? role)
    {
        await using var f = new Fixture(); await f.Seed(); var actor = role is null ? (Guid?)null : await f.Actor(role);
        var result = Assert.IsType<OkObjectResult>(await f.Controller<AdminWorkItemsController>().Create(f.Academy, Valid() with { AssignedUserId = actor }, default));
        var row = Assert.IsType<AdminWorkItem>(result.Value); Assert.Equal(actor, row.AssignedUserId); Assert.Equal("Normal", row.Priority); Assert.Null(row.EntityId); Assert.Null(row.DueAtUtc);
        Assert.IsType<OkObjectResult>(await f.Controller<FinanceGovernanceController>().CreateFollowUp(f.Academy, f.Invoice.Id, new(null, null, actor, null), default));
        Assert.Equal(2, await f.Db.AdminWorkItems.CountAsync(x => x.AcademyId == f.Academy));
    }

    [Theory]
    [InlineData("type-only")][InlineData("id-only")][InlineData("unknown")][InlineData("case")][InlineData("empty")][InlineData("missing")]
    [InlineData("foreign")][InlineData("wrong-table")][InlineData("collections-document")][InlineData("compliance-invoice")]
    public async Task Invalid_entity_pair_is_rejected_without_write(string mode)
    {
        await using var f = new Fixture(); await f.Seed();
        var other = new Invoice { AcademyId = f.Foreign, StudentId = Guid.NewGuid(), InvoiceNumber = "FOREIGN", TotalAmount = 10 }; f.Db.Add(other); await f.Db.SaveChangesAsync();
        var r = Valid() with { EntityType = "Invoice", EntityId = f.Invoice.Id };
        r = mode switch { "type-only" => r with { EntityId = null }, "id-only" => r with { EntityType = null }, "unknown" => r with { EntityType = "Student" },
            "case" => r with { EntityType = "invoice" }, "empty" => r with { EntityId = Guid.Empty }, "missing" => r with { EntityId = Guid.NewGuid() },
            "foreign" => r with { EntityId = other.Id }, "wrong-table" => r with { EntityId = f.Document.Id },
            "collections-document" => r with { Type = "Collections", EntityType = "PersonDocument", EntityId = f.Document.Id },
            "compliance-invoice" => r with { Type = "Compliance" }, _ => throw new InvalidOperationException() };
        var before = await f.Snapshot(); Assert.IsType<BadRequestObjectResult>(await f.Controller<AdminWorkItemsController>().Create(f.Academy, r, default)); Assert.Equal(before, await f.Snapshot());
    }

    [Theory][InlineData("Invoice")][InlineData("PersonDocument")][InlineData(" ")]
    public async Task Known_same_tenant_links_and_blank_unlinked_control_are_valid(string type)
    {
        await using var f = new Fixture(); await f.Seed();
        var r = Valid() with { EntityType = type, EntityId = type == "Invoice" ? f.Invoice.Id : type == "PersonDocument" ? f.Document.Id : null };
        var row = Assert.IsType<AdminWorkItem>(Assert.IsType<OkObjectResult>(await f.Controller<AdminWorkItemsController>().Create(f.Academy, r, default)).Value);
        Assert.Equal(string.IsNullOrWhiteSpace(type) ? null : type, row.EntityType); Assert.Equal(r.EntityId, row.EntityId);
    }

    [Theory][InlineData("type")][InlineData("title")][InlineData("description")]
    public async Task Database_string_overflow_is_400_before_write(string field)
    {
        await using var f = new Fixture(); await f.Seed(); var r = Valid();
        r = field switch { "type" => r with { Type = new('x',61) }, "title" => r with { Title = new('x',241) }, _ => r with { Description = new('x',4001) } };
        var before = await f.Snapshot(); Assert.IsType<BadRequestObjectResult>(await f.Controller<AdminWorkItemsController>().Create(f.Academy, r, default)); Assert.Equal(before, await f.Snapshot());
    }
}
