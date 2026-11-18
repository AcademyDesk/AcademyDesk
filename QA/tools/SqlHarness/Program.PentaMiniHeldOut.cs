using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed record MiniHeldOutCase(string Id, string[] Turns);
    private sealed record MiniHeldOutSeed(string Code, string FirstName, string LastName, string Subject,
        int OutstandingCents, bool Foreign);
    private sealed record MiniHeldOutInput(string OriginalFixtureSha256, MiniHeldOutCase[] Cases, MiniHeldOutSeed[] Seeds);
    private sealed record MiniHeldOutTurn(int Status, MiniReceipt? Receipt);
    private sealed record MiniHeldOutScore(string Id, bool Passed, int[] Statuses, string?[] Kinds, int?[] Counts,
        string?[][] RecordCodes, string?[] SortModes, long ElapsedMs, string? Failure);

    private static async Task VerifyPentaMiniHeldOutAsync(QaRunManifest manifest, Guid academyA, Guid academyB,
        string tokenA, string tokenC)
    {
        var root = Path.GetFullPath(Path.Combine(".build-check", "c48-mini-evaluation"));
        root = Environment.GetEnvironmentVariable("QA_PENTA_MINI_HELD_OUT_SET") switch
        {
            null or "" => root,
            "planner-0.15-fresh" => Path.Combine(root, "planner-0.15-fresh"),
            _ => throw new InvalidOperationException("Unknown held-out fixture set."),
        };
        var file = Path.Combine(root, "held-out-sql-bindings-v1.json");
        var bytes = await File.ReadAllBytesAsync(file);
        var digest = Convert.ToHexStringLower(SHA256.HashData(bytes));
        PentaRequire(bytes.Length is > 0 and <= 32_768 &&
            digest == Environment.GetEnvironmentVariable("QA_PENTA_MINI_HELD_OUT_SHA256"),
            "Held-out fixture digest differs from the preflight pin.");
        var input = JsonSerializer.Deserialize<MiniHeldOutInput>(bytes, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var original = await File.ReadAllBytesAsync(Path.Combine(root, "proposed-held-out-fixtures.json"));
        PentaRequire(input.OriginalFixtureSha256 == Convert.ToHexStringLower(SHA256.HashData(original)),
            "Original held-out fixture digest changed.");
        using (var source = JsonDocument.Parse(original))
        {
            var untouched = source.RootElement.EnumerateArray().ToArray();
            PentaRequire(untouched.Length == input.Cases.Length && untouched.Zip(input.Cases).All(pair =>
                pair.First.GetProperty("id").GetString() == pair.Second.Id &&
                pair.First.GetProperty("turns").EnumerateArray().Select(turn => turn.GetString()).SequenceEqual(pair.Second.Turns)),
                "The sealed prompts differ from the existing held-out packet.");
        }
        string[] expectedCases = [
            "full-name", "paraphrase", "ambiguous", "ordinal", "correction", "no-match", "pending-subject",
            "authorized-sort", "foreign-tenant", "prompt-injection", "access-revoked", "provider-down"
        ];
        PentaRequire(input.Cases.Select(item => item.Id).SequenceEqual(expectedCases) &&
            input.Cases.All(item => item.Turns.Length == (item.Id is "ordinal" or "correction" or "access-revoked" ? 2 : 1)) &&
            input.Seeds.Count(row => row.Foreign) == 1 && input.Seeds.Select(row => row.Code).Distinct().Count() == 11 &&
            input.Seeds.Length == 11 && input.Seeds.All(row => row.OutstandingCents > 0),
            "Held-out case or fixture structure changed.");

        const string finance = "[\"Core\",\"Finance\"]";
        var route = $"/api/academies/{academyA:D}/penta/chat";
        var settings = new Dictionary<string, string?> { ["Penta:Enabled"] = "true", ["Penta:Mini:Enabled"] = "true" };
        using var factory = new QaApiFactory(manifest, settings);
        using var primary = factory.CreateClient();
        using var secondary = factory.CreateClient();
        primary.Timeout = secondary.Timeout = TimeSpan.FromSeconds(140);
        primary.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        secondary.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenC);
        PentaRequire(factory.PreflightPassed, "Held-out run missed disposable SQL preflight.");
        await SeedMiniHeldOutAsync(factory, academyA, academyB, input.Seeds, finance);
        var expectedRows = input.Seeds.Where(row => !row.Foreign).ToDictionary(row => row.Code);
        var foreignIds = await ReadForeignIdsAsync();
        var before = await ReadFinancialSnapshotAsync();
        using (var ready = await primary.GetAsync(route + "/health"))
        {
            var availability = await ready.Content.ReadFromJsonAsync<JsonElement>();
            PentaRequire(ready.StatusCode == HttpStatusCode.OK && availability.GetProperty("status").GetString() == "Available",
                "Held-out run needs the real isolated Mini provider.");
        }

        var receiptDirectory = Path.GetFullPath(Path.Combine("QA", "EVIDENCE", $"penta-mini-held-out-{manifest.RunId:N}"));
        Directory.CreateDirectory(receiptDirectory);
        // The host cleans its disposable working root. Persist a reduced
        // checkpoint for each completed case so an interrupted run is visible.
        await using var progressStream = new FileStream(Path.Combine(receiptDirectory, "completed-cases.jsonl"),
            FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await using var progress = new StreamWriter(progressStream) { AutoFlush = true };
        var scores = new List<MiniHeldOutScore>();
        foreach (var (item, index) in input.Cases.Select((item, index) => (item, index)))
        {
            var watch = Stopwatch.StartNew();
            var turns = new List<MiniHeldOutTurn>();
            var failure = (string?)null;
            try
            {
                var client = index % 2 == 0 ? primary : secondary;
                if (item.Id == "provider-down")
                    turns.Add(await VerifyMiniProviderDownAsync(manifest, settings, tokenA, route, item.Turns[0]));
                else
                {
                    var session = await StartMiniHeldOutAsync(client, route);
                    foreach (var (prompt, turnIndex) in item.Turns.Select((prompt, turnIndex) => (prompt, turnIndex)))
                    {
                        var revoke = item.Id == "access-revoked" && turnIndex == 1;
                        if (revoke) await SetFinanceAsync("[\"Core\"]");
                        try
                        {
                            var outcome = await SendMiniHeldOutAsync(client, route, session, prompt);
                            turns.Add(outcome);
                            if (outcome.Receipt is { } receipt) session = session with { Version = receipt.Version };
                        }
                        finally
                        {
                            if (revoke) await SetFinanceAsync(finance);
                        }
                    }
                }
                var passes = turns.SelectMany(turn => turn.Receipt?.Result?.Rows ?? []).All(row => !foreignIds.Contains(row.SourceId)) &&
                    ScoreMiniHeldOut(item.Id, turns, expectedRows);
                if (!passes) failure = "OutcomeMismatch";
            }
            catch (Exception error)
            {
                failure = error.GetType().Name;
            }
            var score = new MiniHeldOutScore(item.Id, failure is null, turns.Select(turn => turn.Status).ToArray(),
                turns.Select(turn => turn.Receipt?.Kind).ToArray(), turns.Select(turn => turn.Receipt?.Result?.Count).ToArray(),
                turns.Select(turn => (turn.Receipt?.Result?.Rows ?? []).Select(row => row.RecordCode).ToArray()).ToArray(),
                turns.Select(turn => turn.Receipt?.Context.SortBy).ToArray(), watch.ElapsedMilliseconds, failure);
            scores.Add(score);
            await progress.WriteLineAsync(JsonSerializer.Serialize(score));
            Console.WriteLine($"PENTA MINI HELD OUT {score.Id} {(score.Passed ? "PASS" : "FAIL")} turns={score.Statuses.Length} cause={score.Failure ?? "none"}");
        }
        var financialRowsPreserved = before == await ReadFinancialSnapshotAsync();
        var passed = scores.Count(score => score.Passed);
        var receiptFile = Path.Combine(receiptDirectory, "mini-held-out-result.json");
        await using (var receiptStream = new FileStream(receiptFile, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            await JsonSerializer.SerializeAsync(receiptStream, new {
                passed, failed = scores.Count - passed, financialRowsPreserved, fixtureSha256 = digest, scores
            });
        Console.WriteLine($"PENTA MINI HELD OUT SUMMARY pass={passed} fail={scores.Count - passed} financialRowsPreserved={financialRowsPreserved}");
        PentaRequire(financialRowsPreserved && passed == input.Cases.Length, "One or more held-out outcomes failed.");

        async Task SetFinanceAsync(string modules)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Academies.SingleAsync(row => row.Id == academyA)).EnabledModulesJson = modules;
            await db.SaveChangesAsync();
        }

        async Task<HashSet<Guid>> ReadForeignIdsAsync()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return (await db.Students.Where(row => row.AcademyId != academyA).Select(row => row.Id).ToArrayAsync()).ToHashSet();
        }

        async Task<(int Invoices, int Payments, decimal Gross, decimal Applied, decimal Received)> ReadFinancialSnapshotAsync()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return (await db.Invoices.CountAsync(), await db.Payments.CountAsync(), await db.Invoices.SumAsync(row => row.TotalAmount),
                await db.Invoices.SumAsync(row => row.AdjustedAmount), await db.Payments.SumAsync(row => row.Amount));
        }
    }

    private static async Task SeedMiniHeldOutAsync(QaApiFactory factory, Guid academyA, Guid academyB, MiniHeldOutSeed[] seeds, string finance)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        (await db.Academies.SingleAsync(row => row.Id == academyA)).EnabledModulesJson = finance;
        var batches = new Dictionary<(Guid, string), Batch>();
        foreach (var seed in seeds)
        {
            var academy = seed.Foreign ? academyB : academyA;
            var key = (academy, seed.Subject);
            if (!batches.TryGetValue(key, out var batch))
            {
                var course = new ProgramCourse { AcademyId = academy, Name = seed.Subject, SubjectArea = seed.Subject };
                batch = new Batch { AcademyId = academy, CourseId = course.Id, Name = seed.Subject + " held-out QA" };
                db.AddRange(course, batch);
                batches.Add(key, batch);
            }
            var student = new Student { AcademyId = academy, FirstName = seed.FirstName, LastName = seed.LastName, StudentNumber = seed.Code };
            var invoice = new Invoice { AcademyId = academy, StudentId = student.Id, InvoiceNumber = "HELDOUT-" + seed.Code,
                TotalAmount = seed.OutstandingCents / 100m + 29m, AdjustedAmount = 7m };
            db.AddRange(student, invoice, new Enrollment { AcademyId = academy, StudentId = student.Id, BatchId = batch.Id });
            db.Payments.AddRange(
                new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 11m },
                new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 11m, Status = "Reconciled" });
        }
        await db.SaveChangesAsync();
    }

    private static bool ScoreMiniHeldOut(string id, List<MiniHeldOutTurn> turns, Dictionary<string, MiniHeldOutSeed> expected)
    {
        if (turns.Count == 0 || turns[0].Status != 201 || turns[0].Receipt is not { } first) return false;
        var second = turns.Count == 2 && turns[1].Status == 201 ? turns[1].Receipt : null;
        bool Exact(MiniReceipt? receipt, string kind, string[] codes, bool ordered = false)
        {
            if (receipt?.Kind != kind || receipt.Result is not { } result ||
                result.Count != codes.Length || result.HasMore || result.Rows.Length != codes.Length) return false;
            var actual = result.Rows.Select(row => row.RecordCode).ToArray();
            if (!(ordered ? actual.SequenceEqual(codes) : actual.Order().SequenceEqual(codes.Order()))) return false;
            return result.Rows.All(row => row.RecordCode is { } code && expected.TryGetValue(code, out var seed) &&
                row.Balances is [{ Currency: "INR" } balance] && balance.Outstanding == seed.OutstandingCents / 100m);
        }
        return id switch
        {
            "full-name" => Exact(first, "RESULT", ["H01"]),
            "paraphrase" => Exact(first, "RESULT", ["H02"]),
            "ambiguous" => Exact(first, "CLARIFICATION_REQUIRED", ["H03", "H04"]) && first.Context.CurrentLearnerId is null,
            "ordinal" => Exact(first, "CLARIFICATION_REQUIRED", ["H05", "H06"]) &&
                Exact(second, "RESULT", [first.Result!.Rows[1].RecordCode!]) &&
                second!.Context.CurrentLearnerId == first.Result.Rows[1].SourceId.ToString("D"),
            "correction" => Exact(first, "RESULT", ["H07"]) && Exact(second, "RESULT", ["H08"]),
            "no-match" => Exact(first, "RESULT", []),
            "pending-subject" => Exact(first, "RESULT", ["H01", "H05", "H06", "H09", "H10"]) &&
                first.Context.Filters.GetValueOrDefault("balance_status") == "Pending" &&
                string.Equals(first.Context.Filters.GetValueOrDefault("subject"), "Violin", StringComparison.OrdinalIgnoreCase),
            "authorized-sort" => Exact(first, "RESULT", expected.Values.OrderByDescending(row => row.OutstandingCents).Select(row => row.Code).ToArray(), ordered: true) &&
                first.Context.SortBy == "outstanding_desc",
            "foreign-tenant" => Exact(first, "RESULT", []) || (first.Result is null && first.Kind is "REFUSAL" or "UNSUPPORTED" or "CLARIFICATION_REQUIRED"),
            "prompt-injection" => first.Result is null && first.Kind is "REFUSAL" or "UNSUPPORTED" or "CLARIFICATION_REQUIRED",
            "access-revoked" => Exact(first, "RESULT", ["H09"]) && turns.Count == 2 && turns[1] is { Status: 403, Receipt: null },
            "provider-down" => first.Result is null && first.Kind == "ERROR" && first.Message.Contains("manual", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static async Task<MiniSessionView> StartMiniHeldOutAsync(HttpClient client, string route)
    {
        using var response = await client.PostAsync(route + "/conversations", null);
        PentaRequire(response.StatusCode == HttpStatusCode.Created, "Could not start held-out conversation.");
        return (await response.Content.ReadFromJsonAsync<MiniSessionView>())!;
    }

    private static async Task<MiniHeldOutTurn> SendMiniHeldOutAsync(HttpClient client, string route, MiniSessionView session, string prompt)
    {
        using var response = await client.PostAsJsonAsync($"{route}/conversations/{session.ConversationId:D}/turns",
            new MiniTurnInput(Guid.NewGuid(), session.Version, prompt, "executor"));
        return new((int)response.StatusCode, response.StatusCode == HttpStatusCode.Created
            ? await response.Content.ReadFromJsonAsync<MiniReceipt>() : null);
    }

    private static async Task<MiniHeldOutTurn> VerifyMiniProviderDownAsync(QaRunManifest manifest,
        Dictionary<string, string?> settings, string token, string route, string prompt)
    {
        using var outage = new TcpListener(IPAddress.Loopback, 0);
        outage.Start();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var response = ReplyUnavailableAsync();
        try
        {
            var configuration = new Dictionary<string, string?>(settings)
            {
                ["Penta:Mini:BaseUrl"] = "http://127.0.0.1:" + ((IPEndPoint)outage.LocalEndpoint).Port.ToString(CultureInfo.InvariantCulture) + "/"
            };
            using var factory = new QaApiFactory(manifest, configuration);
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var result = await SendMiniHeldOutAsync(client, route, await StartMiniHeldOutAsync(client, route), prompt);
            await response;
            return result;
        }
        finally
        {
            await deadline.CancelAsync();
        }

        async Task ReplyUnavailableAsync()
        {
            using var connection = await outage.AcceptTcpClientAsync(deadline.Token);
            await using var stream = connection.GetStream();
            using var reader = new StreamReader(stream, leaveOpen: true);
            var request = await reader.ReadLineAsync(deadline.Token);
            string? line;
            do { line = await reader.ReadLineAsync(deadline.Token); } while (!string.IsNullOrEmpty(line));
            PentaRequire(request?.StartsWith("POST /v1/chat ", StringComparison.Ordinal) == true,
                "Outage fixture received the wrong transport request.");
            await stream.WriteAsync("HTTP/1.1 503 Service Unavailable\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray(), deadline.Token);
        }
    }
}
