using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyClassMaterialRegressionsAsync(QaApiFactory factory, HttpClient client,
        Guid academyId, Guid batchId, Guid resourceId, string url, byte[] bytes,
        string teacherToken, string adminToken, string tenantBToken)
    {
        Guid studentId, guardianId = Guid.NewGuid(), otherStudentId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            studentId = await db.Students.Where(x => x.AcademyId == academyId && x.FirstName == "Isolated-A").Select(x => x.Id).SingleAsync();
            db.Enrollments.Add(new Enrollment { AcademyId = academyId, BatchId = batchId, StudentId = studentId });
            db.Students.Add(new Student { Id = otherStudentId, AcademyId = academyId, FirstName = "Other recipient", LastName = "Synthetic" });
            db.Enrollments.Add(new Enrollment { AcademyId = academyId, BatchId = batchId, StudentId = otherStudentId });
            db.Guardians.Add(new Guardian { Id = guardianId, AcademyId = academyId, FirstName = "Synthetic", LastName = "Guardian" });
            db.StudentGuardians.Add(new StudentGuardian { AcademyId = academyId, StudentId = studentId,
                GuardianId = guardianId, CanAccessPortal = true, CanViewDocuments = true });
            (await db.LearningResources.SingleAsync(x => x.Id == resourceId)).IsPublished = true;
            await db.SaveChangesAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var item in new[] { (Email: "qa-student-media@example.invalid", Role: "Student", Student: (Guid?)studentId, Guardian: (Guid?)null),
                (Email: "qa-other-student-media@example.invalid", Role: "Student", Student: (Guid?)otherStudentId, Guardian: (Guid?)null),
                (Email: "qa-guardian-media@example.invalid", Role: "Guardian", Student: (Guid?)null, Guardian: (Guid?)guardianId) })
            {
                if (!await roles.RoleExistsAsync(item.Role) && !(await roles.CreateAsync(new ApplicationRole { Name = item.Role })).Succeeded)
                    throw new InvalidOperationException("Synthetic media role setup failed.");
                var user = new ApplicationUser { UserName = item.Email, Email = item.Email, EmailConfirmed = true,
                    AcademyId = academyId, StudentId = item.Student, GuardianId = item.Guardian };
                if (!(await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded || !(await users.AddToRoleAsync(user, item.Role)).Succeeded)
                    throw new InvalidOperationException("Synthetic media user setup failed.");
            }
        }
        var studentToken = await LoginAsync(client, "qa-student-media@example.invalid", "Synthetic!39Ab");
        var otherToken = await LoginAsync(client, "qa-other-student-media@example.invalid", "Synthetic!39Ab");
        var guardianToken = await LoginAsync(client, "qa-guardian-media@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var passed = 0;
        async Task Check(string name, string path, string? bearer, int expected, bool head = false, bool range = false, bool conditional = false)
        {
            using var request = new HttpRequestMessage(head ? HttpMethod.Head : HttpMethod.Get, path);
            if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            if (range) request.Headers.Range = new RangeHeaderValue(0, 7);
            if (conditional) request.Headers.IfModifiedSince = DateTimeOffset.UtcNow.AddDays(1);
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsByteArrayAsync();
            var expectedBytes = head ? Array.Empty<byte>() : range ? bytes.Take(8).ToArray() : bytes;
            var statusMatches = expected == -1 ? response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound : (int)response.StatusCode == expected;
            if (!statusMatches || (expected is 200 or 206 && !body.SequenceEqual(expectedBytes)) ||
                ((expected >= 400 || expected == -1) && (body.SequenceEqual(bytes) || body.SequenceEqual(bytes.Take(8)))))
                throw new InvalidOperationException($"SECURITY-FILE-001 {name} failed, expected {expected}, actual {(int)response.StatusCode}.");
            if (expected is 200 or 206 && (response.Headers.CacheControl?.NoStore != true ||
                !response.Headers.TryGetValues("X-Content-Type-Options", out var values) || !values.Contains("nosniff") ||
                response.Content.Headers.ContentDisposition?.DispositionType != "attachment"))
                throw new InvalidOperationException($"SECURITY-FILE-001 {name} missing private attachment response headers.");
            passed++;
            Console.WriteLine($"MEDIA REGRESSION PASS: {name} HTTP={(int)response.StatusCode}; exact authorized bytes or denied content verified.");
        }
        async Task Change(Func<AcademyDeskDbContext, Task> mutation)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            await mutation(db); await db.SaveChangesAsync();
        }
        await Check("teacher authorized range", url, teacherToken, 206, range: true);
        await Check("admin authorized HEAD", url, adminToken, 200, head: true);
        await Check("anonymous HEAD", url, null, 401, head: true);
        await Check("tenant B HEAD", url, tenantBToken, 404, head: true);
        await Check("anonymous conditional GET", url, null, 401, conditional: true);
        await Check("case variant anonymous", url.ToUpperInvariant(), null, -1);
        await Check("double separator anonymous", url.Replace("/teacher-materials/", "//teacher-materials/"), null, -1);
        await Check("Windows trailing-dot directory alias", url.Replace("/teacher-materials/", "/teacher-materials./"), null, -1);
        await Check("Windows short directory alias", url.Replace("/teacher-materials/", "/TEACHE~1/"), null, -1);
        await Check("student published material", url, studentToken, 200);
        await Check("guardian document grant", url, guardianToken, 200);
        await Change(async db => (await db.Students.SingleAsync(x => x.Id == studentId)).IsActive = false);
        await Check("inactive student entity", url, studentToken, 404);
        await Change(async db => (await db.Students.SingleAsync(x => x.Id == studentId)).IsActive = true);
        await Change(async db => (await db.LearningResources.SingleAsync(x => x.Id == resourceId)).StudentId = studentId);
        await Check("different enrolled recipient", url, otherToken, 404);
        await Change(async db => (await db.LearningResources.SingleAsync(x => x.Id == resourceId)).IsPublished = false);
        await Check("student unpublished range", url, studentToken, 404, range: true);
        await Check("guardian unpublished", url, guardianToken, 404);
        await Check("staff unpublished management", url, teacherToken, 200);
        await Change(async db => (await db.LearningResources.SingleAsync(x => x.Id == resourceId)).IsPublished = true);
        await Change(async db => (await db.StudentGuardians.SingleAsync(x => x.GuardianId == guardianId)).AccessRevokedAtUtc = DateTime.UtcNow);
        await Check("guardian revoked range", url, guardianToken, 404, range: true);
        await Change(async db => (await db.StudentGuardians.SingleAsync(x => x.GuardianId == guardianId)).AccessRevokedAtUtc = null);
        await Change(async db => (await db.StudentGuardians.SingleAsync(x => x.GuardianId == guardianId)).CanViewDocuments = false);
        await Check("guardian document permission removed", url, guardianToken, 404);
        await Change(async db => (await db.Enrollments.SingleAsync(x => x.StudentId == studentId && x.BatchId == batchId)).Status = "Withdrawn");
        await Check("withdrawn student with old token", url, studentToken, 404);
        await Change(async db => (await db.Enrollments.SingleAsync(x => x.StudentId == studentId && x.BatchId == batchId)).Status = "Active");
        await Change(async db => (await db.Teachers.SingleAsync(x => x.AcademyId == academyId)).IsActive = false);
        await Check("inactive teacher entity", url, teacherToken, 404);
        await Change(async db => (await db.Teachers.SingleAsync(x => x.AcademyId == academyId)).IsActive = true);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var teacher = (await users.FindByEmailAsync("qa-teacher-a@example.invalid"))!;
            if (!(await users.RemoveFromRoleAsync(teacher, "Teacher")).Succeeded) throw new InvalidOperationException("Role revocation setup failed.");
        }
        await Check("removed teacher role with old token", url, teacherToken, 404);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var teacher = (await users.FindByEmailAsync("qa-teacher-a@example.invalid"))!;
            if (!(await users.AddToRoleAsync(teacher, "Teacher")).Succeeded) throw new InvalidOperationException("Role restore setup failed.");
            teacher.IsActive = false;
            if (!(await users.UpdateAsync(teacher)).Succeeded) throw new InvalidOperationException("User revocation setup failed.");
        }
        await Check("inactive identity with old token", url, teacherToken, 403);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var teacher = (await users.FindByEmailAsync("qa-teacher-a@example.invalid"))!;
            teacher.IsActive = true;
            if (!(await users.UpdateAsync(teacher)).Succeeded) throw new InvalidOperationException("User restore setup failed.");
        }
        await Change(async db => (await db.Batches.SingleAsync(x => x.Id == batchId)).IsActive = false);
        await Check("inactive batch range", url, studentToken, 404, range: true);
        await Change(async db => (await db.Batches.SingleAsync(x => x.Id == batchId)).IsActive = true);
        await Change(async db => (await db.Academies.SingleAsync(x => x.Id == academyId)).IsActive = false);
        await Check("inactive academy HEAD", url, adminToken, 404, head: true);
        await Change(async db => (await db.Academies.SingleAsync(x => x.Id == academyId)).IsActive = true);

        // Cover the sibling private-learning upload directory with real bytes/row,
        // and keep its legitimate student access; don't just test a missing file.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var upload = await UploadAsync(client, $"/api/academies/{academyId}/resources/upload", "Synthetic admin material", bytes, batchId);
        if (upload.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException("Learning material upload fixture failed.");
        client.DefaultRequestHeaders.Authorization = null;
        string learningUrl;
        using (var scope = factory.Services.CreateScope()) learningUrl = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>()
            .LearningResources.Where(x => x.Title == "Synthetic admin material").Select(x => x.Url).SingleAsync();
        await Check("learning material authorized student", learningUrl, studentToken, 200);
        await Check("learning material anonymous", learningUrl, null, 401);
        await Check("learning material tenant B range", learningUrl, tenantBToken, 404, range: true);

        var environment = factory.Services.GetRequiredService<IWebHostEnvironment>();
        var orphanUrl = $"/uploads/teacher-materials/{Guid.NewGuid():N}.pdf";
        await File.WriteAllBytesAsync(Path.Combine(environment.WebRootPath, orphanUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)), bytes);
        await Check("orphan file without resource row", orphanUrl, teacherToken, 404);
        await Check("orphan file anonymous", orphanUrl, null, 401);
        var publicFile = "qa-public-control.pdf";
        await File.WriteAllBytesAsync(Path.Combine(environment.WebRootPath, publicFile), bytes);
        using (var response = await client.GetAsync("/" + publicFile))
            if (response.StatusCode != HttpStatusCode.OK || !(await response.Content.ReadAsByteArrayAsync()).SequenceEqual(bytes))
                throw new InvalidOperationException("Unrelated public asset control changed.");
        await Change(async db => (await db.LearningResources.SingleAsync(x => x.Id == resourceId)).IsPublished = false);
        Console.WriteLine($"MEDIA REGRESSIONS PASS: {passed} HTTP cases; real SQL/Identity revocations; unrelated public asset control unchanged. Browser/Blob integration NOT RUN.");
    }
}
