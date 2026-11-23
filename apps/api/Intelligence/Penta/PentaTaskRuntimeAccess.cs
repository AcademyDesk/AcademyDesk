using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaTaskAuthority(Guid ActorId, Guid AcademyId, long Version, IReadOnlySet<string> Tools);

/// <summary>Current stored host authority; never accept scope/roles from a plan.</summary>
public sealed class PentaTaskRuntimeAccess(IdentityDbContext identity, AcademyDeskDbContext db,
    UserManager<ApplicationUser> users, IConfiguration config, IHostEnvironment environment)
{
    public bool Enabled => (environment.IsDevelopment() || environment.IsEnvironment("Testing")) &&
        config.GetValue<bool>("Penta:Enabled") && config.GetValue<bool>("Penta:Tasks:Enabled");

    internal async Task<PentaTaskAuthority?> ResolveAsync(ClaimsPrincipal principal, Guid academy, CancellationToken token)
    {
        if (!Enabled || principal.Identity?.IsAuthenticated != true || academy == Guid.Empty ||
            !Guid.TryParse(users.GetUserId(principal), out var actorId)) return null;
        var stamp = principal.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        if (string.IsNullOrEmpty(stamp)) return null;
        var actor = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actorId, token);
        if (actor is not { IsActive: true, IsPlatformOwner: false } || actor.AcademyId != academy || actor.SecurityStamp != stamp) return null;
        var roles = await (from membership in identity.UserRoles.AsNoTracking()
                           join role in identity.Roles.AsNoTracking() on membership.RoleId equals role.Id
                           where membership.UserId == actorId select role.Name).ToArrayAsync(token);
        if (!roles.Any(role => role is "Owner" or "AcademyAdmin")) return null;
        var tenant = await db.Academies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == academy && x.IsActive, token);
        if (tenant is null) return null;
        var tools = PentaTaskCatalog.Keys.Where(key => SubscriptionPlanCatalog.Allows(tenant.EnabledModulesJson,
            SubscriptionPlanCatalog.ModuleForController(Controller(key)))).ToHashSet(StringComparer.Ordinal);
        // Private purpose-bound state fingerprint, not an exposed stamp or role claim.
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            actor.SecurityStamp, roles = roles.Order(StringComparer.Ordinal).ToArray(), tenant.EnabledModulesJson })));
        long version = 0; foreach (var value in bytes.Take(6)) version = (version << 8) | value;
        return new(actorId, academy, version + 1, tools);
    }

    internal static object[] Describe(IReadOnlySet<string> permitted, DateOnly today) => PentaTaskCatalog.Keys
        .Where(permitted.Contains).Select(key => {
            var definition = PentaTaskCatalog.Describe(key, new Dictionary<string, JsonElement>(), today);
            return (object)new { key, revision = 1, entity = definition.Entity, description = Meaning(key), binding = definition.Binding,
                execution_enabled = false,
                fields = definition.Fields.Select(field => new {
                    key = field.Key, kind = field.Kind, required = field.Required, max_length = field.MaximumLength,
                    reference_entity = field.ReferenceEntity, values = field.Values, minimum = field.Minimum, maximum = field.Maximum }),
                outputs = definition.Outputs };
        }).ToArray();

    private static string Meaning(string key) => key switch {
        PentaOnboardingFields.Tool => "Create a new student record. Guardian details can be collected within this intake. Does not create a batch or enrollment.",
        PentaTeacherCreationFields.Tool => "Create a NEW teacher record only when requested. Selecting an existing teacher for a batch is a reference, not teacher creation.",
        PentaTaskCatalog.BatchCreate => "Create a NEW teaching batch or group. Course, teacher and branch are references. Does not enroll students or create dated class sessions.",
        PentaEnrollmentFields.CreateTool => "Admit or assign a student to a batch by creating an enrollment. Can depend on newly created student/batch records.",
        PentaEnrollmentFields.UpdateTool => "Change the status of an EXISTING enrollment, such as pause, activate, complete or withdraw. Not needed for initial admission.",
        PentaEnrollmentFields.TransferTool => "Transfer an EXISTING enrollment to a different batch.",
        PentaTaskCatalog.ClassCreate => "Schedule a NEW dated class session in an existing batch. Referencing the batch or teacher does not create either record.",
        PentaTaskCatalog.ClassReschedule => "Change date or time of an EXISTING class session.",
        PentaPaymentFields.Tool => "Record money already received against an existing invoice. Does not issue an invoice or change fee policy.",
        _ => throw PentaTaskCatalog.Invalid() };

    private static string Controller(string key) => key switch {
        PentaOnboardingFields.Tool => "StudentOnboardingController",
        PentaTeacherCreationFields.Tool => "TeachersController",
        PentaTaskCatalog.BatchCreate => "BatchesController",
        PentaEnrollmentFields.CreateTool or PentaEnrollmentFields.UpdateTool or PentaEnrollmentFields.TransferTool => "EnrollmentsController",
        PentaTaskCatalog.ClassCreate or PentaTaskCatalog.ClassReschedule => "ClassSessionsController",
        PentaPaymentFields.Tool => "PaymentsController",
        _ => throw PentaTaskCatalog.Invalid() };
}
