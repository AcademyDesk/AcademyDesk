using System.Text.Json;

namespace AcademyDesk.Api.Security;

public sealed record SubscriptionPlanDefinition(string Name, int StudentLimit, int StaffLimit, IReadOnlyList<string> Modules);

public static class SubscriptionPlanCatalog
{
    public const string Core = "Core";
    public static readonly IReadOnlyDictionary<string, SubscriptionPlanDefinition> Plans =
        new Dictionary<string, SubscriptionPlanDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            // Trial mirrors Professional so a prospect can evaluate the complete product.
            ["Trial"] = new("Trial", 75, 15, [Core, "Sales", "Engagement", "Finance", "Certificates", "TeacherClassroom", "AcademicGovernance", "MultiBranch", "AccessGovernance", "FinanceControls"]),
            ["Launch"] = new("Launch", 75, 15, [Core, "TeacherClassroom"]),
            ["Growth"] = new("Growth", 250, 40, [Core, "TeacherClassroom", "Sales", "Engagement", "Finance", "Certificates"]),
            ["Professional"] = new("Professional", 750, 120, [Core, "TeacherClassroom", "Sales", "Engagement", "Finance", "Certificates", "AcademicGovernance", "MultiBranch", "AccessGovernance", "FinanceControls"]),
            ["Enterprise"] = new("Enterprise", 5000, 1000, [Core, "TeacherClassroom", "Sales", "Engagement", "Finance", "Certificates", "AcademicGovernance", "MultiBranch", "AccessGovernance", "FinanceControls", "Integrations", "WhiteLabel"])
        };

    public static SubscriptionPlanDefinition Get(string? plan) => Plans.TryGetValue(plan ?? "", out var definition) ? definition : Plans["Launch"];
    public static IReadOnlyList<string> ModulesFor(string? plan) => Get(plan).Modules;
    public static bool Allows(string? modulesJson, string module)
    {
        if (module == Core) return true;
        try { return (JsonSerializer.Deserialize<string[]>(modulesJson ?? "[]") ?? []).Contains(module, StringComparer.OrdinalIgnoreCase); }
        catch { return false; }
    }

    public static string ModuleForController(string controller) => controller switch
    {
        "LeadsController" or "SalesMarketingController" => "Sales",
        "CommunicationSettingsController" or "CommunicationPreferencesController" or "CommunicationTemplatesController" or "NotificationsController" => "Engagement",
        "CertificatesController" or "LearningResourcesController" or "HolidaysController" or "HolidayDeleteController" or "EventsController" => "Certificates",
        "FeePlansController" or "InvoicesController" or "PaymentsController" or "ExpensesController" or "FeeRemindersController" => "Finance",
        "PayrollController" or "TeacherCompensationController" => "Finance",
        "FinanceAdjustmentsController" or "FinanceGovernanceController" => "FinanceControls",
        "AcademicGovernanceController" or "AcademicPeriodsController" or "CourseModulesController" or "CourseModuleStatusController" or "BatchPromotionsController" or "AssessmentsController" or "AssignmentSubmissionsController" => "AcademicGovernance",
        "BranchesController" => "MultiBranch",
        "AccessGrantsController" or "AccessReviewsController" or "AcademyRolesController" or "ComplianceController" or "AcademyExportsController" => "AccessGovernance",
        _ => Core
    };
}
