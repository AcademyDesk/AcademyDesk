using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyComplianceIdentityAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, actor; Student student, inactiveStudent, foreignStudent; Guardian guardian, inactiveGuardian, foreignGuardian;
        var collision = Guid.NewGuid(); PersonDocument priorDocument, foreignDocument; ConsentRecord priorConsent, foreignConsent;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            (await db.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = "[\"Core\",\"AccessGovernance\"]";
            student = new() { AcademyId = academy, FirstName = "Compliance", LastName = "Student" }; inactiveStudent = new() { AcademyId = academy, FirstName = "Compliance", LastName = "Inactive", IsActive = false }; foreignStudent = new() { AcademyId = foreign, FirstName = "Compliance", LastName = "Foreign" };
            guardian = new() { AcademyId = academy, FirstName = "Compliance", LastName = "Parent" }; inactiveGuardian = new() { AcademyId = academy, FirstName = "Compliance", LastName = "Inactive", IsActive = false }; foreignGuardian = new() { AcademyId = foreign, FirstName = "Compliance", LastName = "Foreign" };
            priorDocument = new() { AcademyId = academy, StudentId = student.Id, DocumentType = "Prior ID", FileName = "prior.pdf", SecureReference = "synthetic-prior", Status = "Approved", ReviewedDate = new(2000, 1, 1) };
            foreignDocument = new() { AcademyId = foreign, StudentId = foreignStudent.Id, DocumentType = "Foreign ID", FileName = "foreign.pdf" };
            priorConsent = new() { AcademyId = academy, GuardianId = guardian.Id, ConsentType = "Prior media", EvidenceReference = "synthetic-prior", RecordedAtUtc = new(2000, 1, 1), Granted = true };
            foreignConsent = new() { AcademyId = foreign, GuardianId = foreignGuardian.Id, ConsentType = "Foreign media", Granted = true };
            db.AddRange(student, inactiveStudent, foreignStudent, guardian, inactiveGuardian, foreignGuardian, priorDocument, foreignDocument, priorConsent, foreignConsent,
                new Student { Id = collision, AcademyId = academy, FirstName = "Student", LastName = "Collision" }, new Guardian { Id = collision, AcademyId = academy, FirstName = "Parent", LastName = "Collision" },
                new AdminWorkItem { AcademyId = foreign, Type = "Compliance", Title = "Foreign work sentinel" });
            await db.SaveChangesAsync();
            actor = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
        }
        await CreateAccessActorAsync(factory, "qa-compliance-manager@example.invalid", "Manager", academy);
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var manager = await LoginAsync(client, "qa-compliance-manager@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var route = $"/api/academies/{academy}/compliance"; var count = 0; var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        void Pass(string label) { count++; Console.WriteLine($"COMPLIANCE CASE {label} PASS."); }
        async Task<JsonElement> Snapshot() {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.SerializeToElement(new { documents = await db.PersonDocuments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), consents = await db.ConsentRecords.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), tasks = await db.AdminWorkItems.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        static string Digest(JsonElement value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value.GetRawText())));
        static void Same<T>(T returned, T stored) {
            foreach (var property in typeof(T).GetProperties()) if (!Equals(property.GetValue(returned), property.GetValue(stored))) throw new InvalidOperationException("Complete returned/fresh SQL property differs: " + property.Name);
        }
        static void Preserve(JsonElement before, JsonElement after, string changed, int added, Guid? id = null, params string[] fields) {
            foreach (var key in new[] { "students", "guardians", "links", "academies", "users" }) if (!JsonElement.DeepEquals(before.GetProperty(key), after.GetProperty(key))) throw new InvalidOperationException("Changed person/tenant/Identity source: " + key);
            foreach (var key in new[] { "documents", "consents", "tasks" }) {
                var old = before.GetProperty(key).EnumerateArray().ToArray(); var current = after.GetProperty(key).EnumerateArray().ToArray();
                if (current.Length != old.Length + (key == changed ? added : 0)) throw new InvalidOperationException("Unexpected row count: " + key);
                foreach (var row in old) {
                    var next = current.Single(x => x.GetProperty("Id").GetGuid() == row.GetProperty("Id").GetGuid());
                    foreach (var property in row.EnumerateObject()) if (!(key == changed && row.GetProperty("Id").GetGuid() == id && fields.Contains(property.Name)) && !JsonElement.DeepEquals(property.Value, next.GetProperty(property.Name))) throw new InvalidOperationException("Unrelated/prior/foreign property changed: " + key + "/" + property.Name);
                }
            }
        }
        void Audit(JsonElement before, JsonElement after, string method, string path) {
            var old = before.GetProperty("audits").EnumerateArray().ToArray(); var current = after.GetProperty("audits").EnumerateArray().ToArray();
            if (current.Length != old.Length + 1 || old.Any(x => !current.Any(y => JsonElement.DeepEquals(x, y)))) throw new InvalidOperationException("Unexpected audit count/preservation");
            var added = current.Single(x => !old.Any(y => JsonElement.DeepEquals(x, y)));
            if (added.GetProperty("AcademyId").GetGuid() != academy || added.GetProperty("ActorUserId").GetGuid() != actor || added.GetProperty("Action").GetString() != method + " Compliance" || added.GetProperty("EntityType").GetString() != "Compliance" || JsonDocument.Parse(added.GetProperty("MetadataJson").GetString()!).RootElement.GetProperty("Route").GetString() != path) throw new InvalidOperationException("Actor/route audit differs");
        }
        async Task<(string Body, JsonElement Before, JsonElement After, DateTime Start, DateTime End)> Send(string label, HttpMethod method, string path, object? body, HttpStatusCode expected, string? auth, string? raw = null) {
            await Task.Delay(650); var before = await Snapshot(); var start = DateTime.UtcNow;
            using var request = new HttpRequestMessage(method, path);
            if (auth is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth);
            if (raw is not null) request.Content = new StringContent(raw, System.Text.Encoding.UTF8, "application/json"); else if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await client.SendAsync(request); var end = DateTime.UtcNow; var responseBody = await response.Content.ReadAsStringAsync(); var after = await Snapshot();
            Console.WriteLine("COMPLIANCE EVIDENCE " + JsonSerializer.Serialize(new { label, method = method.Method, path, request = raw ?? (body is null ? null : JsonSerializer.Serialize(body, json)), status = (int)response.StatusCode, response = (int)response.StatusCode < 500 ? responseBody : "Server failure body omitted", beforeDigest = Digest(before), afterDigest = Digest(after) }));
            RequireFinanceStatus(response, expected, label);
            if ((int)expected >= 400 || method == HttpMethod.Get) { if (!JsonElement.DeepEquals(before, after)) throw new InvalidOperationException("Denied/read wrote SQL/Identity/audits: " + label); }
            return (responseBody, before, after, start, end);
        }
        Dictionary<string, object?> Body(bool document, Guid? s, Guid? g, string? reference = null, DateOnly? expiry = null, string? visibility = null, bool granted = true) => document
            ? new() { ["studentId"] = s, ["guardianId"] = g, ["documentType"] = "  ID  ", ["fileName"] = "  synthetic.pdf  ", ["secureReference"] = reference, ["expiryDate"] = expiry, ["visibility"] = visibility }
            : new() { ["studentId"] = s, ["guardianId"] = g, ["consentType"] = "  Media  ", ["granted"] = granted, ["evidenceReference"] = reference };
        async Task<Guid> Create(string label, bool document, Dictionary<string, object?> body) {
            var spoof = Guid.NewGuid(); body["id"] = spoof; body["academyId"] = foreign; body["createdAtUtc"] = "2001-01-01T00:00:00Z"; body["updatedAtUtc"] = "2001-01-01T00:00:00Z"; body["status"] = "Approved"; body["reviewedDate"] = "2001-01-01"; body["recordedAtUtc"] = "2001-01-01T00:00:00Z"; body["withdrawnAtUtc"] = "2001-01-01T00:00:00Z";
            var path = route + (document ? "/documents" : "/consents"); var result = await Send(label, HttpMethod.Post, path, body, HttpStatusCode.OK, admin); Guid id;
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (document) {
                var returned = JsonSerializer.Deserialize<PersonDocument>(result.Body, json)!; var stored = await db.PersonDocuments.AsNoTracking().SingleAsync(x => x.Id == returned.Id); Same(returned, stored); id = stored.Id;
                var expected = new PersonDocument { Id = id, AcademyId = academy, StudentId = (Guid?)body["studentId"], GuardianId = (Guid?)body["guardianId"], DocumentType = "ID", FileName = "synthetic.pdf", SecureReference = ((string?)body["secureReference"])?.Trim(), ExpiryDate = (DateOnly?)body["expiryDate"], Visibility = string.IsNullOrWhiteSpace((string?)body["visibility"]) ? "AdminOnly" : ((string)body["visibility"]!).Trim(), CreatedAtUtc = stored.CreatedAtUtc };
                Same(stored, expected); if (stored.CreatedAtUtc < result.Start || stored.CreatedAtUtc > result.End) throw new InvalidOperationException("Document server clock differs");
            } else {
                var returned = JsonSerializer.Deserialize<ConsentRecord>(result.Body, json)!; var stored = await db.ConsentRecords.AsNoTracking().SingleAsync(x => x.Id == returned.Id); Same(returned, stored); id = stored.Id;
                var expected = new ConsentRecord { Id = id, AcademyId = academy, StudentId = (Guid?)body["studentId"], GuardianId = (Guid?)body["guardianId"], ConsentType = "Media", Granted = (bool)body["granted"]!, EvidenceReference = ((string?)body["evidenceReference"])?.Trim(), CreatedAtUtc = stored.CreatedAtUtc, RecordedAtUtc = stored.RecordedAtUtc, WithdrawnAtUtc = (bool)body["granted"]! ? null : stored.WithdrawnAtUtc };
                Same(stored, expected); if (stored.CreatedAtUtc < result.Start || stored.CreatedAtUtc > result.End || stored.RecordedAtUtc < result.Start || stored.RecordedAtUtc > result.End || !stored.Granted && (stored.WithdrawnAtUtc is null || stored.WithdrawnAtUtc < result.Start || stored.WithdrawnAtUtc > result.End)) throw new InvalidOperationException("Consent server clock differs");
            }
            if (id == Guid.Empty || id == spoof) throw new InvalidOperationException("Caller ID accepted");
            Preserve(result.Before, result.After, document ? "documents" : "consents", 1); Audit(result.Before, result.After, "POST", path); Pass(label); return id;
        }
        foreach (var document in new[] { true, false }) foreach (var mode in new[] { "None", "BothLocal", "BothForeign", "StudentMissing", "StudentForeign", "StudentEmpty", "StudentSwapped", "GuardianMissing", "GuardianForeign", "GuardianEmpty", "GuardianSwapped", "StudentExtraEmpty", "GuardianExtraEmpty" }) {
            Guid? s = null, g = null;
            switch (mode) { case "BothLocal": s = student.Id; g = guardian.Id; break; case "BothForeign": s = foreignStudent.Id; g = foreignGuardian.Id; break; case "StudentMissing": s = Guid.NewGuid(); break; case "StudentForeign": s = foreignStudent.Id; break; case "StudentEmpty": s = Guid.Empty; break; case "StudentSwapped": s = guardian.Id; break; case "GuardianMissing": g = Guid.NewGuid(); break; case "GuardianForeign": g = foreignGuardian.Id; break; case "GuardianEmpty": g = Guid.Empty; break; case "GuardianSwapped": g = student.Id; break; case "StudentExtraEmpty": s = student.Id; g = Guid.Empty; break; case "GuardianExtraEmpty": s = Guid.Empty; g = guardian.Id; break; }
            var label = (document ? "document-" : "consent-") + "reject-" + mode;
            var result = await Send(label, HttpMethod.Post, route + (document ? "/documents" : "/consents"), Body(document, s, g), HttpStatusCode.BadRequest, admin);
            var expected = !document && mode == "None" ? "A consent type and student or guardian are required." : s.HasValue == g.HasValue ? "Select exactly one student or guardian." : s.HasValue ? "Invalid student." : "Invalid guardian.";
            // Existing BadRequest(string) negotiates plain text; the new typed guard returns a JSON message object.
            var error = result.Body.Trim();
            if (error.StartsWith('{')) { using var parsed = JsonDocument.Parse(error); error = parsed.RootElement.GetProperty("message").GetString(); }
            if (error != expected) throw new InvalidOperationException("Person guard error differs"); Pass(label);
        }
        foreach (var document in new[] { true, false }) {
            var path = route + (document ? "/documents" : "/consents");
            foreach (var mode in new[] { "no-body", "null-body", "bad-guid", "missing-type", document ? "bad-date" : "bad-boolean" }) {
                var body = Body(document, student.Id, null); if (mode == "bad-guid") body["studentId"] = "bad-guid"; if (mode == "missing-type") body.Remove(document ? "documentType" : "consentType"); if (mode == "bad-date") body["expiryDate"] = "not-date"; if (mode == "bad-boolean") body["granted"] = "not-boolean";
                var label = (document ? "document-" : "consent-") + "binding-" + mode;
                await Send(label, HttpMethod.Post, path, mode == "no-body" ? null : body, mode == "no-body" ? HttpStatusCode.UnsupportedMediaType : HttpStatusCode.BadRequest, admin, mode == "null-body" ? "null" : null); Pass(label);
            }
            foreach (var field in document ? new[] { "documentType", "fileName" } : new[] { "consentType" }) {
                var body = Body(document, student.Id, null); body[field] = " "; var label = "text-required-" + field;
                await Send(label, HttpMethod.Post, path, body, HttpStatusCode.BadRequest, admin); Pass(label);
            }
        }
        var documents = new List<Guid>(); var consents = new List<Guid>();
        foreach (var document in new[] { true, false }) foreach (var isStudent in new[] { true, false }) foreach (var active in new[] { true, false }) foreach (var optional in new[] { "null", "empty", "filled" }) foreach (var granted in document ? new[] { true } : new[] { true, false }) {
            var person = isStudent ? active ? student.Id : inactiveStudent.Id : active ? guardian.Id : inactiveGuardian.Id;
            var id = await Create((document ? "document-" : "consent-") + (isStudent ? "student-" : "parent-") + (active ? "active-" : "inactive-") + optional + "-" + granted, document, Body(document, isStudent ? person : null, isStudent ? null : person, optional == "null" ? null : optional == "empty" ? "" : "  synthetic reference  ", optional == "filled" ? new(1999, 2, 28) : null, optional == "filled" ? "  StaffRestricted  " : optional == "empty" ? " " : null, granted));
            (document ? documents : consents).Add(id);
        }
        foreach (var document in new[] { true, false }) foreach (var isStudent in new[] { true, false }) await Create((document ? "document" : "consent") + "-typed-collision-" + (isStudent ? "student" : "parent"), document, Body(document, isStudent ? collision : null, isStudent ? null : collision));
        foreach (var document in new[] { true, false }) for (var i = 0; i < 2; i++) await Create((document ? "document" : "consent") + "-same-person-repeat-" + i, document, Body(document, student.Id, null));
        var actions = new[] { ("list-documents", HttpMethod.Get, "/documents", (object?)null), ("list-consents", HttpMethod.Get, "/consents", (object?)null), ("create-document", HttpMethod.Post, "/documents", (object?)Body(true, student.Id, null)), ("create-consent", HttpMethod.Post, "/consents", (object?)Body(false, student.Id, null)), ("review", HttpMethod.Patch, $"/documents/{documents[0]}/review", (object?)new { status = "Approved" }), ("review-task", HttpMethod.Post, $"/documents/{documents[0]}/review-task", (object?)null), ("withdraw", HttpMethod.Patch, $"/consents/{consents[0]}/withdraw", (object?)null) };
        foreach (var action in actions) foreach (var denied in new[] { ("anonymous", (string?)null, HttpStatusCode.Unauthorized), ("foreign-actor", (string?)foreignAdmin, HttpStatusCode.Forbidden), ("teacher", (string?)teacher, HttpStatusCode.Forbidden), ("manager", (string?)manager, HttpStatusCode.Forbidden) }) {
            var label = action.Item1 + "-" + denied.Item1 + "-denied"; await Send(label, action.Item2, route + action.Item3, action.Item4, denied.Item3, denied.Item2); Pass(label);
        }
        foreach (var mode in new[] { "module", "inactive-academy" }) {
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Academies.SingleAsync(x => x.Id == academy); row.EnabledModulesJson = mode == "module" ? "[\"Core\"]" : "[\"Core\",\"AccessGovernance\"]"; row.IsActive = mode != "inactive-academy"; await db.SaveChangesAsync(); }
            foreach (var action in actions) { var label = action.Item1 + "-" + mode + "-denied"; await Send(label, action.Item2, route + action.Item3, action.Item4, HttpStatusCode.Forbidden, admin); Pass(label); }
        }
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Academies.SingleAsync(x => x.Id == academy); row.EnabledModulesJson = "[\"Core\",\"AccessGovernance\"]"; row.IsActive = true; await db.SaveChangesAsync(); }
        foreach (var status in new[] { "PendingReview", "Approved", "Rejected", "Expired" }) {
            var explicitDate = status == "Approved" ? (DateOnly?)new(2020, 2, 29) : null; var path = route + $"/documents/{documents[0]}/review"; var label = "review-canonical-" + status;
            var result = await Send(label, HttpMethod.Patch, path, new { status = status.ToLowerInvariant(), reviewedDate = explicitDate }, HttpStatusCode.OK, admin);
            using var scope = factory.Services.CreateScope(); var stored = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PersonDocuments.AsNoTracking().SingleAsync(x => x.Id == documents[0]); Same(JsonSerializer.Deserialize<PersonDocument>(result.Body, json)!, stored);
            if (stored.Status != status || stored.ReviewedDate != (explicitDate ?? DateOnly.FromDateTime(result.Start))) throw new InvalidOperationException("Canonical status/review date differs");
            Preserve(result.Before, result.After, "documents", 0, stored.Id, "Status", "ReviewedDate"); Audit(result.Before, result.After, "PATCH", path); Pass(label);
        }
        foreach (var (label, method, path, body, expected) in new[] {
            ("review-foreign", HttpMethod.Patch, $"/documents/{foreignDocument.Id}/review", (object?)new { status = "Approved" }, HttpStatusCode.NotFound), ("review-missing", HttpMethod.Patch, $"/documents/{Guid.NewGuid()}/review", (object?)new { status = "Approved" }, HttpStatusCode.NotFound), ("review-status-invalid", HttpMethod.Patch, $"/documents/{documents[0]}/review", (object?)new { status = "Unknown" }, HttpStatusCode.BadRequest),
            ("task-foreign", HttpMethod.Post, $"/documents/{foreignDocument.Id}/review-task", (object?)null, HttpStatusCode.NotFound), ("task-missing", HttpMethod.Post, $"/documents/{Guid.NewGuid()}/review-task", (object?)null, HttpStatusCode.NotFound), ("withdraw-foreign", HttpMethod.Patch, $"/consents/{foreignConsent.Id}/withdraw", (object?)null, HttpStatusCode.NotFound), ("withdraw-missing", HttpMethod.Patch, $"/consents/{Guid.NewGuid()}/withdraw", (object?)null, HttpStatusCode.NotFound) }) {
            await Send(label, method, route + path, body, expected, admin); Pass(label);
        }
        foreach (var raw in new[] { "null", "{\"status\":\"Approved\",\"reviewedDate\":\"bad-date\"}" }) { var label = "review-binding-" + (raw == "null" ? "null" : "bad-date"); await Send(label, HttpMethod.Patch, route + $"/documents/{documents[0]}/review", null, HttpStatusCode.BadRequest, admin, raw); Pass(label); }
        foreach (var (id, label) in new[] { (consents[0], "withdraw-granted"), (consents[1], "withdraw-already-false"), (priorConsent.Id, "withdraw-prior-history"), (priorConsent.Id, "withdraw-repeat-history") }) {
            var path = route + $"/consents/{id}/withdraw"; var result = await Send(label, HttpMethod.Patch, path, null, HttpStatusCode.OK, admin);
            using var scope = factory.Services.CreateScope(); var stored = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().ConsentRecords.AsNoTracking().SingleAsync(x => x.Id == id); Same(JsonSerializer.Deserialize<ConsentRecord>(result.Body, json)!, stored);
            if (stored.Granted || stored.WithdrawnAtUtc is null || stored.WithdrawnAtUtc < result.Start || stored.WithdrawnAtUtc > result.End) throw new InvalidOperationException("Withdrawal lifecycle differs");
            Preserve(result.Before, result.After, "consents", 0, id, "Granted", "WithdrawnAtUtc"); Audit(result.Before, result.After, "PATCH", path); Pass(label);
        }
        foreach (var mode in new[] { "null", "past", "future", "repeat" }) {
            DateOnly? expiry = mode == "null" ? null : DateOnly.FromDateTime(DateTime.UtcNow).AddDays(mode == "future" ? 60 : -10);
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.PersonDocuments.SingleAsync(x => x.Id == documents[0])).ExpiryDate = expiry; await db.SaveChangesAsync(); }
            var path = route + $"/documents/{documents[0]}/review-task"; var label = "review-task-" + mode; var result = await Send(label, HttpMethod.Post, path, null, HttpStatusCode.OK, admin);
            if (result.Body.Length != 0) throw new InvalidOperationException("Intentional task empty200 changed");
            Preserve(result.Before, result.After, "tasks", 1); Audit(result.Before, result.After, "POST", path);
            var priorIds = result.Before.GetProperty("tasks").EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToArray();
            using var taskScope = factory.Services.CreateScope(); var taskDb = taskScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var task = await taskDb.AdminWorkItems.AsNoTracking().SingleAsync(x => !priorIds.Contains(x.Id));
            var expected = new AdminWorkItem { Id = task.Id, AcademyId = academy, Type = "Compliance", Title = "Review ID", Description = "Document synthetic.pdf requires review or renewal.", EntityType = "PersonDocument", EntityId = documents[0], Priority = mode is "past" or "repeat" ? "High" : "Normal", DueAtUtc = expiry?.ToDateTime(TimeOnly.MinValue), CreatedAtUtc = task.CreatedAtUtc };
            Same(task, expected); if (task.CreatedAtUtc < result.Start || task.CreatedAtUtc > result.End) throw new InvalidOperationException("Task clock differs"); Pass(label);
        }
        foreach (var document in new[] { true, false }) {
            var label = (document ? "documents" : "consents") + "-complete-scoped-ordered-readback"; var result = await Send(label, HttpMethod.Get, route + (document ? "/documents" : "/consents"), null, HttpStatusCode.OK, admin);
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (document) { var actual = JsonSerializer.Deserialize<List<PersonDocument>>(result.Body, json)!; var expected = await db.PersonDocuments.AsNoTracking().Where(x => x.AcademyId == academy).OrderBy(x => x.ExpiryDate).ThenBy(x => x.DocumentType).ToListAsync(); if (actual.Count != expected.Count || actual.Any(x => x.AcademyId != academy)) throw new InvalidOperationException("Document list scope/count differs"); for (var i = 0; i < actual.Count; i++) { Same(actual[i], expected.Single(x => x.Id == actual[i].Id)); if (actual[i].ExpiryDate != expected[i].ExpiryDate || actual[i].DocumentType != expected[i].DocumentType) throw new InvalidOperationException("Document sort keys differ"); } }
            else { var actual = JsonSerializer.Deserialize<List<ConsentRecord>>(result.Body, json)!; var expected = await db.ConsentRecords.AsNoTracking().Where(x => x.AcademyId == academy).OrderByDescending(x => x.RecordedAtUtc).ToListAsync(); if (actual.Count != expected.Count || actual.Any(x => x.AcademyId != academy)) throw new InvalidOperationException("Consent list scope/count differs"); for (var i = 0; i < actual.Count; i++) { Same(actual[i], expected.Single(x => x.Id == actual[i].Id)); if (actual[i].RecordedAtUtc != expected[i].RecordedAtUtc) throw new InvalidOperationException("Consent sort keys differ"); } }
            Pass(label);
        }
        if (SubscriptionPlanCatalog.ModuleForController("ComplianceController") != "AccessGovernance" || PermissionCatalog.RequiredFor("ComplianceController") is not null) throw new InvalidOperationException("Accepted module/admin-only policy changed");
        Console.WriteLine($"COMPLIANCE REGRESSION PASS:{count} cases; real Identity/HTTP/SQL typed single subjects, optional/inactive/collision/history, scoped lifecycle/readback, no-write denials and exact actor audits.");
    }
}
