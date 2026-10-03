using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPeopleBranchesAsync(QaApiFactory factory, HttpClient client, bool enforceFixed)
    {
        Guid own, secondOwn, inactiveOwn, foreign, inactiveForeign;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            var other = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var branches = new[] {
                new Branch { AcademyId = academy, Name = "QA Own First" }, new Branch { AcademyId = academy, Name = "QA Own Second" },
                new Branch { AcademyId = academy, Name = "QA Own Inactive", IsActive = false },
                new Branch { AcademyId = other, Name = "QA Foreign" }, new Branch { AcademyId = other, Name = "QA Foreign Inactive", IsActive = false }
            };
            db.AddRange(branches); await db.SaveChangesAsync();
            own = branches[0].Id; secondOwn = branches[1].Id; inactiveOwn = branches[2].Id; foreign = branches[3].Id; inactiveForeign = branches[4].Id;
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        foreach (var kind in new[] { "students", "teachers" })
        {
            (string Label, object? Branch, bool Reject, bool Omit)[] cases = [
                ("own", own, false, false), ("reassign", secondOwn, false, false),
                ("foreign", foreign, true, false), ("missing", Guid.NewGuid(), true, false),
                ("empty-guid", Guid.Empty, true, false), ("foreign-inactive", inactiveForeign, true, false),
                ("clear-null", null, false, false), ("omitted", null, false, true),
                ("own-inactive-current-policy", inactiveOwn, false, false), ("explicit-null-optionals", null, false, false),
                ("malformed", "not-a-guid", true, false)
            ];
            foreach (var test in cases)
            {
                var fixture = await SeedLinkedAsync(factory, kind);
                // Existing valid assignment makes null/omission a real clearing
                // control and invalid submissions an actual no-write check.
                using (var scope = factory.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                    if (kind == "students") (await db.Students.SingleAsync(x => x.Id == fixture.Person)).BranchId = own;
                    else (await db.Teachers.SingleAsync(x => x.Id == fixture.Person)).BranchId = own;
                    await db.SaveChangesAsync();
                }
                var body = LinkedBody("branch-" + test.Label);
                if (test.Omit) body.Remove("branchId"); else body["branchId"] = test.Branch;
                if (test.Label == "explicit-null-optionals") { body["email"] = null; body["phone"] = null; body["specialties"] = null; }
                // Malformed GUID is already model-binding rejected before the
                // controller; foreign/missing/empty IDs are the accepted gap.
                var reproduce = !enforceFixed && test.Reject && test.Label != "malformed";
                var expected = test.Reject && !reproduce ? HttpStatusCode.BadRequest : HttpStatusCode.OK;
                await CheckLinkedAsync(factory, client, fixture, token, body, expected, "branch/" + kind + "/" + test.Label, enforceFixed);

                if (expected == HttpStatusCode.OK)
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var state = await PeopleStateAsync(factory); var identity = await LinkedAccountsAsync(factory);
                    using var list = await client.GetAsync($"/api/academies/{fixture.Academy}/{kind}");
                    RequireFinanceStatus(list, HttpStatusCode.OK, "branch readback");
                    using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
                    var row = json.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == fixture.Person);
                    var branch = row.GetProperty("branchId");
                    if (test.Branch is Guid id ? branch.GetGuid() != id : branch.ValueKind != JsonValueKind.Null)
                        throw new InvalidOperationException("Branch list readback mismatch: " + test.Label);
                    if (state != await PeopleStateAsync(factory) || identity != await LinkedAccountsAsync(factory))
                        throw new InvalidOperationException("Branch readback mutated captured data.");
                }
                Console.WriteLine($"BRANCH CASE {kind}/{test.Label} {(reproduce ? "REPRODUCED" : "PASS")}: HTTP={(int)expected}; {(expected == HttpStatusCode.OK ? "fresh row/account/audit +GET branch readback verified" : "all captured people/accounts/finance/audits unchanged")}.");
            }
        }
        Console.WriteLine($"BRANCH {(enforceFixed ? "REGRESSION PASS" : "BASELINE REPRODUCED")}: 22 cases; foreign/missing/empty/inactive-foreign integrity, own/reassign/null/omission/nullable/GET controls; own inactive remains accepted as current Create policy, not a new policy approval; UI/concurrency/critical NOT RUN.");
    }
}
