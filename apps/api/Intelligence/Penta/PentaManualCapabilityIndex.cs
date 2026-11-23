using System.Reflection;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaManualOperation(string Controller, string Action, string Method, string Route,
    string Module, string Surface, string Integration, string? DraftToolKey);

/// <summary>
/// Source-derived operation inventory for whole-product parity. INTERNAL metadata
/// only, never a permission decision or automatic tool registration. No endpoint
/// publishes this list. Framework Identity/minimal routes are outside this index.
/// The eventual runtime catalog must select reviewed descriptors after current
/// per-operation authorization; controller module metadata alone is insufficient.
/// </summary>
internal static class PentaManualCapabilityIndex
{
    internal static IReadOnlyList<PentaManualOperation> Discover()
    {
        var result = new List<PentaManualOperation>();
        foreach (var controller in typeof(StudentsController).Assembly.GetTypes()
                     .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type)).OrderBy(type => type.Name, StringComparer.Ordinal))
        {
            var prefix = controller.GetCustomAttribute<RouteAttribute>()?.Template;
            if (prefix is null) continue;
            foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                         .OrderBy(method => method.Name, StringComparer.Ordinal))
            foreach (var route in action.GetCustomAttributes<HttpMethodAttribute>())
            foreach (var verb in route.HttpMethods)
            {
                var template = route.Template ?? "";
                var path = template.StartsWith("~/", StringComparison.Ordinal) ? template[2..]
                    : template.StartsWith('/') ? template[1..] : prefix.TrimEnd('/') + (template.Length == 0 ? "" : "/" + template);
                path = path.Replace("[controller]", controller.Name[..^"Controller".Length], StringComparison.OrdinalIgnoreCase)
                    .Replace("[action]", action.GetCustomAttribute<ActionNameAttribute>()?.Name ?? action.Name, StringComparison.OrdinalIgnoreCase);
                var (integration, tool) = Binding(controller.Name, action.Name);
                result.Add(new(controller.Name, action.Name, verb, "/" + path,
                    SubscriptionPlanCatalog.ModuleForController(controller.Name), Surface(controller.Name), integration, tool));
            }
        }
        return result.AsReadOnly();
    }

    private static string Surface(string controller) => controller switch {
        "PlatformControlController" or "PlatformAcademiesController" => "platform",
        "TeacherPortalController" => "teacher",
        "PortalController" => "student-guardian",
        "AuthSessionController" => "authenticated-account",
        "CertificateVerificationController" => "public-verification",
        "WeatherForecastController" => "system-sample",
        _ when controller.StartsWith("Penta", StringComparison.Ordinal) => "ai-infrastructure",
        _ => "academy-administration" };

    private static (string Integration, string? Tool) Binding(string controller, string action) => (controller, action) switch {
        ("StudentOnboardingController", "Create") => ("protected-guided-adapter", PentaOnboardingFields.Tool),
        ("TeachersController", "Create") => ("protected-guided-adapter", PentaTeacherCreationFields.Tool),
        ("BatchesController", "Create") => ("typed-draft-only", PentaTaskCatalog.BatchCreate),
        ("EnrollmentsController", "Create") => ("protected-guided-adapter", PentaEnrollmentFields.CreateTool),
        ("EnrollmentsController", "Update") => ("protected-guided-adapter", PentaEnrollmentFields.UpdateTool),
        ("EnrollmentsController", "Transfer") => ("protected-guided-adapter", PentaEnrollmentFields.TransferTool),
        ("ClassSessionsController", "Create") => ("protected-guided-adapter", PentaTaskCatalog.ClassCreate),
        // The existing guided reschedule adapter covers a subset of Manual Update.
        ("ClassSessionsController", "Update") => ("protected-guided-subset", PentaTaskCatalog.ClassReschedule),
        ("PaymentsController", "Create") => ("protected-guided-adapter", PentaPaymentFields.Tool),
        _ when controller.StartsWith("Penta", StringComparison.Ordinal) => ("ai-protocol-inventory", null),
        _ => ("unbound-in-this-draft-catalog", null) };
}
