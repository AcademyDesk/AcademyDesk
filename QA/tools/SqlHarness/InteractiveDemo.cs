using System.Net;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    // Explicit interactive mode of the existing guarded, disposable SQL harness.
    // Real application authorization and domain services handle every request.
    private static async Task ServeInteractiveDemoAsync(QaRunManifest manifest)
    {
        const int apiPort = 54999;
        const string origin = "http://127.0.0.1:3000";
        var password = Environment.GetEnvironmentVariable("QA_DEMO_PASSWORD");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 16)
            throw new InvalidOperationException("An ephemeral local demo password is required.");
        var flags = new[] { "Enabled", "Mini:Enabled", "FinanceReadEnabled", "PaymentCollectionEnabled",
            "InvoiceIssuanceEnabled", "InvoiceAdjustmentEnabled", "EnrollmentPreviewEnabled", "EnrollmentCreationEnabled",
            "EnrollmentBrowserContractsEnabled", "EnrollmentLifecycleEnabled", "EnrollmentLifecycleBrowserContractsEnabled",
            "ClassSessionSchedulingEnabled", "ClassSessionReschedulingEnabled", "ClassSchedulingBrowserContractsEnabled",
            "MakeupSchedulingEnabled", "TeacherAttendanceEnabled", "PortalReadsEnabled", "PlatformReadsEnabled" };
        var settings = flags.ToDictionary(flag => "Penta:" + flag, _ => (string?)"true");
        settings["Penta:Mini:BaseUrl"] = Environment.GetEnvironmentVariable("QA_DEMO_MINI_URL") ?? "http://127.0.0.1:8000/";
        settings["Cors:AllowedOrigins:0"] = origin;
        using var factory = new QaApiFactory(manifest, settings);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        client.Timeout = TimeSpan.FromSeconds(150);
        PentaRequire(factory.PreflightPassed, "Interactive demo requires the disposable SQL guard.");
        using (var health = await client.GetAsync("/health"))
            PentaRequire(health.StatusCode == HttpStatusCode.OK, "Demo health check failed.");
        using (var anonymous = await client.GetAsync("/api/academies"))
            PentaRequire(anonymous.StatusCode == HttpStatusCode.Unauthorized, "Demo must require authentication.");
        RecordRuntimeEndpoints(factory, manifest);
        var academy = new Academy { Name = "Lotus Learning Academy — Demo", LegalName = "Synthetic demonstration only",
            SubscriptionPlan = "Professional", SubscriptionStatus = "Active", StudentLimit = 750, StaffLimit = 120,
            EnabledModulesJson = JsonSerializer.Serialize(SubscriptionPlanCatalog.ModulesFor("Professional")) };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var branches = new[] { "Central Campus", "Lakeside Studio" }.Select(name => new Branch {
            AcademyId = academy.Id, Name = name, City = "Bengaluru", State = "Karnataka" }).ToArray();
        var subjects = new[] { "Piano", "Guitar", "Vocals", "Mathematics" };
        var teachers = new[] { ("Nisha", "Iyer"), ("Arjun", "Mehta"), ("Ananya", "Rao"), ("Dev", "Shah") }
            .Select((name, i) => new Teacher { AcademyId = academy.Id, FirstName = name.Item1, LastName = name.Item2,
                EmployeeCode = $"DEMO-T{i + 1:00}", Email = $"teacher{i + 1}@demo.example.invalid", Specialties = subjects[i],
                EmploymentType = "PartTime", JoiningDate = today.AddMonths(-6), BranchId = branches[i % 2].Id }).ToArray();
        var courses = subjects.Select((name, i) => new ProgramCourse { AcademyId = academy.Id, Name = name + " Foundations",
            CourseCode = $"DEMO-C{i + 1:00}", SubjectArea = name, AcademyType = i == 3 ? "School" : "Music",
            Level = "Beginner", IsPublished = true, SessionMinutes = 60, WeeklySessions = 2, DurationMonths = 6 }).ToArray();
        var batches = courses.Select((course, i) => new Batch { AcademyId = academy.Id, CourseId = course.Id,
            Name = subjects[i] + " — " + (i % 2 == 0 ? "Evening" : "Weekend"), BatchCode = $"DEMO-B{i + 1:00}",
            TeacherId = teachers[i].Id, BranchId = branches[i % 2].Id, Capacity = 12, WaitlistCapacity = 3,
            StartDate = today.AddMonths(-1), EndDate = today.AddMonths(5), RoomName = "Room " + (i + 1) }).ToArray();
        var names = new[] { ("Meera", "Kapoor"), ("Riya", "Menon"), ("Aarav", "Sharma"), ("Ishaan", "Patel"),
            ("Tara", "Nair"), ("Kabir", "Das"), ("Sana", "Ali"), ("Vivaan", "Rao"), ("Diya", "Joshi"),
            ("Aria", "Sen"), ("Rohan", "Gupta"), ("Anika", "Shah") };
        var learners = names.Select((name, i) => new Student { AcademyId = academy.Id, FirstName = name.Item1, LastName = name.Item2,
            StudentNumber = $"DEMO-S{i + 1:000}", Email = $"learner{i + 1}@demo.example.invalid", BranchId = branches[i % 2].Id,
            AdmissionDate = today.AddMonths(-2), DateOfBirth = new DateOnly(2012 + i % 5, 2, 12), City = "Bengaluru",
            AdminNotes = "Invented demonstration learner. No customer data." }).ToArray();
        var guardian = new Guardian { AcademyId = academy.Id, FirstName = "Kavya", LastName = "Kapoor", Email = "guardian@demo.example.invalid" };
        var sessions = batches.Select((batch, i) => new ClassSession { AcademyId = academy.Id, BatchId = batch.Id,
            TeacherId = teachers[i].Id, BranchId = batch.BranchId, StartUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(10 + i),
            EndUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(11 + i), RoomName = batch.RoomName }).ToArray();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.Add(academy); db.AddRange(branches); db.AddRange(teachers); db.AddRange(courses); db.AddRange(batches);
            db.AddRange(learners); db.Add(guardian); db.AddRange(sessions);
            db.Add(new StudentGuardian { AcademyId = academy.Id, StudentId = learners[0].Id, GuardianId = guardian.Id,
                Relationship = "Parent", IsPrimary = true, CanAccessPortal = true, AccessGrantedAtUtc = DateTime.UtcNow });
            foreach (var (learner, i) in learners.Select((learner, i) => (learner, i)))
            {
                db.Add(new Enrollment { AcademyId = academy.Id, StudentId = learner.Id, BatchId = batches[i % 4].Id, StartDate = today.AddMonths(-1) });
                var invoice = new Invoice { AcademyId = academy.Id, StudentId = learner.Id, InvoiceNumber = $"DEMO-INV-{i + 1:000}",
                    TotalAmount = 3000 + i * 250, Currency = "INR", IssuedDate = today.AddDays(-20), DueDate = today.AddDays(i % 3 == 0 ? -5 : 7),
                    Status = i % 5 == 0 ? "Paid" : i % 3 == 1 ? "PartiallyPaid" : "Issued" };
                db.Add(invoice);
                if (i % 5 == 0 || i % 3 == 1) db.Add(new Payment { AcademyId = academy.Id, InvoiceId = invoice.Id,
                    Amount = i % 5 == 0 ? invoice.TotalAmount : 1000, Currency = "INR", Method = "Cash", Status = "Reconciled",
                    PaidAtUtc = DateTime.UtcNow.AddDays(-3), ReconciledAtUtc = DateTime.UtcNow.AddDays(-2) });
            }
            db.Add(new FeePlan { AcademyId = academy.Id, Name = "Monthly tuition", Amount = 3000, Currency = "INR" });
            db.Add(new Expense { AcademyId = academy.Id, Description = "Demo classroom supplies", Category = "Supplies", Amount = 850, BranchId = branches[0].Id });
            await db.SaveChangesAsync();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var role in new[] { "Owner", "AcademyAdmin", "Teacher", "Student", "Guardian", "PlatformOwner" })
                if (!await roleManager.RoleExistsAsync(role)) PentaRequire((await roleManager.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = true })).Succeeded, "Demo role creation failed.");
            foreach (var role in new[] { "Owner", "AcademyAdmin", "Teacher", "Student", "Guardian", "PlatformOwner" })
            {
                var address = role.ToLowerInvariant() + "@demo.example.invalid";
                var user = new ApplicationUser { UserName = address, Email = address, EmailConfirmed = true,
                    DisplayName = "Demo " + role, AcademyId = role == "PlatformOwner" ? null : academy.Id,
                    IsPlatformOwner = role == "PlatformOwner", TeacherId = role == "Teacher" ? teachers[0].Id : null,
                    StudentId = role == "Student" ? learners[0].Id : null, GuardianId = role == "Guardian" ? guardian.Id : null };
                PentaRequire((await users.CreateAsync(user, password)).Succeeded && (await users.AddToRoleAsync(user, role)).Succeeded, "Demo identity creation failed.");
            }
        }
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls($"http://127.0.0.1:{apiPort}");
        await using var bridge = builder.Build();
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) ||
                context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != apiPort)
            { context.Response.StatusCode = 400; return; }
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.Method is "POST" or "PUT" or "PATCH") request.Content = new StreamContent(context.Request.Body);
            foreach (var header in context.Request.Headers)
            {
                if (header.Key is "Host" or "Connection" or "Transfer-Encoding") continue;
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                context.Response.StatusCode = (int)response.StatusCode;
                foreach (var header in response.Headers.Concat(response.Content.Headers))
                    if (header.Key is not ("Transfer-Encoding" or "Connection")) context.Response.Headers[header.Key] = header.Value.ToArray();
                await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
            }
            catch (HttpRequestException) { context.Response.StatusCode = 503; }
        });
        await bridge.StartAsync();
        var stop = Path.Combine(manifest.Root, "stop-interactive-demo");
        File.WriteAllText(Path.Combine(manifest.Root, "demo-ready.json"), JsonSerializer.Serialize(new { run = manifest.RunId, academyId = academy.Id,
            origin, api = $"http://127.0.0.1:{apiPort}", learners = learners.Length, teachers = teachers.Length, batches = batches.Length, stop,
            expiresAtUtc = DateTime.UtcNow.AddHours(12), synthetic = true }));
        Console.WriteLine($"INTERACTIVE DEMO READY run={manifest.RunId:N} learners=12 teachers=4 batches=4 origin={origin}");
        var deadline = DateTime.UtcNow.AddHours(12);
        while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(1000);
        await bridge.StopAsync();
    }
}
