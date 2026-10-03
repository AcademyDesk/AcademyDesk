namespace AcademyDesk.Api.Security;

public static class PermissionCatalog
{
    public static readonly string[] All =
    [
        "sales.manage", "students.onboard", "students.manage", "student-fees.manage",
        "batches.manage", "scheduling.manage", "attendance.manage", "makeup.manage",
        "finance.manage", "academics.manage", "workforce.manage", "reports.export",
        "communications.manage", "settings.manage"
    ];

    private static readonly IReadOnlyDictionary<string, string[]> ControllerPermissions = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["LeadsController"] = ["sales.manage"], ["SalesMarketingController"] = ["sales.manage"],
        ["StudentOnboardingController"] = ["students.onboard"],
        ["StudentsController"] = ["students.manage"], ["StudentGuardiansController"] = ["students.manage"],
        ["GuardiansController"] = ["students.manage"], ["EnrollmentsController"] = ["students.manage"],
        ["StudentFeeArrangementsController"] = ["student-fees.manage"],
        ["BatchesController"] = ["batches.manage"], ["ClassSessionsController"] = ["scheduling.manage"],
        ["AttendanceController"] = ["attendance.manage"], ["MakeupClassesController"] = ["makeup.manage"],
        ["FeePlansController"] = ["finance.manage"], ["InvoicesController"] = ["finance.manage"],
        ["PaymentsController"] = ["finance.manage"], ["ExpensesController"] = ["finance.manage"],
        ["FinanceAdjustmentsController"] = ["finance.manage"], ["FinanceGovernanceController"] = ["finance.manage"],
        ["FeeRemindersController"] = ["finance.manage"], ["AcademyExportsController"] = ["reports.export"],
        ["CoursesController"] = ["academics.manage"], ["CourseModulesController"] = ["academics.manage"],
        ["AcademicGovernanceController"] = ["academics.manage"], ["AcademicPeriodsController"] = ["academics.manage"],
        ["AssessmentsController"] = ["academics.manage"], ["AssessmentResultsController"] = ["academics.manage"], ["BatchPromotionsController"] = ["academics.manage"],
        ["StaffController"] = ["workforce.manage"], ["PortalAccountsController"] = ["workforce.manage"],
        ["CommunicationPreferencesController"] = ["communications.manage"],
        ["AcademyRolesController"] = ["workforce.manage"], ["AccessGrantsController"] = ["workforce.manage"],
    };

    public static IReadOnlyList<string>? RequiredFor(string controllerName) => ControllerPermissions.GetValueOrDefault(controllerName);

    public static IEnumerable<string> ForSystemRole(string role) => role switch
    {
        "Sales" or "Marketing" => ["sales.manage", "students.onboard"],
        "Operations" => ["batches.manage", "scheduling.manage", "attendance.manage", "makeup.manage"],
        "Manager" => ["batches.manage", "scheduling.manage", "attendance.manage", "makeup.manage", "communications.manage"],
        "FrontDesk" => ["sales.manage", "students.onboard", "students.manage", "student-fees.manage"],
        "FinanceUser" => ["finance.manage", "student-fees.manage", "reports.export"],
        _ => []
    };
}
