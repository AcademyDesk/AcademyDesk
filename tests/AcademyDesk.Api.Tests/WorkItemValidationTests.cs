using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class WorkItemValidationTests
{
    private sealed class Fixture : IDisposable
    {
        public readonly Guid Academy = Guid.NewGuid();
        public readonly AcademyDeskDbContext Db = new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public readonly IdentityDbContext Identity = new(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public AdminWorkItemsController Work => new(Db, Identity);
        public FinanceGovernanceController Finance => new(Db, Identity);
        public async Task<Guid> User(string kind)
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), AcademyId = kind == "foreign" ? Guid.NewGuid() : Academy, IsActive = kind != "inactive", IsPlatformOwner = kind == "platform" };
            if (kind == "missing") return user.Id;
            if (kind == "locked") { user.LockoutEnabled = true; user.LockoutEnd = DateTimeOffset.UtcNow.AddDays(1); }
            Identity.Add(user);
            if (kind != "roleless")
            {
                var role = new ApplicationRole { Id = Guid.NewGuid(), Name = kind == "teacher-lower" ? "teacher" : kind is "Student" or "Guardian" or "Teacher" ? kind : "Operations", AcademyId = kind == "foreign-role" ? Guid.NewGuid() : null };
                Identity.Add(role); Identity.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });
            }
            await Identity.SaveChangesAsync(); return user.Id;
        }
        public async Task<Invoice> Invoice()
        {
            var invoice = new Invoice { AcademyId = Academy, StudentId = Guid.NewGuid(), InvoiceNumber = "QA-WORK", TotalAmount = 100 };
            Db.Add(invoice); await Db.SaveChangesAsync(); return invoice;
        }
        public void Dispose() { Db.Dispose(); Identity.Dispose(); }
    }

    [Theory]
    [InlineData("missing")][InlineData("foreign")][InlineData("inactive")][InlineData("Student")][InlineData("Guardian")][InlineData("Teacher")][InlineData("roleless")][InlineData("foreign-role")][InlineData("platform")][InlineData("empty")][InlineData("locked")][InlineData("teacher-lower")]
    public async Task Invalid_assignee_rejected_by_both_writers_without_rows(string kind)
    {
        using var f = new Fixture(); var invoice = await f.Invoice(); var user = kind == "empty" ? Guid.Empty : await f.User(kind);
        Assert.IsType<BadRequestObjectResult>(await f.Work.Create(f.Academy, new("Operations", "Synthetic", null, null, null, null, user, null), default));
        Assert.IsType<BadRequestObjectResult>(await f.Finance.CreateFollowUp(f.Academy, invoice.Id, new(null, null, user, null), default));
        Assert.Empty(await f.Db.AdminWorkItems.AsNoTracking().ToListAsync());
        Assert.DoesNotContain(f.Db.ChangeTracker.Entries(), x => x.State == EntityState.Added);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Assigned_and_unassigned_controls_preserve_payload_and_defaults(bool assigned)
    {
        using var f = new Fixture(); var invoice = await f.Invoice(); Guid? user = assigned ? await f.User("staff") : null;
        var due = new DateTime(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc);
        var row = Assert.IsType<AdminWorkItem>(Assert.IsType<OkObjectResult>(await f.Work.Create(f.Academy, new(" Operations ", " Synthetic ", " Context ", null, null, null, user, due), default)).Value);
        Assert.Equal("Operations", row.Type); Assert.Equal("Synthetic", row.Title); Assert.Equal("Context", row.Description);
        Assert.Equal("Normal", row.Priority); Assert.Equal("Open", row.Status); Assert.Equal(user, row.AssignedUserId); Assert.Equal(due, row.DueAtUtc); Assert.Null(row.EntityId);
        var follow = Assert.IsType<AdminWorkItem>(Assert.IsType<OkObjectResult>(await f.Finance.CreateFollowUp(f.Academy, invoice.Id, new(" Note ", " High ", user, due), default)).Value);
        Assert.Equal("High", follow.Priority); Assert.Equal("Note", follow.Description); Assert.Equal(user, follow.AssignedUserId); Assert.Equal(due, follow.DueAtUtc);
        Assert.Equal("Invoice", follow.EntityType); Assert.Equal(invoice.Id, follow.EntityId); Assert.Equal("Collections", follow.Type);
        Assert.Equal(2, await f.Db.AdminWorkItems.AsNoTracking().CountAsync());
    }

    [Theory]
    [InlineData("Invoice", "missing")][InlineData("Invoice", "foreign")][InlineData("Invoice", "empty")][InlineData("Invoice", "wrong-type")]
    [InlineData("PersonDocument", "missing")][InlineData("PersonDocument", "foreign")][InlineData("PersonDocument", "empty")][InlineData("PersonDocument", "wrong-type")]
    [InlineData("Unsupported", "own")][InlineData("invoice", "own")][InlineData(null, "own")][InlineData(" ", "own")][InlineData("Invoice", "null")]
    public async Task Invalid_entity_pairs_reject_without_rows(string? type, string kind)
    {
        using var f = new Fixture(); var invoice = await f.Invoice();
        var document = new PersonDocument { AcademyId = f.Academy, DocumentType = "Synthetic", FileName = "file.pdf" }; f.Db.Add(document); await f.Db.SaveChangesAsync();
        Guid? id = kind switch { "null" => null, "empty" => Guid.Empty, "missing" => Guid.NewGuid(), "wrong-type" => type == "Invoice" ? document.Id : invoice.Id, _ => type == "PersonDocument" ? document.Id : invoice.Id };
        if (kind == "foreign") { invoice.AcademyId = Guid.NewGuid(); document.AcademyId = invoice.AcademyId; await f.Db.SaveChangesAsync(); }
        Assert.IsType<BadRequestObjectResult>(await f.Work.Create(f.Academy, new("Operations", "Synthetic", null, null, type, id, null, null), default));
        Assert.Empty(await f.Db.AdminWorkItems.AsNoTracking().ToListAsync());
    }

    [Theory][InlineData("Invoice")][InlineData("PersonDocument")]
    public async Task Supported_same_academy_entity_links_persist_with_normalized_type(string type)
    {
        using var f = new Fixture(); var invoice = await f.Invoice(); var document = new PersonDocument { AcademyId = f.Academy, DocumentType = "ID", FileName = "file.pdf" };
        f.Db.Add(document); await f.Db.SaveChangesAsync(); var id = type == "Invoice" ? invoice.Id : document.Id;
        var row = Assert.IsType<AdminWorkItem>(Assert.IsType<OkObjectResult>(await f.Work.Create(f.Academy, new("Operations", "Synthetic", null, null, " " + type + " ", id, null, null), default)).Value);
        Assert.Equal(type, row.EntityType); Assert.Equal(id, row.EntityId);
        var saved = await f.Db.AdminWorkItems.AsNoTracking().SingleAsync(); Assert.Equal(type, saved.EntityType); Assert.Equal(id, saved.EntityId);
    }

    [Theory][InlineData("Urgent")][InlineData("high")][InlineData("Critical!")]
    public async Task Follow_up_invalid_priority_is_no_write(string priority)
    {
        using var f = new Fixture(); var invoice = await f.Invoice();
        Assert.IsType<BadRequestObjectResult>(await f.Finance.CreateFollowUp(f.Academy, invoice.Id, new(null, priority, null, null), default));
        Assert.Empty(await f.Db.AdminWorkItems.ToListAsync());
    }

    [Theory][InlineData("type")][InlineData("title")][InlineData("description")]
    public async Task Oversized_text_rejects_before_SQL(string field)
    {
        using var f = new Fixture();
        Assert.IsType<BadRequestObjectResult>(await f.Work.Create(f.Academy, new(field == "type" ? new('x', 61) : "Operations", field == "title" ? new('x', 241) : "Synthetic", field == "description" ? new('x', 4001) : null, null, null, null, null, null), default));
        Assert.Empty(await f.Db.AdminWorkItems.ToListAsync());
    }
}
