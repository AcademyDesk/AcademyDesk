using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyOnboardingDatesAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null; var count = 0;
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), users = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().Users.CountAsync() });
        }
        async Task Check(string kind, string field, string shape, HttpStatusCode expected, bool anonymous = false, bool foreignRoute = false)
        {
            await Task.Delay(650); var before = await Snapshot();
            var body = kind == "teacher" ? new Dictionary<string, object?> { ["firstName"] = "Date fixture", ["lastName"] = "Teacher", ["email"] = "qa-date@example.invalid", ["specialties"] = "Piano" }
                : new Dictionary<string, object?> { ["studentFirstName"] = "Date fixture", ["studentLastName"] = "Learner", ["dateOfBirth"] = "2000-01-01" };
            if (shape == "omitted") body.Remove(field);
            else body[field] = shape switch { "null" => null, "empty" => "", "valid" => "2001-02-03", _ => "not-a-date" };
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/academies/{(foreignRoute ? foreign : academy)}/{(kind == "teacher" ? "teachers" : "student-onboarding")}") { Content = JsonContent.Create(body) };
            if (!anonymous) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            using var response = await client.SendAsync(request); var label = kind + " " + field + " " + shape + (anonymous ? " anonymous" : foreignRoute ? " foreign" : "");
            RequireFinanceStatus(response, expected, label);
            if (expected is HttpStatusCode.OK or HttpStatusCode.Created)
            {
                using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var id = result.RootElement.GetProperty("id").GetGuid();
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var date = shape == "valid" ? new DateOnly(2001, 2, 3) : (DateOnly?)null;
                if (kind == "teacher")
                {
                    var saved = await db.Teachers.AsNoTracking().SingleAsync(x => x.Id == id);
                    if (saved.AcademyId != academy || saved.DateOfBirth != (field == "dateOfBirth" ? date : null) || saved.JoiningDate != (field == "joiningDate" ? date : null)) throw new InvalidOperationException("Teacher optional dates differ in fresh SQL.");
                }
                else
                {
                    var saved = await db.Students.AsNoTracking().SingleAsync(x => x.Id == id);
                    if (saved.AcademyId != academy || saved.DateOfBirth != new DateOnly(2000, 1, 1) || saved.AdmissionDate != (date ?? DateOnly.FromDateTime(DateTime.UtcNow))) throw new InvalidOperationException("Student required/default dates differ in fresh SQL.");
                }
            }
            else
            {
                if (before != await Snapshot()) throw new InvalidOperationException("Invalid/denied intake changed SQL rows: " + label);
                if (expected == HttpStatusCode.BadRequest && shape is "empty" or "malformed")
                {
                    using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    if (!problem.RootElement.TryGetProperty("errors", out var errors) || !errors.EnumerateObject().Any(x => x.Name.Contains(field, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Missing field-specific date binding error.");
                }
            }
            count++; Console.WriteLine("PASS: ONBOARDINGDATE " + label);
        }
        foreach (var field in new[] { "dateOfBirth", "joiningDate" }) foreach (var shape in new[] { "omitted", "null", "empty", "valid", "malformed" })
            await Check("teacher", field, shape, shape is "empty" or "malformed" ? HttpStatusCode.BadRequest : HttpStatusCode.Created);
        foreach (var shape in new[] { "omitted", "null", "empty", "valid", "malformed" }) await Check("student", "admissionDate", shape, shape is "empty" or "malformed" ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        foreach (var shape in new[] { "omitted", "null", "empty", "malformed" }) await Check("student", "dateOfBirth", shape, HttpStatusCode.BadRequest);
        foreach (var kind in new[] { "teacher", "student" })
        {
            await Check(kind, kind == "teacher" ? "joiningDate" : "admissionDate", "null", HttpStatusCode.Unauthorized, anonymous: true);
            await Check(kind, kind == "teacher" ? "joiningDate" : "admissionDate", "null", HttpStatusCode.Forbidden, foreignRoute: true);
        }
        if (count != 23) throw new InvalidOperationException("Incomplete date matrix.");
        Console.WriteLine($"PASS: ONBOARDINGDATE all {count} SQL/Identity/HTTP cases, optional/required binding, fresh persistence and no-write controls.");
    }
}
