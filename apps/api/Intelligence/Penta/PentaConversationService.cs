using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>
/// Testing-only private synthetic conversations. All SQL writes and mandatory
/// audit are atomic; there is no provider, domain action or free-text transcript.
/// </summary>
public sealed class PentaConversationService(AcademyDeskDbContext db, PentaPilotPolicy policy,
    UserManager<ApplicationUser> users, TimeProvider clock, IConfiguration configuration,
    IHostEnvironment environment, IdentityDbContext identityDb)
{
    public bool IsAvailable => environment.IsEnvironment("Testing") && policy.IsAvailable &&
        configuration.GetValue<bool>("Penta:ConversationContractsEnabled");

    public async Task<PentaConversationOutcome> CreateAsync(ClaimsPrincipal principal,
        Guid academyId, PentaConversationCreateRequest? request, CancellationToken token)
    {
        if (!IsAvailable) return new(404);
        if (!await policy.AllowsAsync(principal, academyId, token)) return new(403);
        if (request is null || request.RequestId == Guid.Empty || request.AdditionalProperties is { Count: > 0 })
            return new(400, Error: "A valid conversation request ID is required.");
        var actorId = (await users.GetUserAsync(principal))!.Id;
        if (db.Database.IsRelational() && !db.Database.IsSqlServer()) return new(503);
        await using var transaction = db.Database.IsSqlServer()
            ? await db.Database.BeginTransactionAsync(token) : null;
        if (!await LockAndAuthorizeAsync(principal, academyId, token)) return new(403);
        var existing = await db.PentaConversations.AsNoTracking().SingleOrDefaultAsync(x =>
            x.AcademyId == academyId && x.ActorUserId == actorId && x.RequestId == request.RequestId, token);
        if (existing is not null)
        {
            if (Expired(existing)) return new(410, Error: "This test conversation expired. Start a new one.");
            Audit(academyId, actorId, existing.Id, "PentaConversationReplayed");
            await db.SaveChangesAsync(token);
            if (transaction is not null) await transaction.CommitAsync(token);
            return new(200, Summary(existing));
        }
        var today = clock.GetUtcNow().UtcDateTime.Date;
        var createdToday = db.PentaConversations.Where(x => x.AcademyId == academyId && x.CreatedAtUtc >= today);
        if (await createdToday.CountAsync(token) >= 100 ||
            await createdToday.CountAsync(x => x.ActorUserId == actorId, token) >= 20)
            return new(429, Error: "Synthetic conversation daily limit reached.");
        var now = clock.GetUtcNow().UtcDateTime;
        var conversation = new PentaConversation
        {
            AcademyId = academyId, ActorUserId = actorId, RequestId = request.RequestId,
            ContextVersion = 0, CreatedAtUtc = now, ExpiresAtUtc = now.AddHours(24)
        };
        db.PentaConversations.Add(conversation);
        Audit(academyId, actorId, conversation.Id, "PentaConversationCreated");
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return new(201, Summary(conversation));
    }

    public async Task<PentaConversationOutcome> ReadAsync(ClaimsPrincipal principal, Guid academyId,
        Guid conversationId, CancellationToken token)
    {
        if (!IsAvailable) return new(404);
        if (!await policy.AllowsAsync(principal, academyId, token)) return new(403);
        var actorId = (await users.GetUserAsync(principal))!.Id;
        if (db.Database.IsRelational() && !db.Database.IsSqlServer()) return new(503);
        await using var transaction = db.Database.IsSqlServer()
            ? await db.Database.BeginTransactionAsync(token) : null;
        if (!await LockAndAuthorizeAsync(principal, academyId, token)) return new(403);
        var conversation = await Owned(academyId, actorId, conversationId).AsNoTracking().SingleOrDefaultAsync(token);
        if (conversation is null) return new(404);
        if (Expired(conversation)) return new(410, Error: "This test conversation expired. Start a new one.");
        var messages = await db.PentaConversationMessages.AsNoTracking().Where(x =>
            x.AcademyId == academyId && x.ActorUserId == actorId && x.ConversationId == conversationId)
            .OrderBy(x => x.Sequence).Take(PentaConversationContract.MaximumTurns * 2 + 1).ToListAsync(token);
        if (!ValidHistory(conversation, messages)) return InvalidStored();
        Audit(academyId, actorId, conversationId, "PentaConversationRead");
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return new(200, new PentaConversationHistory(Summary(conversation), Views(messages)));
    }

    public async Task<PentaConversationOutcome> AppendAsync(ClaimsPrincipal principal, Guid academyId,
        Guid conversationId, PentaConversationTurnRequest? request, CancellationToken token)
    {
        if (!IsAvailable) return new(404);
        if (!await policy.AllowsAsync(principal, academyId, token)) return new(403);
        if (!PentaConversationContract.Valid(request))
            return new(400, Error: "Only valid synthetic conversation contract requests are enabled.");
        var actorId = (await users.GetUserAsync(principal))!.Id;
        if (db.Database.IsRelational() && !db.Database.IsSqlServer()) return new(503);
        await using var transaction = db.Database.IsSqlServer()
            ? await db.Database.BeginTransactionAsync(token) : null;
        // Academy-first ordering matches existing PENTA operations. SQL requests
        // from different HTTP scopes serialize before checking version or replay.
        if (!await LockAndAuthorizeAsync(principal, academyId, token)) return new(403);
        var conversation = await Owned(academyId, actorId, conversationId).SingleOrDefaultAsync(token);
        if (conversation is null) return new(404);
        if (Expired(conversation)) return new(410, Error: "This test conversation expired. Start a new one.");
        var digest = Digest(request!);
        var previous = await db.PentaConversationTurns.AsNoTracking().SingleOrDefaultAsync(x =>
            x.AcademyId == academyId && x.ActorUserId == actorId && x.ConversationId == conversationId &&
            x.RequestId == request!.RequestId, token);
        if (previous is not null)
        {
            if (previous.InputDigest != digest) return new(409, Error: "This request ID was used for different turn details.");
            var saved = await db.PentaConversationMessages.AsNoTracking().Where(x =>
                x.AcademyId == academyId && x.ActorUserId == actorId && x.ConversationId == conversationId &&
                x.TurnId == previous.Id).OrderBy(x => x.Sequence).ToListAsync(token);
            if (!ValidReceipt(previous, saved, request!)) return InvalidStored();
            Audit(academyId, actorId, conversationId, "PentaConversationTurnReplayed");
            await db.SaveChangesAsync(token);
            if (transaction is not null) await transaction.CommitAsync(token);
            return new(200, Receipt(previous, saved));
        }
        if (conversation.ContextVersion != request!.ExpectedContextVersion)
            return new(409, Error: "The conversation changed. Refresh its context before submitting another turn.");
        if (conversation.ContextVersion >= PentaConversationContract.MaximumTurns)
            return new(429, Error: "Synthetic conversation turn limit reached. Start a new conversation.");
        var now = clock.GetUtcNow().UtcDateTime;
        var turn = new PentaConversationTurn
        {
            AcademyId = academyId, ActorUserId = actorId, ConversationId = conversationId,
            RequestId = request.RequestId, ExpectedContextVersion = request.ExpectedContextVersion,
            CompletedContextVersion = conversation.ContextVersion + 1, Capability = request.Capability!,
            InputDigest = digest, CreatedAtUtc = now
        };
        var messages = new[]
        {
            Message(turn, conversation.ContextVersion * 2 + 1, "User", request.Text!, now),
            Message(turn, conversation.ContextVersion * 2 + 2, "Assistant", PentaConversationContract.Reply, now)
        };
        conversation.ContextVersion = turn.CompletedContextVersion;
        db.PentaConversationTurns.Add(turn);
        db.PentaConversationMessages.AddRange(messages);
        Audit(academyId, actorId, conversationId, "PentaConversationTurnRecorded");
        await db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return new(201, Receipt(turn, messages));
    }

    private IQueryable<PentaConversation> Owned(Guid academyId, Guid actorId, Guid conversationId) =>
        db.PentaConversations.Where(x => x.AcademyId == academyId && x.ActorUserId == actorId && x.Id == conversationId);

    private async Task<bool> LockAndAuthorizeAsync(ClaimsPrincipal principal, Guid academyId, CancellationToken token)
    {
        if (db.Database.IsSqlServer() && !await db.Academies.FromSqlInterpolated(
            $"SELECT * FROM [Academies] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {academyId}")
            .AsNoTracking().AnyAsync(token)) return false;
        // Revocation is checked again after the serialization boundary, not just
        // before waiting for another request. No authority comes from message text.
        // Discard only this read-only Identity scope's cached entities so an
        // actor disabled while the SQL lock was awaited is reloaded from SQL.
        identityDb.ChangeTracker.Clear();
        return await policy.AllowsAsync(principal, academyId, token);
    }

    private bool Expired(PentaConversation conversation) => conversation.ExpiresAtUtc <= clock.GetUtcNow().UtcDateTime;
    private static PentaConversationSummary Summary(PentaConversation conversation) =>
        new(conversation.Id, conversation.ContextVersion, conversation.ExpiresAtUtc);
    private static string Digest(PentaConversationTurnRequest request) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { request.ExpectedContextVersion, request.Text, request.Capability }))));
    private static PentaConversationMessage Message(PentaConversationTurn turn, long sequence, string role, string content, DateTime now) =>
        new() { AcademyId = turn.AcademyId, ActorUserId = turn.ActorUserId, ConversationId = turn.ConversationId,
            TurnId = turn.Id, Sequence = sequence, Role = role, Content = content, CreatedAtUtc = now };
    private static PentaConversationMessageView[] Views(IEnumerable<PentaConversationMessage> messages) =>
        messages.Select(x => new PentaConversationMessageView(x.Sequence, x.Role, x.Content)).ToArray();
    private static PentaConversationTurnReceipt Receipt(PentaConversationTurn turn, IEnumerable<PentaConversationMessage> messages) =>
        new(turn.ConversationId, turn.Id, turn.RequestId, turn.CompletedContextVersion, turn.Capability, Views(messages));
    private static PentaConversationOutcome InvalidStored() => new(409, Error: "Stored test conversation failed integrity validation.");

    private static bool ValidReceipt(PentaConversationTurn turn, IReadOnlyList<PentaConversationMessage> messages,
        PentaConversationTurnRequest request) => turn.CompletedContextVersion == turn.ExpectedContextVersion + 1 &&
        turn.ExpectedContextVersion >= 0 && turn.CompletedContextVersion <= PentaConversationContract.MaximumTurns &&
        turn.Capability == request.Capability && messages.Count == 2 &&
        messages[0].Sequence == turn.ExpectedContextVersion * 2 + 1 && messages[0].Role == "User" && messages[0].Content == request.Text &&
        messages[1].Sequence == turn.ExpectedContextVersion * 2 + 2 && messages[1].Role == "Assistant" &&
        messages[1].Content == PentaConversationContract.Reply;

    private static bool ValidHistory(PentaConversation conversation, IReadOnlyList<PentaConversationMessage> messages) =>
        conversation.ContextVersion is >= 0 and <= PentaConversationContract.MaximumTurns &&
        messages.Count == conversation.ContextVersion * 2 && messages.Select((message, index) =>
            message.Sequence == index + 1 && (index % 2 == 0
                ? message.Role == "User" && message.Content is PentaConversationContract.FirstInput or PentaConversationContract.FollowupInput
                : message.Role == "Assistant" && message.Content == PentaConversationContract.Reply)).All(x => x);

    private void Audit(Guid academyId, Guid actorId, Guid conversationId, string action) => db.AuditLogs.Add(new AuditLog
    {
        AcademyId = academyId, ActorUserId = actorId, EntityId = conversationId,
        EntityType = "PentaConversation", Action = action, OccurredAtUtc = clock.GetUtcNow().UtcDateTime,
        MetadataJson = "{\"policy\":\"penta.conversation.synthetic.v1\",\"synthetic\":true,\"noDomainEffect\":true}"
    });
}
