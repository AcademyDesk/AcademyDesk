namespace AcademyDesk.Api.Security;

/// <summary>
/// Opts an action into the academy SQL save/audit boundary. Identity writes
/// require explicit IncludeIdentity and matching SQL configuration. Do not
/// apply to actions that own a transaction or write files or Blob.
/// This does not change authorization or platform-owner bypass behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class AtomicAcademyMutationAttribute : Attribute
{
    public bool IncludeIdentity { get; set; }
}
