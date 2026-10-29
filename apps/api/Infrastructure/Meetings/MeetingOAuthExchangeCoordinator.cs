using System.Security.Claims;

namespace AcademyDesk.Api.Infrastructure.Meetings;

// Inactive internal coordinator: durable admission commits BEFORE network, and
// never holds a SQL transaction open across the token endpoint. A crash/fault
// leaves Started for fenced unknown-outcome recovery, not a reusable auth code.
internal sealed class MeetingOAuthExchangeCoordinator(MeetingOAuthAttemptStore store, MeetingOAuthCodeExchange exchange)
{
    public async Task<MeetingOAuthPersistenceOutcome> ExecuteAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingOAuthOutcome claim, string? clientSecret, CancellationToken token = default)
    {
        var started = await store.BeginExchangeAsync(actor, academy, registration, claim, token);
        if (started.StatusCode != 201 || started.Permit is null) return started;
        var result = await exchange.ExchangeAsync(actor, academy, registration, claim, clientSecret, token);
        return await store.FinishExchangeAsync(actor, academy, registration, started.Permit, result, token);
    }
}
