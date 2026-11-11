using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>Purpose binding only. It grants no tenant,role,approval or domain authority.</summary>
internal sealed class PentaEnrollmentBrowserSession(IDataProtectionProvider protection)
{
    private const string Purpose = "academy.enrollment-browser-session.v1";

    internal static Guid Key(Guid academy, Guid actor, Guid request)
    {
        if (academy == Guid.Empty || actor == Guid.Empty || request == Guid.Empty)
            throw new ArgumentException("A nonempty enrollment conversation scope is required.");
        return new Guid(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { Purpose, academy, actor, request })).AsSpan(0, 16));
    }

    internal string Seal(PentaMiniSession session, string stamp)
    {
        if (!Scope(session) || string.IsNullOrEmpty(stamp)) throw new ArgumentException("A current enrollment conversation scope is required.");
        return Protector(session).Protect(stamp);
    }

    internal bool Matches(PentaMiniSession session, string stamp)
    {
        if (!Scope(session) || string.IsNullOrEmpty(stamp) || string.IsNullOrEmpty(session.ProtectedOnboardingState)) return false;
        try { return Protector(session).Unprotect(session.ProtectedOnboardingState) == stamp; }
        catch (Exception error) when (error is CryptographicException or FormatException or ArgumentException) { return false; }
    }

    private static bool Scope(PentaMiniSession session) => session.Id != Guid.Empty && session.AcademyId != Guid.Empty &&
        session.ActorUserId != Guid.Empty && session.Version == 0 && session.PendingRequestId is null;

    private IDataProtector Protector(PentaMiniSession session) => protection.CreateProtector(Purpose,
        session.Id.ToString("D"), session.AcademyId.ToString("D"), session.ActorUserId.ToString("D"),
        FormattableString.Invariant($"{session.Version}:{session.ExpiresAtUtc.Ticks}"));
}
