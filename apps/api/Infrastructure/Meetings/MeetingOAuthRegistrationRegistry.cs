using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcademyDesk.Api.Infrastructure.Meetings;

// Server configuration only, not an API/model DTO. No secrets or configurable
// provider endpoints. A registration is NOT a qualified adapter or connection.
internal sealed record MeetingOAuthRegistrationInput(
    [property: JsonIgnore] string Provider, [property: JsonIgnore] int Revision,
    [property: JsonIgnore] string ClientId, [property: JsonIgnore] string CallbackUri,
    [property: JsonIgnore] string WorkspaceReturnUri, [property: JsonIgnore] string? FlowProfileId = null,
    [property: JsonIgnore] string? DirectoryTenant = null)
{
    public override string ToString() => "[Meeting OAuth registration redacted]";
}

internal sealed class MeetingOAuthRegistration
{
    [JsonIgnore] public string Provider { get; }
    [JsonIgnore] public string ClientId { get; }
    [JsonIgnore] public Uri Callback { get; }
    [JsonIgnore] public Uri WorkspaceReturn { get; }
    [JsonIgnore] public string Fingerprint { get; }
    [JsonIgnore] public MeetingOAuthFlowProfile? Flow { get; }
    internal MeetingOAuthRegistration(MeetingOAuthRegistrationInput input)
    {
        Provider = input.Provider; ClientId = input.ClientId;
        Callback = new(input.CallbackUri); WorkspaceReturn = new(input.WorkspaceReturnUri);
        Flow = MeetingOAuthFlowProfile.Resolve(input.Provider, input.FlowProfileId, input.DirectoryTenant);
        // Older registry-only configurations stay unqualified and cannot exchange.
        // Changing the reviewed flow/tenant/endpoints/scopes invalidates old state.
        var fingerprintFields = Flow is null
            ? new object[] { "meeting-registration/0.1-unqualified", input.Provider, input.Revision, input.ClientId, input.CallbackUri, input.WorkspaceReturnUri }
            : new object[] { "meeting-registration/0.2-profile", input.Provider, input.Revision, input.ClientId, input.CallbackUri, input.WorkspaceReturnUri, Flow.FingerprintMaterial };
        Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(fingerprintFields))));
    }
    public override string ToString() => "[Meeting OAuth registration redacted]";
}

internal sealed class MeetingOAuthRegistrationRegistry
{
    private readonly Dictionary<string, MeetingOAuthRegistration> registrations = new(StringComparer.Ordinal);
    public bool IsValid { get; private init; }
    public static MeetingOAuthRegistrationRegistry Disabled => new();
    public MeetingOAuthRegistration? Resolve(string provider) => IsValid && provider is not null && registrations.TryGetValue(provider, out var found) ? found : null;
    internal static string CallbackPath(string provider) => $"/api/meetings/oauth/{provider}/callback";
    internal static bool ValidRandom(string? value) => value is { Length: 43 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    // Exact canonical origins are trusted operator configuration, NEVER Request.Host,
    // forwarding headers, prompts or an ordinary CommunicationChannel draft.
    public static MeetingOAuthRegistrationRegistry Create(string? apiOrigin, string? workspaceOrigin,
        IReadOnlyCollection<MeetingOAuthRegistrationInput>? inputs)
    {
        if (!ValidOrigin(apiOrigin) || !ValidOrigin(workspaceOrigin) || inputs is null || inputs.Count is < 1 or > 3) return Disabled;
        var registry = new MeetingOAuthRegistrationRegistry { IsValid = true };
        foreach (var input in inputs)
        {
            if (input is null || input.Provider is not ("GoogleWorkspace" or "Microsoft365" or "Zoom") || input.Revision <= 0 ||
                input.ClientId is not { Length: >= 1 and <= 512 } || !input.ClientId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.') ||
                (input.FlowProfileId == MeetingOAuthFlowProfile.MicrosoftIdentityProfile && (!Guid.TryParseExact(input.ClientId, "D", out var appId) || appId == Guid.Empty || input.ClientId != appId.ToString("D"))) ||
                input.CallbackUri != apiOrigin + CallbackPath(input.Provider) ||
                input.WorkspaceReturnUri != workspaceOrigin + "/communication-settings" ||
                ((input.FlowProfileId is not null || input.DirectoryTenant is not null) && MeetingOAuthFlowProfile.Resolve(input.Provider, input.FlowProfileId, input.DirectoryTenant) is null) ||
                !registry.registrations.TryAdd(input.Provider, new(input))) return Disabled;
        }
        return registry;
    }
    private static bool ValidOrigin(string? origin) => origin is { Length: <= 256 } && origin.All(c => c is >= '!' and <= '~') &&
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Scheme == "https" &&
        uri.HostNameType == UriHostNameType.Dns && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
        origin == uri.GetLeftPart(UriPartial.Authority);
}
