# Source-backed module groups

These 23 functional groups are audit taxonomy, not folders or claims of independent services. All 70 controller classes with actions and all 79 page files are assigned. Shared profile and diagnostics groups have no dedicated page.

| Module | Scope | Controllers | Routes |
| --- | --- | --- | --- |
| AUTH | Authentication and accounts | AuthSessionController; PortalAccountsController; StaffController; AcademyRolesController; AccessGrantsController | /login; /register; /portal-accounts; /staff; /access-review; /access-review/sign-off |
| PLATFORM | Platform and tenant lifecycle | PlatformAcademiesController; PlatformControlController; AcademyPlatformServicesController | /platform; /platform/control; /platform-services |
| ACADEMY | Academy configuration and branches | AcademiesController; BranchesController | /; /admin/control; /branches |
| SALES | Leads, campaigns and trials | LeadsController; SalesMarketingController | /leads; /sales-marketing; /sales-campaigns; /trial-bookings |
| STUDENT | Student intake and management | StudentsController; StudentOnboardingController; StudentImportsController | /students; /student-onboarding; /student-management; /student-profile; /student-overview |
| GUARDIAN | Guardians and delegated access | GuardiansController; StudentGuardiansController | /guardians; /guardian-profile |
| TEACHER | Teaching team and compensation | TeachersController; TeacherCompensationController | /teachers; /teacher-onboarding; /teacher-profile; /teacher-overview; /teacher-payments |
| PROFILE | Cross-person profile summaries | ProfilesController |  |
| CURRICULUM | Courses, modules and prerequisites | CoursesController; CourseModulesController; CourseModuleStatusController | /courses; /curriculum |
| BATCH | Batches, enrollment and progression | BatchesController; EnrollmentsController; BatchPromotionsController | /batches; /batch-setup; /enrollments; /batch-promotions |
| SCHEDULE | Sessions, leave and make-up | ClassSessionsController; LeaveRequestsController; MakeupClassesController; HolidaysController; HolidayDeleteController; EventsController | /schedule; /calendar; /leave; /makeup; /holidays; /events; /meeting-links |
| ATTENDANCE | Attendance | AttendanceController | /attendance |
| ACADEMIC | Academic governance and grading | AcademicGovernanceController; AcademicPeriodsController; AssessmentsController; AssessmentResultsController; GradingSchemeLifecycleController | /academic-governance; /academic-periods; /assessments; /assessment-governance |
| LEARNING | Assignments, practice and learning resources | AssignmentsController; AssignmentSubmissionsController; LessonPlansController; LearningResourcesController; MusicPiecesController; MusicProgressController; PracticeLogsController | /assignments; /submission-review; /lesson-plans; /resources; /music; /practice-logs |
| FEES | Fee arrangements and billing | FeePlansController; StudentFeeArrangementsController; InvoicesController; FeeRemindersController | /fee-plans; /student-fees; /invoices; /fee-reminders |
| FINANCE | Collections, expenses and governance | PaymentsController; ExpensesController; FinanceAdjustmentsController; FinanceGovernanceController | /payments; /expenses; /finance-adjustments; /finance-governance; /finance-policy; /finance-reconciliation; /finance; /finance-summary |
| PAYROLL | Payroll | PayrollController | /payroll |
| COMMUNICATION | Messages and consent preferences | NotificationsController; CommunicationSettingsController; CommunicationTemplatesController; CommunicationPreferencesController | /communications; /communication-settings; /message-templates; /communication-preferences |
| COMPLIANCE | Documents, consent, certificates | ComplianceController; CertificatesController; CertificateVerificationController | /compliance; /certificates |
| OPERATIONS | Queues, audits, exports, reports | AdminWorkItemsController; AdminIntelligenceController; AccessReviewsController; AuditLogsController; AcademyExportsController; DashboardController | /work-queue; /admin-intelligence; /activity; /data-operations; /reports; /dashboard |
| TEACHERPORTAL | Teacher self-service | TeacherPortalController | /teacher |
| FAMILY | Student and guardian self-service | PortalController | /portal |
| DIAGNOSTICS | Health and sample API | WeatherForecastController |  |
