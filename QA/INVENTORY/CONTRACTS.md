# Request / response record contracts

Nullable annotations, defaults and DTO types from source. Default ASP.NET JSON uses camelCase; verify real serialized responses separately.

| Source | Contract | Fields |
| --- | --- | --- |
| apps/api/Controllers/AcademicGovernanceController.cs:4 | GradingSchemeRequest | string Name,decimal PassingPercent,string? BandsJson |
| apps/api/Controllers/AcademicGovernanceController.cs:4 | PrerequisiteRequest | Guid CourseId,Guid RequiredCourseId |
| apps/api/Controllers/AcademicPeriodsController.cs:56 | AcademicYearRequest | string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent |
| apps/api/Controllers/AcademicPeriodsController.cs:57 | AcademicTermRequest | Guid AcademicYearId, string Name, DateOnly StartDate, DateOnly EndDate |
| apps/api/Controllers/AcademicPeriodsController.cs:58 | AcademicYearSummary | Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent, bool IsClosed |
| apps/api/Controllers/AcademicPeriodsController.cs:59 | AcademicTermSummary | Guid Id, Guid AcademicYearId, string Name, DateOnly StartDate, DateOnly EndDate, bool IsClosed |
| apps/api/Controllers/AcademicPeriodsController.cs:60 | AcademicPeriodsSummary | IReadOnlyList<AcademicYearSummary> Years, IReadOnlyList<AcademicTermSummary> Terms |
| apps/api/Controllers/AcademiesController.cs:91 | CreateAcademyRequest | string Name, string? LegalName, string? CountryCode, string? TimeZone |
| apps/api/Controllers/AcademiesController.cs:96 | UpdateAcademyRequest | string Name,string? LegalName,string? CountryCode,string? TimeZone |
| apps/api/Controllers/AcademiesController.cs:97 | SetAcademyActiveRequest | bool IsActive |
| apps/api/Controllers/AcademiesController.cs:99 | AcademySummary | Guid Id, string Name, string? LegalName, string CountryCode, string TimeZone, bool IsActive, string SubscriptionPlan, string SubscriptionStatus, string EnabledModulesJson |
| apps/api/Controllers/AcademyPlatformServicesController.cs:67 | SubmitPlatformPaymentRequest | string? Reference |
| apps/api/Controllers/AcademyPlatformServicesController.cs:68 | CreateAcademySupportCaseRequest | string Subject, string? Priority, string Description |
| apps/api/Controllers/AcademyPlatformServicesController.cs:69 | RespondToSupportCaseRequest | string Message |
| apps/api/Controllers/AcademyRolesController.cs:57 | CreateAcademyRoleRequest | string Name, IReadOnlyList<string>? Permissions |
| apps/api/Controllers/AcademyRolesController.cs:58 | RoleAssignmentRequest | Guid RoleId |
| apps/api/Controllers/AccessGrantsController.cs:70 | CreateAccessGrantRequest | Guid UserId, IReadOnlyList<string>? Permissions, bool IsPermanent, DateTimeOffset? ExpiresAtUtc, string? Reason |
| apps/api/Controllers/AccessReviewsController.cs:4 | AccessReviewRequest | string? Notes,bool CreateFollowUp |
| apps/api/Controllers/AdminWorkItemsController.cs:9 | CreateWorkItem | string Type,string Title,string? Description,string? Priority,string? EntityType,Guid? EntityId,Guid? AssignedUserId,DateTime? DueAtUtc |
| apps/api/Controllers/AdminWorkItemsController.cs:9 | WorkStatus | string Status |
| apps/api/Controllers/AdminWorkItemsController.cs:10 | CollectionStateRequest | string EscalationStage,DateOnly? PromisedPaymentDate |
| apps/api/Controllers/AssessmentsController.cs:54 | CreateAssessmentRequest | Guid BatchId, string Title, string? Type, decimal MaxScore, Guid? GradingSchemeId, DateTime? ScheduledAtUtc, bool IsPublished |
| apps/api/Controllers/AssessmentsController.cs:55 | AssessmentSummary | Guid Id, Guid BatchId, string Title, string Type, decimal MaxScore, DateTime? ScheduledAtUtc, bool IsPublished |
| apps/api/Controllers/AssessmentsController.cs:56 | PublishAssessmentRequest | bool IsPublished |
| apps/api/Controllers/AssessmentsController.cs:57 | RecordAssessmentResultRequest | Guid StudentId, decimal Score, string? Grade, string? Remarks, bool IsPublished |
| apps/api/Controllers/AssessmentsController.cs:58 | AssessmentResultSummary | Guid Id, Guid StudentId, decimal Score, string? Grade, string? Remarks, bool IsPublished |
| apps/api/Controllers/AssignmentsController.cs:35 | CreateAssignmentRequest | Guid BatchId, string Title, string? Description, DateTime? DueAtUtc, string? Type, bool IsPublished |
| apps/api/Controllers/AssignmentsController.cs:36 | AssignmentSummary | Guid Id, Guid BatchId, string Title, string? Description, DateTime? DueAtUtc, string Type, bool IsPublished |
| apps/api/Controllers/AssignmentsController.cs:37 | PublishAssignmentRequest | bool IsPublished |
| apps/api/Controllers/AssignmentSubmissionsController.cs:5 | ReviewRequest | string? Feedback |
| apps/api/Controllers/AttendanceController.cs:38 | MarkAttendanceRequest | Guid StudentId, string Status, string? Notes |
| apps/api/Controllers/AttendanceController.cs:39 | AttendanceSummary | Guid Id, Guid StudentId, string Status, DateTime MarkedAtUtc, string? Notes |
| apps/api/Controllers/AuditLogsController.cs:26 | AuditLogSummary | Guid Id, Guid? ActorUserId, string Action, string EntityType, Guid? EntityId, string? MetadataJson, string? IpAddress, DateTime OccurredAtUtc |
| apps/api/Controllers/AuthSessionController.cs:100 | SessionSummary | string DisplayName, string? Email, string? PhoneNumber, IReadOnlyList<string> Roles, Guid? AcademyId, bool IsPlatformOwner, string Workspace, string? ProfileImageUrl |
| apps/api/Controllers/AuthSessionController.cs:101 | UpdateSessionProfileRequest | string? DisplayName, string? PhoneNumber |
| apps/api/Controllers/AuthSessionController.cs:102 | ChangeSessionPasswordRequest | string? CurrentPassword, string? NewPassword |
| apps/api/Controllers/AuthSessionController.cs:103 | ProfileImageSummary | string ProfileImageUrl |
| apps/api/Controllers/BatchesController.cs:78 | CreateBatchRequest | string Name, string? BatchCode, Guid CourseId, Guid? TeacherId, Guid? BranchId, int Capacity, int WaitlistCapacity, string? DeliveryMode, string? MeetingPattern, string? RoomName, string? EnrollmentStatus, string? AdminNotes, DateOnly? StartDate, DateOnly? EndDate, string? ClassType = null, int? SessionMinutes = null, int? SessionsPerWeek = null, string? MeetingDaysJson = null, string? MeetingLink = null) : BatchRequest(Name, BatchCode, CourseId, TeacherId, BranchId, Capacity, WaitlistCapacity, DeliveryMode, MeetingPattern, RoomName, EnrollmentStatus, AdminNotes, StartDate, EndDate, ClassType, SessionMinutes, SessionsPerWeek, MeetingDaysJson, MeetingLink |
| apps/api/Controllers/BatchesController.cs:79 | UpdateBatchRequest | string Name, string? BatchCode, Guid CourseId, Guid? TeacherId, Guid? BranchId, int Capacity, int WaitlistCapacity, string? DeliveryMode, string? MeetingPattern, string? RoomName, string? EnrollmentStatus, string? AdminNotes, DateOnly? StartDate, DateOnly? EndDate, bool IsActive) : BatchRequest(Name, BatchCode, CourseId, TeacherId, BranchId, Capacity, WaitlistCapacity, DeliveryMode, MeetingPattern, RoomName, EnrollmentStatus, AdminNotes, StartDate, EndDate |
| apps/api/Controllers/BatchesController.cs:80 | BatchMeetingTime | string Day, string StartTime |
| apps/api/Controllers/BatchesController.cs:81 | BatchSummary | Guid Id, string Name, string? BatchCode, Guid CourseId, Guid? TeacherId, Guid? BranchId, int Capacity, int WaitlistCapacity, string DeliveryMode, string? MeetingPattern, string? MeetingLink, string? RoomName, string EnrollmentStatus, string? AdminNotes, DateOnly? StartDate, DateOnly? EndDate, bool IsActive, int ActiveEnrolments |
| apps/api/Controllers/BatchPromotionsController.cs:4 | PromotionRequest | Guid StudentId,Guid SourceBatchId,Guid TargetBatchId,DateOnly EffectiveDate,string? Notes |
| apps/api/Controllers/BatchPromotionsController.cs:4 | PromotionDecision | string Status,string? Notes |
| apps/api/Controllers/BranchesController.cs:76 | CreateBranchRequest | string Name, string? AddressLine1, string? City, string? State, string? PostalCode |
| apps/api/Controllers/BranchesController.cs:83 | BranchSummary | Guid Id, string Name, string? City, string? State, string? PostalCode, bool IsActive |
| apps/api/Controllers/BranchesController.cs:90 | UpdateBranchRequest | string Name,string? AddressLine1,string? City,string? State,string? PostalCode,bool IsActive |
| apps/api/Controllers/CertificatesController.cs:113 | IssueCertificateRequest | Guid StudentId, Guid? BatchId, string Title, string? TemplateKey, string? DesignKey, int? ArtworkX, int? ArtworkY, int? ArtworkSize, DateOnly? IssuedDate, string? Notes |
| apps/api/Controllers/CertificatesController.cs:114 | CertificateSummary | Guid Id, string CertificateNumber, Guid StudentId, Guid? BatchId, string Title, string TemplateKey, string DesignKey, int ArtworkX, int ArtworkY, int ArtworkSize, string VerificationCode, DateOnly IssuedDate, string Status, string? Notes |
| apps/api/Controllers/CertificatesController.cs:115 | CertificateBranding | string AcademyName, string? LogoUrl, string AccentColor, string? SignatoryName |
| apps/api/Controllers/CertificatesController.cs:116 | UpdateCertificateBrandingRequest | string? AccentColor, string? SignatoryName |
| apps/api/Controllers/CertificatesController.cs:117 | UpdateCertificateStatusRequest | string Status |
| apps/api/Controllers/CertificateVerificationController.cs:24 | VerifiedCertificate | string AcademyName, string CertificateNumber, string Title, DateOnly IssuedDate, string Status |
| apps/api/Controllers/ClassSessionsController.cs:54 | CreateClassSessionRequest | Guid BatchId, Guid? TeacherId, Guid? BranchId, DateTime StartUtc, DateTime EndUtc, string? DeliveryMode, string? RoomName |
| apps/api/Controllers/ClassSessionsController.cs:55 | ClassSessionSummary | Guid Id, Guid BatchId, Guid? TeacherId, Guid? BranchId, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? RoomName, string Status |
| apps/api/Controllers/ClassSessionsController.cs:56 | UpdateClassSessionRequest | DateTime StartUtc, DateTime EndUtc, string? DeliveryMode, string? RoomName, string Status |
| apps/api/Controllers/CommunicationPreferencesController.cs:47 | SaveCommunicationPreferenceRequest | bool EmailAllowed, bool WhatsAppAllowed, bool MarketingAllowed, string? Notes |
| apps/api/Controllers/CommunicationPreferencesController.cs:48 | CommunicationPreferenceSummary | Guid Id, Guid RecipientId, string RecipientType, bool EmailAllowed, bool WhatsAppAllowed, bool MarketingAllowed, DateTime? EmailOptedInAtUtc, DateTime? WhatsAppOptedInAtUtc, DateTime? OptedOutAtUtc, string? Notes |
| apps/api/Controllers/CommunicationSettingsController.cs:94 | SaveCommunicationChannelRequest | string Provider, string Status, string? SenderName, string? SenderAddress, string? ReplyToAddress, string? PhoneNumber, string? ExternalAccountReference, bool MessagesEnabled |
| apps/api/Controllers/CommunicationSettingsController.cs:95 | CommunicationChannelSummary | Guid Id, string Channel, string Provider, string Status, string? SenderName, string? SenderAddress, string? ReplyToAddress, string? PhoneNumber, string? ExternalAccountReference, bool MessagesEnabled, bool HasSecureConnection |
| apps/api/Controllers/CommunicationTemplatesController.cs:137 | SaveCommunicationTemplateRequest | string Channel, string Name, string TemplateKey, string Category, string? TemplateGroup, string Status, string? Language, string? ProviderTemplateName, string? Subject, string Body, bool IsActive |
| apps/api/Controllers/CommunicationTemplatesController.cs:138 | CommunicationTemplateSummary | Guid Id, string Channel, string Name, string TemplateKey, string Category, string TemplateGroup, string Status, string Language, string? ProviderTemplateName, string? Subject, string Body, bool IsActive |
| apps/api/Controllers/CommunicationTemplatesController.cs:139 | StarterTemplateResult | int AddedCount, string Message |
| apps/api/Controllers/CommunicationTemplatesController.cs:140 | AddStarterTemplatesRequest | IReadOnlyList<string> TemplateIds |
| apps/api/Controllers/CommunicationTemplatesController.cs:141 | StarterTemplateDefinition | string Id, string Channel, string TemplateGroup, string Name, string TemplateKey, string Category, string? Subject, string Body |
| apps/api/Controllers/ComplianceController.cs:91 | DocumentRequest | Guid? StudentId, Guid? GuardianId, string DocumentType, string FileName, string? SecureReference, DateOnly? ExpiryDate, string? Visibility |
| apps/api/Controllers/ComplianceController.cs:92 | DocumentReviewRequest | string Status, DateOnly? ReviewedDate |
| apps/api/Controllers/ComplianceController.cs:93 | ConsentRequest | Guid? StudentId, Guid? GuardianId, string ConsentType, bool Granted, string? EvidenceReference |
| apps/api/Controllers/CourseModulesController.cs:3 | CreateModule | Guid CourseId,string Title,string? Description,int Sequence |
| apps/api/Controllers/CourseModulesController.cs:4 | PublicationRequest | bool IsPublished |
| apps/api/Controllers/CourseModuleStatusController.cs:22 | PublishModuleRequest | bool IsPublished |
| apps/api/Controllers/CoursesController.cs:51 | CreateCourseRequest | string Name, string? CourseCode, string? AcademyType, string? SubjectArea, string? Level, string? Description, int? DurationMonths, int? WeeklySessions, int? SessionMinutes, int? MinimumAge, int? MaximumAge, string? DeliveryMode, string? Prerequisites, string? LearningOutcomes, bool IsPublished) : CourseRequest(Name, CourseCode, AcademyType, SubjectArea, Level, Description, DurationMonths, WeeklySessions, SessionMinutes, MinimumAge, MaximumAge, DeliveryMode, Prerequisites, LearningOutcomes, IsPublished |
| apps/api/Controllers/CoursesController.cs:52 | UpdateCourseRequest | string Name, string? CourseCode, string? AcademyType, string? SubjectArea, string? Level, string? Description, int? DurationMonths, int? WeeklySessions, int? SessionMinutes, int? MinimumAge, int? MaximumAge, string? DeliveryMode, string? Prerequisites, string? LearningOutcomes, bool IsPublished, bool IsActive) : CourseRequest(Name, CourseCode, AcademyType, SubjectArea, Level, Description, DurationMonths, WeeklySessions, SessionMinutes, MinimumAge, MaximumAge, DeliveryMode, Prerequisites, LearningOutcomes, IsPublished |
| apps/api/Controllers/CoursesController.cs:53 | CourseSummary | Guid Id, string Name, string? CourseCode, string AcademyType, string? SubjectArea, string? Level, string? Description, int? DurationMonths, int? WeeklySessions, int? SessionMinutes, int? MinimumAge, int? MaximumAge, string? DeliveryMode, string? Prerequisites, string? LearningOutcomes, bool IsPublished, bool IsActive |
| apps/api/Controllers/DashboardController.cs:71 | DashboardSummary | int ActiveStudents, int ActiveTeachers, int ActiveCourses, int ActiveBatches, int OpenLeads, int AttendanceRecordsLast30Days, int PresentAttendanceLast30Days, decimal TotalInvoiced, decimal TotalPaid, decimal OutstandingBalance, int ClassesToday, IReadOnlyList<DashboardScheduleItem> TodaySchedule, IReadOnlyList<DashboardActivityItem> RecentActivity |
| apps/api/Controllers/DashboardController.cs:72 | DashboardScheduleItem | Guid Id, string BatchName, string? TeacherName, DateTime StartUtc, DateTime EndUtc, string Status, string? RoomName, string DeliveryMode |
| apps/api/Controllers/DashboardController.cs:73 | DashboardActivityItem | Guid Id, string Action, string EntityType, DateTime OccurredAtUtc |
| apps/api/Controllers/EnrollmentsController.cs:70 | CreateEnrollmentRequest | Guid StudentId, Guid BatchId, DateOnly? StartDate, string? Status |
| apps/api/Controllers/EnrollmentsController.cs:71 | EnrollmentSummary | Guid Id, Guid StudentId, Guid BatchId, DateOnly StartDate, DateOnly? EndDate, string Status |
| apps/api/Controllers/EnrollmentsController.cs:72 | UpdateEnrollmentRequest | string Status, DateOnly? EndDate, string? LifecycleReason |
| apps/api/Controllers/EnrollmentsController.cs:73 | TransferEnrollmentRequest | Guid TargetBatchId, DateOnly? TransferDate |
| apps/api/Controllers/EventsController.cs:26 | CreateEventRequest | string Title, string? Type, Guid? BranchId, DateTime StartUtc, DateTime EndUtc, string? Venue, int? Capacity, string? Notes |
| apps/api/Controllers/EventsController.cs:27 | EventSummary | Guid Id, string Title, string Type, Guid? BranchId, DateTime StartUtc, DateTime EndUtc, string? Venue, int? Capacity, string Status, string? Notes |
| apps/api/Controllers/EventsController.cs:28 | UpdateEventStatusRequest | string Status |
| apps/api/Controllers/ExpensesController.cs:30 | CreateExpenseRequest | string Description, decimal Amount, string? Currency, string? Category, Guid? BranchId, DateOnly? ExpenseDate |
| apps/api/Controllers/ExpensesController.cs:31 | ExpenseSummary | Guid Id, string Description, decimal Amount, string Currency, string Category, Guid? BranchId, DateOnly ExpenseDate, string Status |
| apps/api/Controllers/ExpensesController.cs:32 | UpdateExpenseRequest | string Description, decimal Amount, string? Category, Guid? BranchId, DateOnly ExpenseDate, string? Status |
| apps/api/Controllers/FeePlansController.cs:29 | CreateFeePlanRequest | string Name, decimal Amount, string? Currency, string? Frequency |
| apps/api/Controllers/FeePlansController.cs:30 | FeePlanSummary | Guid Id, string Name, decimal Amount, string Currency, string Frequency, bool IsActive |
| apps/api/Controllers/FeePlansController.cs:31 | UpdateFeePlanRequest | string Name, decimal Amount, string? Frequency, bool IsActive |
| apps/api/Controllers/FeeRemindersController.cs:35 | QueueFeeReminderRequest | Guid? InvoiceId, string? Channel |
| apps/api/Controllers/FeeRemindersController.cs:36 | FeeReminderResult | int QueuedCount, string Message |
| apps/api/Controllers/FinanceAdjustmentsController.cs:48 | CreateFinanceAdjustmentRequest | Guid InvoiceId, string? Type, decimal Amount, string? Reason |
| apps/api/Controllers/FinanceAdjustmentsController.cs:49 | FinanceAdjustmentDecisionRequest | bool Approve, string? Notes |
| apps/api/Controllers/FinanceAdjustmentsController.cs:50 | FinanceAdjustmentSummary | Guid Id, Guid InvoiceId, string Type, decimal Amount, string Currency, string Reason, string Status, DateTime? ApprovedAtUtc, string? ApprovalNotes |
| apps/api/Controllers/FinanceGovernanceController.cs:17 | FinanceSettingsRequest | string? TaxRegistrationNumber,string? TaxLabel,decimal TaxRatePercent,int DefaultPaymentTermsDays,bool TaxInclusivePricing,string? InvoiceLogoUrl,string? InvoiceAuthorityName,string? InvoiceAuthorityTitle,string? InvoiceSignatureUrl,string? InvoiceTemplateKey,string? PayslipTemplateKey |
| apps/api/Controllers/FinanceGovernanceController.cs:17 | FinanceSettingsSummary | string TaxRegistrationNumber,string TaxLabel,decimal TaxRatePercent,int DefaultPaymentTermsDays,bool TaxInclusivePricing,string? InvoiceLogoUrl,string? InvoiceAuthorityName,string? InvoiceAuthorityTitle,string? InvoiceSignatureUrl,string InvoiceTemplateKey,string PayslipTemplateKey |
| apps/api/Controllers/FinanceGovernanceController.cs:17 | CollectionFollowUpRequest | string? Note,string? Priority,Guid? AssignedUserId,DateTime? DueAtUtc |
| apps/api/Controllers/GradingSchemeLifecycleController.cs:5 | SchemeStatusRequest | bool IsActive |
| apps/api/Controllers/GuardiansController.cs:70 | CreateGuardianRequest | string FirstName, string LastName, string? Email, string? Phone |
| apps/api/Controllers/GuardiansController.cs:71 | GuardianSummary | Guid Id, string FirstName, string LastName, string? Email, string? Phone, bool IsActive |
| apps/api/Controllers/GuardiansController.cs:72 | UpdateGuardianRequest | string FirstName,string LastName,string? Email,string? Phone,bool IsActive |
| apps/api/Controllers/InvoicesController.cs:34 | CreateInvoiceRequest | Guid StudentId, Guid? FeePlanId, decimal? Amount, DateOnly? DueDate |
| apps/api/Controllers/InvoicesController.cs:35 | InvoiceSummary | Guid Id, string InvoiceNumber, Guid StudentId, Guid? FeePlanId, decimal TotalAmount, decimal AdjustedAmount, decimal PaidAmount, string Currency, DateOnly IssuedDate, DateOnly DueDate, string Status){public decimal Balance => Math.Max(0, TotalAmount-AdjustedAmount-PaidAmount |
| apps/api/Controllers/InvoicesController.cs:36 | UpdateInvoiceStatusRequest | string Status |
| apps/api/Controllers/LeadsController.cs:72 | CreateLeadRequest | string FullName, string? Email, string? Phone, DateOnly? DateOfBirth, string? ParentName, string? ProgramInterest, string? Source, DateTime? FollowUpAtUtc, string? Notes |
| apps/api/Controllers/LeadsController.cs:73 | UpdateLeadStageRequest | string Stage, DateTime? FollowUpAtUtc |
| apps/api/Controllers/LeadsController.cs:74 | UpdateLeadNotesRequest | string? Notes |
| apps/api/Controllers/LeadsController.cs:75 | ConvertLeadRequest | string? FirstName, string? LastName |
| apps/api/Controllers/LeadsController.cs:76 | LeadSummary | Guid Id, string FullName, string? Email, string? Phone, DateOnly? DateOfBirth, string? ParentName, string? ProgramInterest, string Source, string Stage, Guid? BranchId, Guid? AssignedTeacherId, DateTime? FollowUpAtUtc, string? Notes, Guid? ConvertedStudentId |
| apps/api/Controllers/LeadsController.cs:77 | LeadConversionSummary | Guid LeadId, Guid StudentId, string FirstName, string LastName |
| apps/api/Controllers/LearningResourcesController.cs:12 | CreateResourceRequest | string Title,string? Description,string? Type,string Url,Guid? BatchId,Guid? CourseId,bool IsPublished |
| apps/api/Controllers/LearningResourcesController.cs:12 | ResourceSummary | Guid Id,string Title,string? Description,string Type,string Url,Guid? BatchId,Guid? CourseId,bool IsPublished |
| apps/api/Controllers/LearningResourcesController.cs:12 | PublishResourceRequest | bool IsPublished |
| apps/api/Controllers/LeaveRequestsController.cs:14 | CreateLeaveRequest | string RequesterType,Guid? StudentId,Guid? TeacherId,DateOnly StartDate,DateOnly EndDate,string Reason |
| apps/api/Controllers/LeaveRequestsController.cs:15 | DecideLeaveRequest | string Status,string? Notes |
| apps/api/Controllers/LeaveRequestsController.cs:16 | LeaveSummary | Guid Id,string RequesterType,Guid? StudentId,Guid? TeacherId,DateOnly StartDate,DateOnly EndDate,string Reason,string Status,string? DecisionNotes |
| apps/api/Controllers/LessonPlansController.cs:3 | CreateLessonPlan | Guid BatchId,Guid? CourseModuleId,Guid? ClassSessionId,string Title,string? Objectives |
| apps/api/Controllers/LessonPlansController.cs:4 | UpdateLessonStatus | string Status |
| apps/api/Controllers/MakeupClassesController.cs:52 | CreateMakeupRequest | Guid StudentId, Guid BatchId, Guid? TeacherId, DateTime? StartUtc, string? DeliveryMode, string? Venue, string? MeetingLink, bool UseNextScheduledClass, string? Notes |
| apps/api/Controllers/MakeupClassesController.cs:53 | MakeupSummary | Guid Id, Guid StudentId, Guid BatchId, Guid? TeacherId, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? Venue, string? MeetingLink, bool UsesNextScheduledClass, string Status, string? Notes |
| apps/api/Controllers/MakeupClassesController.cs:54 | UpdateMakeupRequest | string Status |
| apps/api/Controllers/MusicPiecesController.cs:27 | CreateMusicPieceRequest | string Title, string? Composer, string? Instrument, string? Genre, string? Difficulty, int? DurationMinutes |
| apps/api/Controllers/MusicPiecesController.cs:28 | MusicPieceSummary | Guid Id, string Title, string? Composer, string? Instrument, string? Genre, string Difficulty, int? DurationMinutes |
| apps/api/Controllers/MusicPiecesController.cs:29 | SetMusicPieceActiveRequest | bool IsActive |
| apps/api/Controllers/MusicProgressController.cs:36 | AssignMusicPieceRequest | Guid StudentId, Guid MusicPieceId, DateOnly? TargetDate, string? Notes |
| apps/api/Controllers/MusicProgressController.cs:37 | UpdateMusicProgressRequest | string Status, DateOnly? TargetDate, decimal? Score, string? Notes |
| apps/api/Controllers/MusicProgressController.cs:38 | MusicProgressSummary | Guid Id, Guid StudentId, Guid MusicPieceId, string Status, DateOnly? TargetDate, decimal? Score, string? Notes |
| apps/api/Controllers/NotificationsController.cs:71 | CreateNotificationRequest | Guid? RecipientId, string? RecipientType, string? Title, string? Message, string? Channel, DateTime? ScheduledAtUtc, Guid? TemplateId, Dictionary<string, string>? Variables, bool IsImportant = false, int? DisplayHours = null |
| apps/api/Controllers/NotificationsController.cs:72 | NotificationSummary | Guid Id, Guid? RecipientId, string RecipientType, string Title, string Message, string Channel, string Status, Guid? TemplateId, string? VariablesJson, string? FailureReason, DateTime? ScheduledAtUtc, DateTime? SentAtUtc |
| apps/api/Controllers/NotificationsController.cs:73 | UpdateNotificationStatusRequest | string Status |
| apps/api/Controllers/PaymentsController.cs:41 | RecordPaymentRequest | Guid InvoiceId, decimal Amount, string? Method, string? Reference |
| apps/api/Controllers/PaymentsController.cs:42 | PaymentSummary | Guid Id, Guid InvoiceId, decimal Amount, string Currency, string Method, string Status, string? Reference, DateTime PaidAtUtc, string? ReconciliationReference, DateTime? ReconciledAtUtc |
| apps/api/Controllers/PaymentsController.cs:43 | UpdatePaymentStatusRequest | string Status |
| apps/api/Controllers/PaymentsController.cs:44 | ReconcilePaymentRequest | string Reference |
| apps/api/Controllers/PayrollController.cs:59 | SavePayrollProfileRequest | string WorkerType, Guid? TeacherId, string? StaffUserId, string WorkerName, string PaymentModel, decimal? MonthlyAmount, decimal? AmountPerCycle, int? SessionsPerCycle, DateOnly? EffectiveFrom, bool IsActive = true |
| apps/api/Controllers/PayrollController.cs:60 | CreatePayrollPayoutRequest | Guid PayrollProfileId, string PeriodLabel, int? SessionsCovered, decimal? GrossAmount, decimal Deductions, string? PaymentMethod, string? Reference, DateTime? PaidAtUtc |
| apps/api/Controllers/PayrollController.cs:61 | PayrollProfileSummary | Guid Id, string WorkerType, Guid? TeacherId, string? StaffUserId, string WorkerName, string PaymentModel, decimal? MonthlyAmount, decimal? AmountPerCycle, int? SessionsPerCycle, DateOnly EffectiveFrom, bool IsActive |
| apps/api/Controllers/PayrollController.cs:62 | PayrollPayoutSummary | Guid Id, Guid PayrollProfileId, string WorkerName, string WorkerType, string PayslipNumber, string PeriodLabel, int? SessionsCovered, decimal GrossAmount, decimal Deductions, decimal NetAmount, string Currency, string Status, string PaymentMethod, string? Reference, DateTime PaidAtUtc |
| apps/api/Controllers/PlatformAcademiesController.cs:82 | OnboardAcademyRequest | string AcademyName, string? LegalName, string? CountryCode, string? TimeZone, string AdminUserName, string? AdminDisplayName, string Password |
| apps/api/Controllers/PlatformAcademiesController.cs:83 | SetPlatformAcademyStatusRequest | bool IsActive |
| apps/api/Controllers/PlatformAcademiesController.cs:84 | TenantConfigurationRequest | string SubscriptionPlan, string SubscriptionStatus, DateTime? SubscriptionEndsAtUtc |
| apps/api/Controllers/PlatformControlController.cs:269 | UpdatePlatformSettingsRequest | string PlatformName, string? SupportEmail, string? DefaultCurrency, int DefaultTrialDays, int DataRetentionDays, bool MaintenanceMode, string? StatusMessage |
| apps/api/Controllers/PlatformControlController.cs:270 | DeleteActivityLogsRequest | DateTime FromUtc, DateTime ToUtc, string? Scope |
| apps/api/Controllers/PlatformControlController.cs:271 | CreatePlatformAnnouncementRequest | Guid AcademyId, string Title, string Message, int DisplayHours |
| apps/api/Controllers/PlatformControlController.cs:272 | TenantOnboardingRequest | string? Status, string? CurrentSection, string? PrimaryContactName, string? PrimaryContactRole, string? PrimaryContactEmail, string? PrimaryContactPhone, string? Country, string? State, string? City, string? PostalCode, string? AddressLine1, string? AddressLine2, string? BusinessType, string? OperatingSince, string? Website, string? BranchSummary, string? FinanceModel, string? BillingFrequency, string? PaymentCollectionMethods, string? TeacherPaymentModels, int? TeacherCount, int? StudentCount, int? SubjectCount, string? SubjectTypes, string? DeliveryModes, string? ClassRatios, string? BatchAndClassSetup, string? OperationalNotes, string? DocumentsJson |
| apps/api/Controllers/PlatformControlController.cs:273 | SetPlatformAdminActiveRequest | bool IsActive |
| apps/api/Controllers/PlatformControlController.cs:274 | ResetPlatformAdminPasswordRequest | string NewPassword |
| apps/api/Controllers/PlatformControlController.cs:275 | CreatePlatformSupportCaseRequest | Guid AcademyId, string Subject, string? Priority, string? Description |
| apps/api/Controllers/PlatformControlController.cs:276 | UpdatePlatformSupportCaseRequest | string Status, string Priority |
| apps/api/Controllers/PlatformControlController.cs:277 | CreatePlatformBillingInvoiceRequest | Guid AcademyId, string InvoiceNumber, decimal Amount, string? Currency, DateOnly PeriodStart, DateOnly PeriodEnd, DateOnly DueDate |
| apps/api/Controllers/PlatformControlController.cs:278 | UpdatePlatformInvoiceStatusRequest | string Status |
| apps/api/Controllers/PortalAccountsController.cs:33 | CreatePortalAccountRequest | string Role, string Email, string Password, string? DisplayName, Guid? StudentId, Guid? GuardianId, Guid? TeacherId |
| apps/api/Controllers/PortalController.cs:416 | PortalStudentDetails | string Name, string? Email, string? Phone, string FirstName, string LastName, string? PreferredName, string? Gender, DateOnly? DateOfBirth, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone, IReadOnlyList<PortalBatch> Batches, IReadOnlyList<PortalSession> Schedule, IReadOnlyList<PortalAssignment> Assignments, IReadOnlyList<PortalAttendance> Attendance, PortalAttendanceSummary AttendanceSummary, IReadOnlyList<PortalMusicProgress> Music, IReadOnlyList<PortalResource> Resources, IReadOnlyList<PortalPracticeLog> PracticeLogs, PortalPracticeSummary PracticeSummary, IReadOnlyList<PortalLessonPlan> LessonPlans, IReadOnlyList<PortalCourseModule> Modules, IReadOnlyList<PortalCertificate> Certificates, IReadOnlyList<PortalInvoice> Invoices, IReadOnlyList<PortalAssessmentResult> AssessmentResults, IReadOnlyList<PortalClassHistory> ClassHistory, IReadOnlyList<PortalCycleProgress> CycleProgress |
| apps/api/Controllers/PortalController.cs:417 | PortalBatch | Guid Id, string Name |
| apps/api/Controllers/PortalController.cs:418 | PortalSession | Guid Id, string BatchName, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? RoomName, string? MeetingLink, string Status |
| apps/api/Controllers/PortalController.cs:419 | PortalAssignment | Guid Id, string Title, string? Description, string Type, DateTime? DueAtUtc |
| apps/api/Controllers/PortalController.cs:420 | PortalAttendance | DateTime StartUtc, string Status |
| apps/api/Controllers/PortalController.cs:421 | PortalAttendanceSummary | int Total, int Present, int Absent, int Late, int Other |
| apps/api/Controllers/PortalController.cs:422 | PortalMusicProgress | string Title, string Status, DateOnly? TargetDate |
| apps/api/Controllers/PortalController.cs:423 | PortalResource | string Title, string Type, string Url |
| apps/api/Controllers/PortalController.cs:424 | PortalPracticeLog | DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? TeacherFeedback, string Status |
| apps/api/Controllers/PortalController.cs:425 | PortalPracticeSummary | int LogCount, int TotalMinutes |
| apps/api/Controllers/PortalController.cs:426 | PortalInvoice | Guid Id, string InvoiceNumber, decimal TotalAmount, decimal Balance, string Currency, DateOnly DueDate, string Status |
| apps/api/Controllers/PortalController.cs:427 | PortalAssessmentResult | string Title, string Type, decimal MaxScore, decimal Score, string? Grade, string? Remarks |
| apps/api/Controllers/PortalController.cs:428 | PortalLessonPlan | Guid BatchId, string Title, string? Objectives, string Status |
| apps/api/Controllers/PortalController.cs:429 | PortalCourseModule | Guid CourseId, string Title, string? Description, int Sequence |
| apps/api/Controllers/PortalController.cs:430 | PortalCertificate | string CertificateNumber, string Title, DateOnly IssuedDate, string? Notes |
| apps/api/Controllers/PortalController.cs:431 | PortalClassResource | string Title, string? Description, string Type, string Url |
| apps/api/Controllers/PortalController.cs:432 | PortalClassHistory | Guid SessionId, string BatchName, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string Status, string AttendanceStatus, IReadOnlyList<PortalClassResource> Resources |
| apps/api/Controllers/PortalController.cs:433 | PortalCycleProgress | Guid BatchId, string BatchName, int SessionMinutes, int CycleTotal, int CompletedInCycle, int RemainingInCycle, IReadOnlyList<DateTime> CoveredDates, IReadOnlyList<DateTime> UpcomingDates |
| apps/api/Controllers/PortalController.cs:435 | PortalStudentProfileRequest | string? FirstName, string? LastName, string? PreferredName, string? Gender, DateOnly? DateOfBirth, string? Email, string? Phone, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone |
| apps/api/Controllers/PortalController.cs:436 | PortalGuardianProfileRequest | string? Email, string? Phone |
| apps/api/Controllers/PortalController.cs:437 | PortalChangePasswordRequest | string CurrentPassword, string NewPassword |
| apps/api/Controllers/PortalController.cs:438 | PortalLeaveRequest | DateOnly StartDate, DateOnly EndDate, string Reason |
| apps/api/Controllers/PortalController.cs:439 | PortalLeaveSummary | Guid Id, DateOnly StartDate, DateOnly EndDate, string Reason, string Status, string? DecisionNotes |
| apps/api/Controllers/PortalController.cs:440 | PortalPracticeLogRequest | DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? Notes |
| apps/api/Controllers/PortalController.cs:441 | PortalNotificationSummary | Guid Id, string Title, string Message, string Channel, string Status, DateTime CreatedAtUtc, DateTime? SentAtUtc |
| apps/api/Controllers/PortalController.cs:442 | PortalAnnouncementSummary | Guid Id, string Title, string Message, DateTime CreatedAtUtc |
| apps/api/Controllers/PortalController.cs:443 | PortalAnnouncementCandidate | Guid Id, string Title, string Message, DateTime CreatedAtUtc, string? VariablesJson |
| apps/api/Controllers/PortalController.cs:444 | PortalChildSummary | Guid Id, string Name, string? Email, string? Phone, bool IsActive, int ActiveEnrollmentCount |
| apps/api/Controllers/PortalController.cs:445 | PortalEventSummary | Guid Id, string Title, string Type, DateTime StartUtc, DateTime EndUtc, string? Venue, string? Notes |
| apps/api/Controllers/PracticeLogsController.cs:5 | ReviewPracticeLogRequest | string? TeacherFeedback |
| apps/api/Controllers/ProfilesController.cs:190 | ContactSummary | Guid Id, string Name, string? Email, string? Phone, string? Relationship |
| apps/api/Controllers/ProfilesController.cs:191 | EnrollmentProfileSummary | string BatchName, string CourseName, string Status, DateOnly StartDate, DateOnly? EndDate |
| apps/api/Controllers/ProfilesController.cs:192 | StatusCount | string Status, int Count |
| apps/api/Controllers/ProfilesController.cs:193 | InvoiceProfileSummary | string InvoiceNumber, decimal TotalAmount, string Currency, DateOnly DueDate, string Status, string? SubjectName, decimal PaidAmount, DateTime? LastPaidAtUtc |
| apps/api/Controllers/ProfilesController.cs:194 | MusicProgressProfileSummary | string Title, string? Instrument, string Status, decimal? Score |
| apps/api/Controllers/ProfilesController.cs:195 | PracticeProfileSummary | DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? Notes, string Status |
| apps/api/Controllers/ProfilesController.cs:196 | CommunicationProfileSummary | string Title, string Channel, string Status, DateTime CreatedAtUtc |
| apps/api/Controllers/ProfilesController.cs:197 | StudentProfileSummary | Guid Id, string Name, string? Email, string? Phone, string? StudentNumber, string? PreferredName, string? Gender, DateOnly? DateOfBirth, DateOnly? AdmissionDate, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone, string? MedicalOrAccessibilityNotes, string? AdminNotes, IReadOnlyList<ContactSummary> Guardians, IReadOnlyList<EnrollmentProfileSummary> Enrollments, IReadOnlyList<StatusCount> Attendance, IReadOnlyList<StatusCount> CurrentMonthAttendance, IReadOnlyList<InvoiceProfileSummary> Invoices, IReadOnlyList<MusicProgressProfileSummary> MusicProgress, IReadOnlyList<PracticeProfileSummary> PracticeLogs, IReadOnlyList<CommunicationProfileSummary> Communications |
| apps/api/Controllers/ProfilesController.cs:198 | GuardianStudentSummary | Guid Id, string Name, string? Relationship |
| apps/api/Controllers/ProfilesController.cs:199 | GuardianInvoiceSummary | Guid StudentId, string InvoiceNumber, decimal TotalAmount, string Currency, DateOnly DueDate, string Status |
| apps/api/Controllers/ProfilesController.cs:200 | GuardianProfileSummary | Guid Id, string Name, string? Email, string? Phone, string? PreferredName, string? AddressLine1, string? City, string? State, string? PostalCode, string? PreferredLanguage, IReadOnlyList<GuardianStudentSummary> Students, IReadOnlyList<GuardianInvoiceSummary> Invoices, IReadOnlyList<CommunicationProfileSummary> Communications |
| apps/api/Controllers/ProfilesController.cs:201 | TeacherAssignedBatchSummary | Guid Id, string BatchName, string CourseName, bool IsActive, int CompletedSessions, int PendingSessions, int CurrentCycleCompleted, int CurrentCyclePending |
| apps/api/Controllers/ProfilesController.cs:202 | TeacherClassSummary | Guid Id, Guid BatchId, string BatchName, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? RoomName, string Status |
| apps/api/Controllers/ProfilesController.cs:203 | TeacherAssignedStudentSummary | Guid Id, string Name, string BatchName, string Subject |
| apps/api/Controllers/ProfilesController.cs:204 | TeacherAvailabilitySummary | string Day, string? From, string? To |
| apps/api/Controllers/ProfilesController.cs:205 | LeaveProfileSummary | DateOnly StartDate, DateOnly EndDate, string Status, string Reason |
| apps/api/Controllers/ProfilesController.cs:206 | TeacherProfileSummary | Guid Id, string Name, string? Email, string? Phone, string? Specialties, string? EmployeeCode, string? PreferredName, string? EmploymentType, DateOnly? DateOfBirth, DateOnly? JoiningDate, string? Qualifications, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone, string? AdminNotes, IReadOnlyList<string> Subjects, IReadOnlyList<TeacherAssignedStudentSummary> Students, IReadOnlyList<TeacherAvailabilitySummary> Availability, IReadOnlyList<TeacherAssignedBatchSummary> Batches, IReadOnlyList<TeacherClassSummary> Classes, IReadOnlyList<LeaveProfileSummary> LeaveRequests |
| apps/api/Controllers/ProfilesController.cs:207 | UpdateStudentAdminProfileRequest | string? StudentNumber, string? PreferredName, string? Gender, DateOnly? DateOfBirth, DateOnly? AdmissionDate, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone, string? MedicalOrAccessibilityNotes, string? AdminNotes |
| apps/api/Controllers/ProfilesController.cs:208 | UpdateTeacherAdminProfileRequest | string? EmployeeCode, string? PreferredName, string? EmploymentType, DateOnly? DateOfBirth, DateOnly? JoiningDate, string? Qualifications, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone, string? AdminNotes, string? Specialties, string? AvailabilityJson |
| apps/api/Controllers/ProfilesController.cs:209 | UpdateGuardianAdminProfileRequest | string? PreferredName, string? AddressLine1, string? City, string? State, string? PostalCode, string? PreferredLanguage |
| apps/api/Controllers/SalesMarketingController.cs:70 | CampaignRequest | string Name, string? Channel, DateOnly StartDate, DateOnly? EndDate, decimal Budget, string? Status |
| apps/api/Controllers/SalesMarketingController.cs:71 | CampaignUpdateRequest | string Status, decimal Budget, DateOnly? EndDate |
| apps/api/Controllers/SalesMarketingController.cs:72 | TrialRequest | Guid LeadId, Guid? BatchId, Guid? TeacherId, DateTime ScheduledAtUtc, string? Notes |
| apps/api/Controllers/SalesMarketingController.cs:73 | TrialStatusRequest | string Status |
| apps/api/Controllers/StaffController.cs:152 | CreateStaffAccountRequest | string Email, string DisplayName, string Password, string Role, Guid? TeacherId |
| apps/api/Controllers/StaffController.cs:153 | StaffAccountSummary | Guid Id, string DisplayName, string Email, Guid? TeacherId, IReadOnlyList<string> Roles, bool IsActive |
| apps/api/Controllers/StaffController.cs:154 | StaffStatusRequest | bool IsActive |
| apps/api/Controllers/StaffController.cs:155 | StaffRoleRequest | string Role |
| apps/api/Controllers/StaffController.cs:156 | StaffPasswordRequest | string NewPassword |
| apps/api/Controllers/StudentFeeArrangementsController.cs:45 | FeeArrangementRequest | Guid? CourseId, string SubjectName, decimal Amount, string Frequency, DateOnly? EffectiveFrom |
| apps/api/Controllers/StudentFeeArrangementsController.cs:46 | FeeArrangementSummary | Guid Id, Guid? CourseId, string SubjectName, decimal Amount, string Frequency, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive |
| apps/api/Controllers/StudentFeeArrangementsController.cs:47 | AdmissionFeeRequest | decimal? Amount, DateOnly? DueDate |
| apps/api/Controllers/StudentFeeArrangementsController.cs:48 | AdmissionFeeSummary | decimal? Amount, DateOnly? DueDate |
| apps/api/Controllers/StudentGuardiansController.cs:72 | LinkGuardianRequest | Guid GuardianId, string? Relationship, bool IsPrimary, bool AllowParentPortalAccess = false, bool AllowAcademicProgress = true, bool AllowFinance = true, bool AllowDocuments = true, bool AllowLeave = true |
| apps/api/Controllers/StudentGuardiansController.cs:73 | ParentPortalAccessRequest | bool AllowPortalAccess, bool AllowAcademicProgress = true, bool AllowFinance = true, bool AllowDocuments = true, bool AllowLeave = true |
| apps/api/Controllers/StudentGuardiansController.cs:74 | StudentGuardianSummary | Guid GuardianId, string FirstName, string LastName, string? Email, string? Phone, string? Relationship, bool IsPrimary, bool CanAccessPortal, bool CanViewAcademicProgress, bool CanViewFinance, bool CanViewDocuments, bool CanManageLeave, DateTime? AccessGrantedAtUtc, DateTime? AccessRevokedAtUtc |
| apps/api/Controllers/StudentImportsController.cs:30 | StudentImportRequest | IReadOnlyList<StudentImportRow>? Rows |
| apps/api/Controllers/StudentImportsController.cs:31 | StudentImportRow | string FirstName, string LastName, string? StudentNumber, string? Email, string? Phone, DateOnly? AdmissionDate |
| apps/api/Controllers/StudentImportsController.cs:32 | StudentImportValidation | int RowCount, IReadOnlyList<object> Errors |
| apps/api/Controllers/StudentOnboardingController.cs:71 | StudentOnboardingRequest | string StudentFirstName, string StudentLastName, DateOnly? DateOfBirth, string? StudentEmail, string? StudentPhone, string? StudentAddressLine1, string? StudentCity, Guid? BranchId, string? ParentFirstName, string? ParentLastName, string? ParentEmail, string? ParentPhone, string? ParentAddressLine1, string? ParentCity, string? Relationship, string? StudentUserName, string? StudentTemporaryPassword, string? ParentUserName, string? ParentTemporaryPassword, string? StudentNumber = null, string? PreferredName = null, string? Gender = null, DateOnly? AdmissionDate = null, string? StudentState = null, string? StudentPostalCode = null, string? EmergencyContactName = null, string? EmergencyContactPhone = null, string? MedicalOrAccessibilityNotes = null, bool AllowParentPortalAccess = false, bool AllowAcademicProgress = true, bool AllowFinance = true, bool AllowDocuments = true, bool AllowLeave = true |
| apps/api/Controllers/StudentsController.cs:101 | CreateStudentRequest | string FirstName, string LastName, DateOnly? DateOfBirth, string? Email, string? Phone, Guid? BranchId |
| apps/api/Controllers/StudentsController.cs:102 | StudentSummary | Guid Id, string FirstName, string LastName, string? Email, string? Phone, Guid? BranchId, bool IsActive |
| apps/api/Controllers/StudentsController.cs:103 | UpdateStudentRequest | string FirstName,string LastName,string? Email,string? Phone,Guid? BranchId,bool IsActive |
| apps/api/Controllers/StudentsController.cs:104 | StudentOverviewSummary | int TotalStudents, int ActiveStudents, int InactiveStudents, decimal TotalAdmissionFees, decimal TotalSubjectFees, decimal OutstandingFees, decimal OverdueFees, int ActiveSubjectFeeArrangements |
| apps/api/Controllers/TeacherCompensationController.cs:63 | TeacherCompensationSummary | Guid TeacherId, string Model, decimal? MonthlySalary, decimal? StandardHourlyRate, decimal? BeginnerHourlyRate, decimal? IntermediateHourlyRate, decimal? AdvancedHourlyRate, DateOnly? EffectiveFrom |
| apps/api/Controllers/TeacherCompensationController.cs:64 | UpdateTeacherCompensationRequest | string Model, decimal? MonthlySalary, decimal? StandardHourlyRate, decimal? BeginnerHourlyRate, decimal? IntermediateHourlyRate, decimal? AdvancedHourlyRate, DateOnly? EffectiveFrom |
| apps/api/Controllers/TeacherPortalController.cs:656 | TeacherContext | Guid AcademyId, ClassSession Session |
| apps/api/Controllers/TeacherPortalController.cs:659 | TeacherPortalSummary | string FirstName, string LastName, IReadOnlyList<TeacherBatchSummary> Batches, IReadOnlyList<TeacherSessionSummary> Sessions |
| apps/api/Controllers/TeacherPortalController.cs:660 | TeacherBatchSummary | Guid Id, string Name, int Capacity, string DeliveryMode, string? MeetingLink, string? RoomName |
| apps/api/Controllers/TeacherPortalController.cs:661 | TeacherSessionSummary | Guid Id, Guid BatchId, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? RoomName, string Status, string? TeacherAttendanceStatus |
| apps/api/Controllers/TeacherPortalController.cs:662 | TeacherHolidaySummary | Guid Id, string Name, DateOnly HolidayDate, bool IsClosed |
| apps/api/Controllers/TeacherPortalController.cs:663 | TeacherCalendarSummary | IReadOnlyList<TeacherSessionSummary> Sessions, IReadOnlyList<TeacherHolidaySummary> Holidays |
| apps/api/Controllers/TeacherPortalController.cs:664 | TeacherRosterStudent | Guid Id, string FirstName, string LastName |
| apps/api/Controllers/TeacherPortalController.cs:665 | TeacherMarkAttendanceRequest | Guid StudentId, string Status, string? Notes |
| apps/api/Controllers/TeacherPortalController.cs:666 | TeacherAttendanceSummary | Guid StudentId, string Status, string? Notes |
| apps/api/Controllers/TeacherPortalController.cs:667 | TeacherSessionStatusRequest | string Status |
| apps/api/Controllers/TeacherPortalController.cs:668 | TeacherBulkAttendanceRequest | IReadOnlyList<TeacherBulkAttendanceItem> Records |
| apps/api/Controllers/TeacherPortalController.cs:669 | TeacherBulkAttendanceItem | Guid StudentId, string Status, string? Notes |
| apps/api/Controllers/TeacherPortalController.cs:670 | TeacherSessionAttendanceRequest | string Status |
| apps/api/Controllers/TeacherPortalController.cs:671 | TeacherPortalProfileRequest | string? FirstName, string? LastName, string? PreferredName, string? Email, string? Phone, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone |
| apps/api/Controllers/TeacherPortalController.cs:672 | TeacherPortalProfileSummary | string FirstName, string LastName, string? PreferredName, string? Email, string? Phone, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone |
| apps/api/Controllers/TeacherPortalController.cs:673 | TeacherLeaveRequest | DateOnly StartDate, DateOnly EndDate, string Reason |
| apps/api/Controllers/TeacherPortalController.cs:674 | TeacherLeaveSummary | Guid Id, DateOnly StartDate, DateOnly EndDate, string Reason, string Status, string? DecisionNotes |
| apps/api/Controllers/TeacherPortalController.cs:675 | TeacherPracticeLogSummary | Guid Id, Guid StudentId, string StudentName, DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? Notes, string? TeacherFeedback, string Status |
| apps/api/Controllers/TeacherPortalController.cs:676 | TeacherPracticeReviewRequest | string? TeacherFeedback |
| apps/api/Controllers/TeacherPortalController.cs:677 | TeacherAssignmentSummary | Guid Id, Guid BatchId, Guid? StudentId, string Title, string? Description, DateTime? DueAtUtc, string Type, bool IsPublished |
| apps/api/Controllers/TeacherPortalController.cs:678 | TeacherCreateAssignmentRequest | Guid BatchId, Guid? StudentId, string Title, string? Description, DateTime? DueAtUtc, string? Type, bool IsPublished |
| apps/api/Controllers/TeacherPortalController.cs:679 | TeacherPublishAssignmentRequest | bool IsPublished |
| apps/api/Controllers/TeacherPortalController.cs:680 | TeacherSubmissionSummary | Guid Id, Guid StudentId, string StudentName, string? ResponseText, string Status, DateTime SubmittedAtUtc, string? TeacherFeedback |
| apps/api/Controllers/TeacherPortalController.cs:681 | TeacherSubmissionReviewRequest | string? Feedback |
| apps/api/Controllers/TeacherPortalController.cs:682 | TeacherLessonPlanSummary | Guid Id, Guid BatchId, Guid? CourseModuleId, Guid? ClassSessionId, string Title, string? Objectives, string Status |
| apps/api/Controllers/TeacherPortalController.cs:683 | TeacherCreateLessonPlanRequest | Guid BatchId, Guid? CourseModuleId, Guid? ClassSessionId, string Title, string? Objectives |
| apps/api/Controllers/TeacherPortalController.cs:684 | TeacherLessonPlanStatusRequest | string Status |
| apps/api/Controllers/TeacherPortalController.cs:685 | TeacherAssessmentSummary | Guid Id, Guid BatchId, string Title, string Type, decimal MaxScore, DateTime? ScheduledAtUtc, bool IsPublished |
| apps/api/Controllers/TeacherPortalController.cs:686 | TeacherCreateAssessmentRequest | Guid BatchId, string Title, string? Type, decimal MaxScore, DateTime? ScheduledAtUtc, bool IsPublished |
| apps/api/Controllers/TeacherPortalController.cs:687 | TeacherPublishAssessmentRequest | bool IsPublished |
| apps/api/Controllers/TeacherPortalController.cs:688 | TeacherAssessmentResultSummary | Guid Id, Guid StudentId, string StudentName, decimal Score, string? Grade, string? Remarks, bool IsPublished |
| apps/api/Controllers/TeacherPortalController.cs:689 | TeacherRecordAssessmentResultRequest | Guid StudentId, decimal Score, string? Grade, string? Remarks, bool IsPublished |
| apps/api/Controllers/TeacherPortalController.cs:690 | TeacherResourceSummary | Guid Id, Guid BatchId, Guid? StudentId, Guid? ClassSessionId, string Title, string? Description, string Type, string Url, DateTime CreatedAtUtc |
| apps/api/Controllers/TeacherPortalController.cs:691 | TeacherClassroomActivitySummary | IReadOnlyList<TeacherResourceSummary> Resources, IReadOnlyList<TeacherAssignmentSummary> Homework |
| apps/api/Controllers/TeacherPortalController.cs:692 | TeacherCreateResourceNoteRequest | Guid BatchId, Guid? StudentId, Guid? ClassSessionId, string Title, string Notes, string? Type |
| apps/api/Controllers/TeacherPortalController.cs:694 | TeacherProgressSummary | int CompletedClasses, int UpcomingClasses, int AttendanceRecords, int PresentOrOnline |
| apps/api/Controllers/TeacherPortalController.cs:695 | TeacherBatchProgressSummary | Guid BatchId, string BatchName, int SessionMinutes, int SessionsPerWeek, string PaymentCycle, int CycleTotal, int CompletedInCycle, int RemainingInCycle, bool PaymentReady, IReadOnlyList<DateTime> CoveredClassDates, IReadOnlyList<TeacherScheduledClassSummary> UpcomingClasses |
| apps/api/Controllers/TeacherPortalController.cs:696 | TeacherScheduledClassSummary | DateTime StartUtc, DateTime EndUtc |
| apps/api/Controllers/TeacherPortalController.cs:697 | TeacherPaymentSummary | string? PaymentModel, decimal? MonthlyAmount, decimal? AmountPerCycle, int? SessionsPerCycle, IReadOnlyList<TeacherPayslipSummary> Payslips |
| apps/api/Controllers/TeacherPortalController.cs:698 | TeacherPayslipSummary | Guid Id, string PayslipNumber, string PeriodLabel, decimal GrossAmount, decimal Deductions, decimal NetAmount, string Currency, string Status, string PaymentMethod, string? Reference, DateTime PaidAtUtc |
| apps/api/Controllers/TeachersController.cs:74 | CreateTeacherRequest | string FirstName, string LastName, string? Email, string? Phone, string? Specialties, Guid? BranchId, string? Qualifications = null, string? EmploymentType = null, DateOnly? DateOfBirth = null, DateOnly? JoiningDate = null, string? AddressLine1 = null, string? City = null, string? State = null, string? PostalCode = null, string? CertificationsJson = null, string? AvailabilityJson = null, string? CompensationJson = null |
| apps/api/Controllers/TeachersController.cs:75 | TeacherSummary | Guid Id, string FirstName, string LastName, string? Email, string? Phone, string? Specialties, Guid? BranchId, bool IsActive |
| apps/api/Controllers/TeachersController.cs:76 | UpdateTeacherRequest | string FirstName,string LastName,string? Email,string? Phone,string? Specialties,Guid? BranchId,bool IsActive |

## Class-based multipart contracts

| Source | Contract | Definition |
| --- | --- | --- |
| apps/api/Controllers/LearningResourcesController.cs:12 | UploadResourceRequest | public sealed class UploadResourceRequest{public string? Title{get;set;}public string? Description{get;set;}public string? Type{get;set;}public Guid? BatchId{get;set;}public Guid? CourseId{get;set;}public bool IsPublished{get;set;}=true;public IFormFile? File{get;set;}} |
| apps/api/Controllers/PortalController.cs:434 | PortalSubmissionRequest | public sealed class PortalSubmissionRequest { public string? ResponseText { get; set; } public IFormFile? File { get; set; } } |
| apps/api/Controllers/TeacherPortalController.cs:693 | TeacherUploadResourceRequest | public sealed class TeacherUploadResourceRequest { public Guid BatchId { get; set; } public Guid? StudentId { get; set; } public Guid? ClassSessionId { get; set; } public string? Title { get; set; } public string? Description { get; set; } public string? Type { get; set; } public IFormFile? File { get; set; } } |

## Client calls and serialized bodies

| Source | Function | Method | API path | Request expression |
| --- | --- | --- | --- | --- |
| apps/web/src/app/academic-governance/page.tsx:35 | load | GET or dynamic init | '/api/academies/${academyId}/courses' |  |
| apps/web/src/app/academic-governance/page.tsx:36 | load | GET or dynamic init | '/api/academies/${academyId}/academic-governance/grading-schemes' |  |
| apps/web/src/app/academic-governance/page.tsx:39 | load | GET or dynamic init | '/api/academies/${academyId}/academic-governance/prerequisites' |  |
| apps/web/src/app/academic-governance/page.tsx:62 | AcademicGovernancePage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/academic-governance/page.tsx:97 | submit | POST | '/api/academies/${academy.id}/academic-governance/${path}' | JSON.stringify(body) |
| apps/web/src/app/academic-governance/page.tsx:136 | toggleScheme | PATCH | '/api/academies/${academy.id}/grading-schemes/${item.id}/status' | JSON.stringify({ isActive: !item.isActive }) |
| apps/web/src/app/academic-periods/page.tsx:41 | load | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/academic-periods/page.tsx:44 | load | GET or dynamic init | '/api/academies/${rows[0].id}/academic-periods' |  |
| apps/web/src/app/academic-periods/page.tsx:84 | save | POST | '/api/academies/${academy.id}/academic-periods/${kind}' | JSON.stringify(body) |
| apps/web/src/app/academic-periods/page.tsx:106 | close | PATCH | '/api/academies/${academy.id}/academic-periods/${kind}/${id}/close' |  |
| apps/web/src/app/access-review/page.tsx:55 | load | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/access-review/page.tsx:60 | load | GET or dynamic init | '/api/academies/${academies[0].id}/staff' |  |
| apps/web/src/app/access-review/page.tsx:61 | load | GET or dynamic init | '/api/academies/${academies[0].id}/access-grants' |  |
| apps/web/src/app/access-review/page.tsx:86 | grant | POST | '/api/academies/${academy.id}/access-grants' | JSON.stringify({ userId, permissions: selected, isPermanent: permanent, expiresAtUtc: permanent ? null : new Date('${expiryDate}T23:59:59').toISOString(), reason: form.get("reason"), }) |
| apps/web/src/app/access-review/page.tsx:115 | revoke | PATCH | '/api/academies/${academy.id}/access-grants/${id}/revoke' |  |
| apps/web/src/app/access-review/sign-off/page.tsx:2 | load | GET or dynamic init | '/api/academies' |  |
| apps/web/src/app/access-review/sign-off/page.tsx:2 | load | GET or dynamic init | '/api/academies/${x[0].id}/access-reviews' |  |
| apps/web/src/app/access-review/sign-off/page.tsx:2 | save | POST | '/api/academies/${a.id}/access-reviews' | JSON.stringify({notes:f.get('notes'),createFollowUp:f.get('followUp')==='on'}) |
| apps/web/src/app/activity/page.tsx:24 | load | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/activity/page.tsx:30 | load | GET or dynamic init | '/api/academies/${academies[0].id}/audit-logs' |  |
| apps/web/src/app/admin/control/page.tsx:12 | AcademyControlPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/admin/control/page.tsx:13 | saveProfile | PUT | '/api/academies/${academy.id}' | JSON.stringify({ name: form.get("name"), legalName: form.get("legalName") \|\| null, countryCode, timeZone }) |
| apps/web/src/app/admin-intelligence/page.tsx:13 | Intelligence | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/admin-intelligence/page.tsx:13 | Intelligence | GET or dynamic init | '/api/academies/${academies[0].id}/admin-intelligence' |  |
| apps/web/src/app/assessment-governance/page.tsx:21 | AssessmentGovernance | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/assessment-governance/page.tsx:24 | AssessmentGovernance | GET or dynamic init | '/api/academies/${academies[0].id}/academic-governance/grading-schemes' |  |
| apps/web/src/app/assessments/page.tsx:68 | load | GET or dynamic init | '/api/academies/${id}/batches' |  |
| apps/web/src/app/assessments/page.tsx:69 | load | GET or dynamic init | '/api/academies/${id}/students' |  |
| apps/web/src/app/assessments/page.tsx:70 | load | GET or dynamic init | '/api/academies/${id}/enrollments' |  |
| apps/web/src/app/assessments/page.tsx:71 | load | GET or dynamic init | '/api/academies/${id}/assessments' |  |
| apps/web/src/app/assessments/page.tsx:72 | load | GET or dynamic init | '/api/academies/${id}/grading-schemes/active' |  |
| apps/web/src/app/assessments/page.tsx:100 | loadResults | GET or dynamic init | '/api/academies/${academy.id}/assessments/${id}/results' |  |
| apps/web/src/app/assessments/page.tsx:110 | AssessmentsPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/assessments/page.tsx:134 | createAssessment | POST | '/api/academies/${academy.id}/assessments' | JSON.stringify({ batchId, title, type, maxScore: Number(maxScore), gradingSchemeId: gradingSchemeId \|\| null, scheduledAtUtc: scheduledDate ? new Date('${scheduledDate}T${scheduledTime}:00').toISOString() : null, isPublished: true, }) |
| apps/web/src/app/assessments/page.tsx:184 | saveResult | POST | '/api/academies/${academy.id}/assessments/${selected.id}/results' | JSON.stringify({ studentId, score, grade: grade \|\| null, remarks: remarks \|\| null, isPublished: true, }) |
| apps/web/src/app/assignments/page.tsx:23 | load | GET or dynamic init | '/api/academies/${id}/batches' |  |
| apps/web/src/app/assignments/page.tsx:23 | load | GET or dynamic init | '/api/academies/${id}/assignments' |  |
| apps/web/src/app/assignments/page.tsx:24 | initialise | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/assignments/page.tsx:25 | createAssignment | POST | '/api/academies/${academy.id}/assignments' | JSON.stringify({ batchId, title, description: description \|\| null, dueAtUtc: dueAt ? new Date(dueAt).toISOString() : null, type, isPublished: published }) |
| apps/web/src/app/attendance/page.tsx:38 | loadWorkspace | GET or dynamic init | '/api/academies/${id}/students' |  |
| apps/web/src/app/attendance/page.tsx:38 | loadWorkspace | GET or dynamic init | '/api/academies/${id}/batches' |  |
| apps/web/src/app/attendance/page.tsx:38 | loadWorkspace | GET or dynamic init | '/api/academies/${id}/enrollments' |  |
| apps/web/src/app/attendance/page.tsx:38 | loadWorkspace | GET or dynamic init | '/api/academies/${id}/sessions' |  |
| apps/web/src/app/attendance/page.tsx:49 | loadRecords | GET or dynamic init | '/api/academies/${academy.id}/sessions/${id}/attendance' |  |
| apps/web/src/app/attendance/page.tsx:54 | initialise | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/attendance/page.tsx:65 | mark | POST | '/api/academies/${academy.id}/sessions/${sessionId}/attendance' | JSON.stringify({ studentId, status, notes: notesByStudent[studentId] \|\| null }) |
| apps/web/src/app/batch-promotions/page.tsx:52 | load | GET or dynamic init | '/api/academies/${academyId}/students' |  |
| apps/web/src/app/batch-promotions/page.tsx:53 | load | GET or dynamic init | '/api/academies/${academyId}/batches' |  |
| apps/web/src/app/batch-promotions/page.tsx:54 | load | GET or dynamic init | '/api/academies/${academyId}/enrollments' |  |
| apps/web/src/app/batch-promotions/page.tsx:55 | load | GET or dynamic init | '/api/academies/${academyId}/batch-promotions' |  |
| apps/web/src/app/batch-promotions/page.tsx:78 | BatchPromotionsPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/batch-promotions/page.tsx:121 | create | POST | '/api/academies/${academy.id}/batch-promotions' | JSON.stringify({ studentId, sourceBatchId, targetBatchId, effectiveDate, notes: data.get("notes") \|\| null, }) |
| apps/web/src/app/batch-promotions/page.tsx:158 | decide | PATCH | '/api/academies/${academy.id}/batch-promotions/${item.id}/decision' | JSON.stringify({ status, notes }) |
| apps/web/src/app/batches/page.tsx:84 | loadAcademies | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/batches/page.tsx:95 | loadWorkspace | GET or dynamic init | '/api/academies/${id}/courses' |  |
| apps/web/src/app/batches/page.tsx:96 | loadWorkspace | GET or dynamic init | '/api/academies/${id}/teachers' |  |
| apps/web/src/app/batches/page.tsx:97 | loadWorkspace | GET or dynamic init | '/api/academies/${id}/branches' |  |
| apps/web/src/app/batches/page.tsx:98 | loadWorkspace | GET or dynamic init | '/api/academies/${id}/batches' |  |
| apps/web/src/app/batches/page.tsx:133 | createBatch | POST | '/api/academies/${academyId}/batches' | JSON.stringify({ name, batchCode: batchCode \|\| null, courseId, teacherId: null, branchId: branchId \|\| null, capacity: Number(capacity), waitlistCapacity: Number(waitlistCapacity), deliveryMode, classType, sessionMinutes: Number(sessionMinutes), sessionsPerWeek: Number(sessionsPerWeek), meetingDaysJson: JSON.stringify(meetingDays.map((day) => ({ day, startTime: meetingTimes[day] }))), meetingLink: meetingLink \|\| null, meetingPattern: meetingDays.map((day) => '${day} ${meetingTimes[day] ?? ""}'.trim()).join(" · ") \|\| null, roomName: roomName \|\| null, enrollmentStatus, adminNotes: null, startDate: startDate \|\| null, endDate: null, }) |
| apps/web/src/app/batches/page.tsx:187 | saveBatch | PUT | '/api/academies/${academyId}/batches/${batch.id}' | JSON.stringify({ name: editName, courseId: editCourseId, teacherId: editTeacherId \|\| null, branchId: editBranchId \|\| null, capacity: Number(editCapacity), startDate: editStartDate \|\| null, endDate: null, isActive: batch.isActive, }) |
| apps/web/src/app/batches/page.tsx:212 | toggleActive | PUT | '/api/academies/${academyId}/batches/${batch.id}' | JSON.stringify({ name: batch.name, courseId: batch.courseId, teacherId: batch.teacherId, branchId: batch.branchId, capacity: batch.capacity, startDate: batch.startDate, endDate: batch.endDate, isActive: !batch.isActive, }) |
| apps/web/src/app/branches/page.tsx:10 | loadAcademies | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/branches/page.tsx:11 | loadBranches | GET or dynamic init | '/api/academies/${id}/branches' |  |
| apps/web/src/app/branches/page.tsx:13 | createBranch | POST | '/api/academies/${academyId}/branches' | JSON.stringify({ name, city, state }) |
| apps/web/src/app/branches/page.tsx:15 | saveBranch | PUT | '/api/academies/${academyId}/branches/${branch.id}' | JSON.stringify({ name: editName, city: editCity \|\| null, state: editState \|\| null, isActive: branch.isActive }) |
| apps/web/src/app/branches/page.tsx:16 | toggleActive | PUT | '/api/academies/${academyId}/branches/${branch.id}' | JSON.stringify({ name: branch.name, city: branch.city, state: branch.state, isActive: !branch.isActive }) |
| apps/web/src/app/calendar/page.tsx:145 | CalendarPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/calendar/page.tsx:151 | CalendarPage | GET or dynamic init | '/api/academies/${id}/${path}' |  |
| apps/web/src/app/certificates/page.tsx:90 | load | GET or dynamic init | '/api/academies/${id}/students' |  |
| apps/web/src/app/certificates/page.tsx:91 | load | GET or dynamic init | '/api/academies/${id}/batches' |  |
| apps/web/src/app/certificates/page.tsx:92 | load | GET or dynamic init | '/api/academies/${id}/certificates' |  |
| apps/web/src/app/certificates/page.tsx:93 | load | GET or dynamic init | '/api/academies/${id}/certificates/branding' |  |
| apps/web/src/app/certificates/page.tsx:114 | CertificatesPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/certificates/page.tsx:129 | saveBranding | PUT | '/api/academies/${academy.id}/certificates/branding' | JSON.stringify({ accentColor: branding.accentColor, signatoryName: branding.signatoryName \|\| null, }) |
| apps/web/src/app/certificates/page.tsx:156 | uploadLogo | POST | '/api/academies/${academy.id}/certificates/branding/logo' |  |
| apps/web/src/app/certificates/page.tsx:173 | issue | POST | '/api/academies/${academy.id}/certificates' | JSON.stringify({ studentId, batchId: batchId \|\| null, title, templateKey: themeKey, issuedDate: issuedDate \|\| null, notes: notes \|\| null, }) |
| apps/web/src/app/communication-preferences/page.tsx:38 | load | GET or dynamic init | '/api/academies/${academyId}/students' |  |
| apps/web/src/app/communication-preferences/page.tsx:39 | load | GET or dynamic init | '/api/academies/${academyId}/guardians' |  |
| apps/web/src/app/communication-preferences/page.tsx:40 | load | GET or dynamic init | '/api/academies/${academyId}/communication-preferences' |  |
| apps/web/src/app/communication-preferences/page.tsx:49 | CommunicationPreferencesPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/communication-preferences/page.tsx:78 | save | PUT | '/api/academies/${academy.id}/communication-preferences/${recipientType}/${recipientId}' | JSON.stringify({ emailAllowed, whatsAppAllowed, marketingAllowed, notes, }) |
| apps/web/src/app/communication-settings/page.tsx:16 | load | GET or dynamic init | '/api/academies/${academyId}/communication-settings' |  |
| apps/web/src/app/communication-settings/page.tsx:17 | CommunicationSettingsPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/communication-settings/page.tsx:18 | save | PUT | '/api/academies/${academy.id}/communication-settings/${channel}' | JSON.stringify({ ...draft, replyToAddress: null }) |
| apps/web/src/app/communication-settings/page.tsx:19 | saveMeeting | PUT | '/api/academies/${academy.id}/communication-settings/Meeting' | JSON.stringify({ provider: meeting.provider, status: meeting.status, senderName: meeting.organizerName, senderAddress: meeting.organizerEmail, replyToAddress: null, phoneNumber: null, externalAccountReference: meeting.reference, messagesEnabled: false }) |
| apps/web/src/app/communications/page.tsx:67 | load | GET or dynamic init | '/api/academies/${academyId}/students' |  |
| apps/web/src/app/communications/page.tsx:68 | load | GET or dynamic init | '/api/academies/${academyId}/guardians' |  |
| apps/web/src/app/communications/page.tsx:69 | load | GET or dynamic init | '/api/academies/${academyId}/teachers' |  |
| apps/web/src/app/communications/page.tsx:70 | load | GET or dynamic init | '/api/academies/${academyId}/communication-templates' |  |
| apps/web/src/app/communications/page.tsx:71 | load | GET or dynamic init | '/api/academies/${academyId}/notifications' |  |
| apps/web/src/app/communications/page.tsx:82 | CommunicationsPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/communications/page.tsx:115 | create | POST | '/api/academies/${academy.id}/notifications' | JSON.stringify({ recipientId: isAnnouncement ? null : recipientId, recipientType, title, message: body, channel: isAnnouncement ? "InApp" : channel, scheduledAtUtc, templateId: templateId \|\| null, variables: isAnnouncement ? { ...variables, audiences: announcementAudience } : variables, isImportant: isAnnouncement, displayHours: isAnnouncement ? Number(displayHours) : null, }) |
| apps/web/src/app/compliance/page.tsx:31 | load | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/compliance/page.tsx:35 | load | GET or dynamic init | '/api/academies/${current.id}/compliance/documents' |  |
| apps/web/src/app/compliance/page.tsx:35 | load | GET or dynamic init | '/api/academies/${current.id}/compliance/consents' |  |
| apps/web/src/app/compliance/page.tsx:36 | load | GET or dynamic init | '/api/academies/${current.id}/students' |  |
| apps/web/src/app/compliance/page.tsx:36 | load | GET or dynamic init | '/api/academies/${current.id}/guardians' |  |
| apps/web/src/app/compliance/page.tsx:47 | addDocument | POST | '/api/academies/${academy.id}/compliance/documents' | JSON.stringify({ studentId: form.get("personType") === "student" ? form.get("personId") : null, guardianId: form.get("personType") === "guardian" ? form.get("personId") : null, documentType: form.get("documentType"), fileName: form.get("fileName"), secureReference: form.get("secureReference"), expiryDate: form.get("expiryDate") \|\| null, visibility: form.get("visibility") }) |
| apps/web/src/app/compliance/page.tsx:54 | addConsent | POST | '/api/academies/${academy.id}/compliance/consents' | JSON.stringify({ studentId: form.get("personType") === "student" ? form.get("personId") : null, guardianId: form.get("personType") === "guardian" ? form.get("personId") : null, consentType: form.get("consentType"), granted: true, evidenceReference: form.get("evidenceReference") }) |
| apps/web/src/app/compliance/page.tsx:60 | review | PATCH | '/api/academies/${academy.id}/compliance/documents/${document.id}/review' | JSON.stringify({ status }) |
| apps/web/src/app/compliance/page.tsx:65 | withdraw | PATCH | '/api/academies/${academy.id}/compliance/consents/${consent.id}/withdraw' |  |
| apps/web/src/app/courses/page.tsx:33 | loadAcademies | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/courses/page.tsx:41 | loadCourses | GET or dynamic init | '/api/academies/${id}/courses' |  |
| apps/web/src/app/courses/page.tsx:59 | createCourse | POST | '/api/academies/${academyId}/courses' | JSON.stringify({ name, academyType: type, level }) |
| apps/web/src/app/courses/page.tsx:79 | saveCourse | PUT | '/api/academies/${academyId}/courses/${course.id}' | JSON.stringify({ name: editName, academyType: editType, level: editLevel \|\| null, description: course.description, durationMonths: null, isActive: course.isActive, }) |
| apps/web/src/app/courses/page.tsx:102 | toggleActive | PUT | '/api/academies/${academyId}/courses/${course.id}' | JSON.stringify({ name: course.name, academyType: course.academyType, level: course.level, description: course.description, durationMonths: null, isActive: !course.isActive, }) |
| apps/web/src/app/curriculum/page.tsx:31 | load | GET or dynamic init | '/api/academies/${academyId}/courses' |  |
| apps/web/src/app/curriculum/page.tsx:32 | load | GET or dynamic init | '/api/academies/${academyId}/course-modules' |  |
| apps/web/src/app/curriculum/page.tsx:45 | Curriculum | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/curriculum/page.tsx:59 | add | POST | '/api/academies/${academy.id}/course-modules' | JSON.stringify({ courseId, title, description: null, sequence: Number(sequence), }) |
| apps/web/src/app/curriculum/page.tsx:81 | publish | PATCH | '/api/academies/${academy.id}/course-modules/${module.id}/publication' | JSON.stringify({ isPublished: !module.isPublished }) |
| apps/web/src/app/dashboard/page.tsx:107 | loadDashboard | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/dashboard/page.tsx:123 | loadDashboard | GET or dynamic init | '/api/academies/${academy.id}/dashboard' |  |
| apps/web/src/app/dashboard/page.tsx:126 | loadDashboard | GET or dynamic init | "/api/auth/session" |  |
| apps/web/src/app/dashboard/page.tsx:145 | loadDashboard | GET or dynamic init | '/api/academies/${academy.id}/students' |  |
| apps/web/src/app/dashboard/page.tsx:146 | loadDashboard | GET or dynamic init | '/api/academies/${academy.id}/invoices' |  |
| apps/web/src/app/dashboard/page.tsx:147 | loadDashboard | GET or dynamic init | '/api/academies/${academy.id}/payroll/payouts' |  |
| apps/web/src/app/dashboard/page.tsx:148 | loadDashboard | GET or dynamic init | '/api/academies/${academy.id}/sessions' |  |
| apps/web/src/app/dashboard/page.tsx:152 | loadDashboard | GET or dynamic init | '/api/academies/${academy.id}/sessions/${item.id}/attendance' |  |
| apps/web/src/app/data-operations/page.tsx:12 | DataOperations | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/data-operations/page.tsx:13 | download | GET or dynamic init | '/api/academies/${academy.id}/exports/${resource}' |  |
| apps/web/src/app/data-operations/page.tsx:15 | validate | POST | '/api/academies/${academy.id}/imports/students/validate' | JSON.stringify({ rows }) |
| apps/web/src/app/data-operations/page.tsx:16 | importRows | POST | '/api/academies/${academy.id}/imports/students' | JSON.stringify({ rows }) |
| apps/web/src/app/enrollments/page.tsx:30 | load | GET or dynamic init | '/api/academies/${id}/students' |  |
| apps/web/src/app/enrollments/page.tsx:30 | load | GET or dynamic init | '/api/academies/${id}/batches' |  |
| apps/web/src/app/enrollments/page.tsx:30 | load | GET or dynamic init | '/api/academies/${id}/enrollments' |  |
| apps/web/src/app/enrollments/page.tsx:41 | initialise | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/enrollments/page.tsx:46 | createEnrollment | POST | '/api/academies/${academy.id}/enrollments' | JSON.stringify({ studentId, batchId, startDate: startDate \|\| null, status: initialStatus }) |
| apps/web/src/app/enrollments/page.tsx:51 | updateStatus | PUT | '/api/academies/${academy.id}/enrollments/${enrollment.id}' | JSON.stringify({ status, endDate: status === "Active" ? null : enrollment.endDate }) |
| apps/web/src/app/events/page.tsx:44 | load | GET or dynamic init | '/api/academies/${academyId}/events' |  |
| apps/web/src/app/events/page.tsx:45 | load | GET or dynamic init | '/api/academies/${academyId}/branches' |  |
| apps/web/src/app/events/page.tsx:55 | EventsPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/events/page.tsx:70 | create | POST | '/api/academies/${academy.id}/events' | JSON.stringify({ title, type, branchId: branchId \|\| null, startUtc: new Date('${startDate}T${startTime}:00').toISOString(), endUtc: new Date('${endDate}T${endTime}:00').toISOString(), venue: venue \|\| null, capacity: null, notes: null, }) |
| apps/web/src/app/expenses/page.tsx:14 | load | GET or dynamic init | '/api/academies/${id}/expenses' |  |
| apps/web/src/app/expenses/page.tsx:14 | load | GET or dynamic init | '/api/academies/${id}/branches' |  |
| apps/web/src/app/expenses/page.tsx:15 | ExpensesPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/expenses/page.tsx:16 | createExpense | POST | '/api/academies/${academy.id}/expenses' | JSON.stringify({ description, amount: Number(amount), currency: "INR", category, branchId: branchId \|\| null, expenseDate: expenseDate \|\| null }) |
| apps/web/src/app/fee-plans/page.tsx:31 | load | GET or dynamic init | '/api/academies/${id}/fee-plans' |  |
| apps/web/src/app/fee-plans/page.tsx:40 | initialise | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/fee-plans/page.tsx:66 | createPlan | POST | '/api/academies/${academy.id}/fee-plans' | JSON.stringify({ name, amount: Number(amount), currency: "INR", frequency }) |
| apps/web/src/app/fee-plans/page.tsx:82 | savePlan | PUT | '/api/academies/${academy.id}/fee-plans/${plan.id}' | JSON.stringify({ ...next, isActive }) |
| apps/web/src/app/fee-reminders/page.tsx:5 | load | GET or dynamic init | '/api/academies/${x}/${v}' |  |
| apps/web/src/app/fee-reminders/page.tsx:5 | FeeRemindersPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/fee-reminders/page.tsx:5 | queue | POST | '/api/academies/${a.id}/fee-reminders' | JSON.stringify({invoiceId:id??null,channel:"InApp"}) |
| apps/web/src/app/finance/page.tsx:91 | load | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/finance/page.tsx:105 | load | GET or dynamic init | '/api/academies/${currentAcademy.id}/finance-governance/summary' |  |
| apps/web/src/app/finance/page.tsx:109 | load | GET or dynamic init | '/api/academies/${currentAcademy.id}/finance-governance/collections' |  |
| apps/web/src/app/finance/page.tsx:131 | downloadExport | GET or dynamic init | '/api/academies/${academy.id}/exports/${kind}' |  |
| apps/web/src/app/finance-adjustments/page.tsx:48 | load | GET or dynamic init | '/api/academies/${id}/invoices' |  |
| apps/web/src/app/finance-adjustments/page.tsx:49 | load | GET or dynamic init | '/api/academies/${id}/finance-adjustments' |  |
| apps/web/src/app/finance-adjustments/page.tsx:67 | AdjustmentsPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/finance-adjustments/page.tsx:93 | save | POST | '/api/academies/${academy.id}/finance-adjustments' | JSON.stringify({ invoiceId: requestInvoiceId, type: requestType, amount: Number(data.get("amount")), reason: data.get("reason"), }) |
| apps/web/src/app/finance-governance/page.tsx:31 | load | GET or dynamic init | '/api/academies/${id}/finance-adjustments' |  |
| apps/web/src/app/finance-governance/page.tsx:32 | load | GET or dynamic init | '/api/academies/${id}/finance-governance/collections' |  |
| apps/web/src/app/finance-governance/page.tsx:33 | load | GET or dynamic init | '/api/academies/${id}/admin-work-items?type=Collections' |  |
| apps/web/src/app/finance-governance/page.tsx:33 | load | GET or dynamic init | '/api/academies/${id}/finance-governance/settings' |  |
| apps/web/src/app/finance-governance/page.tsx:40 | FinanceGovernancePage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/finance-governance/page.tsx:47 | decide | PATCH | '/api/academies/${academy.id}/finance-adjustments/${id}/approval' | JSON.stringify({ approve, notes }) |
| apps/web/src/app/finance-governance/page.tsx:54 | createFollowUp | POST | '/api/academies/${academy.id}/finance-governance/collections/${selectedInvoice.id}/follow-up' | JSON.stringify({ note: data.get("note"), priority: data.get("priority"), assignedUserId: null, dueAtUtc: data.get("dueAtUtc") \|\| null }) |
| apps/web/src/app/finance-governance/page.tsx:59 | updateCollection | PATCH | '/api/academies/${academy.id}/admin-work-items/${item.id}/collections' | JSON.stringify({ escalationStage: data.get("stage"), promisedPaymentDate: data.get("promiseDate") \|\| null }) |
| apps/web/src/app/finance-governance/page.tsx:61 | saveDocumentSettings | PUT | '/api/academies/${academy.id}/finance-governance/settings' | JSON.stringify({ ...documentSettings, invoiceLogoUrl: data.get("invoiceLogoUrl"), invoiceAuthorityName: data.get("invoiceAuthorityName"), invoiceAuthorityTitle: data.get("invoiceAuthorityTitle"), invoiceSignatureUrl: data.get("invoiceSignatureUrl"), invoiceTemplateKey: data.get("invoiceTemplateKey"), payslipTemplateKey: data.get("payslipTemplateKey") }) |
| apps/web/src/app/finance-policy/page.tsx:13 | FinancePolicy | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/finance-policy/page.tsx:13 | FinancePolicy | GET or dynamic init | '/api/academies/${academies[0].id}/finance-governance/settings' |  |
| apps/web/src/app/finance-policy/page.tsx:14 | save | PUT | '/api/academies/${academy.id}/finance-governance/settings' | JSON.stringify({ taxRegistrationNumber: form.get("taxRegistrationNumber"), taxLabel: form.get("taxLabel"), taxRatePercent: Number(form.get("taxRatePercent")), defaultPaymentTermsDays: Number(form.get("defaultPaymentTermsDays")), taxInclusivePricing: form.get("taxInclusivePricing") === "on", invoiceLogoUrl: form.get("invoiceLogoUrl"), invoiceAuthorityName: form.get("invoiceAuthorityName"), invoiceAuthorityTitle: form.get("invoiceAuthorityTitle"), invoiceSignatureUrl: form.get("invoiceSignatureUrl"), invoiceTemplateKey: form.get("invoiceTemplateKey"), payslipTemplateKey: form.get("payslipTemplateKey") }) |
| apps/web/src/app/finance-reconciliation/page.tsx:29 | load | GET or dynamic init | '/api/academies/${id}/invoices' |  |
| apps/web/src/app/finance-reconciliation/page.tsx:29 | load | GET or dynamic init | '/api/academies/${id}/payments' |  |
| apps/web/src/app/finance-reconciliation/page.tsx:37 | ReconciliationPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/finance-reconciliation/page.tsx:50 | reconcile | PATCH | '/api/academies/${academy.id}/payments/${selectedId}/reconcile' | JSON.stringify({ reference: reference.trim() }) |
| apps/web/src/app/finance-summary/page.tsx:21 | FinanceSummary | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/finance-summary/page.tsx:24 | FinanceSummary | GET or dynamic init | '/api/academies/${academies[0].id}/finance-governance/summary' |  |
| apps/web/src/app/guardian-profile/page.tsx:41 | load | GET or dynamic init | '/api/academies/${academyId}/guardians/${id}/profile' |  |
| apps/web/src/app/guardian-profile/page.tsx:53 | GuardianProfilePage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/guardian-profile/page.tsx:61 | GuardianProfilePage | GET or dynamic init | '/api/academies/${academies[0].id}/guardians' |  |
| apps/web/src/app/guardian-profile/page.tsx:92 | save | PUT | '/api/academies/${academy.id}/guardians/${guardianId}/profile' | JSON.stringify(form) |
| apps/web/src/app/guardians/page.tsx:52 | load | GET or dynamic init | '/api/academies/${academyId}/students' |  |
| apps/web/src/app/guardians/page.tsx:53 | load | GET or dynamic init | '/api/academies/${academyId}/guardians' |  |
| apps/web/src/app/guardians/page.tsx:66 | loadLinks | GET or dynamic init | '/api/academies/${academy.id}/students/${id}/guardians' |  |
| apps/web/src/app/guardians/page.tsx:76 | initialise | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/guardians/page.tsx:105 | createGuardian | POST | '/api/academies/${academy.id}/guardians' | JSON.stringify({ firstName, lastName, email: email \|\| null, phone: phone \|\| null, }) |
| apps/web/src/app/guardians/page.tsx:129 | linkGuardian | POST | '/api/academies/${academy.id}/students/${studentId}/guardians' | JSON.stringify({ guardianId, relationship, isPrimary }) |
| apps/web/src/app/guardians/page.tsx:156 | saveGuardian | PUT | '/api/academies/${academy.id}/guardians/${guardian.id}' | JSON.stringify({ firstName: editFirstName, lastName: editLastName, email: editEmail \|\| null, phone: editPhone \|\| null, isActive: guardian.isActive, }) |
| apps/web/src/app/guardians/page.tsx:179 | toggleActive | PUT | '/api/academies/${academy.id}/guardians/${guardian.id}' | JSON.stringify({ firstName: guardian.firstName, lastName: guardian.lastName, email: guardian.email, phone: guardian.phone, isActive: !guardian.isActive, }) |
| apps/web/src/app/holidays/page.tsx:31 | load | GET or dynamic init | '/api/academies/${current.id}/holidays' |  |
| apps/web/src/app/holidays/page.tsx:37 | HolidaysPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/holidays/page.tsx:48 | addDefaults | POST | '/api/academies/${academy.id}/holidays/india-2026-defaults' |  |
| apps/web/src/app/holidays/page.tsx:60 | create | POST | '/api/academies/${academy.id}/holidays' | JSON.stringify(form) |
| apps/web/src/app/holidays/page.tsx:77 | remove | DELETE | '/api/academies/${academy.id}/holidays/${id}' |  |
| apps/web/src/app/invoices/page.tsx:15 | load | GET or dynamic init | '/api/academies/${id}/students' |  |
| apps/web/src/app/invoices/page.tsx:15 | load | GET or dynamic init | '/api/academies/${id}/fee-plans' |  |
| apps/web/src/app/invoices/page.tsx:15 | load | GET or dynamic init | '/api/academies/${id}/invoices' |  |
| apps/web/src/app/invoices/page.tsx:15 | load | GET or dynamic init | '/api/academies/${id}/finance-governance/settings' |  |
| apps/web/src/app/invoices/page.tsx:16 | InvoicesPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/invoices/page.tsx:18 | createInvoice | POST | '/api/academies/${academy.id}/invoices' | JSON.stringify({ studentId, feePlanId: feePlanId \|\| null, amount: amount ? Number(amount) : null, dueDate: dueDate \|\| null }) |
| apps/web/src/app/leads/page.tsx:69 | load | GET or dynamic init | '/api/academies/${academyId}/leads' |  |
| apps/web/src/app/leads/page.tsx:79 | initialise | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/leads/page.tsx:100 | create | POST | '/api/academies/${academy.id}/leads' | JSON.stringify({ fullName, email: email \|\| null, phone: phone \|\| null, dateOfBirth: dateOfBirth \|\| null, parentName: parentName \|\| null, programInterest: programInterest \|\| null, source, followUpAtUtc: followUpDate ? new Date('${followUpDate}T${followUpTime}:00').toISOString() : null, notes: notes \|\| null, }) |
| apps/web/src/app/leads/page.tsx:134 | setStage | PATCH | '/api/academies/${academy.id}/leads/${lead.id}/stage' | JSON.stringify({ stage, followUpAtUtc: lead.followUpAtUtc }) |
| apps/web/src/app/leads/page.tsx:156 | convert | POST | '/api/academies/${academy.id}/leads/${lead.id}/convert' | JSON.stringify({ firstName: null, lastName: null }) |
| apps/web/src/app/leave/page.tsx:4 | load | GET or dynamic init | '/api/academies/${z}/students' |  |
| apps/web/src/app/leave/page.tsx:4 | load | GET or dynamic init | '/api/academies/${z}/teachers' |  |
| apps/web/src/app/leave/page.tsx:4 | load | GET or dynamic init | '/api/academies/${z}/leave-requests' |  |
| apps/web/src/app/leave/page.tsx:4 | LeavePage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/leave/page.tsx:4 | create | POST | '/api/academies/${a.id}/leave-requests' | JSON.stringify({requesterType:type,studentId:type==="Student"?id:null,teacherId:type==="Teacher"?id:null,startDate:start,endDate:end,reason}) |
| apps/web/src/app/leave/page.tsx:4 | decide | PATCH | '/api/academies/${a.id}/leave-requests/${x.id}' | JSON.stringify({status,notes:null}) |
| apps/web/src/app/lesson-plans/page.tsx:1 | load | GET or dynamic init | '/api/academies/${x}/batches' |  |
| apps/web/src/app/lesson-plans/page.tsx:1 | load | GET or dynamic init | '/api/academies/${x}/lesson-plans' |  |
| apps/web/src/app/lesson-plans/page.tsx:1 | LessonPlans | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/lesson-plans/page.tsx:1 | add | POST | '/api/academies/${a.id}/lesson-plans' | JSON.stringify({batchId:batch,courseModuleId:null,classSessionId:null,title,objectives:obj\|\|null}) |
| apps/web/src/app/login/page.tsx:23 | login | POST | '${apiUrl}/api/auth/login?useCookies=false' | JSON.stringify({ email, password }) |
| apps/web/src/app/login/page.tsx:33 | login | GET or dynamic init | '${apiUrl}/api/auth/session' |  |
| apps/web/src/app/makeup/page.tsx:21 | load | GET or dynamic init | '/api/academies/${academyId}/${path}' |  |
| apps/web/src/app/makeup/page.tsx:22 | MakeupPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/makeup/page.tsx:23 | create | POST | '/api/academies/${academy.id}/makeup-classes' | JSON.stringify({ studentId, batchId, teacherId: teacherId \|\| null, startUtc: mode === "Manual" ? toUtc(start) : null, deliveryMode, venue: venue \|\| null, meetingLink: meetingLink \|\| null, useNextScheduledClass: mode === "NextScheduled", notes: null }) |
| apps/web/src/app/makeup/page.tsx:24 | updateStatus | PATCH | '/api/academies/${academy.id}/makeup-classes/${item.id}' | JSON.stringify({ status }) |
| apps/web/src/app/meeting-links/page.tsx:78 | MeetingLinksPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/meeting-links/page.tsx:91 | MeetingLinksPage | GET or dynamic init | '/api/academies/${current.id}/${p}' |  |
| apps/web/src/app/message-templates/page.tsx:71 | load | GET or dynamic init | '/api/academies/${academyId}/communication-templates' |  |
| apps/web/src/app/message-templates/page.tsx:72 | load | GET or dynamic init | '/api/academies/${academyId}/communication-templates/starter-templates' |  |
| apps/web/src/app/message-templates/page.tsx:81 | MessageTemplatesPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/message-templates/page.tsx:120 | addSelected | POST | '/api/academies/${academy.id}/communication-templates/starter-templates' | JSON.stringify({ templateIds: selectedIds }) |
| apps/web/src/app/message-templates/page.tsx:140 | create | POST | '/api/academies/${academy.id}/communication-templates' | JSON.stringify(form) |
| apps/web/src/app/music/page.tsx:60 | load | GET or dynamic init | '/api/academies/${academyId}/students' |  |
| apps/web/src/app/music/page.tsx:61 | load | GET or dynamic init | '/api/academies/${academyId}/music-pieces' |  |
| apps/web/src/app/music/page.tsx:62 | load | GET or dynamic init | '/api/academies/${academyId}/music-progress' |  |
| apps/web/src/app/music/page.tsx:78 | MusicPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/music/page.tsx:94 | addPiece | POST | '/api/academies/${academy.id}/music-pieces' | JSON.stringify({ title, composer: composer \|\| null, instrument, genre: null, difficulty, durationMinutes: null, }) |
| apps/web/src/app/music/page.tsx:117 | assign | POST | '/api/academies/${academy.id}/music-progress' | JSON.stringify({ studentId, musicPieceId: pieceId, targetDate: targetDate \|\| null, notes: null, }) |
| apps/web/src/app/music/page.tsx:138 | updateProgress | PATCH | '/api/academies/${academy.id}/music-progress/${item.id}' | JSON.stringify({ status, targetDate: item.targetDate \|\| null, score: item.score \|\| null, notes: item.notes \|\| null, }) |
| apps/web/src/app/payments/page.tsx:27 | load | GET or dynamic init | '/api/academies/${id}/students' |  |
| apps/web/src/app/payments/page.tsx:27 | load | GET or dynamic init | '/api/academies/${id}/invoices' |  |
| apps/web/src/app/payments/page.tsx:27 | load | GET or dynamic init | '/api/academies/${id}/payments' |  |
| apps/web/src/app/payments/page.tsx:33 | initialise | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/payments/page.tsx:50 | recordPayment | POST | '/api/academies/${academy.id}/payments' | JSON.stringify({ invoiceId, amount: Number(amount), method, reference: reference \|\| null }) |
| apps/web/src/app/payroll/page.tsx:17 | load | GET or dynamic init | '/api/academies/${academyId}/teachers' |  |
| apps/web/src/app/payroll/page.tsx:17 | load | GET or dynamic init | '/api/academies/${academyId}/staff' |  |
| apps/web/src/app/payroll/page.tsx:17 | load | GET or dynamic init | '/api/academies/${academyId}/payroll/profiles' |  |
| apps/web/src/app/payroll/page.tsx:17 | load | GET or dynamic init | '/api/academies/${academyId}/payroll/payouts' |  |
| apps/web/src/app/payroll/page.tsx:18 | PayrollPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/payroll/page.tsx:21 | createProfile | POST | '/api/academies/${academy.id}/payroll/profiles' | JSON.stringify({ workerType, teacherId: workerType === "Teacher" ? workerId : null, staffUserId: workerType === "Staff" ? workerId : null, workerName: worker?.name, paymentModel, monthlyAmount: paymentModel === "Monthly" ? Number(form.get("monthlyAmount")) : null, amountPerCycle: paymentModel === "SessionBlock" ? Number(form.get("amountPerCycle")) : null, sessionsPerCycle: paymentModel === "SessionBlock" ? Number(form.get("sessionsPerCycle")) : null, effectiveFrom, isActive: true }) |
| apps/web/src/app/payroll/page.tsx:22 | pay | POST | '/api/academies/${academy.id}/payroll/payouts' | JSON.stringify({ payrollProfileId: payoutProfileId, periodLabel: form.get("periodLabel"), sessionsCovered: currentProfile?.paymentModel === "SessionBlock" ? Number(form.get("sessionsCovered")) : null, grossAmount: currentProfile?.paymentModel === "SessionBlock" ? Number(form.get("grossAmount")) : null, deductions: Number(form.get("deductions") \|\| 0), paymentMethod, reference: form.get("reference"), paidAtUtc: null }) |
| apps/web/src/app/platform/control/page.tsx:197 | load | GET or dynamic init | "/api/platform/academies" |  |
| apps/web/src/app/platform/control/page.tsx:198 | load | GET or dynamic init | "/api/platform/admins" |  |
| apps/web/src/app/platform/control/page.tsx:199 | load | GET or dynamic init | "/api/platform/support-cases" |  |
| apps/web/src/app/platform/control/page.tsx:200 | load | GET or dynamic init | "/api/platform/billing-invoices" |  |
| apps/web/src/app/platform/control/page.tsx:201 | load | GET or dynamic init | "/api/platform/settings" |  |
| apps/web/src/app/platform/control/page.tsx:202 | load | GET or dynamic init | "/api/platform/audit" |  |
| apps/web/src/app/platform/control/page.tsx:203 | load | GET or dynamic init | "/api/platform/health" |  |
| apps/web/src/app/platform/control/page.tsx:204 | load | GET or dynamic init | "/api/auth/session" |  |
| apps/web/src/app/platform/control/page.tsx:205 | load | GET or dynamic init | "/api/platform/announcements" |  |
| apps/web/src/app/platform/control/page.tsx:233 | loadActivityLogs | GET or dynamic init | '/api/platform/activity-logs?${query.toString()}' |  |
| apps/web/src/app/platform/control/page.tsx:241 | deleteActivityLogs | DELETE | "/api/platform/activity-logs" | JSON.stringify({ fromUtc: from.toISOString(), toUtc: end.toISOString(), scope: activityScope === "All" ? null : activityScope === "Platform owner" ? "PlatformOwner" : "AcademyAdmin" }) |
| apps/web/src/app/platform/control/page.tsx:287 | PlatformControlPage | GET or dynamic init | '/api/platform/academies/${selectedAcademy}/onboarding' |  |
| apps/web/src/app/platform/control/page.tsx:363 | saveTenantOnboarding | PUT | '/api/platform/academies/${selectedAcademy}/onboarding' | JSON.stringify({ status: "InProgress", currentSection: onboardingSection, primaryContactName: field("primaryContactName", tenantProfile?.primaryContactName), primaryContactRole: field("primaryContactRole", tenantProfile?.primaryContactRole), primaryContactEmail: field("primaryContactEmail", tenantProfile?.primaryContactEmail), primaryContactPhone: field("primaryContactPhone", tenantProfile?.primaryContactPhone), country: field("country", tenantProfile?.country), state: field("state", tenantProfile?.state), city: field("city", tenantProfile?.city), postalCode: field("postalCode", tenantProfile?.postalCode), addressLine1: field("addressLine1", tenantProfile?.addressLine1), addressLine2: field("addressLine2", tenantProfile?.addressLine2), businessType: field("businessType", tenantProfile?.businessType), operatingSince: field("operatingSince", tenantProfile?.operatingSince), website: field("website", tenantProfile?.website), branchSummary: field("branchSummary", tenantProfile?.branchSummary), financeModel: field("financeModel", tenantProfile?.financeModel), billingFrequency: field("billingFrequency", tenantProfile?.billingFrequency), paymentCollectionMethods: field("paymentCollectionMethods", tenantProfile?.paymentCollectionMethods), teacherPaymentModels: field("teacherPaymentModels", tenantProfile?.teacherPaymentModels), teacherCount: number("teacherCount", tenantProfile?.teacherCount), studentCount: number("studentCount", tenantProfile?.studentCount), subjectCount: number("subjectCount", tenantProfile?.subjectCount), subjectTypes: field("subjectTypes", tenantProfile?.subjectTypes), deliveryModes: field("deliveryModes", tenantProfile?.deliveryModes), classRatios: field("classRatios", tenantProfile?.classRatios), batchAndClassSetup: field("batchAndClassSetup", tenantProfile?.batchAndClassSetup), operationalNotes: field("operationalNotes", tenantProfile?.operationalNotes), documentsJson: field("documentsJson", tenantProfile?.documentsJson) \|\| "[]" }) |
| apps/web/src/app/platform/control/page.tsx:381 | createTenantFromOnboarding | POST | "/api/platform/academies" | JSON.stringify({ academyName: form.get("academyName"), legalName: form.get("legalName") \|\| null, adminUserName: form.get("adminUserName"), adminDisplayName: form.get("adminDisplayName") \|\| null, password: form.get("password"), countryCode: "IN", timeZone: "Asia/Kolkata" }) |
| apps/web/src/app/platform/control/page.tsx:391 | createTenantFromOnboarding | PUT | '/api/platform/academies/${payload.id}/onboarding' | JSON.stringify({ status: "InProgress", currentSection: "Complete", primaryContactName: text("primaryContactName"), primaryContactRole: text("primaryContactRole"), primaryContactEmail: text("primaryContactEmail"), primaryContactPhone: text("primaryContactPhone"), country: text("country"), state: text("state"), city: text("city"), postalCode: text("postalCode"), addressLine1: text("addressLine1"), addressLine2: text("addressLine2"), businessType: text("businessType"), operatingSince: text("operatingSince"), website: text("website"), branchSummary: text("branchSummary"), financeModel: text("financeModel"), billingFrequency: text("billingFrequency"), paymentCollectionMethods: text("paymentCollectionMethods"), teacherPaymentModels: text("teacherPaymentModels"), teacherCount: number("teacherCount"), studentCount: number("studentCount"), subjectCount: number("subjectCount"), subjectTypes: text("subjectTypes"), deliveryModes: text("deliveryModes"), classRatios: text("classRatios"), batchAndClassSetup: text("batchAndClassSetup"), operationalNotes: text("operationalNotes"), documentsJson: text("documentsJson") \|\| "[]" }) |
| apps/web/src/app/platform/control/page.tsx:447 | request | GET or dynamic init | path | JSON.stringify(body) |
| apps/web/src/app/platform/control/page.tsx:521 | uploadOwnerImage | POST | "/api/auth/session/profile-image" |  |
| apps/web/src/app/platform/control/page.tsx:530 | publishAnnouncement | POST | "/api/platform/announcements" | JSON.stringify({ academyId: form.get("academyId"), title: form.get("title"), message: form.get("body"), displayHours: Number(form.get("hours")) }) |
| apps/web/src/app/platform/page.tsx:110 | load | GET or dynamic init | "/api/platform/academies" |  |
| apps/web/src/app/platform/page.tsx:111 | load | GET or dynamic init | "/api/platform/overview" |  |
| apps/web/src/app/platform/page.tsx:112 | load | GET or dynamic init | "/api/platform/health" |  |
| apps/web/src/app/platform/page.tsx:113 | load | GET or dynamic init | "/api/auth/session" |  |
| apps/web/src/app/platform/page.tsx:134 | setStatus | PATCH | '/api/platform/academies/${academy.id}/status' | JSON.stringify({ isActive: !academy.isActive }) |
| apps/web/src/app/platform/page.tsx:161 | onboardAcademy | POST | "/api/platform/academies" | JSON.stringify({ academyName, legalName: legalName \|\| null, adminUserName, adminDisplayName: adminDisplayName \|\| null, password: temporaryPassword, countryCode: "IN", timeZone: "Asia/Kolkata" }) |
| apps/web/src/app/platform-services/page.tsx:25 | load | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/platform-services/page.tsx:31 | load | GET or dynamic init | '/api/academies/${selected.id}/platform-services/billing-invoices' |  |
| apps/web/src/app/platform-services/page.tsx:32 | load | GET or dynamic init | '/api/academies/${selected.id}/platform-services/support-cases' |  |
| apps/web/src/app/platform-services/page.tsx:43 | submitPayment | POST | '/api/academies/${academy.id}/platform-services/billing-invoices/${invoice.id}/payment-submission' | JSON.stringify({ reference: references[invoice.id] \|\| null }) |
| apps/web/src/app/platform-services/page.tsx:50 | createCase | POST | '/api/academies/${academy.id}/platform-services/support-cases' | JSON.stringify({ subject: form.get("subject"), priority, description: form.get("description") }) |
| apps/web/src/app/platform-services/page.tsx:57 | respond | POST | '/api/academies/${academy.id}/platform-services/support-cases/${item.id}/response' | JSON.stringify({ message: responses[item.id] }) |
| apps/web/src/app/portal/page.tsx:143 | load | GET or dynamic init | '/api/portal/students/${student}' |  |
| apps/web/src/app/portal/page.tsx:144 | load | GET or dynamic init | '/api/portal/students/${student}/leave-requests' |  |
| apps/web/src/app/portal/page.tsx:145 | load | GET or dynamic init | "/api/portal/notifications" |  |
| apps/web/src/app/portal/page.tsx:146 | load | GET or dynamic init | "/api/portal/announcements" |  |
| apps/web/src/app/portal/page.tsx:156 | Portal | GET or dynamic init | "/api/portal/me" |  |
| apps/web/src/app/portal/page.tsx:266 | StudentPortalProfile | GET or dynamic init | "/api/auth/session" |  |
| apps/web/src/app/portal/page.tsx:268 | upload | POST | "/api/auth/session/profile-image" |  |
| apps/web/src/app/portal/page.tsx:290 | openNotifications | PATCH | '/api/portal/notifications/${notice.id}/read' |  |
| apps/web/src/app/portal/page.tsx:538 | downloadPortalFile | GET or dynamic init | path |  |
| apps/web/src/app/portal/page.tsx:565 | go | POST | '/api/portal/students/${id}/assignments/${x.id}/submit' | (() => { const body = new FormData(); body.append("responseText", v); if (file) body.append("file", file); return body; })() |
| apps/web/src/app/portal/page.tsx:734 | go | GET or dynamic init | url | JSON.stringify(body) |
| apps/web/src/app/portal-accounts/page.tsx:24 | PortalAccounts | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/portal-accounts/page.tsx:30 | PortalAccounts | GET or dynamic init | '/api/academies/${x[0].id}/students' |  |
| apps/web/src/app/portal-accounts/page.tsx:31 | PortalAccounts | GET or dynamic init | '/api/academies/${x[0].id}/guardians' |  |
| apps/web/src/app/portal-accounts/page.tsx:32 | PortalAccounts | GET or dynamic init | '/api/academies/${x[0].id}/teachers' |  |
| apps/web/src/app/portal-accounts/page.tsx:52 | create | POST | '/api/academies/${a.id}/portal-accounts' | JSON.stringify({ role, email, password, displayName, studentId: role === "Student" ? id : null, guardianId: role === "Guardian" ? id : null, teacherId: role === "Teacher" ? id : null, }) |
| apps/web/src/app/practice-logs/page.tsx:2 | Page | GET or dynamic init | '/api/academies' |  |
| apps/web/src/app/practice-logs/page.tsx:2 | Page | GET or dynamic init | '/api/academies/${z[0].id}/students' |  |
| apps/web/src/app/practice-logs/page.tsx:2 | Page | GET or dynamic init | '/api/academies/${z[0].id}/practice-logs' |  |
| apps/web/src/app/practice-logs/page.tsx:2 | save | POST | '/api/academies/${a.id}/practice-logs' | JSON.stringify(f) |
| apps/web/src/app/practice-logs/page.tsx:2 | review | PATCH | '/api/academies/${a.id}/practice-logs/${id}/review' | JSON.stringify({teacherFeedback:feedback}) |
| apps/web/src/app/register/page.tsx:19 | register | POST | '${apiUrl}/api/auth/register' | JSON.stringify({ email, password }) |
| apps/web/src/app/reports/page.tsx:59 | load | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/reports/page.tsx:79 | load | GET or dynamic init | '/api/academies/${id}/students' |  |
| apps/web/src/app/reports/page.tsx:80 | load | GET or dynamic init | '/api/academies/${id}/teachers' |  |
| apps/web/src/app/reports/page.tsx:81 | load | GET or dynamic init | '/api/academies/${id}/batches' |  |
| apps/web/src/app/reports/page.tsx:82 | load | GET or dynamic init | '/api/academies/${id}/enrollments' |  |
| apps/web/src/app/reports/page.tsx:83 | load | GET or dynamic init | '/api/academies/${id}/sessions' |  |
| apps/web/src/app/reports/page.tsx:84 | load | GET or dynamic init | '/api/academies/${id}/invoices' |  |
| apps/web/src/app/reports/page.tsx:85 | load | GET or dynamic init | '/api/academies/${id}/payments' |  |
| apps/web/src/app/reports/page.tsx:86 | load | GET or dynamic init | '/api/academies/${id}/expenses' |  |
| apps/web/src/app/reports/page.tsx:87 | load | GET or dynamic init | '/api/academies/${id}/payroll/payouts' |  |
| apps/web/src/app/reports/page.tsx:115 | load | GET or dynamic init | '/api/academies/${id}/sessions/${session.id}/attendance' |  |
| apps/web/src/app/resources/page.tsx:44 | load | GET or dynamic init | '/api/academies/${academyId}/resources' |  |
| apps/web/src/app/resources/page.tsx:45 | load | GET or dynamic init | '/api/academies/${academyId}/batches' |  |
| apps/web/src/app/resources/page.tsx:46 | load | GET or dynamic init | '/api/academies/${academyId}/courses' |  |
| apps/web/src/app/resources/page.tsx:57 | Resources | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/resources/page.tsx:79 | create | POST | '/api/academies/${academy.id}/resources/upload' |  |
| apps/web/src/app/resources/page.tsx:84 | create | POST | '/api/academies/${academy.id}/resources' | JSON.stringify({ title, description: null, type, url, batchId: batch \|\| null, courseId: course \|\| null, isPublished: true, }) |
| apps/web/src/app/sales-campaigns/page.tsx:45 | load | GET or dynamic init | '/api/academies/${id}/sales-marketing/campaigns' |  |
| apps/web/src/app/sales-campaigns/page.tsx:56 | SalesCampaignsPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/sales-campaigns/page.tsx:73 | create | POST | '/api/academies/${academy.id}/sales-marketing/campaigns' | JSON.stringify({ name, channel, startDate, budget: Number(budget \|\| 0), status, }) |
| apps/web/src/app/sales-campaigns/page.tsx:95 | update | PATCH | '/api/academies/${academy.id}/sales-marketing/campaigns/${campaign.id}' | JSON.stringify({ status: nextStatus, budget: campaign.budget, endDate: campaign.endDate \|\| null, }) |
| apps/web/src/app/sales-marketing/page.tsx:44 | SalesMarketingContent | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/sales-marketing/page.tsx:49 | SalesMarketingContent | GET or dynamic init | '/api/academies/${academies[0].id}/leads' |  |
| apps/web/src/app/schedule/page.tsx:62 | load | GET or dynamic init | '/api/academies/${id}/batches' |  |
| apps/web/src/app/schedule/page.tsx:63 | load | GET or dynamic init | '/api/academies/${id}/teachers' |  |
| apps/web/src/app/schedule/page.tsx:64 | load | GET or dynamic init | '/api/academies/${id}/branches' |  |
| apps/web/src/app/schedule/page.tsx:65 | load | GET or dynamic init | '/api/academies/${id}/sessions' |  |
| apps/web/src/app/schedule/page.tsx:85 | initialise | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/schedule/page.tsx:121 | createSession | POST | '/api/academies/${academy.id}/sessions' | JSON.stringify({ batchId, teacherId: teacherId \|\| null, branchId: branchId \|\| null, startUtc: istInputToUtc(startLocal), endUtc: istInputToUtc(endLocal), deliveryMode, roomName: roomName \|\| null, }) |
| apps/web/src/app/schedule/page.tsx:147 | updateStatus | PUT | '/api/academies/${academy.id}/sessions/${session.id}' | JSON.stringify({ startUtc: session.startUtc, endUtc: session.endUtc, deliveryMode: session.deliveryMode, roomName: session.roomName, status, }) |
| apps/web/src/app/staff/page.tsx:45 | load | GET or dynamic init | '/api/academies/${academyId}/staff' |  |
| apps/web/src/app/staff/page.tsx:58 | initialise | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/staff/page.tsx:79 | create | POST | '/api/academies/${academy.id}/staff' | JSON.stringify({ email, displayName, password, role, }) |
| apps/web/src/app/staff/page.tsx:110 | offboard | POST | '/api/academies/${academy.id}/staff/${person.id}/offboard' |  |
| apps/web/src/app/staff/page.tsx:124 | updateRole | PATCH | '/api/academies/${academy.id}/staff/${person.id}/role' | JSON.stringify({ role: nextRole }) |
| apps/web/src/app/student-fees/page.tsx:23 | StudentFeesPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/student-fees/page.tsx:27 | StudentFeesPage | GET or dynamic init | '/api/academies/${current.id}/students' |  |
| apps/web/src/app/student-fees/page.tsx:41 | StudentFeesPage | GET or dynamic init | '/api/academies/${academy.id}/students/${studentId}/fee-arrangements/admission-fee' |  |
| apps/web/src/app/student-fees/page.tsx:55 | saveAdmissionFee | PUT | '/api/academies/${academy.id}/students/${studentId}/fee-arrangements/admission-fee' | JSON.stringify({ amount: admissionAmount ? Number(admissionAmount) : null, dueDate: admissionDueDate \|\| null }) |
| apps/web/src/app/student-onboarding/page.tsx:22 | StudentOnboardingPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/student-onboarding/page.tsx:45 | submit | POST | '/api/academies/${academy.id}/student-onboarding' | JSON.stringify(body) |
| apps/web/src/app/student-overview/page.tsx:46 | StudentOverviewPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/student-overview/page.tsx:53 | StudentOverviewPage | GET or dynamic init | '/api/academies/${current.id}/students/overview' |  |
| apps/web/src/app/student-overview/page.tsx:54 | StudentOverviewPage | GET or dynamic init | '/api/academies/${current.id}/sessions' |  |
| apps/web/src/app/student-overview/page.tsx:55 | StudentOverviewPage | GET or dynamic init | '/api/academies/${current.id}/enrollments' |  |
| apps/web/src/app/student-overview/page.tsx:56 | StudentOverviewPage | GET or dynamic init | '/api/academies/${current.id}/students' |  |
| apps/web/src/app/student-overview/page.tsx:57 | StudentOverviewPage | GET or dynamic init | '/api/academies/${current.id}/batches' |  |
| apps/web/src/app/student-overview/page.tsx:73 | StudentOverviewPage | GET or dynamic init | '/api/academies/${current.id}/invoices' |  |
| apps/web/src/app/student-overview/page.tsx:74 | StudentOverviewPage | GET or dynamic init | '/api/academies/${current.id}/students/${student.id}/fee-arrangements' |  |
| apps/web/src/app/student-overview/page.tsx:74 | StudentOverviewPage | GET or dynamic init | '/api/academies/${current.id}/students/${student.id}/fee-arrangements/admission-fee' |  |
| apps/web/src/app/student-profile/page.tsx:99 | StudentProfilePage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/student-profile/page.tsx:106 | StudentProfilePage | GET or dynamic init | '/api/academies/${academy.id}/students' |  |
| apps/web/src/app/student-profile/page.tsx:114 | StudentProfilePage | GET or dynamic init | '/api/academies/${academy.id}/students/${selected}/profile' |  |
| apps/web/src/app/student-profile/page.tsx:129 | select | GET or dynamic init | '/api/academies/${academyId}/students/${id}/profile' |  |
| apps/web/src/app/students/page.tsx:41 | load | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/students/page.tsx:51 | load | GET or dynamic init | '/api/academies/${current.id}/students' |  |
| apps/web/src/app/students/page.tsx:52 | load | GET or dynamic init | '/api/academies/${current.id}/batches' |  |
| apps/web/src/app/students/page.tsx:68 | toggle | PUT | '/api/academies/${academy.id}/students/${student.id}' | JSON.stringify({ firstName: student.firstName, lastName: student.lastName, email: student.email, phone: student.phone, branchId: null, isActive: !student.isActive, }) |
| apps/web/src/app/students/page.tsx:99 | assignBatch | POST | '/api/academies/${academy.id}/enrollments' | JSON.stringify({ studentId: assignmentStudentId, batchId: assignmentBatchId, startDate: null, status: "Active" }) |
| apps/web/src/app/submission-review/page.tsx:22 | SubmissionReviewPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/submission-review/page.tsx:26 | SubmissionReviewPage | GET or dynamic init | '/api/academies/${academies[0].id}/assignment-submissions' |  |
| apps/web/src/app/submission-review/page.tsx:40 | review | PATCH | '/api/academies/${academy.id}/assignment-submissions/${id}/review' | JSON.stringify({ feedback }) |
| apps/web/src/app/teacher/page.tsx:69 | loadRoster | GET or dynamic init | '/api/teacher/sessions/${id}/roster' |  |
| apps/web/src/app/teacher/page.tsx:70 | loadRoster | GET or dynamic init | '/api/teacher/sessions/${id}/attendance' |  |
| apps/web/src/app/teacher/page.tsx:77 | load | GET or dynamic init | "/api/teacher/me" |  |
| apps/web/src/app/teacher/page.tsx:91 | Teacher | GET or dynamic init | "/api/portal/announcements" |  |
| apps/web/src/app/teacher/page.tsx:92 | Teacher | GET or dynamic init | "/api/portal/notifications" |  |
| apps/web/src/app/teacher/page.tsx:98 | submitAttendance | POST | '/api/teacher/sessions/${sid}/attendance/bulk' | JSON.stringify({ records: records.map((record) => ({ ...record, notes: null })) }) |
| apps/web/src/app/teacher/page.tsx:99 | submitAttendance | PUT | '/api/teacher/sessions/${sid}/teacher-attendance' | JSON.stringify({ status: teacherStatus }) |
| apps/web/src/app/teacher/page.tsx:241 | TeacherPortalProfile | GET or dynamic init | "/api/auth/session" |  |
| apps/web/src/app/teacher/page.tsx:252 | upload | POST | "/api/auth/session/profile-image" |  |
| apps/web/src/app/teacher/page.tsx:305 | TeacherCalendar | GET or dynamic init | '/api/teacher/calendar?year=${month.getFullYear()}&month=${month.getMonth() + 1}' |  |
| apps/web/src/app/teacher/page.tsx:394 | updateStatus | PATCH | '/api/teacher/sessions/${sessionId}/status' | JSON.stringify({ status }) |
| apps/web/src/app/teacher/page.tsx:431 | refresh | GET or dynamic init | '/api/teacher/resources?batchId=${id}' |  |
| apps/web/src/app/teacher/page.tsx:432 | refresh | GET or dynamic init | '/api/teacher/classroom-activity?batchId=${id}${studentId ? '&studentId=${studentId}' : ""}${fromDate ? '&fromUtc=${encodeURIComponent(fromDate)}' : ""}${toDate ? '&toUtc=${encodeURIComponent(toDate)}' : ""}' |  |
| apps/web/src/app/teacher/page.tsx:442 | addNote | POST | "/api/teacher/resources/note" | JSON.stringify({ batchId, studentId: studentId \|\| null, classSessionId: sessionId \|\| null, title: form.get("title"), notes: form.get("notes"), type: "Note" }) |
| apps/web/src/app/teacher/page.tsx:447 | upload | POST | "/api/teacher/resources/upload" | form |
| apps/web/src/app/teacher/page.tsx:477 | TeacherProgress | GET or dynamic init | "/api/teacher/progress" |  |
| apps/web/src/app/teacher/page.tsx:478 | TeacherProgress | GET or dynamic init | '/api/teacher/payments${query}' |  |
| apps/web/src/app/teacher/page.tsx:487 | TeacherOverviewFinance | GET or dynamic init | "/api/teacher/payments" |  |
| apps/web/src/app/teacher/page.tsx:500 | TeacherBatchProgress | GET or dynamic init | "/api/teacher/batch-progress" |  |
| apps/web/src/app/teacher/page.tsx:515 | TeacherTasks | GET or dynamic init | "/api/teacher/practice-logs" |  |
| apps/web/src/app/teacher/page.tsx:533 | create | POST | path | JSON.stringify(body) |
| apps/web/src/app/teacher/page.tsx:548 | reviewPractice | PATCH | '/api/teacher/practice-logs/${log.id}/review' | JSON.stringify({ teacherFeedback: feedback[log.id] ?? log.teacherFeedback ?? null }) |
| apps/web/src/app/teacher/page.tsx:633 | TeacherSelfService | GET or dynamic init | "/api/teacher/profile" |  |
| apps/web/src/app/teacher/page.tsx:633 | TeacherSelfService | GET or dynamic init | "/api/teacher/leave-requests" |  |
| apps/web/src/app/teacher/page.tsx:636 | save | path === /api/teacher/profile ? PUT : POST | path | JSON.stringify( Object.fromEntries(new FormData(event.currentTarget)), ) |
| apps/web/src/app/teacher/page.tsx:651 | uploadPicture | POST | "/api/auth/session/profile-image" |  |
| apps/web/src/app/teacher-onboarding/page.tsx:21 | TeacherOnboardingPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/teacher-onboarding/page.tsx:61 | submit | POST | '/api/academies/${academy.id}/teachers' | JSON.stringify(body) |
| apps/web/src/app/teacher-overview/page.tsx:27 | TeacherOverviewPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/teacher-overview/page.tsx:32 | TeacherOverviewPage | GET or dynamic init | '/api/academies/${current.id}/teachers' |  |
| apps/web/src/app/teacher-overview/page.tsx:33 | TeacherOverviewPage | GET or dynamic init | '/api/academies/${current.id}/sessions' |  |
| apps/web/src/app/teacher-overview/page.tsx:34 | TeacherOverviewPage | GET or dynamic init | '/api/academies/${current.id}/leave-requests' |  |
| apps/web/src/app/teacher-overview/page.tsx:35 | TeacherOverviewPage | GET or dynamic init | '/api/academies/${current.id}/batches' |  |
| apps/web/src/app/teacher-overview/page.tsx:36 | TeacherOverviewPage | GET or dynamic init | '/api/academies/${current.id}/payroll/payouts' |  |
| apps/web/src/app/teacher-payments/page.tsx:21 | loadCompensation | GET or dynamic init | '/api/academies/${academyId}/teachers/${id}/compensation' |  |
| apps/web/src/app/teacher-payments/page.tsx:26 | TeacherPaymentsPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/teacher-payments/page.tsx:26 | TeacherPaymentsPage | GET or dynamic init | '/api/academies/${current.id}/teachers' |  |
| apps/web/src/app/teacher-payments/page.tsx:29 | save | PUT | '/api/academies/${academy.id}/teachers/${teacherId}/compensation' | JSON.stringify(payload) |
| apps/web/src/app/teacher-profile/page.tsx:69 | load | GET or dynamic init | '/api/academies/${academyId}/teachers/${id}/profile' |  |
| apps/web/src/app/teacher-profile/page.tsx:81 | TeacherProfilePage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/teacher-profile/page.tsx:89 | TeacherProfilePage | GET or dynamic init | '/api/academies/${academies[0].id}/teachers' |  |
| apps/web/src/app/teacher-profile/page.tsx:120 | save | PUT | '/api/academies/${academy.id}/teachers/${teacherId}/profile' | JSON.stringify({ ...form, dateOfBirth: form.dateOfBirth \|\| null, joiningDate: form.joiningDate \|\| null, }) |
| apps/web/src/app/teachers/page.tsx:55 | load | GET or dynamic init | '/api/academies/${id}/teachers' |  |
| apps/web/src/app/teachers/page.tsx:56 | load | GET or dynamic init | '/api/academies/${id}/branches' |  |
| apps/web/src/app/teachers/page.tsx:57 | load | GET or dynamic init | '/api/academies/${id}/batches' |  |
| apps/web/src/app/teachers/page.tsx:58 | load | GET or dynamic init | '/api/academies/${id}/payroll/payouts' |  |
| apps/web/src/app/teachers/page.tsx:74 | initialise | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/teachers/page.tsx:109 | saveTeacher | PUT | '/api/academies/${academy.id}/teachers/${teacher.id}' | JSON.stringify({ firstName: editFirstName, lastName: editLastName, email: editEmail \|\| null, phone: editPhone \|\| null, specialties: editSpecialties \|\| null, branchId: editBranchId \|\| null, isActive: teacher.isActive, }) |
| apps/web/src/app/teachers/page.tsx:134 | toggleActive | PUT | '/api/academies/${academy.id}/teachers/${teacher.id}' | JSON.stringify({ firstName: teacher.firstName, lastName: teacher.lastName, email: teacher.email, phone: teacher.phone, specialties: teacher.specialties, branchId: teacher.branchId, isActive: !teacher.isActive, }) |
| apps/web/src/app/teachers/page.tsx:165 | assignBatch | PUT | '/api/academies/${academy.id}/batches/${batch.id}' | JSON.stringify({ name: batch.name, batchCode: batch.batchCode ?? null, courseId: batch.courseId, teacherId: assignmentTeacherId, branchId: batch.branchId ?? null, capacity: batch.capacity, waitlistCapacity: batch.waitlistCapacity ?? 0, deliveryMode: batch.deliveryMode ?? "InPerson", meetingPattern: batch.meetingPattern ?? null, roomName: batch.roomName ?? null, enrollmentStatus: batch.enrollmentStatus ?? "Open", adminNotes: batch.adminNotes ?? null, startDate: batch.startDate ?? null, endDate: batch.endDate ?? null, isActive: batch.isActive, }) |
| apps/web/src/app/trial-bookings/page.tsx:42 | load | GET or dynamic init | '/api/academies/${id}/leads' |  |
| apps/web/src/app/trial-bookings/page.tsx:43 | load | GET or dynamic init | '/api/academies/${id}/teachers' |  |
| apps/web/src/app/trial-bookings/page.tsx:44 | load | GET or dynamic init | '/api/academies/${id}/sales-marketing/trials' |  |
| apps/web/src/app/trial-bookings/page.tsx:60 | TrialBookingsPage | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/trial-bookings/page.tsx:77 | create | POST | '/api/academies/${academy.id}/sales-marketing/trials' | JSON.stringify({ leadId, teacherId: teacherId \|\| null, scheduledAtUtc: new Date( '${scheduledDate}T${scheduledTime}:00', ).toISOString(), notes: notes \|\| null, }) |
| apps/web/src/app/trial-bookings/page.tsx:103 | update | PATCH | '/api/academies/${academy.id}/sales-marketing/trials/${trial.id}/status' | JSON.stringify({ status }) |
| apps/web/src/app/work-queue/page.tsx:11 | load | GET or dynamic init | "/api/academies" |  |
| apps/web/src/app/work-queue/page.tsx:11 | load | GET or dynamic init | '/api/academies/${academies[0].id}/admin-work-items${status === "All" ? "" : '?status=${status}'}' |  |
| apps/web/src/app/work-queue/page.tsx:11 | load | GET or dynamic init | '/api/academies/${academies[0].id}/staff' |  |
| apps/web/src/app/work-queue/page.tsx:13 | create | POST | '/api/academies/${academy.id}/admin-work-items' | JSON.stringify({ type, title: form.get("title"), description: form.get("description") \|\| null, priority, entityType: null, entityId: null, assignedUserId: assignedUserId \|\| null, dueAtUtc }) |
| apps/web/src/app/work-queue/page.tsx:14 | move | PATCH | '/api/academies/${academy.id}/admin-work-items/${item.id}/status' | JSON.stringify({ status }) |
| apps/web/src/components/enterprise-shell.tsx:247 | EnterpriseShell | GET or dynamic init | "/api/academies" |  |
| apps/web/src/components/enterprise-shell.tsx:248 | EnterpriseShell | GET or dynamic init | "/api/auth/session" |  |
| apps/web/src/components/enterprise-shell.tsx:263 | loadAnnouncements | GET or dynamic init | "/api/portal/announcements" |  |
| apps/web/src/components/enterprise-shell.tsx:371 | uploadProfileImage | POST | "/api/auth/session/profile-image" |  |
| apps/web/src/components/student-admin-profile.tsx:61 | save | PUT | '/api/academies/${academyId}/students/${studentId}/profile' | JSON.stringify({ ...form, dateOfBirth: form.dateOfBirth \|\| null, admissionDate: form.admissionDate \|\| null, }) |
| apps/web/src/components/student-fee-arrangements.tsx:29 | load | GET or dynamic init | '/api/academies/${academyId}/students/${studentId}/fee-arrangements' |  |
| apps/web/src/components/student-fee-arrangements.tsx:39 | add | POST | '/api/academies/${academyId}/students/${studentId}/fee-arrangements' | JSON.stringify({ subjectName, amount: Number(amount), frequency, effectiveFrom: new Date().toISOString().slice(0, 10), }) |
| apps/web/src/components/workspace-nav.tsx:25 | WorkspaceNav | GET or dynamic init | "/api/portal/announcements" |  |
| apps/web/src/lib/api.ts:60 | request | GET or dynamic init | '${apiUrl}${path}' |  |
| apps/web/src/lib/api.ts:70 | academyApi | POST | '${apiUrl}/api/auth/refresh' | JSON.stringify({ refreshToken }) |
