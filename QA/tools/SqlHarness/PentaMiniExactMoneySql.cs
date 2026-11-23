using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMiniExactMoneySqlAsync(QaRunManifest manifest, Guid academy, string token)
    {
        const string code = "QA-C55-EXACT";
        var planner = new MiniFaultProvider { Next = new("0.1", "TOOL_REQUEST", "Request prepared.",
            new("SearchLearners", new() { ["name"] = JsonSerializer.SerializeToElement(code) }), "READ", MiniState.Empty) };
        var fault = new MiniAuditFault();
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> {
            ["Penta:Enabled"] = "true", ["Penta:Mini:Enabled"] = "true"
        }, isolatedMiniProvider: planner, isolatedDomainInterceptor: fault);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        PentaRequire(factory.PreflightPassed, "Exact-money test missed disposable SQL guard.");
        var learner = new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "Exact", StudentNumber = code };
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = "[\"Core\",\"Finance\"]";
            var small = new Invoice { AcademyId = academy, StudentId = learner.Id, InvoiceNumber = code + "-INR", TotalAmount = 1000.01m, Currency = "INR" };
            db.AddRange(learner, small, new Invoice { AcademyId = academy, StudentId = learner.Id, InvoiceNumber = code + "-USD",
                TotalAmount = 90071992547409.91m, Currency = "USD" }, new Payment { AcademyId = academy, InvoiceId = small.Id,
                Amount = 1000m, Currency = "INR", Status = "Reconciled" });
            await db.SaveChangesAsync();
        }
        var before = await LedgerAsync();
        var root = $"/api/academies/{academy:D}/penta/chat/conversations";
        using var created = await client.PostAsync(root, null);
        RequireFinanceStatus(created, HttpStatusCode.Created, "exact-money session");
        var session = (await created.Content.ReadFromJsonAsync<MiniSessionView>())!;
        var request = Guid.NewGuid();
        var url = $"{root}/{session.ConversationId:D}/turns";
        using (var sent = await client.PostAsJsonAsync(url, new MiniTurnInput(request, 0, "Find the synthetic exact-money learner.", "executor")))
        {
            RequireFinanceStatus(sent, HttpStatusCode.Created, "exact-money original turn");
            Check(await sent.Content.ReadAsStringAsync());
        }
        var baseline = await StateAsync();
        PentaRequire(baseline.Turns == 1 && baseline.Audits == 2 && planner.Calls == 1, "Exact-money turn was not saved once with mandatory audits.");
        using (var recovered = await client.GetAsync(url + $"/{request:D}"))
        {
            RequireFinanceStatus(recovered, HttpStatusCode.OK, "exact-money recovery");
            Check(await recovered.Content.ReadAsStringAsync());
            PentaRequire(recovered.Headers.CacheControl?.NoStore == true, "Exact recovery was cacheable.");
        }
        // An actual old-format protected snapshot has no exact-money property.
        // Preserve its original typed decimals, rather than fabricate a replacement ledger read.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var actor = await db.PentaMiniSessions.Where(x => x.Id == session.ConversationId).Select(x => x.ActorUserId).SingleAsync();
            var turn = await db.PentaMiniTurns.SingleAsync(x => x.SessionId == session.ConversationId && x.RequestId == request);
            var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector(
                "PentaMini.local.v1", academy.ToString("D"), actor.ToString("D"), session.ConversationId.ToString("D"));
            var old = JsonNode.Parse(protector.Unprotect(turn.ProtectedReceipt!))!.AsObject();
            PentaRequire(old.Remove("ExactMoney"), "The new snapshot did not contain its projected view.");
            turn.ProtectedReceipt = protector.Protect(old.ToJsonString());
            await db.SaveChangesAsync();
        }
        using (var recovered = await client.GetAsync(url + $"/{request:D}"))
        {
            RequireFinanceStatus(recovered, HttpStatusCode.OK, "legacy exact-money recovery during provider downtime");
            Check(await recovered.Content.ReadAsStringAsync());
        }
        var legacyState = await StateAsync();
        PentaRequire(legacyState == (1, 4) && planner.Calls == 1, "Legacy recovery redispatched or changed its turn.");
        await VerifyLegacyVariantAsync("unsupported precision", receipt => receipt with {
            Result = receipt.Result! with { Rows = [receipt.Result.Rows[0] with { Balances = [new LearnerBalance("INR", 0.001m)] }] }
        }, body => {
            PentaRequire(!body.TryGetProperty("exactMoney", out _), "Unsupported precision became an exact-money claim.");
            PentaRequire(body.GetProperty("result").GetProperty("rows")[0].GetProperty("balances")[0].GetProperty("outstanding").GetDecimal() == 0.001m,
                "Legacy unsupported precision was silently rounded.");
        });
        await VerifyLegacyVariantAsync("receipt size fallback", receipt => {
            var currencies = new[] { "USD", "INR", "GBP", "EUR", "AUD", "CAD", "JPY", "CNY", "SGD", "CHF", "NZD", "SEK", "NOK", "DKK", "AED", "SAR", "ZAR", "BRL", "KRW", "HKD" };
            var row = receipt.Result!.Rows[0] with { DisplayName = "", Subjects = [],
                Balances = currencies.Select(currency => new LearnerBalance(currency, decimal.MaxValue)).ToArray() };
            var candidate = receipt with { Result = receipt.Result with { Rows = [row] } };
            // Tune only the invented legacy display text. All IDs still refer to
            // the real active SQL learner; the ledger itself is never changed.
            var original = JsonNode.Parse(JsonSerializer.Serialize(candidate))!.AsObject();
            original.Remove("ExactMoney");
            row = row with { DisplayName = new string('n', MiniReceipt.MaximumSerializedCharacters - 50 - original.ToJsonString().Length) };
            candidate = candidate with { Result = candidate.Result! with { Rows = [row] } };
            var encoded = JsonSerializer.Serialize(candidate);
            PentaRequire(encoded.Length <= MiniReceipt.MaximumSerializedCharacters && candidate.ExactMoney is null,
                "Near-bound legacy fixture did not exercise optional projection suppression.");
            var hypothetical = JsonNode.Parse(encoded)!.AsObject();
            hypothetical["ExactMoney"] = JsonSerializer.SerializeToNode(PentaMiniMoney.FromResult(candidate.Result));
            PentaRequire(hypothetical.ToJsonString().Length > MiniReceipt.MaximumSerializedCharacters,
                "Size fixture would fit with the projection and did not test the bound.");
            return candidate;
        }, body => PentaRequire(!body.TryGetProperty("exactMoney", out _), "Oversized optional projection was returned."));
        var valid = await StateAsync();
        PentaRequire(valid == (1, 6) && planner.Calls == 1, "Legacy boundary recovery repeated a turn or missed mandatory audit.");
        fault.Enabled = true;
        try
        {
            using var failed = await client.GetAsync(url + $"/{request:D}");
            RequireFinanceStatus(failed, HttpStatusCode.ServiceUnavailable, "exact-money replay audit failure");
            var body = await failed.Content.ReadAsStringAsync();
            PentaRequire(!body.Contains("90071992547409.91", StringComparison.Ordinal) && !body.Contains(learner.Id.ToString("D"), StringComparison.OrdinalIgnoreCase),
                "Audit failure exposed exact financial results.");
        }
        finally { fault.Enabled = false; }
        PentaRequire(await StateAsync() == valid && planner.Calls == 1 && before == await LedgerAsync(),
            "Exact-money recovery changed ledger data, committed a failed audit or repeated planning.");
        Console.WriteLine("PENTA MINI EXACT MONEY SQL PASS: one cent, large/mixed currency strings, original and legacy GET, unsupported precision, receipt-size fallback, provider downtime, audit rollback, one dispatch and unchanged ledger.");

        async Task VerifyLegacyVariantAsync(string name, Func<MiniReceipt, MiniReceipt> variant, Action<JsonElement> check)
        {
            string originalProtected;
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var actor = await db.PentaMiniSessions.Where(x => x.Id == session.ConversationId).Select(x => x.ActorUserId).SingleAsync();
                var turn = await db.PentaMiniTurns.SingleAsync(x => x.SessionId == session.ConversationId && x.RequestId == request);
                originalProtected = turn.ProtectedReceipt!;
                var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector(
                    "PentaMini.local.v1", academy.ToString("D"), actor.ToString("D"), session.ConversationId.ToString("D"));
                var receipt = variant(JsonSerializer.Deserialize<MiniReceipt>(protector.Unprotect(originalProtected))!);
                PentaRequire(PentaMiniRecovery.ValidReceipt(receipt, session.ConversationId, request, 0), "Legacy boundary fixture violated recovery identity.");
                var encoded = JsonSerializer.Serialize(receipt);
                PentaRequire(encoded.Length <= MiniReceipt.MaximumSerializedCharacters, "Legacy fixture exceeded its stored bound.");
                turn.ProtectedReceipt = protector.Protect(encoded);
                await db.SaveChangesAsync();
            }
            try
            {
                using var recovered = await client.GetAsync(url + $"/{request:D}");
                RequireFinanceStatus(recovered, HttpStatusCode.OK, name + " protected legacy GET");
                var raw = await recovered.Content.ReadAsStringAsync();
                PentaRequire(raw.Length <= MiniReceipt.MaximumSerializedCharacters && recovered.Headers.CacheControl?.NoStore == true,
                    "Legacy boundary response exceeded its limit or became cacheable.");
                using var json = JsonDocument.Parse(raw);
                check(json.RootElement);
                PentaRequire(planner.Calls == 1 && before == await LedgerAsync(), "Legacy boundary GET redispatched or changed the ledger.");
                Console.WriteLine($"PENTA MINI EXACT MONEY {name} protected GET PASS");
            }
            finally
            {
                await using var scope = factory.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var turn = await db.PentaMiniTurns.SingleAsync(x => x.SessionId == session.ConversationId && x.RequestId == request);
                turn.ProtectedReceipt = originalProtected;
                await db.SaveChangesAsync();
            }
        }

        void Check(string raw)
        {
            PentaRequire(raw.Length <= MiniReceipt.MaximumSerializedCharacters, "Exact-money wire receipt exceeded its bound.");
            using var doc = JsonDocument.Parse(raw);
            var money = doc.RootElement.GetProperty("exactMoney");
            var row = money.GetProperty("rows").EnumerateArray().Single();
            var balances = row.GetProperty("balances").EnumerateArray().ToDictionary(x => x.GetProperty("currency").GetString()!, x => x.GetProperty("outstandingExact").GetString());
            PentaRequire(money.GetProperty("version").GetString() == "mini-exact-money.v1" && row.GetProperty("sourceId").GetGuid() == learner.Id &&
                balances.Count == 2 && balances["INR"] == "0.01" && balances["USD"] == "90071992547409.91", "Exact SQL cents/currency strings did not survive the protected wire receipt.");
        }
        async Task<(int Turns, int Audits)> StateAsync()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var id = request.ToString("D");
            return (await db.PentaMiniTurns.CountAsync(x => x.SessionId == session.ConversationId),
                await db.AuditLogs.CountAsync(x => x.EntityId == session.ConversationId && x.MetadataJson != null && x.MetadataJson.Contains(id)));
        }
        async Task<(int Invoices, decimal Gross, int Payments, decimal Collected)> LedgerAsync()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return (await db.Invoices.CountAsync(), await db.Invoices.SumAsync(x => x.TotalAmount), await db.Payments.CountAsync(), await db.Payments.SumAsync(x => x.Amount));
        }
    }
}
