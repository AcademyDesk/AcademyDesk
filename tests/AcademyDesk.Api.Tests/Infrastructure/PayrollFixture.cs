using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests.Infrastructure;

// Legitimate synthetic same-academy workers for retained payroll unit fixtures.
internal static class PayrollFixture
{
    internal static PayrollController Controller(AcademyDeskDbContext db, string? additionalStaffId = null)
    {
        var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase("qa-payroll-identity-" + Guid.NewGuid()).Options);
        foreach (var profile in db.PayrollProfiles.ToArray())
        {
            if (profile.TeacherId is Guid teacher && !db.Teachers.Any(x => x.Id == teacher))
                db.Teachers.Add(new Teacher { Id = teacher, AcademyId = profile.AcademyId, FirstName = "Synthetic", LastName = "Payroll" });
            if (Guid.TryParse(profile.StaffUserId, out var staff) && !identity.Users.Local.Any(x => x.Id == staff))
                identity.Users.Add(new ApplicationUser { Id = staff, AcademyId = profile.AcademyId, DisplayName = "Synthetic payroll staff" });
            if (Guid.TryParse(additionalStaffId, out var extra) && !identity.Users.Local.Any(x => x.Id == extra))
                identity.Users.Add(new ApplicationUser { Id = extra, AcademyId = profile.AcademyId, DisplayName = "Synthetic replacement staff" });
        }
        db.SaveChanges(); identity.SaveChanges();
        return new PayrollController(db, identity);
    }
}
