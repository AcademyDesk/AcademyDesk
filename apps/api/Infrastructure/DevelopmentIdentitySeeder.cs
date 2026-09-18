using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure;

public static class DevelopmentIdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var role in new[] { "PlatformOwner", "AcademyAdmin", "Manager", "FinanceUser", "Teacher", "Student" })
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new ApplicationRole { Name = role });

        var academy = await db.Academies.FirstOrDefaultAsync();
        if (academy is null) { academy = new Academy { Name = "Hayansh Music Academy", CountryCode = "IN", TimeZone = "Asia/Kolkata" }; db.Academies.Add(academy); await db.SaveChangesAsync(); }
        var teacher = await db.Teachers.FirstOrDefaultAsync(x => x.AcademyId == academy.Id && x.Email == "hayansh1@academydesk.local");
        if (teacher is null) { teacher = new Teacher { AcademyId = academy.Id, FirstName = "Hayansh", LastName = "Teacher", Email = "hayansh1@academydesk.local" }; db.Teachers.Add(teacher); }
        var student = await db.Students.FirstOrDefaultAsync(x => x.AcademyId == academy.Id && x.Email == "hayansh@academydesk.local");
        if (student is null) { student = new Student { AcademyId = academy.Id, FirstName = "Haynsh", LastName = "Student", Email = "hayansh@academydesk.local" }; db.Students.Add(student); }
        await db.SaveChangesAsync();
        await EnsureUser(users, "Shashank", "Shashank@academydesk.local", "Shashank", null, true, "PlatformOwner");
        await EnsureUser(users, "Kavya", "Kavya@academydesk.local", "Kavya", academy.Id, false, "AcademyAdmin");
        await EnsureUser(users, "Finance", "finance@academydesk.local", "Finance user", academy.Id, false, "FinanceUser");
        await EnsureUser(users, "Haynsh", "Haynsh@academydesk.local", "Haynsh", academy.Id, false, "Student", student.Id, null);
        await EnsureUser(users, "Hayansh1", "Hayansh1@academydesk.local", "Hayansh1", academy.Id, false, "Teacher", null, teacher.Id);
    }

    private static async Task EnsureUser(UserManager<ApplicationUser> users, string userName, string email, string displayName, Guid? academyId, bool platformOwner, string role, Guid? studentId = null, Guid? teacherId = null)
    {
        var user = await users.FindByEmailAsync(email) ?? await users.FindByNameAsync(userName);
        if (user is null)
        {
            // ASP.NET Core's built-in Identity login endpoint authenticates against
            // UserName even though its JSON property is named "email". Keep the
            // friendly ID in DisplayName and store the login address as UserName.
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = displayName, AcademyId = academyId, IsPlatformOwner = platformOwner, StudentId = studentId, TeacherId = teacherId };
            var result = await users.CreateAsync(user, "Test@123");
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
        else
        {
            user.UserName = email;
            user.Email = email;
            user.EmailConfirmed = true;
            user.DisplayName = displayName;
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            user.AcademyId = academyId;
            user.IsPlatformOwner = platformOwner;
            user.StudentId = studentId;
            user.TeacherId = teacherId;
            await users.UpdateAsync(user);

            // These are intentionally fixed, development-only demo accounts.  Resetting
            // them on startup makes the documented test sign-ins deterministic even if
            // a previous local test changed a password.
            var resetToken = await users.GeneratePasswordResetTokenAsync(user);
            var reset = await users.ResetPasswordAsync(user, resetToken, "Test@123");
            if (!reset.Succeeded) throw new InvalidOperationException(string.Join("; ", reset.Errors.Select(x => x.Description)));
        }
        if (!await users.CheckPasswordAsync(user, "Test@123")) throw new InvalidOperationException($"Unable to verify development sign-in for {userName}.");
        if (!await users.IsInRoleAsync(user, role)) await users.AddToRoleAsync(user, role);
    }
}
