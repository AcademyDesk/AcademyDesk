using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

        // Development-only teacher workspace data. It keeps every Teacher Admin
        // screen and the seeded teacher portal meaningful on a fresh local install.
        var piano = await EnsureCourse(db, academy.Id, "Piano Foundations", "MUS-PIANO-01", "Piano", "Beginner");
        var violin = await EnsureCourse(db, academy.Id, "Violin Essentials", "MUS-VIOLIN-01", "Violin", "Intermediate");
        var vocals = await EnsureCourse(db, academy.Id, "Contemporary Vocals", "MUS-VOCAL-01", "Vocals", "All levels");
        var hayansh = await EnsureTeacher(db, academy.Id, "Hayansh", "Teacher", "hayansh1@academydesk.local", "T-1001", "Piano", "Monthly", 32000m, null, true);
        var ananya = await EnsureTeacher(db, academy.Id, "Ananya", "Rao", "ananya.rao@academydesk.local", "T-1002", "Violin", "Monthly", 38000m, null, true);
        var arjun = await EnsureTeacher(db, academy.Id, "Arjun", "Mehta", "arjun.mehta@academydesk.local", "T-1003", "Vocals, Guitar", "Hourly", null, 850m, true);
        _ = await EnsureTeacher(db, academy.Id, "Nisha", "Iyer", "nisha.iyer@academydesk.local", "T-1004", "Keyboard", "Monthly", 28000m, null, false);
        await db.SaveChangesAsync();

        var pianoBatch = await EnsureBatch(db, academy.Id, piano.Id, hayansh.Id, "Piano Foundations — Evening", "PF-SEP-01", "Offline", "Studio 1");
        var violinBatch = await EnsureBatch(db, academy.Id, violin.Id, ananya.Id, "Violin Essentials — Weekend", "VE-SEP-01", "Hybrid", "https://meet.example.com/violin");
        _ = await EnsureBatch(db, academy.Id, vocals.Id, arjun.Id, "Contemporary Vocals — Online", "CV-SEP-01", "Online", "https://meet.example.com/vocals");
        var roster = new[]
        {
            student,
            await EnsureStudent(db, academy.Id, "Aarav", "Sharma", "aarav.sharma@academydesk.local", "ST-1002"),
            await EnsureStudent(db, academy.Id, "Meera", "Kapoor", "meera.kapoor@academydesk.local", "ST-1003"),
            await EnsureStudent(db, academy.Id, "Riya", "Menon", "riya.menon@academydesk.local", "ST-1004")
        };
        await db.SaveChangesAsync();
        foreach (var learner in roster)
            if (!await db.Enrollments.AnyAsync(x => x.AcademyId == academy.Id && x.StudentId == learner.Id && x.BatchId == pianoBatch.Id))
                db.Enrollments.Add(new Enrollment { AcademyId = academy.Id, StudentId = learner.Id, BatchId = pianoBatch.Id, StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)) });
        var nextSession = DateTime.UtcNow.Date.AddDays(1).AddHours(12);
        if (!await db.ClassSessions.AnyAsync(x => x.AcademyId == academy.Id && x.BatchId == pianoBatch.Id && x.StartUtc == nextSession))
            db.ClassSessions.Add(new ClassSession { AcademyId = academy.Id, BatchId = pianoBatch.Id, TeacherId = hayansh.Id, StartUtc = nextSession, EndUtc = nextSession.AddHours(1), DeliveryMode = "Offline", RoomName = "Studio 1", Status = "Scheduled" });
        if (!await db.ClassSessions.AnyAsync(x => x.AcademyId == academy.Id && x.BatchId == violinBatch.Id && x.StartUtc == nextSession.AddDays(1)))
            db.ClassSessions.Add(new ClassSession { AcademyId = academy.Id, BatchId = violinBatch.Id, TeacherId = ananya.Id, StartUtc = nextSession.AddDays(1), EndUtc = nextSession.AddDays(1).AddHours(1), DeliveryMode = "Hybrid", RoomName = "Studio 2", Status = "Scheduled" });
        await db.SaveChangesAsync();
        var hayanshPayroll = await db.PayrollProfiles.FirstOrDefaultAsync(x => x.AcademyId == academy.Id && x.TeacherId == hayansh.Id);
        if (hayanshPayroll is null)
        {
            hayanshPayroll = new PayrollProfile { AcademyId = academy.Id, TeacherId = hayansh.Id, WorkerType = "Teacher", WorkerName = "Hayansh Teacher", PaymentModel = "Monthly", MonthlyAmount = 32000m, EffectiveFrom = new DateOnly(2025, 4, 1) };
            db.PayrollProfiles.Add(hayanshPayroll);
            await db.SaveChangesAsync();
        }
        if (!await db.PayrollPayouts.AnyAsync(x => x.AcademyId == academy.Id && x.PayrollProfileId == hayanshPayroll.Id))
            db.PayrollPayouts.Add(new PayrollPayout { AcademyId = academy.Id, PayrollProfileId = hayanshPayroll.Id, PayslipNumber = "PS-DEMO-1001", PeriodLabel = "August 2026", GrossAmount = 32000m, Deductions = 1200m, NetAmount = 30800m, Status = "Paid", PaymentMethod = "Bank transfer", Reference = "DEMO-PAY-1001", PaidAtUtc = DateTime.UtcNow.AddDays(-12) });
        if (!await db.LearningResources.AnyAsync(x => x.AcademyId == academy.Id && x.BatchId == pianoBatch.Id))
        {
            db.LearningResources.AddRange(
                new LearningResource { AcademyId = academy.Id, BatchId = pianoBatch.Id, Title = "Week 1 — C major scale practice", Description = "Practice hands separately at a slow tempo for 10 minutes each day.", Type = "Homework material", Url = "note://piano-week-1", IsPublished = true },
                new LearningResource { AcademyId = academy.Id, BatchId = pianoBatch.Id, Title = "Lesson note — posture and hand shape", Description = "Use this class note to revise the technique covered in today’s lesson.", Type = "Class note", Url = "note://piano-posture", IsPublished = true });
        }
        await db.SaveChangesAsync();

        // Development-only sample records make the Platform Owner workspace useful
        // immediately after a local install. They are inserted only when absent and
        // can be removed from the Platform Control Centre during testing.
        if (!await db.PlatformSupportCases.AnyAsync(x => x.AcademyId == academy.Id))
        {
            db.PlatformSupportCases.Add(new PlatformSupportCase
            {
                AcademyId = academy.Id,
                Subject = "Review demo academy onboarding",
                Priority = "Normal",
                Description = "Development sample for validating the Platform Owner support workflow."
            });
        }
        if (!await db.PlatformBillingInvoices.AnyAsync(x => x.InvoiceNumber == "AD-DEMO-0001"))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.PlatformBillingInvoices.Add(new PlatformBillingInvoice
            {
                AcademyId = academy.Id,
                InvoiceNumber = "AD-DEMO-0001",
                Amount = 2999m,
                Currency = "INR",
                Status = "Issued",
                PeriodStart = today.AddDays(-30),
                PeriodEnd = today,
                DueDate = today.AddDays(7)
            });
        }
        if (!await db.PlatformAuditEntries.AnyAsync(x => x.Action == "Development platform data initialized"))
        {
            db.PlatformAuditEntries.Add(new PlatformAuditEntry
            {
                ActorName = "Development seed",
                Action = "Development platform data initialized",
                EntityType = "Platform",
                EntityId = academy.Id,
                MetadataJson = "{\"purpose\":\"local workflow validation\"}"
            });
        }
        await db.SaveChangesAsync();

        await EnsureUser(users, "Shashank", "Shashank@academydesk.local", "Shashank", null, true, "PlatformOwner", password: "Test\\@123");
        await EnsureUser(users, "Kavya", "Kavya@academydesk.local", "Kavya", academy.Id, false, "AcademyAdmin");
        await EnsureUser(users, "Finance", "finance@academydesk.local", "Finance user", academy.Id, false, "FinanceUser");
        await EnsureUser(users, "Haynsh", "Haynsh@academydesk.local", "Haynsh", academy.Id, false, "Student", student.Id, null);
        // Active teaching profiles have matching portal accounts, so the Teacher
        // workspace can be tested with a real linked profile rather than a stub.
        await EnsureUser(users, "Hayansh1", "Hayansh1@academydesk.local", "Hayansh1", academy.Id, false, "Teacher", null, hayansh.Id);
        await EnsureUser(users, "Ananya.Rao", "ananya.rao@academydesk.local", "Ananya Rao", academy.Id, false, "Teacher", null, ananya.Id);
        await EnsureUser(users, "Arjun.Mehta", "arjun.mehta@academydesk.local", "Arjun Mehta", academy.Id, false, "Teacher", null, arjun.Id);
    }

    private static async Task EnsureUser(UserManager<ApplicationUser> users, string userName, string email, string displayName, Guid? academyId, bool platformOwner, string role, Guid? studentId = null, Guid? teacherId = null, string password = "Test@123")
    {
        var user = await users.FindByEmailAsync(email) ?? await users.FindByNameAsync(userName);
        var created = false;
        if (user is null)
        {
            // ASP.NET Core's built-in Identity login endpoint authenticates against
            // UserName even though its JSON property is named "email". Keep the
            // friendly ID in DisplayName and store the login address as UserName.
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = displayName, AcademyId = academyId, IsPlatformOwner = platformOwner, StudentId = studentId, TeacherId = teacherId };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            created = true;
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

        }
        if (created && !await users.CheckPasswordAsync(user, password)) throw new InvalidOperationException($"Unable to verify development sign-in for {userName}.");
        if (!await users.IsInRoleAsync(user, role)) await users.AddToRoleAsync(user, role);
    }

    private static async Task<ProgramCourse> EnsureCourse(AcademyDeskDbContext db, Guid academyId, string name, string code, string subject, string level)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.AcademyId == academyId && x.CourseCode == code);
        if (course is not null) return course;
        course = new ProgramCourse { AcademyId = academyId, Name = name, CourseCode = code, AcademyType = "Music", SubjectArea = subject, Level = level, DurationMonths = 6, WeeklySessions = 2, SessionMinutes = 60, DeliveryMode = "Hybrid", IsPublished = true };
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return course;
    }

    private static async Task<Teacher> EnsureTeacher(AcademyDeskDbContext db, Guid academyId, string firstName, string lastName, string email, string employeeCode, string subjects, string paymentModel, decimal? monthlySalary, decimal? hourlyRate, bool active)
    {
        var teacher = await db.Teachers.FirstOrDefaultAsync(x => x.AcademyId == academyId && x.Email == email);
        var compensation = paymentModel == "Monthly"
            ? JsonSerializer.Serialize(new { model = "Monthly", monthlySalary, effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow) })
            : JsonSerializer.Serialize(new { model = "Hourly", standardHourlyRate = hourlyRate, beginnerHourlyRate = hourlyRate, intermediateHourlyRate = hourlyRate + 100m, advancedHourlyRate = hourlyRate + 200m, effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow) });
        if (teacher is null)
        {
            teacher = new Teacher { AcademyId = academyId, FirstName = firstName, LastName = lastName, Email = email };
            db.Teachers.Add(teacher);
        }
        teacher.EmployeeCode = employeeCode;
        teacher.PreferredName = firstName;
        teacher.Phone = "+91 90000 00000";
        teacher.Specialties = subjects;
        teacher.Qualifications = "Advanced diploma in music education";
        teacher.CertificationsJson = JsonSerializer.Serialize(subjects.Split(", ").Select(subject => new { subject, certification = "Certified specialist" }));
        teacher.AvailabilityJson = "[{\"day\":\"Monday\",\"available\":true,\"from\":\"15:00\",\"to\":\"20:00\"},{\"day\":\"Wednesday\",\"available\":true,\"from\":\"15:00\",\"to\":\"20:00\"},{\"day\":\"Saturday\",\"available\":true,\"from\":\"09:00\",\"to\":\"16:00\"}]";
        teacher.EmploymentType = active ? "Full-time" : "Part-time";
        teacher.DateOfBirth = new DateOnly(1990, 5, 15);
        teacher.JoiningDate = new DateOnly(2025, 4, 1);
        teacher.AddressLine1 = "Indiranagar";
        teacher.City = "Bengaluru";
        teacher.State = "Karnataka";
        teacher.PostalCode = "560038";
        teacher.EmergencyContactName = "Academy contact";
        teacher.EmergencyContactPhone = "+91 90000 00001";
        teacher.CompensationJson = compensation;
        teacher.IsActive = active;
        await db.SaveChangesAsync();
        return teacher;
    }

    private static async Task<Student> EnsureStudent(AcademyDeskDbContext db, Guid academyId, string firstName, string lastName, string email, string number)
    {
        var student = await db.Students.FirstOrDefaultAsync(x => x.AcademyId == academyId && x.Email == email);
        if (student is not null) return student;
        student = new Student { AcademyId = academyId, FirstName = firstName, LastName = lastName, Email = email, StudentNumber = number, AdmissionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)), IsActive = true };
        db.Students.Add(student);
        await db.SaveChangesAsync();
        return student;
    }

    private static async Task<Batch> EnsureBatch(AcademyDeskDbContext db, Guid academyId, Guid courseId, Guid teacherId, string name, string code, string deliveryMode, string venue)
    {
        var batch = await db.Batches.FirstOrDefaultAsync(x => x.AcademyId == academyId && x.BatchCode == code);
        if (batch is null)
        {
            batch = new Batch { AcademyId = academyId, Name = name, BatchCode = code, CourseId = courseId };
            db.Batches.Add(batch);
        }
        batch.TeacherId = teacherId;
        batch.Capacity = 12;
        batch.WaitlistCapacity = 4;
        batch.ClassType = "Group";
        batch.SessionMinutes = 60;
        batch.SessionsPerWeek = 2;
        batch.MeetingDaysJson = "[\"Monday\",\"Wednesday\"]";
        batch.DeliveryMode = deliveryMode;
        batch.MeetingPattern = "Twice weekly";
        batch.RoomName = deliveryMode == "Online" ? null : venue;
        batch.MeetingLink = deliveryMode is "Online" or "Hybrid" ? venue : null;
        batch.EnrollmentStatus = "Open";
        batch.StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        batch.IsActive = true;
        await db.SaveChangesAsync();
        return batch;
    }
}
