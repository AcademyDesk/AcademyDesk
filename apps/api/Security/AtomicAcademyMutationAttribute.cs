namespace AcademyDesk.Api.Security;

/// <summary>
/// Opts an action into the academy SQL save/audit boundary. Identity writes
/// require explicit IncludeIdentity and matching SQL configuration. Do not
/// apply to actions that own a transaction or write files or Blob.
/// This never grants authorization; platform-owner transaction/audit protection
/// requires explicit opt-in and leaves the default bypass unchanged.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class AtomicAcademyMutationAttribute : Attribute
{
    public bool IncludeIdentity { get; set; }
    // Opt-in transaction/audit protection only; action authorization still applies.
    // Existing platform-owner bypass behavior remains unchanged on other actions.
    public bool IncludePlatformOwner { get; set; }
}
