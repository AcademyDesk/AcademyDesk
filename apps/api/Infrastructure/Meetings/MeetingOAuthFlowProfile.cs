using System.Text.Json;

namespace AcademyDesk.Api.Infrastructure.Meetings;

// Reviewed protocol candidate, NOT provider/account qualification. No client may
// supply endpoints, scopes, tenant aliases, model roles or credential URLs.
internal sealed class MeetingOAuthFlowProfile
{
    internal const string GoogleMetadataProfile = "google-calendar-owned-metadata-web/2";
    internal const string GoogleMetadataScope = "https://www.googleapis.com/auth/calendar.calendars.readonly";
    internal const string MicrosoftIdentityProfile = "microsoft-online-meetings-identity-work/2";
    internal const string ZoomConfidentialProfile = "zoom-confidential-pkce/2";
    internal bool IsGoogleIdentityProfile => Id is "google-calendar-owned-web/1" or GoogleMetadataProfile;
    internal bool IsMicrosoftIdentityProfile => Id == MicrosoftIdentityProfile;
    internal bool IsMicrosoftProfile => Id is "microsoft-online-meetings-work/1" or MicrosoftIdentityProfile;
    internal bool IsZoomConfidentialProfile => Id == ZoomConfidentialProfile;
    internal bool IsZoomProfile => Id is "zoom-public-pkce/1" or ZoomConfidentialProfile;
    public string Id { get; }
    public Uri AuthorizeEndpoint { get; }
    public Uri TokenEndpoint { get; }
    public Uri AccountEndpoint { get; }
    public string? DirectoryTenant { get; }
    public bool RequiresClientSecret { get; }
    public IReadOnlyList<string> RequestedScopes { get; }
    public IReadOnlyList<string> RequiredTokenScopes { get; }
    public string FingerprintMaterial => JsonSerializer.Serialize(new object?[] { Id, AuthorizeEndpoint.AbsoluteUri, TokenEndpoint.AbsoluteUri,
        AccountEndpoint.AbsoluteUri, DirectoryTenant, RequiresClientSecret, RequestedScopes, RequiredTokenScopes, "S256", "authorization_code" });
    private MeetingOAuthFlowProfile(string id, string authorize, string token, string account, bool secret,
        string[] requested, string[] required, string? tenant = null)
    {
        Id = id; AuthorizeEndpoint = new(authorize); TokenEndpoint = new(token); AccountEndpoint = new(account);
        RequiresClientSecret = secret; RequestedScopes = Array.AsReadOnly(requested); RequiredTokenScopes = Array.AsReadOnly(required); DirectoryTenant = tenant;
    }
    public static MeetingOAuthFlowProfile? Resolve(string provider, string? id, string? tenant)
    {
        if (provider == "GoogleWorkspace" && id == "google-calendar-owned-web/1" && tenant is null)
            return new(id, "https://accounts.google.com/o/oauth2/v2/auth", "https://oauth2.googleapis.com/token", "https://openidconnect.googleapis.com/v1/userinfo", true,
                ["openid", "email", "https://www.googleapis.com/auth/calendar.events.owned"], ["https://www.googleapis.com/auth/calendar.events.owned"]);
        // Separate opt-in candidate. Never silently broaden an existing grant:
        // this profile changes registration fingerprints and needs fresh consent.
        if (provider == "GoogleWorkspace" && id == GoogleMetadataProfile && tenant is null)
            return new(id, "https://accounts.google.com/o/oauth2/v2/auth", "https://oauth2.googleapis.com/token", "https://openidconnect.googleapis.com/v1/userinfo", true,
                ["openid", "email", "https://www.googleapis.com/auth/calendar.events.owned", GoogleMetadataScope],
                ["https://www.googleapis.com/auth/calendar.events.owned", GoogleMetadataScope]);
        if (provider == "Microsoft365" && id == "microsoft-online-meetings-work/1" && Guid.TryParseExact(tenant, "D", out var directory) && directory != Guid.Empty && tenant == directory.ToString("D"))
            return new(id, $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize", $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token", "https://graph.microsoft.com/v1.0/me", true,
                ["offline_access", "https://graph.microsoft.com/User.Read", "https://graph.microsoft.com/OnlineMeetings.ReadWrite"],
                ["User.Read", "OnlineMeetings.ReadWrite"], tenant);
        if (provider == "Microsoft365" && id == MicrosoftIdentityProfile && Guid.TryParseExact(tenant, "D", out var workDirectory) && workDirectory != Guid.Empty && tenant == workDirectory.ToString("D") && tenant != "9188040d-6c67-4c5b-b112-36a304b66dad")
            return new(id, $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize", $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token", "https://graph.microsoft.com/v1.0/me", true,
                ["openid", "profile", "offline_access", "https://graph.microsoft.com/User.Read", "https://graph.microsoft.com/OnlineMeetings.ReadWrite"],
                ["User.Read", "OnlineMeetings.ReadWrite"], tenant);
        if (provider == "Zoom" && id == "zoom-public-pkce/1" && tenant is null)
            return new(id, "https://zoom.us/oauth/authorize", "https://zoom.us/oauth/token", "https://api.zoom.us/v2/users/me", false,
                ["user:read:user", "meeting:read:meeting", "meeting:write:meeting"], ["user:read:user", "meeting:read:meeting", "meeting:write:meeting"]);
        // Separate inactive backend candidate. Do not switch public-client IDs,
        // borrow a confidential secret for a public ID or inherit old consent.
        if (provider == "Zoom" && id == ZoomConfidentialProfile && tenant is null)
            return new(id, "https://zoom.us/oauth/authorize", "https://zoom.us/oauth/token", "https://api.zoom.us/v2/users/me", true,
                ["user:read:user", "meeting:read:meeting", "meeting:write:meeting"], ["user:read:user", "meeting:read:meeting", "meeting:write:meeting"]);
        return null;
    }
    public string NormalizeScope(string scope) => IsMicrosoftProfile && scope.StartsWith("https://graph.microsoft.com/", StringComparison.Ordinal)
        ? scope["https://graph.microsoft.com/".Length..] : IsGoogleIdentityProfile && scope == "https://www.googleapis.com/auth/userinfo.email" ? "email" : scope;
    public override string ToString() => "[OAuth flow profile]";
}
