using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyTeacherAvailabilityAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, teacher, foreignTeacher;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            var other = new Teacher { AcademyId = foreign, FirstName = "Availability", LastName = "Foreign", AdminNotes = "Foreign sentinel" };
            db.Teachers.Add(other); await db.SaveChangesAsync(); foreignTeacher = other.Id;
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var otherAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null; var count = 0;
        var cases = new (string Label, string? Json, int Slots)[]
        {
            ("optional null", null, 0), ("empty", "", 0), ("whitespace", "   ", 0), ("JSON null", "null", 0), ("empty list", "[]", 0),
            ("null entry", "[null]", 0), ("mixed list", "[null,{\"day\":\"Monday\",\"from\":\"09:00\",\"to\":\"10:00\"},null]", 1),
            ("case insensitive", "[{\"Day\":\"Tuesday\",\"From\":null,\"To\":null}]", 1), ("empty object entry", "[{}]", 0),
            ("object root", "{}", 0), ("scalar root", "1", 0), ("invalid syntax", "broken", 0), ("invalid day type", "[{\"day\":42}]", 0)
        };
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            return JsonSerializer.Serialize(await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Teachers.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, Guid tenant, Guid record, string? token, string? json = null)
        {
            await Task.Delay(650);
            using var request = new HttpRequestMessage(method, $"/api/academies/{tenant}/teachers/{record}/profile");
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (method == HttpMethod.Put) request.Content = JsonContent.Create(new { preferredName = "Saved synthetic name", availabilityJson = json });
            return await client.SendAsync(request);
        }
        void Projection(HttpResponseMessage response, JsonDocument doc, int slots, string label)
        {
            RequireFinanceStatus(response, HttpStatusCode.OK, label);
            if (doc.RootElement.GetProperty("availability").GetArrayLength() != slots) throw new InvalidOperationException("Availability projection differs: " + label);
            if (slots == 1 && doc.RootElement.GetProperty("availability")[0].GetProperty("day").GetString() is not ("Monday" or "Tuesday")) throw new InvalidOperationException("Valid slot lost.");
        }
        foreach (var item in cases)
        {
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                (await db.Teachers.SingleAsync(x => x.Id == teacher)).AvailabilityJson = item.Json; await db.SaveChangesAsync();
            }
            var before = await Snapshot();
            using (var get = await Send(HttpMethod.Get, academy, teacher, admin))
            {
                RequireFinanceStatus(get, HttpStatusCode.OK, "stored " + item.Label);
                using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync()); Projection(get, doc, item.Slots, item.Label);
            }
            if (before != await Snapshot()) throw new InvalidOperationException("Read modified stored rows.");
            count++; Console.WriteLine("PASS: AVAILABILITY stored " + item.Label);
            using (var put = await Send(HttpMethod.Put, academy, teacher, admin, item.Json))
            {
                RequireFinanceStatus(put, HttpStatusCode.OK, "save " + item.Label);
                using var doc = JsonDocument.Parse(await put.Content.ReadAsStringAsync()); Projection(put, doc, item.Slots, item.Label);
                if (doc.RootElement.GetProperty("preferredName").GetString() != "Saved synthetic name") throw new InvalidOperationException("Save response lost metadata.");
            }
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var saved = await db.Teachers.AsNoTracking().SingleAsync(x => x.Id == teacher);
                if (saved.AvailabilityJson != (string.IsNullOrWhiteSpace(item.Json) ? null : item.Json.Trim()) || saved.PreferredName != "Saved synthetic name" || saved.DateOfBirth is not null || saved.JoiningDate is not null) throw new InvalidOperationException("Fresh SQL differs from existing optional metadata contract.");
                if ((await db.Teachers.AsNoTracking().SingleAsync(x => x.Id == foreignTeacher)).AdminNotes != "Foreign sentinel") throw new InvalidOperationException("Foreign record changed.");
            }
            count++; Console.WriteLine("PASS: AVAILABILITY saved " + item.Label);
            using (var get = await Send(HttpMethod.Get, academy, teacher, admin))
            {
                using var doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync()); Projection(get, doc, item.Slots, "fresh " + item.Label);
            }
            count++; Console.WriteLine("PASS: AVAILABILITY fresh " + item.Label);
        }
        var denied = new (string Label, Guid Tenant, Guid Record, string? Token, HttpStatusCode Status)[]
        {
            ("anonymous", academy, teacher, null, HttpStatusCode.Unauthorized), ("teacher write", academy, teacher, teacherToken, HttpStatusCode.Forbidden),
            ("foreign actor", academy, teacher, otherAdmin, HttpStatusCode.Forbidden), ("foreign route", foreign, foreignTeacher, admin, HttpStatusCode.Forbidden),
            ("foreign record", academy, foreignTeacher, admin, HttpStatusCode.NotFound), ("missing record", academy, Guid.NewGuid(), admin, HttpStatusCode.NotFound)
        };
        foreach (var item in denied)
        {
            var before = await Snapshot(); using var put = await Send(HttpMethod.Put, item.Tenant, item.Record, item.Token, "[null]");
            RequireFinanceStatus(put, item.Status, item.Label); if (before != await Snapshot()) throw new InvalidOperationException("Denied request changed teacher rows: " + item.Label);
            count++; Console.WriteLine("PASS: AVAILABILITY no-write " + item.Label);
        }
        if (count != 45) throw new InvalidOperationException("Incomplete availability matrix.");
        Console.WriteLine($"PASS: AVAILABILITY all {count} SQL/Identity/HTTP cases, fresh persistence/readability and protected no-write controls.");
    }
}
