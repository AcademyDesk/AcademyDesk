using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcademyDesk.Api.Infrastructure.Meetings;

// Trusted deployment/operator policy input only; never a route, prompt, model
// output, ordinary CommunicationChannel field or self-asserted approval DTO.
internal sealed record MeetingAccountPolicyInput(
    [property: JsonIgnore] Guid Academy, [property: JsonIgnore] string Provider, [property: JsonIgnore] int Revision,
    [property: JsonIgnore] string Registration, [property: JsonIgnore] string Subject, [property: JsonIgnore] string? Account,
    [property: JsonIgnore] string? Directory, [property: JsonIgnore] DateTimeOffset ValidFrom, [property: JsonIgnore] DateTimeOffset ValidUntil)
{
    public override string ToString() => "[Meeting account policy input redacted]";
}
internal sealed class MeetingAccountPolicy
{
    [JsonIgnore] internal MeetingAccountPolicyInput Input { get; }
    [JsonIgnore] internal string Fingerprint { get; }
    internal MeetingAccountPolicy(MeetingAccountPolicyInput input)
    {
        Input = input;
        Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new object?[] {
            "meeting-account-policy/1", input.Academy, input.Provider, input.Revision, input.Registration, input.Subject, input.Account, input.Directory, input.ValidFrom, input.ValidUntil }))));
    }
    public override string ToString() => "[Meeting account policy redacted]";
}
internal sealed class MeetingAccountPolicyRegistry
{
    private readonly Dictionary<(Guid Academy, string Provider), MeetingAccountPolicy> policies = new();
    internal bool IsValid { get; private init; }
    internal static MeetingAccountPolicyRegistry Disabled => new();
    // Snapshot copied into immutable values. Any ambiguity/malformed entry
    // disables the complete snapshot, not a permissive partial fallback.
    internal static MeetingAccountPolicyRegistry Create(IReadOnlyCollection<MeetingAccountPolicyInput>? inputs)
    {
        if (inputs is null || inputs.Count is < 1 or > 1024) return Disabled;
        var registry = new MeetingAccountPolicyRegistry { IsValid = true };
        var owners = new HashSet<(string Provider, string? Directory, string AccountKey)>();
        bool Id(string? value) => value is { Length: > 0 and <= 255 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~');
        foreach (var i in inputs)
        {
            if (i is null || i.Academy == Guid.Empty || i.Provider is not ("GoogleWorkspace" or "Microsoft365" or "Zoom") || i.Revision < 1 ||
                i.Registration is not { Length: 64 } || !i.Registration.All(Uri.IsHexDigit) || !Id(i.Subject) ||
                i.ValidFrom.Offset != TimeSpan.Zero || i.ValidUntil.Offset != TimeSpan.Zero || i.ValidUntil <= i.ValidFrom ||
                (i.Provider == "Zoom" ? !Id(i.Account) || i.Directory is not null : i.Account is not null) ||
                (i.Provider == "Microsoft365" ? !Guid.TryParseExact(i.Subject, "D", out var sub) || sub == Guid.Empty || i.Subject != sub.ToString("D") ||
                    !Guid.TryParseExact(i.Directory, "D", out var dir) || dir == Guid.Empty || i.Directory != dir.ToString("D") : i.Directory is not null) ||
                !owners.Add((i.Provider, i.Directory, i.Provider == "Zoom" ? i.Account! : i.Subject)) ||
                !registry.policies.TryAdd((i.Academy, i.Provider), new(i))) return Disabled;
        }
        return registry;
    }
    internal MeetingAccountPolicy? Resolve(Guid academy, MeetingOAuthRegistration registration, DateTimeOffset now) => IsValid &&
        policies.TryGetValue((academy, registration.Provider), out var p) && p.Input.Registration == registration.Fingerprint && registration.Flow is not null &&
        p.Input.Directory == registration.Flow.DirectoryTenant && p.Input.ValidFrom <= now && p.Input.ValidUntil > now ? p : null;
    public override string ToString() => "[Meeting account policy registry redacted]";
}

internal sealed record MeetingAccountPolicyOutcome(int StatusCode, string Code, bool AccountPolicyMatched = false, string? PolicyFingerprint = null)
{
    public bool ProviderVerified => false;
    public bool Connected => false;
    public bool CanExecute => false;
    public bool CanRetry => false;
}
