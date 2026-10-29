using System.Net;
using System.Security.Claims;

namespace AcademyDesk.Api.Infrastructure.Meetings;

// Inactive candidate, no DI/routes. Future activation requires distinct reviewed
// Google project-wide grant consent, secure handler and actual provider qualification.
internal sealed class MeetingProviderRevocation(MeetingOAuthAttemptStore store, HttpMessageInvoker transport)
{
    public async Task<MeetingRevocationOutcome> ExecuteAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingOAuthExchangePermit permit, int expectedBindingVersion, bool disconnectConfirmed, bool googleProjectGrantConfirmed, CancellationToken token = default)
    {
        var start = await store.BeginRevocationAsync(actor, academy, registration, permit, expectedBindingVersion, disconnectConfirmed, googleProjectGrantConfirmed, token);
        return start.StatusCode == 201 && start.Material is not null ? await SendStartedAsync(actor, academy, registration, start.Material, token) : start;
    }
    internal async Task<MeetingRevocationOutcome> SendStartedAsync(ClaimsPrincipal actor, Guid academy, MeetingOAuthRegistration registration,
        MeetingRevocationMaterial material, CancellationToken token = default)
    {
        if (!await store.MaySendRevocationAsync(actor, academy, registration, material, token) || !material.TryTake()) return new(409, "RevocationSendUnavailable");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/revoke")
        { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = material.RefreshToken }) };
        request.Headers.CacheControl = new() { NoStore = true };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(10));
        var acknowledged = false;
        try
        {
            using var response = await transport.SendAsync(request, deadline.Token);
            // Conservative candidate: only a 200 with empty unencoded body is an
            // acknowledgement. No provider error text is logged or returned.
            if (response.StatusCode == HttpStatusCode.OK && response.Content.Headers.ContentEncoding.Count == 0 && response.Content.Headers.ContentLength is null or 0)
            {
                using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
                acknowledged = await stream.ReadAsync(new byte[1], deadline.Token) == 0;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException) { }
        // Cancellation/fault here leaves a durable Started marker to recover as
        // unknown. Neither coordinator nor recovery resends an uncertain request.
        return await store.FinishRevocationAsync(actor, academy, registration, material, acknowledged, token);
    }
}
