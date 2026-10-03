# Controller action inventory

Attribute routes and action signatures extracted from source; Identity mapped endpoints, health and development OpenAPI are recorded separately in the main inventory. Validations below are candidate branches, not test results. Last-action chunks can include private helpers/records; consult source anchors.

| Source | Method | Route | Action / request signature | Global academy gate | Permission catalog | Data sets | Save calls |
| --- | --- | --- | --- | --- | --- | --- | --- |
| apps/api/Controllers/AcademicGovernanceController.cs:3 | GET | /api/academies/{academyId:guid}/academic-governance/grading-schemes | Task<ActionResult> Schemes(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | academics.manage | GradingSchemes | 0 |
| apps/api/Controllers/AcademicGovernanceController.cs:3 | POST | /api/academies/{academyId:guid}/academic-governance/grading-schemes | Task<ActionResult> AddScheme(Guid academyId,GradingSchemeRequest r,CancellationToken t) | AcademyAccessFilter + action checks | academics.manage | GradingSchemes | 1 |
| apps/api/Controllers/AcademicGovernanceController.cs:3 | GET | /api/academies/{academyId:guid}/academic-governance/prerequisites | Task<ActionResult> Prerequisites(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | academics.manage | CoursePrerequisites | 0 |
| apps/api/Controllers/AcademicGovernanceController.cs:3 | POST | /api/academies/{academyId:guid}/academic-governance/prerequisites | Task<ActionResult> AddPrerequisite(Guid academyId,PrerequisiteRequest r,CancellationToken t) | AcademyAccessFilter + action checks | academics.manage | Courses; CoursePrerequisites | 1 |
| apps/api/Controllers/AcademicPeriodsController.cs:12 | GET | /api/academies/{academyId:guid}/academic-periods | Task<ActionResult<AcademicPeriodsSummary>> List(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | academics.manage | AcademicYears; AcademicTerms | 0 |
| apps/api/Controllers/AcademicPeriodsController.cs:20 | POST | /api/academies/{academyId:guid}/academic-periods/years | Task<ActionResult<AcademicYearSummary>> CreateYear(Guid academyId, AcademicYearRequest request, CancellationToken token) | AcademyAccessFilter + action checks | academics.manage | AcademicYears | 1 |
| apps/api/Controllers/AcademicPeriodsController.cs:29 | POST | /api/academies/{academyId:guid}/academic-periods/terms | Task<ActionResult<AcademicTermSummary>> CreateTerm(Guid academyId, AcademicTermRequest request, CancellationToken token) | AcademyAccessFilter + action checks | academics.manage | AcademicYears; AcademicTerms | 1 |
| apps/api/Controllers/AcademicPeriodsController.cs:38 | PATCH | /api/academies/{academyId:guid}/academic-periods/years/{yearId:guid}/close | Task<ActionResult> CloseYear(Guid academyId, Guid yearId, CancellationToken token) | AcademyAccessFilter + action checks | academics.manage | AcademicYears; AcademicTerms | 1 |
| apps/api/Controllers/AcademicPeriodsController.cs:47 | PATCH | /api/academies/{academyId:guid}/academic-periods/terms/{termId:guid}/close | Task<ActionResult> CloseTerm(Guid academyId, Guid termId, CancellationToken token) | AcademyAccessFilter + action checks | academics.manage | AcademicTerms | 1 |
| apps/api/Controllers/AcademiesController.cs:19 | GET | /api/academies | Task<ActionResult<IReadOnlyList<AcademySummary>>> List(CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Academies | 0 |
| apps/api/Controllers/AcademiesController.cs:35 | POST | /api/academies | Task<ActionResult<AcademySummary>> Create( CreateAcademyRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Academies | 1 |
| apps/api/Controllers/AcademiesController.cs:83 | PUT | /api/academies/{academyId:guid} | Task<ActionResult<AcademySummary>> Update(Guid academyId, UpdateAcademyRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Academies | 1 |
| apps/api/Controllers/AcademiesController.cs:86 | PATCH | /api/academies/{academyId:guid}/active | Task<ActionResult> SetActive(Guid academyId, SetAcademyActiveRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Academies | 1 |
| apps/api/Controllers/AcademyExportsController.cs:11 | GET | /api/academies/{academyId:guid}/exports/{resource} | Task<ActionResult> Export(Guid academyId, string resource, CancellationToken token) | AcademyAccessFilter + action checks | reports.export | Students; Guardians; Enrollments; Invoices; Payments | 0 |
| apps/api/Controllers/AcademyPlatformServicesController.cs:14 | GET | /api/academies/{academyId:guid}/platform-services/billing-invoices | Task<ActionResult> BillingInvoices(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | PlatformBillingInvoices | 0 |
| apps/api/Controllers/AcademyPlatformServicesController.cs:21 | POST | /api/academies/{academyId:guid}/platform-services/billing-invoices/{invoiceId:guid}/payment-submission | Task<ActionResult> SubmitPayment(Guid academyId, Guid invoiceId, SubmitPlatformPaymentRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | PlatformBillingInvoices | 1 |
| apps/api/Controllers/AcademyPlatformServicesController.cs:35 | GET | /api/academies/{academyId:guid}/platform-services/support-cases | Task<ActionResult> SupportCases(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | PlatformSupportCases | 0 |
| apps/api/Controllers/AcademyPlatformServicesController.cs:42 | POST | /api/academies/{academyId:guid}/platform-services/support-cases | Task<ActionResult> CreateSupportCase(Guid academyId, CreateAcademySupportCaseRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | PlatformSupportCases | 1 |
| apps/api/Controllers/AcademyPlatformServicesController.cs:52 | POST | /api/academies/{academyId:guid}/platform-services/support-cases/{caseId:guid}/response | Task<ActionResult> RespondToSupportCase(Guid academyId, Guid caseId, RespondToSupportCaseRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | PlatformSupportCases | 1 |
| apps/api/Controllers/AcademyRolesController.cs:14 | GET | /api/academies/{academyId:guid}/roles | Task<ActionResult> List(Guid academyId) | AcademyAccessFilter + action checks | workforce.manage |  | 0 |
| apps/api/Controllers/AcademyRolesController.cs:24 | POST | /api/academies/{academyId:guid}/roles | Task<ActionResult> Create(Guid academyId, CreateAcademyRoleRequest request) | AcademyAccessFilter + action checks | workforce.manage |  | 0 |
| apps/api/Controllers/AcademyRolesController.cs:36 | PUT | /api/academies/{academyId:guid}/roles/staff/{staffId:guid}/assignment | Task<ActionResult> Assign(Guid academyId, Guid staffId, RoleAssignmentRequest request) | AcademyAccessFilter + action checks | workforce.manage |  | 0 |
| apps/api/Controllers/AccessGrantsController.cs:16 | GET | /api/academies/{academyId:guid}/access-grants | Task<ActionResult> List(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | workforce.manage | AccessGrants | 0 |
| apps/api/Controllers/AccessGrantsController.cs:33 | POST | /api/academies/{academyId:guid}/access-grants | Task<ActionResult> Create(Guid academyId, CreateAccessGrantRequest request, CancellationToken token) | AcademyAccessFilter + action checks | workforce.manage | AccessGrants | 1 |
| apps/api/Controllers/AccessGrantsController.cs:51 | PATCH | /api/academies/{academyId:guid}/access-grants/{grantId:guid}/revoke | Task<ActionResult> Revoke(Guid academyId, Guid grantId, CancellationToken token) | AcademyAccessFilter + action checks | workforce.manage | AccessGrants | 1 |
| apps/api/Controllers/AccessReviewsController.cs:3 | GET | /api/academies/{academyId:guid}/access-reviews | Task<ActionResult> List(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | AccessReviews | 0 |
| apps/api/Controllers/AccessReviewsController.cs:3 | POST | /api/academies/{academyId:guid}/access-reviews | Task<ActionResult> SignOff(Guid academyId,AccessReviewRequest r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | AccessReviews; AdminWorkItems | 1 |
| apps/api/Controllers/AdminIntelligenceController.cs:10 | GET | /api/academies/{academyId:guid}/admin-intelligence | Task<ActionResult> Get(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Enrollments; Batches; Invoices; ClassSessions; Teachers; AttendanceRecords | 0 |
| apps/api/Controllers/AdminWorkItemsController.cs:5 | GET | /api/academies/{academyId:guid}/admin-work-items | Task<ActionResult> List(Guid academyId,string? status,string? type,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | AdminWorkItems | 0 |
| apps/api/Controllers/AdminWorkItemsController.cs:6 | POST | /api/academies/{academyId:guid}/admin-work-items | Task<ActionResult> Create(Guid academyId,CreateWorkItem r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | AdminWorkItems | 1 |
| apps/api/Controllers/AdminWorkItemsController.cs:7 | PATCH | /api/academies/{academyId:guid}/admin-work-items/{id:guid}/status | Task<ActionResult> Status(Guid academyId,Guid id,WorkStatus r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | AdminWorkItems | 1 |
| apps/api/Controllers/AdminWorkItemsController.cs:8 | PATCH | /api/academies/{academyId:guid}/admin-work-items/{id:guid}/collections | Task<ActionResult> CollectionState(Guid academyId,Guid id,CollectionStateRequest r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | AdminWorkItems | 1 |
| apps/api/Controllers/AssessmentsController.cs:12 | GET | /api/academies/{academyId:guid}/assessments | Task<ActionResult<IReadOnlyList<AssessmentSummary>>> List(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | academics.manage | Assessments | 0 |
| apps/api/Controllers/AssessmentsController.cs:15 | POST | /api/academies/{academyId:guid}/assessments | Task<ActionResult<AssessmentSummary>> Create(Guid academyId, CreateAssessmentRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | academics.manage | Batches; GradingSchemes; Assessments | 1 |
| apps/api/Controllers/AssessmentsController.cs:25 | PATCH | /api/academies/{academyId:guid}/assessments/{assessmentId:guid}/publish | Task<ActionResult> Publish(Guid academyId, Guid assessmentId, PublishAssessmentRequest request, CancellationToken token) | AcademyAccessFilter + action checks | academics.manage | Assessments | 1 |
| apps/api/Controllers/AssessmentsController.cs:34 | GET | /api/academies/{academyId:guid}/assessments/{assessmentId:guid}/results | Task<ActionResult<IReadOnlyList<AssessmentResultSummary>>> List(Guid academyId, Guid assessmentId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | AssessmentResults | 0 |
| apps/api/Controllers/AssessmentsController.cs:37 | POST | /api/academies/{academyId:guid}/assessments/{assessmentId:guid}/results | Task<ActionResult<AssessmentResultSummary>> Upsert(Guid academyId, Guid assessmentId, RecordAssessmentResultRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | Assessments; Students; AssessmentResults; GradingSchemes | 1 |
| apps/api/Controllers/AssignmentsController.cs:12 | GET | /api/academies/{academyId:guid}/assignments | Task<ActionResult<IReadOnlyList<AssignmentSummary>>> List(Guid academyId, Guid? batchId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | Assignments | 0 |
| apps/api/Controllers/AssignmentsController.cs:21 | POST | /api/academies/{academyId:guid}/assignments | Task<ActionResult<AssignmentSummary>> Create(Guid academyId, CreateAssignmentRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | Batches; Assignments | 1 |
| apps/api/Controllers/AssignmentsController.cs:30 | PATCH | /api/academies/{academyId:guid}/assignments/{assignmentId:guid}/publish | Task<ActionResult> Publish(Guid academyId, Guid assignmentId, PublishAssignmentRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Assignments | 1 |
| apps/api/Controllers/AssignmentSubmissionsController.cs:4 | GET | /api/academies/{academyId:guid}/assignment-submissions | Task<ActionResult<IReadOnlyList<AssignmentSubmission>>>List(Guid academyId,Guid? assignmentId,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | AssignmentSubmissions | 0 |
| apps/api/Controllers/AssignmentSubmissionsController.cs:4 | POST | /api/academies/{academyId:guid}/assignment-submissions | Task<ActionResult<AssignmentSubmission>>Submit(Guid academyId,AssignmentSubmission x,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | Assignments; AssignmentSubmissions | 1 |
| apps/api/Controllers/AssignmentSubmissionsController.cs:4 | PATCH | /api/academies/{academyId:guid}/assignment-submissions/{id:guid}/review | Task<ActionResult<AssignmentSubmission>>Review(Guid academyId,Guid id,ReviewRequest r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | AssignmentSubmissions | 1 |
| apps/api/Controllers/AttendanceController.cs:14 | GET | /api/academies/{academyId:guid}/sessions/{sessionId:guid}/attendance | Task<ActionResult<IReadOnlyList<AttendanceSummary>>> List(Guid academyId, Guid sessionId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | attendance.manage | AttendanceRecords | 0 |
| apps/api/Controllers/AttendanceController.cs:21 | POST | /api/academies/{academyId:guid}/sessions/{sessionId:guid}/attendance | Task<ActionResult<AttendanceSummary>> Mark(Guid academyId, Guid sessionId, MarkAttendanceRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | attendance.manage | ClassSessions; Enrollments; AttendanceRecords | 1 |
| apps/api/Controllers/AuditLogsController.cs:11 | GET | /api/academies/{academyId:guid}/audit-logs | Task<ActionResult<IReadOnlyList<AuditLogSummary>>> List(Guid academyId, string? action, string? entityType, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | AuditLogs | 0 |
| apps/api/Controllers/AuditLogsController.cs:21 | GET | /api/academies/{academyId:guid}/audit-logs/export.csv | Task<IActionResult> ExportCsv(Guid academyId, string? action, string? entityType, DateTime? fromUtc, DateTime? toUtc, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | AuditLogs | 0 |
| apps/api/Controllers/AuthSessionController.cs:13 | GET | /api/auth/session | Task<ActionResult<SessionSummary>> Current(CancellationToken token) | Action/middleware checks | no catalog mapping |  | 0 |
| apps/api/Controllers/AuthSessionController.cs:24 | PUT | /api/auth/session/profile | Task<ActionResult<SessionSummary>> UpdateProfile(UpdateSessionProfileRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping |  | 0 |
| apps/api/Controllers/AuthSessionController.cs:42 | POST | /api/auth/session/change-password | Task<ActionResult> ChangePassword(ChangeSessionPasswordRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping |  | 0 |
| apps/api/Controllers/AuthSessionController.cs:55 | POST | /api/auth/session/profile-image | Task<ActionResult<ProfileImageSummary>> UploadProfileImage(IFormFile image, CancellationToken token) | Action/middleware checks | no catalog mapping |  | 0 |
| apps/api/Controllers/BatchesController.cs:13 | GET | /api/academies/{academyId:guid}/batches | Task<ActionResult<IReadOnlyList<BatchSummary>>> List(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | batches.manage | Batches; Enrollments | 0 |
| apps/api/Controllers/BatchesController.cs:22 | POST | /api/academies/{academyId:guid}/batches | Task<ActionResult<BatchSummary>> Create(Guid academyId, CreateBatchRequest request, CancellationToken token) | AcademyAccessFilter + action checks | batches.manage | Academies; Batches | 1 |
| apps/api/Controllers/BatchesController.cs:30 | PUT | /api/academies/{academyId:guid}/batches/{batchId:guid} | Task<ActionResult<BatchSummary>> Update(Guid academyId, Guid batchId, UpdateBatchRequest request, CancellationToken token) | AcademyAccessFilter + action checks | batches.manage | Batches; Enrollments; ClassSessions; Courses; Branches; Teachers | 1 |
| apps/api/Controllers/BatchPromotionsController.cs:3 | GET | /api/academies/{academyId:guid}/batch-promotions | Task<ActionResult> List(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | academics.manage | BatchPromotions | 0 |
| apps/api/Controllers/BatchPromotionsController.cs:3 | POST | /api/academies/{academyId:guid}/batch-promotions | Task<ActionResult> Create(Guid academyId,PromotionRequest r,CancellationToken t) | AcademyAccessFilter + action checks | academics.manage | Enrollments; Batches; BatchPromotions | 1 |
| apps/api/Controllers/BatchPromotionsController.cs:3 | PATCH | /api/academies/{academyId:guid}/batch-promotions/{id:guid}/decision | Task<ActionResult> Decide(Guid academyId,Guid id,PromotionDecision r,CancellationToken t) | AcademyAccessFilter + action checks | academics.manage | BatchPromotions; Enrollments | 1 |
| apps/api/Controllers/BranchesController.cs:12 | GET | /api/academies/{academyId:guid}/branches | Task<ActionResult<IReadOnlyList<BranchSummary>>> List( Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | Academies; Branches | 0 |
| apps/api/Controllers/BranchesController.cs:33 | POST | /api/academies/{academyId:guid}/branches | Task<ActionResult<BranchSummary>> Create( Guid academyId, CreateBranchRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | Academies; Branches | 1 |
| apps/api/Controllers/BranchesController.cs:72 | PUT | /api/academies/{academyId:guid}/branches/{branchId:guid} | Task<ActionResult<BranchSummary>> Update(Guid academyId,Guid branchId,UpdateBranchRequest request,CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Branches | 1 |
| apps/api/Controllers/CertificatesController.cs:24 | GET | /api/academies/{academyId:guid}/certificates | Task<ActionResult<IReadOnlyList<CertificateSummary>>> List(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Certificates | 0 |
| apps/api/Controllers/CertificatesController.cs:29 | GET | /api/academies/{academyId:guid}/certificates/branding | Task<ActionResult<CertificateBranding>> GetBranding(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Academies | 0 |
| apps/api/Controllers/CertificatesController.cs:36 | PUT | /api/academies/{academyId:guid}/certificates/branding | Task<ActionResult<CertificateBranding>> UpdateBranding(Guid academyId, UpdateCertificateBrandingRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Academies | 1 |
| apps/api/Controllers/CertificatesController.cs:47 | POST | /api/academies/{academyId:guid}/certificates/branding/logo | Task<ActionResult<CertificateBranding>> UploadLogo(Guid academyId, [FromForm] IFormFile logo, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Academies | 1 |
| apps/api/Controllers/CertificatesController.cs:71 | POST | /api/academies/{academyId:guid}/certificates | Task<ActionResult<CertificateSummary>> Issue(Guid academyId, IssueCertificateRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Students; Batches; Certificates; AuditLogs | 1 |
| apps/api/Controllers/CertificatesController.cs:94 | PATCH | /api/academies/{academyId:guid}/certificates/{certificateId:guid}/status | Task<ActionResult> UpdateStatus(Guid academyId, Guid certificateId, UpdateCertificateStatusRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Certificates; AuditLogs | 1 |
| apps/api/Controllers/CertificateVerificationController.cs:13 | GET | /api/certificates/verify/{verificationCode} | Task<ActionResult<VerifiedCertificate>> Verify(string verificationCode, CancellationToken token) | Action/middleware checks | no catalog mapping | Certificates; Academies | 0 |
| apps/api/Controllers/ClassSessionsController.cs:12 | GET | /api/academies/{academyId:guid}/sessions | Task<ActionResult<IReadOnlyList<ClassSessionSummary>>> List(Guid academyId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | scheduling.manage | ClassSessions | 0 |
| apps/api/Controllers/ClassSessionsController.cs:22 | POST | /api/academies/{academyId:guid}/sessions | Task<ActionResult<ClassSessionSummary>> Create(Guid academyId, CreateClassSessionRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | scheduling.manage | Batches; Teachers; Branches; ClassSessions | 1 |
| apps/api/Controllers/ClassSessionsController.cs:40 | PUT | /api/academies/{academyId:guid}/sessions/{sessionId:guid} | Task<ActionResult<ClassSessionSummary>> Update(Guid academyId, Guid sessionId, UpdateClassSessionRequest request, CancellationToken token) | AcademyAccessFilter + action checks | scheduling.manage | ClassSessions | 1 |
| apps/api/Controllers/CommunicationPreferencesController.cs:18 | GET | /api/academies/{academyId:guid}/communication-preferences | Task<ActionResult<IReadOnlyList<CommunicationPreferenceSummary>>> List(Guid academyId, string? recipientType, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | CommunicationPreferences | 0 |
| apps/api/Controllers/CommunicationPreferencesController.cs:27 | PUT | /api/academies/{academyId:guid}/communication-preferences/{recipientType}/{recipientId:guid} | Task<ActionResult<CommunicationPreferenceSummary>> Save(Guid academyId, string recipientType, Guid recipientId, SaveCommunicationPreferenceRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | Students; Guardians; CommunicationPreferences | 1 |
| apps/api/Controllers/CommunicationSettingsController.cs:22 | GET | /api/academies/{academyId:guid}/communication-settings | Task<ActionResult<IReadOnlyList<CommunicationChannelSummary>>> List(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | CommunicationChannels | 0 |
| apps/api/Controllers/CommunicationSettingsController.cs:34 | PUT | /api/academies/{academyId:guid}/communication-settings/{channel} | Task<ActionResult<CommunicationChannelSummary>> Save( Guid academyId, string channel, SaveCommunicationChannelRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | CommunicationChannels; AuditLogs | 1 |
| apps/api/Controllers/CommunicationTemplatesController.cs:23 | GET | /api/academies/{academyId:guid}/communication-templates | Task<ActionResult<IReadOnlyList<CommunicationTemplateSummary>>> List(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | CommunicationTemplates | 0 |
| apps/api/Controllers/CommunicationTemplatesController.cs:33 | POST | /api/academies/{academyId:guid}/communication-templates | Task<ActionResult<CommunicationTemplateSummary>> Create(Guid academyId, SaveCommunicationTemplateRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | CommunicationTemplates; AuditLogs | 1 |
| apps/api/Controllers/CommunicationTemplatesController.cs:45 | GET | /api/academies/{academyId:guid}/communication-templates/starter-templates | Task<ActionResult<IReadOnlyList<StarterTemplateDefinition>>> StarterTemplateCatalogue(Guid academyId) | AcademyAccessFilter + action checks | no catalog mapping |  | 0 |
| apps/api/Controllers/CommunicationTemplatesController.cs:52 | POST | /api/academies/{academyId:guid}/communication-templates/starter-templates | Task<ActionResult<StarterTemplateResult>> AddStarterTemplates(Guid academyId, AddStarterTemplatesRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | CommunicationTemplates; AuditLogs | 1 |
| apps/api/Controllers/CommunicationTemplatesController.cs:91 | PUT | /api/academies/{academyId:guid}/communication-templates/{templateId:guid} | Task<ActionResult<CommunicationTemplateSummary>> Update(Guid academyId, Guid templateId, SaveCommunicationTemplateRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | CommunicationTemplates | 1 |
| apps/api/Controllers/ComplianceController.cs:12 | GET | /api/academies/{academyId:guid}/compliance/documents | Task<ActionResult> Documents(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | PersonDocuments | 0 |
| apps/api/Controllers/ComplianceController.cs:17 | POST | /api/academies/{academyId:guid}/compliance/documents/{documentId:guid}/review-task | Task<ActionResult> CreateReviewTask(Guid academyId, Guid documentId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | PersonDocuments; AdminWorkItems | 1 |
| apps/api/Controllers/ComplianceController.cs:27 | POST | /api/academies/{academyId:guid}/compliance/documents | Task<ActionResult> AddDocument(Guid academyId, DocumentRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | PersonDocuments | 1 |
| apps/api/Controllers/ComplianceController.cs:45 | PATCH | /api/academies/{academyId:guid}/compliance/documents/{documentId:guid}/review | Task<ActionResult> ReviewDocument(Guid academyId, Guid documentId, DocumentReviewRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | PersonDocuments | 1 |
| apps/api/Controllers/ComplianceController.cs:58 | GET | /api/academies/{academyId:guid}/compliance/consents | Task<ActionResult> Consents(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | ConsentRecords | 0 |
| apps/api/Controllers/ComplianceController.cs:63 | POST | /api/academies/{academyId:guid}/compliance/consents | Task<ActionResult> AddConsent(Guid academyId, ConsentRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | ConsentRecords | 1 |
| apps/api/Controllers/ComplianceController.cs:79 | PATCH | /api/academies/{academyId:guid}/compliance/consents/{consentId:guid}/withdraw | Task<ActionResult> WithdrawConsent(Guid academyId, Guid consentId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | ConsentRecords | 1 |
| apps/api/Controllers/CourseModulesController.cs:2 | GET | /api/academies/{academyId:guid}/course-modules | Task<ActionResult> List(Guid academyId,Guid? courseId,CancellationToken t) | AcademyAccessFilter + action checks | academics.manage | CourseModules | 0 |
| apps/api/Controllers/CourseModulesController.cs:2 | POST | /api/academies/{academyId:guid}/course-modules | Task<ActionResult> Create(Guid academyId,CreateModule r,CancellationToken t) | AcademyAccessFilter + action checks | academics.manage | Courses; CourseModules | 1 |
| apps/api/Controllers/CourseModulesController.cs:2 | PATCH | /api/academies/{academyId:guid}/course-modules/{moduleId:guid}/publication | Task<ActionResult> SetPublication(Guid academyId,Guid moduleId,PublicationRequest r,CancellationToken t) | AcademyAccessFilter + action checks | academics.manage | CourseModules | 1 |
| apps/api/Controllers/CourseModuleStatusController.cs:11 | PATCH | /api/academies/{academyId:guid}/course-modules/{moduleId:guid}/publish | Task<ActionResult> Publish(Guid academyId, Guid moduleId, PublishModuleRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | CourseModules | 1 |
| apps/api/Controllers/CoursesController.cs:12 | GET | /api/academies/{academyId:guid}/courses | Task<ActionResult<IReadOnlyList<CourseSummary>>> List(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | academics.manage | Courses | 0 |
| apps/api/Controllers/CoursesController.cs:15 | POST | /api/academies/{academyId:guid}/courses | Task<ActionResult<CourseSummary>> Create(Guid academyId, CreateCourseRequest request, CancellationToken token) | AcademyAccessFilter + action checks | academics.manage | Academies; Courses | 1 |
| apps/api/Controllers/CoursesController.cs:26 | PUT | /api/academies/{academyId:guid}/courses/{courseId:guid} | Task<ActionResult<CourseSummary>> Update(Guid academyId, Guid courseId, UpdateCourseRequest request, CancellationToken token) | AcademyAccessFilter + action checks | academics.manage | Courses | 1 |
| apps/api/Controllers/DashboardController.cs:15 | GET | /api/academies/{academyId:guid}/dashboard | Task<ActionResult<DashboardSummary>> Summary(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Academies; Invoices; Payments; AttendanceRecords; ClassSessions; Batches; Teachers; AuditLogs; Students; Courses; Leads | 0 |
| apps/api/Controllers/EnrollmentsController.cs:12 | GET | /api/academies/{academyId:guid}/enrollments | Task<ActionResult<IReadOnlyList<EnrollmentSummary>>> List(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | students.manage | Enrollments | 0 |
| apps/api/Controllers/EnrollmentsController.cs:19 | POST | /api/academies/{academyId:guid}/enrollments | Task<ActionResult<EnrollmentSummary>> Create(Guid academyId, CreateEnrollmentRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | students.manage | Students; Batches; CoursePrerequisites; Enrollments | 1 |
| apps/api/Controllers/EnrollmentsController.cs:42 | PUT | /api/academies/{academyId:guid}/enrollments/{enrollmentId:guid} | Task<ActionResult<EnrollmentSummary>> Update(Guid academyId, Guid enrollmentId, UpdateEnrollmentRequest request, CancellationToken token) | AcademyAccessFilter + action checks | students.manage | Enrollments | 1 |
| apps/api/Controllers/EnrollmentsController.cs:55 | POST | /api/academies/{academyId:guid}/enrollments/{enrollmentId:guid}/transfer | Task<ActionResult<EnrollmentSummary>> Transfer(Guid academyId, Guid enrollmentId, TransferEnrollmentRequest request, CancellationToken token) | AcademyAccessFilter + action checks | students.manage | Enrollments; Batches | 1 |
| apps/api/Controllers/EventsController.cs:12 | GET | /api/academies/{academyId:guid}/events | Task<ActionResult<IReadOnlyList<EventSummary>>> List(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | AcademyEvents | 0 |
| apps/api/Controllers/EventsController.cs:14 | POST | /api/academies/{academyId:guid}/events | Task<ActionResult<EventSummary>> Create(Guid academyId, CreateEventRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Branches; AcademyEvents | 1 |
| apps/api/Controllers/EventsController.cs:22 | PATCH | /api/academies/{academyId:guid}/events/{eventId:guid}/status | Task<ActionResult> UpdateStatus(Guid academyId, Guid eventId, UpdateEventStatusRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | AcademyEvents | 1 |
| apps/api/Controllers/ExpensesController.cs:12 | GET | /api/academies/{academyId:guid}/expenses | Task<ActionResult<IReadOnlyList<ExpenseSummary>>> List(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | finance.manage | Expenses | 0 |
| apps/api/Controllers/ExpensesController.cs:15 | POST | /api/academies/{academyId:guid}/expenses | Task<ActionResult<ExpenseSummary>> Create(Guid academyId, CreateExpenseRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | finance.manage | Academies; Branches; Expenses | 1 |
| apps/api/Controllers/ExpensesController.cs:25 | PUT | /api/academies/{academyId:guid}/expenses/{expenseId:guid} | Task<ActionResult> Update(Guid academyId, Guid expenseId, UpdateExpenseRequest request, CancellationToken token) | AcademyAccessFilter + action checks | finance.manage | Expenses | 1 |
| apps/api/Controllers/FeePlansController.cs:12 | GET | /api/academies/{academyId:guid}/fee-plans | Task<ActionResult<IReadOnlyList<FeePlanSummary>>> List(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | finance.manage | FeePlans | 0 |
| apps/api/Controllers/FeePlansController.cs:15 | POST | /api/academies/{academyId:guid}/fee-plans | Task<ActionResult<FeePlanSummary>> Create(Guid academyId, CreateFeePlanRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | finance.manage | Academies; FeePlans | 1 |
| apps/api/Controllers/FeePlansController.cs:24 | PUT | /api/academies/{academyId:guid}/fee-plans/{feePlanId:guid} | Task<ActionResult> Update(Guid academyId, Guid feePlanId, UpdateFeePlanRequest request, CancellationToken token) | AcademyAccessFilter + action checks | finance.manage | FeePlans | 1 |
| apps/api/Controllers/FeeRemindersController.cs:12 | POST | /api/academies/{academyId:guid}/fee-reminders | Task<ActionResult<FeeReminderResult>> Queue(Guid academyId, QueueFeeReminderRequest request, CancellationToken token) | AcademyAccessFilter + action checks | finance.manage | Invoices; Payments; Notifications; AuditLogs | 1 |
| apps/api/Controllers/FinanceAdjustmentsController.cs:12 | GET | /api/academies/{academyId:guid}/finance-adjustments | Task<ActionResult<IReadOnlyList<FinanceAdjustmentSummary>>> List(Guid academyId, Guid? invoiceId, CancellationToken token) | AcademyAccessFilter + action checks | finance.manage | FinanceAdjustments | 0 |
| apps/api/Controllers/FinanceAdjustmentsController.cs:19 | POST | /api/academies/{academyId:guid}/finance-adjustments | Task<ActionResult<FinanceAdjustmentSummary>> Create(Guid academyId, CreateFinanceAdjustmentRequest request, CancellationToken token) | AcademyAccessFilter + action checks | finance.manage | Invoices; FinanceAdjustments | 1 |
| apps/api/Controllers/FinanceAdjustmentsController.cs:28 | PATCH | /api/academies/{academyId:guid}/finance-adjustments/{adjustmentId:guid}/approval | Task<ActionResult<FinanceAdjustmentSummary>> Decide(Guid academyId, Guid adjustmentId, FinanceAdjustmentDecisionRequest request, CancellationToken token) | AcademyAccessFilter + action checks | finance.manage | FinanceAdjustments; Invoices; Payments | 1 |
| apps/api/Controllers/FinanceGovernanceController.cs:11 | GET | /api/academies/{academyId:guid}/finance-governance/settings | Task<ActionResult<FinanceSettingsSummary>> Get(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | finance.manage | AcademyFinanceSettings | 0 |
| apps/api/Controllers/FinanceGovernanceController.cs:12 | PUT | /api/academies/{academyId:guid}/finance-governance/settings | Task<ActionResult<FinanceSettingsSummary>> Save(Guid academyId,FinanceSettingsRequest r,CancellationToken t) | AcademyAccessFilter + action checks | finance.manage | AcademyFinanceSettings | 1 |
| apps/api/Controllers/FinanceGovernanceController.cs:13 | GET | /api/academies/{academyId:guid}/finance-governance/collections | Task<ActionResult> Collections(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | finance.manage | Invoices | 0 |
| apps/api/Controllers/FinanceGovernanceController.cs:14 | GET | /api/academies/{academyId:guid}/finance-governance/summary | Task<ActionResult> Summary(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | finance.manage | Invoices; Payments | 0 |
| apps/api/Controllers/FinanceGovernanceController.cs:15 | POST | /api/academies/{academyId:guid}/finance-governance/collections/{invoiceId:guid}/follow-up | Task<ActionResult> CreateFollowUp(Guid academyId,Guid invoiceId,CollectionFollowUpRequest r,CancellationToken t) | AcademyAccessFilter + action checks | finance.manage | Invoices; AdminWorkItems | 1 |
| apps/api/Controllers/GradingSchemeLifecycleController.cs:4 | GET | /api/academies/{academyId:guid}/grading-schemes/active | Task<ActionResult> Active(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | GradingSchemes | 0 |
| apps/api/Controllers/GradingSchemeLifecycleController.cs:4 | PATCH | /api/academies/{academyId:guid}/grading-schemes/{schemeId:guid}/status | Task<ActionResult> Status(Guid academyId,Guid schemeId,SchemeStatusRequest r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | GradingSchemes | 1 |
| apps/api/Controllers/GuardiansController.cs:14 | GET | /api/academies/{academyId:guid}/guardians | Task<ActionResult<IReadOnlyList<GuardianSummary>>> List(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | students.manage | Guardians | 0 |
| apps/api/Controllers/GuardiansController.cs:25 | POST | /api/academies/{academyId:guid}/guardians | Task<ActionResult<GuardianSummary>> Create(Guid academyId, CreateGuardianRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | students.manage | Academies; Guardians | 1 |
| apps/api/Controllers/GuardiansController.cs:37 | PUT | /api/academies/{academyId:guid}/guardians/{guardianId:guid} | Task<ActionResult<GuardianSummary>> Update(Guid academyId, Guid guardianId, UpdateGuardianRequest request, CancellationToken token) | AcademyAccessFilter + action checks | students.manage | Guardians | 1 |
| apps/api/Controllers/HolidayDeleteController.cs:11 | DELETE | /api/academies/{academyId:guid}/holidays/{holidayId:guid} | Task<ActionResult> Delete(Guid academyId, Guid holidayId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | AcademyHolidays | 1 |
| apps/api/Controllers/HolidaysController.cs:9 | GET | /api/academies/{academyId:guid}/holidays | Task<ActionResult<IReadOnlyList<AcademyHoliday>>> List(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | AcademyHolidays | 0 |
| apps/api/Controllers/HolidaysController.cs:10 | POST | /api/academies/{academyId:guid}/holidays | Task<ActionResult<AcademyHoliday>> Create(Guid academyId,AcademyHoliday x,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping |  | 1 |
| apps/api/Controllers/HolidaysController.cs:11 | POST | /api/academies/{academyId:guid}/holidays/india-2026-defaults | Task<ActionResult> India(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | AcademyHolidays | 1 |
| apps/api/Controllers/HolidaysController.cs:12 | DELETE | /api/academies/{academyId:guid}/holidays/{id:guid} | Task<ActionResult> Delete(Guid academyId,Guid id,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | AcademyHolidays; Remove | 1 |
| apps/api/Controllers/InvoicesController.cs:12 | GET | /api/academies/{academyId:guid}/invoices | Task<ActionResult<IReadOnlyList<InvoiceSummary>>> List(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | finance.manage | Invoices; Payments | 0 |
| apps/api/Controllers/InvoicesController.cs:15 | POST | /api/academies/{academyId:guid}/invoices | Task<ActionResult<InvoiceSummary>> Create(Guid academyId, CreateInvoiceRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | finance.manage | Students; FeePlans; Invoices | 1 |
| apps/api/Controllers/InvoicesController.cs:29 | PATCH | /api/academies/{academyId:guid}/invoices/{invoiceId:guid}/status | Task<ActionResult> UpdateStatus(Guid academyId, Guid invoiceId, UpdateInvoiceStatusRequest request, CancellationToken token) | AcademyAccessFilter + action checks | finance.manage | Invoices | 1 |
| apps/api/Controllers/LeadsController.cs:14 | GET | /api/academies/{academyId:guid}/leads | Task<ActionResult<IReadOnlyList<LeadSummary>>> List(Guid academyId, string? stage, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | sales.manage | Leads | 0 |
| apps/api/Controllers/LeadsController.cs:24 | POST | /api/academies/{academyId:guid}/leads | Task<ActionResult<LeadSummary>> Create(Guid academyId, CreateLeadRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | sales.manage | Leads | 1 |
| apps/api/Controllers/LeadsController.cs:34 | PATCH | /api/academies/{academyId:guid}/leads/{leadId:guid}/stage | Task<ActionResult<LeadSummary>> UpdateStage(Guid academyId, Guid leadId, UpdateLeadStageRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | sales.manage | Leads | 1 |
| apps/api/Controllers/LeadsController.cs:46 | PATCH | /api/academies/{academyId:guid}/leads/{leadId:guid}/notes | Task<ActionResult<LeadSummary>> UpdateNotes(Guid academyId, Guid leadId, UpdateLeadNotesRequest request, CancellationToken token) | AcademyAccessFilter + action checks | sales.manage | Leads | 1 |
| apps/api/Controllers/LeadsController.cs:50 | POST | /api/academies/{academyId:guid}/leads/{leadId:guid}/convert | Task<ActionResult<LeadConversionSummary>> Convert(Guid academyId, Guid leadId, ConvertLeadRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | sales.manage | Leads; Students; AuditLogs | 1 |
| apps/api/Controllers/LearningResourcesController.cs:6 | GET | /api/academies/{academyId:guid}/resources | Task<ActionResult> List(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | LearningResources | 0 |
| apps/api/Controllers/LearningResourcesController.cs:7 | POST | /api/academies/{academyId:guid}/resources | Task<ActionResult> Create(Guid academyId,CreateResourceRequest r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | LearningResources | 1 |
| apps/api/Controllers/LearningResourcesController.cs:8 | POST | /api/academies/{academyId:guid}/resources/upload | Task<ActionResult> Upload(Guid academyId,[FromForm]UploadResourceRequest r,IWebHostEnvironment environment,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | LearningResources | 1 |
| apps/api/Controllers/LearningResourcesController.cs:9 | PATCH | /api/academies/{academyId:guid}/resources/{resourceId:guid}/publish | Task<ActionResult> Publish(Guid academyId,Guid resourceId,PublishResourceRequest r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | LearningResources; Batches; Courses | 1 |
| apps/api/Controllers/LeaveRequestsController.cs:10 | GET | /api/academies/{academyId:guid}/leave-requests | Task<ActionResult> List(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | LeaveRequests | 0 |
| apps/api/Controllers/LeaveRequestsController.cs:11 | POST | /api/academies/{academyId:guid}/leave-requests | Task<ActionResult> Create(Guid academyId,CreateLeaveRequest r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | Students; Teachers; LeaveRequests | 1 |
| apps/api/Controllers/LeaveRequestsController.cs:12 | PATCH | /api/academies/{academyId:guid}/leave-requests/{id:guid} | Task<ActionResult> Decide(Guid academyId,Guid id,DecideLeaveRequest r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | LeaveRequests | 1 |
| apps/api/Controllers/LessonPlansController.cs:2 | GET | /api/academies/{academyId:guid}/lesson-plans | Task<ActionResult> List(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | LessonPlans | 0 |
| apps/api/Controllers/LessonPlansController.cs:2 | POST | /api/academies/{academyId:guid}/lesson-plans | Task<ActionResult> Create(Guid academyId,CreateLessonPlan r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | Batches; LessonPlans | 1 |
| apps/api/Controllers/LessonPlansController.cs:2 | PATCH | /api/academies/{academyId:guid}/lesson-plans/{id:guid} | Task<ActionResult> Status(Guid academyId,Guid id,UpdateLessonStatus r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | LessonPlans | 1 |
| apps/api/Controllers/MakeupClassesController.cs:12 | GET | /api/academies/{academyId:guid}/makeup-classes | Task<ActionResult> List(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | makeup.manage | MakeupClasses | 0 |
| apps/api/Controllers/MakeupClassesController.cs:15 | POST | /api/academies/{academyId:guid}/makeup-classes | Task<ActionResult> Create(Guid academyId, CreateMakeupRequest request, CancellationToken token) | AcademyAccessFilter + action checks | makeup.manage | Batches; Students; ClassSessions; Teachers; MakeupClasses | 1 |
| apps/api/Controllers/MakeupClassesController.cs:37 | PATCH | /api/academies/{academyId:guid}/makeup-classes/{id:guid} | Task<ActionResult> UpdateStatus(Guid academyId, Guid id, UpdateMakeupRequest request, CancellationToken token) | AcademyAccessFilter + action checks | makeup.manage | MakeupClasses; Notifications; StudentGuardians | 1 |
| apps/api/Controllers/MusicPiecesController.cs:12 | GET | /api/academies/{academyId:guid}/music-pieces | Task<ActionResult<IReadOnlyList<MusicPieceSummary>>> List(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | MusicPieces | 0 |
| apps/api/Controllers/MusicPiecesController.cs:15 | POST | /api/academies/{academyId:guid}/music-pieces | Task<ActionResult<MusicPieceSummary>> Create(Guid academyId, CreateMusicPieceRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | MusicPieces | 1 |
| apps/api/Controllers/MusicPiecesController.cs:23 | PATCH | /api/academies/{academyId:guid}/music-pieces/{pieceId:guid}/active | Task<ActionResult> SetActive(Guid academyId, Guid pieceId, SetMusicPieceActiveRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | MusicPieces | 1 |
| apps/api/Controllers/MusicProgressController.cs:13 | GET | /api/academies/{academyId:guid}/music-progress | Task<ActionResult<IReadOnlyList<MusicProgressSummary>>> List(Guid academyId, Guid? studentId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | StudentMusicProgress | 0 |
| apps/api/Controllers/MusicProgressController.cs:20 | POST | /api/academies/{academyId:guid}/music-progress | Task<ActionResult<MusicProgressSummary>> Assign(Guid academyId, AssignMusicPieceRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Students; MusicPieces; StudentMusicProgress | 1 |
| apps/api/Controllers/MusicProgressController.cs:28 | PATCH | /api/academies/{academyId:guid}/music-progress/{progressId:guid} | Task<ActionResult<MusicProgressSummary>> Update(Guid academyId, Guid progressId, UpdateMusicProgressRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | StudentMusicProgress | 1 |
| apps/api/Controllers/NotificationsController.cs:14 | GET | /api/academies/{academyId:guid}/notifications | Task<ActionResult<IReadOnlyList<NotificationSummary>>> List(Guid academyId, Guid? recipientId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | Notifications | 0 |
| apps/api/Controllers/NotificationsController.cs:22 | POST | /api/academies/{academyId:guid}/notifications | Task<ActionResult<NotificationSummary>> Create(Guid academyId, CreateNotificationRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | Academies; CommunicationTemplates; CommunicationPreferences; CommunicationChannels; Notifications; AuditLogs | 1 |
| apps/api/Controllers/NotificationsController.cs:66 | PATCH | /api/academies/{academyId:guid}/notifications/{notificationId:guid}/status | Task<ActionResult> UpdateStatus(Guid academyId, Guid notificationId, UpdateNotificationStatusRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Notifications | 1 |
| apps/api/Controllers/PaymentsController.cs:12 | GET | /api/academies/{academyId:guid}/payments | Task<ActionResult<IReadOnlyList<PaymentSummary>>> List(Guid academyId, Guid? invoiceId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | finance.manage | Payments | 0 |
| apps/api/Controllers/PaymentsController.cs:20 | POST | /api/academies/{academyId:guid}/payments | Task<ActionResult<PaymentSummary>> Create(Guid academyId, RecordPaymentRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | finance.manage | Invoices; Payments | 1 |
| apps/api/Controllers/PaymentsController.cs:33 | PATCH | /api/academies/{academyId:guid}/payments/{paymentId:guid}/status | Task<ActionResult> UpdateStatus(Guid academyId, Guid paymentId, UpdatePaymentStatusRequest request, CancellationToken token) | AcademyAccessFilter + action checks | finance.manage | Payments | 1 |
| apps/api/Controllers/PaymentsController.cs:36 | PATCH | /api/academies/{academyId:guid}/payments/{paymentId:guid}/reconcile | Task<ActionResult> Reconcile(Guid academyId, Guid paymentId, ReconcilePaymentRequest request, CancellationToken token) | AcademyAccessFilter + action checks | finance.manage | Payments | 1 |
| apps/api/Controllers/PayrollController.cs:12 | GET | /api/academies/{academyId:guid}/payroll/profiles | Task<ActionResult<IReadOnlyList<PayrollProfileSummary>>> Profiles(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | PayrollProfiles | 0 |
| apps/api/Controllers/PayrollController.cs:17 | POST | /api/academies/{academyId:guid}/payroll/profiles | Task<ActionResult<PayrollProfileSummary>> CreateProfile(Guid academyId, SavePayrollProfileRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Teachers; PayrollProfiles | 1 |
| apps/api/Controllers/PayrollController.cs:27 | PUT | /api/academies/{academyId:guid}/payroll/profiles/{profileId:guid} | Task<ActionResult<PayrollProfileSummary>> UpdateProfile(Guid academyId, Guid profileId, SavePayrollProfileRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | PayrollProfiles | 1 |
| apps/api/Controllers/PayrollController.cs:36 | GET | /api/academies/{academyId:guid}/payroll/payouts | Task<ActionResult<IReadOnlyList<PayrollPayoutSummary>>> Payouts(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | PayrollPayouts; PayrollProfiles | 0 |
| apps/api/Controllers/PayrollController.cs:41 | POST | /api/academies/{academyId:guid}/payroll/payouts | Task<ActionResult<PayrollPayoutSummary>> Pay(Guid academyId, CreatePayrollPayoutRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | PayrollProfiles; PayrollPayouts | 1 |
| apps/api/Controllers/PlatformAcademiesController.cs:18 | GET | /api/platform/academies | Task<ActionResult> List(CancellationToken token) | Action/middleware checks | no catalog mapping | Academies; Branches; Students; Teachers | 0 |
| apps/api/Controllers/PlatformAcademiesController.cs:25 | POST | /api/platform/academies | Task<ActionResult> Onboard(OnboardAcademyRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | Academies | 3 |
| apps/api/Controllers/PlatformAcademiesController.cs:47 | PATCH | /api/platform/academies/{academyId:guid}/status | Task<ActionResult> SetStatus(Guid academyId, SetPlatformAcademyStatusRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Academies | 1 |
| apps/api/Controllers/PlatformAcademiesController.cs:59 | PUT | /api/platform/academies/{academyId:guid}/configuration | Task<ActionResult> Configure(Guid academyId, TenantConfigurationRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Academies; PlatformAuditEntries | 1 |
| apps/api/Controllers/PlatformControlController.cs:17 | GET | /api/platform/overview | Task<ActionResult> Overview(CancellationToken token) | Action/middleware checks | no catalog mapping | Academies; PlatformBillingInvoices; Students; PlatformSupportCases; PlatformAuditEntries | 0 |
| apps/api/Controllers/PlatformControlController.cs:38 | POST | /api/platform/announcements | Task<ActionResult> CreateAnnouncement(CreatePlatformAnnouncementRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | Academies; Notifications | 1 |
| apps/api/Controllers/PlatformControlController.cs:48 | GET | /api/platform/announcements | Task<ActionResult> ListAnnouncements(CancellationToken token) | Action/middleware checks | no catalog mapping | Notifications; Academies | 0 |
| apps/api/Controllers/PlatformControlController.cs:70 | GET | /api/platform/academies/{academyId:guid}/onboarding | Task<ActionResult> GetTenantOnboarding(Guid academyId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Academies; TenantOnboardingProfiles | 0 |
| apps/api/Controllers/PlatformControlController.cs:78 | PUT | /api/platform/academies/{academyId:guid}/onboarding | Task<ActionResult> SaveTenantOnboarding(Guid academyId, TenantOnboardingRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Academies; TenantOnboardingProfiles | 1 |
| apps/api/Controllers/PlatformControlController.cs:94 | GET | /api/platform/settings | Task<ActionResult<PlatformSettings>> GetSettings(CancellationToken token) | Action/middleware checks | no catalog mapping |  | 0 |
| apps/api/Controllers/PlatformControlController.cs:101 | PUT | /api/platform/settings | Task<ActionResult<PlatformSettings>> SaveSettings(UpdatePlatformSettingsRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping |  | 1 |
| apps/api/Controllers/PlatformControlController.cs:111 | GET | /api/platform/admins | Task<ActionResult> Admins(CancellationToken token) | Action/middleware checks | no catalog mapping | Academies | 0 |
| apps/api/Controllers/PlatformControlController.cs:120 | PATCH | /api/platform/admins/{userId:guid}/active | Task<ActionResult> SetAdminActive(Guid userId, SetPlatformAdminActiveRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping |  | 1 |
| apps/api/Controllers/PlatformControlController.cs:129 | POST | /api/platform/admins/{userId:guid}/reset-password | Task<ActionResult> ResetAdminPassword(Guid userId, ResetPlatformAdminPasswordRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping |  | 1 |
| apps/api/Controllers/PlatformControlController.cs:139 | GET | /api/platform/support-cases | Task<ActionResult> ListSupportCases(CancellationToken token) | Action/middleware checks | no catalog mapping | PlatformSupportCases; Academies | 0 |
| apps/api/Controllers/PlatformControlController.cs:146 | POST | /api/platform/support-cases | Task<ActionResult> CreateSupportCase(CreatePlatformSupportCaseRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | Academies; PlatformSupportCases | 1 |
| apps/api/Controllers/PlatformControlController.cs:154 | PATCH | /api/platform/support-cases/{caseId:guid} | Task<ActionResult> UpdateSupportCase(Guid caseId, UpdatePlatformSupportCaseRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | PlatformSupportCases | 1 |
| apps/api/Controllers/PlatformControlController.cs:162 | GET | /api/platform/billing-invoices | Task<ActionResult> ListInvoices(CancellationToken token) | Action/middleware checks | no catalog mapping | PlatformBillingInvoices; Academies | 0 |
| apps/api/Controllers/PlatformControlController.cs:169 | POST | /api/platform/billing-invoices | Task<ActionResult> CreateInvoice(CreatePlatformBillingInvoiceRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | Academies; PlatformBillingInvoices | 1 |
| apps/api/Controllers/PlatformControlController.cs:177 | PATCH | /api/platform/billing-invoices/{invoiceId:guid}/status | Task<ActionResult> UpdateInvoiceStatus(Guid invoiceId, UpdatePlatformInvoiceStatusRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | PlatformBillingInvoices | 1 |
| apps/api/Controllers/PlatformControlController.cs:184 | GET | /api/platform/audit | Task<ActionResult> AuditLog(CancellationToken token) | Action/middleware checks | no catalog mapping | PlatformAuditEntries | 0 |
| apps/api/Controllers/PlatformControlController.cs:187 | GET | /api/platform/activity-logs | Task<ActionResult> ActivityLogs(string? scope, DateTime? fromUtc, DateTime? toUtc, CancellationToken token) | Action/middleware checks | no catalog mapping | PlatformAuditEntries; AuditLogs; Academies | 0 |
| apps/api/Controllers/PlatformControlController.cs:209 | DELETE | /api/platform/activity-logs | Task<ActionResult> DeleteActivityLogs(DeleteActivityLogsRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | PlatformAuditEntries; AuditLogs | 1 |
| apps/api/Controllers/PlatformControlController.cs:224 | GET | /api/platform/health | Task<ActionResult> Health(CancellationToken token) | Action/middleware checks | no catalog mapping | PlatformSettings; PlatformAuditEntries | 2 |
| apps/api/Controllers/PortalAccountsController.cs:15 | POST | /api/academies/{academyId:guid}/portal-accounts | Task<ActionResult> Create(Guid academyId, CreatePortalAccountRequest request, CancellationToken token) | AcademyAccessFilter + action checks | workforce.manage | Students; Guardians; Teachers | 0 |
| apps/api/Controllers/PortalController.cs:19 | GET | /api/portal/me | Task<ActionResult> Me(CancellationToken token) | Action/middleware checks | no catalog mapping | Students; Enrollments; Invoices; Guardians; StudentGuardians | 0 |
| apps/api/Controllers/PortalController.cs:42 | POST | /api/portal/change-password | Task<ActionResult> ChangePassword(PortalChangePasswordRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping |  | 0 |
| apps/api/Controllers/PortalController.cs:60 | GET | /api/portal/notifications | Task<ActionResult> Notifications(CancellationToken token) | Action/middleware checks | no catalog mapping | Notifications | 0 |
| apps/api/Controllers/PortalController.cs:76 | GET | /api/portal/announcements | Task<ActionResult<IReadOnlyList<PortalAnnouncementSummary>>> Announcements(CancellationToken token) | Action/middleware checks | no catalog mapping | Notifications | 0 |
| apps/api/Controllers/PortalController.cs:93 | PATCH | /api/portal/notifications/{notificationId:guid}/read | Task<ActionResult> MarkNotificationRead(Guid notificationId, CancellationToken token) | Action/middleware checks | no catalog mapping | Notifications | 1 |
| apps/api/Controllers/PortalController.cs:108 | GET | /api/portal/events | Task<ActionResult> UpcomingEvents(CancellationToken token) | Action/middleware checks | no catalog mapping | AcademyEvents | 0 |
| apps/api/Controllers/PortalController.cs:122 | GET | /api/portal/guardians/{guardianId:guid}/children | Task<ActionResult> GuardianChildren(Guid guardianId, CancellationToken token) | Action/middleware checks | no catalog mapping | StudentGuardians; Students; Enrollments | 0 |
| apps/api/Controllers/PortalController.cs:136 | GET | /api/portal/students/{studentId:guid} | Task<ActionResult<PortalStudentDetails>> Student(Guid studentId, CancellationToken token) | Action/middleware checks | no catalog mapping | Students; Enrollments; Batches; ClassSessions; Assignments; AttendanceRecords; StudentMusicProgress; MusicPieces; LearningResources; PracticeLogs; LessonPlans; CourseModules; Certificates; Invoices; Payments; AssessmentResults; Assessments | 0 |
| apps/api/Controllers/PortalController.cs:228 | POST | /api/portal/students/{studentId:guid}/assignments/{assignmentId:guid}/submit | Task<ActionResult> SubmitAssignment(Guid studentId, Guid assignmentId, [FromForm] PortalSubmissionRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | Assignments; Enrollments; AssignmentSubmissions; Batches; Notifications | 1 |
| apps/api/Controllers/PortalController.cs:264 | GET | /api/portal/students/{studentId:guid}/invoices/{invoiceId:guid}/download | Task<IActionResult> DownloadInvoice(Guid studentId, Guid invoiceId, CancellationToken token) | Action/middleware checks | no catalog mapping | Invoices; Payments | 0 |
| apps/api/Controllers/PortalController.cs:276 | GET | /api/portal/students/{studentId:guid}/certificates/{certificateNumber}/download | Task<IActionResult> DownloadCertificate(Guid studentId, string certificateNumber, CancellationToken token) | Action/middleware checks | no catalog mapping | Certificates | 0 |
| apps/api/Controllers/PortalController.cs:287 | GET | /api/portal/students/{studentId:guid}/leave-requests | Task<ActionResult<IReadOnlyList<PortalLeaveSummary>>> StudentLeaveRequests(Guid studentId, CancellationToken token) | Action/middleware checks | no catalog mapping | LeaveRequests | 0 |
| apps/api/Controllers/PortalController.cs:300 | POST | /api/portal/students/{studentId:guid}/leave-requests | Task<ActionResult<PortalLeaveSummary>> RequestStudentLeave(Guid studentId, PortalLeaveRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | LeaveRequests | 1 |
| apps/api/Controllers/PortalController.cs:321 | POST | /api/portal/students/{studentId:guid}/practice-logs | Task<ActionResult> LogPractice(Guid studentId, PortalPracticeLogRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | PracticeLogs; StudentGuardians; Students | 1 |
| apps/api/Controllers/PortalController.cs:379 | PUT | /api/portal/students/{studentId:guid}/profile | Task<ActionResult> UpdateProfile(Guid studentId, PortalStudentProfileRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | Students | 1 |
| apps/api/Controllers/PortalController.cs:398 | PUT | /api/portal/guardians/{guardianId:guid}/profile | Task<ActionResult> UpdateGuardianProfile(Guid guardianId, PortalGuardianProfileRequest request, CancellationToken token) | Action/middleware checks | no catalog mapping | Guardians | 1 |
| apps/api/Controllers/PracticeLogsController.cs:4 | GET | /api/academies/{academyId:guid}/practice-logs | Task<ActionResult<IReadOnlyList<PracticeLog>>> List(Guid academyId,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | PracticeLogs | 0 |
| apps/api/Controllers/PracticeLogsController.cs:4 | POST | /api/academies/{academyId:guid}/practice-logs | Task<ActionResult<PracticeLog>> Create(Guid academyId,PracticeLog x,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | PracticeLogs | 1 |
| apps/api/Controllers/PracticeLogsController.cs:4 | PATCH | /api/academies/{academyId:guid}/practice-logs/{id:guid}/review | Task<ActionResult<PracticeLog>> Review(Guid academyId,Guid id,ReviewPracticeLogRequest r,CancellationToken t) | AcademyAccessFilter + action checks | no catalog mapping | PracticeLogs | 1 |
| apps/api/Controllers/ProfilesController.cs:12 | GET | /api/academies/{academyId:guid}/guardians/{guardianId:guid}/profile | Task<ActionResult<GuardianProfileSummary>> Guardian(Guid academyId, Guid guardianId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Guardians; StudentGuardians; Students; Invoices; Notifications | 0 |
| apps/api/Controllers/ProfilesController.cs:26 | GET | /api/academies/{academyId:guid}/students/{studentId:guid}/profile | Task<ActionResult<StudentProfileSummary>> Student(Guid academyId, Guid studentId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Students; StudentGuardians; Guardians; Enrollments; Batches; Courses; AttendanceRecords; Invoices; FeePlans; Payments; StudentMusicProgress; MusicPieces; PracticeLogs; Notifications | 0 |
| apps/api/Controllers/ProfilesController.cs:67 | GET | /api/academies/{academyId:guid}/teachers/{teacherId:guid}/profile | Task<ActionResult<TeacherProfileSummary>> Teacher(Guid academyId, Guid teacherId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Teachers; Batches; Courses; ClassSessions; LeaveRequests; Enrollments; Students | 0 |
| apps/api/Controllers/ProfilesController.cs:109 | PUT | /api/academies/{academyId:guid}/students/{studentId:guid}/profile | Task<ActionResult<StudentProfileSummary>> UpdateStudentProfile(Guid academyId, Guid studentId, UpdateStudentAdminProfileRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Students | 1 |
| apps/api/Controllers/ProfilesController.cs:134 | PUT | /api/academies/{academyId:guid}/teachers/{teacherId:guid}/profile | Task<ActionResult<TeacherProfileSummary>> UpdateTeacherProfile(Guid academyId, Guid teacherId, UpdateTeacherAdminProfileRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Teachers | 1 |
| apps/api/Controllers/ProfilesController.cs:161 | PUT | /api/academies/{academyId:guid}/guardians/{guardianId:guid}/profile | Task<ActionResult<GuardianProfileSummary>> UpdateGuardianProfile(Guid academyId, Guid guardianId, UpdateGuardianAdminProfileRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Guardians | 1 |
| apps/api/Controllers/SalesMarketingController.cs:15 | GET | /api/academies/{academyId:guid}/sales-marketing/campaigns | Task<ActionResult> Campaigns(Guid academyId, CancellationToken t) | AcademyAccessFilter + action checks | sales.manage | SalesCampaigns | 0 |
| apps/api/Controllers/SalesMarketingController.cs:19 | POST | /api/academies/{academyId:guid}/sales-marketing/campaigns | Task<ActionResult> CreateCampaign(Guid academyId, CampaignRequest r, CancellationToken t) | AcademyAccessFilter + action checks | sales.manage | SalesCampaigns | 1 |
| apps/api/Controllers/SalesMarketingController.cs:30 | PATCH | /api/academies/{academyId:guid}/sales-marketing/campaigns/{id:guid} | Task<ActionResult> UpdateCampaign(Guid academyId, Guid id, CampaignUpdateRequest r, CancellationToken t) | AcademyAccessFilter + action checks | sales.manage | SalesCampaigns | 1 |
| apps/api/Controllers/SalesMarketingController.cs:41 | GET | /api/academies/{academyId:guid}/sales-marketing/trials | Task<ActionResult> Trials(Guid academyId, CancellationToken t) | AcademyAccessFilter + action checks | sales.manage | TrialClassBookings | 0 |
| apps/api/Controllers/SalesMarketingController.cs:45 | POST | /api/academies/{academyId:guid}/sales-marketing/trials | Task<ActionResult> CreateTrial(Guid academyId, TrialRequest r, CancellationToken t) | AcademyAccessFilter + action checks | sales.manage | Leads; Batches; Teachers; TrialClassBookings | 1 |
| apps/api/Controllers/SalesMarketingController.cs:58 | PATCH | /api/academies/{academyId:guid}/sales-marketing/trials/{id:guid}/status | Task<ActionResult> UpdateTrialStatus(Guid academyId, Guid id, TrialStatusRequest r, CancellationToken t) | AcademyAccessFilter + action checks | sales.manage | TrialClassBookings | 1 |
| apps/api/Controllers/StaffController.cs:17 | GET | /api/academies/{academyId:guid}/staff | Task<ActionResult<IReadOnlyList<StaffAccountSummary>>> List(Guid academyId) | AcademyAccessFilter + action checks | workforce.manage |  | 0 |
| apps/api/Controllers/StaffController.cs:38 | POST | /api/academies/{academyId:guid}/staff | Task<ActionResult<StaffAccountSummary>> Create(Guid academyId, CreateStaffAccountRequest request) | AcademyAccessFilter + action checks | workforce.manage |  | 0 |
| apps/api/Controllers/StaffController.cs:78 | PATCH | /api/academies/{academyId:guid}/staff/{staffId:guid}/status | Task<ActionResult> UpdateStatus(Guid academyId, Guid staffId, StaffStatusRequest request) | AcademyAccessFilter + action checks | workforce.manage |  | 0 |
| apps/api/Controllers/StaffController.cs:91 | PATCH | /api/academies/{academyId:guid}/staff/{staffId:guid}/role | Task<ActionResult> UpdateRole(Guid academyId, Guid staffId, StaffRoleRequest request) | AcademyAccessFilter + action checks | workforce.manage |  | 0 |
| apps/api/Controllers/StaffController.cs:114 | PATCH | /api/academies/{academyId:guid}/staff/{staffId:guid}/password | Task<ActionResult> ResetPassword(Guid academyId, Guid staffId, StaffPasswordRequest request) | AcademyAccessFilter + action checks | workforce.manage |  | 0 |
| apps/api/Controllers/StaffController.cs:128 | POST | /api/academies/{academyId:guid}/staff/{staffId:guid}/offboard | Task<ActionResult> Offboard(Guid academyId, Guid staffId) | AcademyAccessFilter + action checks | workforce.manage |  | 0 |
| apps/api/Controllers/StudentFeeArrangementsController.cs:12 | GET | /api/academies/{academyId:guid}/students/{studentId:guid}/fee-arrangements/admission-fee | Task<ActionResult> AdmissionFee(Guid academyId, Guid studentId, CancellationToken token) | AcademyAccessFilter + action checks | student-fees.manage | Students | 0 |
| apps/api/Controllers/StudentFeeArrangementsController.cs:19 | PUT | /api/academies/{academyId:guid}/students/{studentId:guid}/fee-arrangements/admission-fee | Task<ActionResult> UpdateAdmissionFee(Guid academyId, Guid studentId, AdmissionFeeRequest request, CancellationToken token) | AcademyAccessFilter + action checks | student-fees.manage | Students | 1 |
| apps/api/Controllers/StudentFeeArrangementsController.cs:31 | GET | /api/academies/{academyId:guid}/students/{studentId:guid}/fee-arrangements | Task<ActionResult> List(Guid academyId, Guid studentId, CancellationToken token) | AcademyAccessFilter + action checks | student-fees.manage | StudentFeeArrangements | 0 |
| apps/api/Controllers/StudentFeeArrangementsController.cs:34 | POST | /api/academies/{academyId:guid}/students/{studentId:guid}/fee-arrangements | Task<ActionResult> Create(Guid academyId, Guid studentId, FeeArrangementRequest request, CancellationToken token) | AcademyAccessFilter + action checks | student-fees.manage | Students; Courses; StudentFeeArrangements | 1 |
| apps/api/Controllers/StudentGuardiansController.cs:12 | GET | /api/academies/{academyId:guid}/students/{studentId:guid}/guardians | Task<ActionResult<IReadOnlyList<StudentGuardianSummary>>> List(Guid academyId, Guid studentId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | students.manage | StudentGuardians | 0 |
| apps/api/Controllers/StudentGuardiansController.cs:25 | POST | /api/academies/{academyId:guid}/students/{studentId:guid}/guardians | Task<ActionResult<StudentGuardianSummary>> Link(Guid academyId, Guid studentId, LinkGuardianRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | students.manage | Students; Guardians; StudentGuardians | 1 |
| apps/api/Controllers/StudentGuardiansController.cs:48 | PATCH | /api/academies/{academyId:guid}/students/{studentId:guid}/guardians/{guardianId:guid}/portal-access | Task<ActionResult<StudentGuardianSummary>> SetPortalAccess(Guid academyId, Guid studentId, Guid guardianId, ParentPortalAccessRequest request, CancellationToken token) | AcademyAccessFilter + action checks | students.manage | StudentGuardians | 1 |
| apps/api/Controllers/StudentGuardiansController.cs:67 | DELETE | /api/academies/{academyId:guid}/students/{studentId:guid}/guardians/{guardianId:guid} | Task<ActionResult> Unlink(Guid academyId, Guid studentId, Guid guardianId, CancellationToken token) | AcademyAccessFilter + action checks | students.manage | StudentGuardians | 1 |
| apps/api/Controllers/StudentImportsController.cs:11 | POST | /api/academies/{academyId:guid}/imports/students/validate | ActionResult Validate(Guid academyId, StudentImportRequest request) | AcademyAccessFilter + action checks | no catalog mapping |  | 0 |
| apps/api/Controllers/StudentImportsController.cs:13 | POST | /api/academies/{academyId:guid}/imports/students | Task<ActionResult> Import(Guid academyId, StudentImportRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Students | 1 |
| apps/api/Controllers/StudentOnboardingController.cs:16 | POST | /api/academies/{academyId:guid}/student-onboarding | Task<ActionResult> Create(Guid academyId, StudentOnboardingRequest request, CancellationToken token) | AcademyAccessFilter + action checks | students.onboard | Students; Guardians; StudentGuardians | 2 |
| apps/api/Controllers/StudentsController.cs:14 | GET | /api/academies/{academyId:guid}/students/overview | Task<ActionResult<StudentOverviewSummary>> Overview(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | students.manage | Students; Invoices; StudentFeeArrangements | 0 |
| apps/api/Controllers/StudentsController.cs:32 | GET | /api/academies/{academyId:guid}/students | Task<ActionResult<IReadOnlyList<StudentSummary>>> List(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | students.manage | Students | 0 |
| apps/api/Controllers/StudentsController.cs:43 | POST | /api/academies/{academyId:guid}/students | Task<ActionResult<StudentSummary>> Create(Guid academyId, CreateStudentRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | students.manage | Academies; Branches; Students | 1 |
| apps/api/Controllers/StudentsController.cs:67 | PUT | /api/academies/{academyId:guid}/students/{studentId:guid} | Task<ActionResult<StudentSummary>> Update(Guid academyId, Guid studentId, UpdateStudentRequest request, CancellationToken token) | AcademyAccessFilter + action checks | students.manage | Students | 1 |
| apps/api/Controllers/TeacherCompensationController.cs:12 | GET | /api/academies/{academyId:guid}/teachers/{teacherId:guid}/compensation | Task<ActionResult<TeacherCompensationSummary>> Get(Guid academyId, Guid teacherId, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Teachers | 0 |
| apps/api/Controllers/TeacherCompensationController.cs:19 | PUT | /api/academies/{academyId:guid}/teachers/{teacherId:guid}/compensation | Task<ActionResult<TeacherCompensationSummary>> Save(Guid academyId, Guid teacherId, UpdateTeacherCompensationRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Teachers | 1 |
| apps/api/Controllers/TeacherPortalController.cs:21 | GET | /api/teacher/me | Task<ActionResult<TeacherPortalSummary>> Me(CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Teachers; Batches; ClassSessions | 0 |
| apps/api/Controllers/TeacherPortalController.cs:47 | GET | /api/teacher/calendar | Task<ActionResult<TeacherCalendarSummary>> Calendar(int year, int month, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | ClassSessions; AcademyHolidays | 0 |
| apps/api/Controllers/TeacherPortalController.cs:67 | GET | /api/teacher/batch-progress | Task<ActionResult<IReadOnlyList<TeacherBatchProgressSummary>>> BatchProgress(CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Batches; ClassSessions; PayrollProfiles | 0 |
| apps/api/Controllers/TeacherPortalController.cs:105 | GET | /api/teacher/profile | Task<ActionResult<TeacherPortalProfileSummary>> Profile(CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Teachers | 0 |
| apps/api/Controllers/TeacherPortalController.cs:115 | PUT | /api/teacher/profile | Task<ActionResult> UpdateProfile(TeacherPortalProfileRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Teachers | 1 |
| apps/api/Controllers/TeacherPortalController.cs:145 | GET | /api/teacher/leave-requests | Task<ActionResult<IReadOnlyList<TeacherLeaveSummary>>> LeaveRequests(CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | LeaveRequests | 0 |
| apps/api/Controllers/TeacherPortalController.cs:158 | POST | /api/teacher/leave-requests | Task<ActionResult<TeacherLeaveSummary>> RequestLeave(TeacherLeaveRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | LeaveRequests | 1 |
| apps/api/Controllers/TeacherPortalController.cs:179 | GET | /api/teacher/practice-logs | Task<ActionResult<IReadOnlyList<TeacherPracticeLogSummary>>> PracticeLogs(CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Enrollments; Batches; PracticeLogs; Students | 0 |
| apps/api/Controllers/TeacherPortalController.cs:196 | PATCH | /api/teacher/practice-logs/{practiceLogId:guid}/review | Task<ActionResult> ReviewPracticeLog(Guid practiceLogId, TeacherPracticeReviewRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | PracticeLogs; Enrollments; Batches | 1 |
| apps/api/Controllers/TeacherPortalController.cs:213 | GET | /api/teacher/assignments | Task<ActionResult<IReadOnlyList<TeacherAssignmentSummary>>> Assignments(CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Assignments; Batches | 0 |
| apps/api/Controllers/TeacherPortalController.cs:226 | POST | /api/teacher/assignments | Task<ActionResult<TeacherAssignmentSummary>> CreateAssignment(TeacherCreateAssignmentRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Enrollments; Assignments | 1 |
| apps/api/Controllers/TeacherPortalController.cs:253 | PATCH | /api/teacher/assignments/{assignmentId:guid}/publish | Task<ActionResult> PublishAssignment(Guid assignmentId, TeacherPublishAssignmentRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Assignments | 1 |
| apps/api/Controllers/TeacherPortalController.cs:267 | GET | /api/teacher/assignments/{assignmentId:guid}/submissions | Task<ActionResult<IReadOnlyList<TeacherSubmissionSummary>>> Submissions(Guid assignmentId, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Assignments; AssignmentSubmissions; Students | 0 |
| apps/api/Controllers/TeacherPortalController.cs:282 | PATCH | /api/teacher/submissions/{submissionId:guid}/review | Task<ActionResult> ReviewSubmission(Guid submissionId, TeacherSubmissionReviewRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | AssignmentSubmissions; Assignments | 1 |
| apps/api/Controllers/TeacherPortalController.cs:297 | GET | /api/teacher/lesson-plans | Task<ActionResult<IReadOnlyList<TeacherLessonPlanSummary>>> LessonPlans(CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | LessonPlans; Batches | 0 |
| apps/api/Controllers/TeacherPortalController.cs:310 | POST | /api/teacher/lesson-plans | Task<ActionResult<TeacherLessonPlanSummary>> CreateLessonPlan(TeacherCreateLessonPlanRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | LessonPlans | 1 |
| apps/api/Controllers/TeacherPortalController.cs:323 | PATCH | /api/teacher/lesson-plans/{lessonPlanId:guid}/status | Task<ActionResult> UpdateLessonPlanStatus(Guid lessonPlanId, TeacherLessonPlanStatusRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | LessonPlans | 1 |
| apps/api/Controllers/TeacherPortalController.cs:336 | GET | /api/teacher/assessments | Task<ActionResult<IReadOnlyList<TeacherAssessmentSummary>>> Assessments(CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Assessments; Batches | 0 |
| apps/api/Controllers/TeacherPortalController.cs:349 | POST | /api/teacher/assessments | Task<ActionResult<TeacherAssessmentSummary>> CreateAssessment(TeacherCreateAssessmentRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Assessments | 1 |
| apps/api/Controllers/TeacherPortalController.cs:371 | PATCH | /api/teacher/assessments/{assessmentId:guid}/publish | Task<ActionResult> PublishAssessment(Guid assessmentId, TeacherPublishAssessmentRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Assessments | 1 |
| apps/api/Controllers/TeacherPortalController.cs:383 | GET | /api/teacher/assessments/{assessmentId:guid}/results | Task<ActionResult<IReadOnlyList<TeacherAssessmentResultSummary>>> AssessmentResults(Guid assessmentId, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Assessments; AssessmentResults; Students | 0 |
| apps/api/Controllers/TeacherPortalController.cs:398 | POST | /api/teacher/assessments/{assessmentId:guid}/results | Task<ActionResult<TeacherAssessmentResultSummary>> RecordAssessmentResult(Guid assessmentId, TeacherRecordAssessmentResultRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Assessments; Enrollments; AssessmentResults; Notifications; Students | 1 |
| apps/api/Controllers/TeacherPortalController.cs:420 | GET | /api/teacher/resources | Task<ActionResult<IReadOnlyList<TeacherResourceSummary>>> Resources(Guid? batchId, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | LearningResources; Batches | 0 |
| apps/api/Controllers/TeacherPortalController.cs:433 | GET | /api/teacher/classroom-activity | Task<ActionResult<TeacherClassroomActivitySummary>> ClassroomActivity(Guid batchId, Guid? studentId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Enrollments; LearningResources; Assignments | 0 |
| apps/api/Controllers/TeacherPortalController.cs:451 | POST | /api/teacher/resources/note | Task<ActionResult<TeacherResourceSummary>> CreateClassNote(TeacherCreateResourceNoteRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Enrollments; LearningResources | 1 |
| apps/api/Controllers/TeacherPortalController.cs:466 | POST | /api/teacher/resources/upload | Task<ActionResult<TeacherResourceSummary>> UploadClassMaterial([FromForm] TeacherUploadResourceRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Enrollments; LearningResources | 1 |
| apps/api/Controllers/TeacherPortalController.cs:490 | GET | /api/teacher/progress | Task<ActionResult<TeacherProgressSummary>> Progress(CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | ClassSessions; AttendanceRecords | 0 |
| apps/api/Controllers/TeacherPortalController.cs:503 | GET | /api/teacher/payments | Task<ActionResult<TeacherPaymentSummary>> Payments(int? year, int? month, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | PayrollProfiles; PayrollPayouts; Batches; Enrollments; Notifications | 0 |
| apps/api/Controllers/TeacherPortalController.cs:532 | GET | /api/teacher/sessions/{sessionId:guid}/roster | Task<ActionResult<IReadOnlyList<TeacherRosterStudent>>> Roster(Guid sessionId, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Enrollments; Students | 0 |
| apps/api/Controllers/TeacherPortalController.cs:548 | POST | /api/teacher/sessions/{sessionId:guid}/attendance | Task<ActionResult<TeacherAttendanceSummary>> MarkAttendance( Guid sessionId, TeacherMarkAttendanceRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Enrollments; AttendanceRecords; Notifications | 1 |
| apps/api/Controllers/TeacherPortalController.cs:584 | GET | /api/teacher/sessions/{sessionId:guid}/attendance | Task<ActionResult<IReadOnlyList<TeacherAttendanceSummary>>> Attendance(Guid sessionId, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | AttendanceRecords | 0 |
| apps/api/Controllers/TeacherPortalController.cs:597 | PATCH | /api/teacher/sessions/{sessionId:guid}/status | Task<ActionResult> UpdateSessionStatus(Guid sessionId, TeacherSessionStatusRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping |  | 1 |
| apps/api/Controllers/TeacherPortalController.cs:609 | POST | /api/teacher/sessions/{sessionId:guid}/attendance/bulk | Task<ActionResult> MarkAttendanceBulk(Guid sessionId, TeacherBulkAttendanceRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | Enrollments; AttendanceRecords; Notifications | 1 |
| apps/api/Controllers/TeacherPortalController.cs:635 | PUT | /api/teacher/sessions/{sessionId:guid}/teacher-attendance | Task<ActionResult> MarkTeacherAttendance(Guid sessionId, TeacherSessionAttendanceRequest request, CancellationToken cancellationToken) | Action/middleware checks | no catalog mapping | ClassSessions | 1 |
| apps/api/Controllers/TeachersController.cs:14 | GET | /api/academies/{academyId:guid}/teachers | Task<ActionResult<IReadOnlyList<TeacherSummary>>> List(Guid academyId, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | Teachers | 0 |
| apps/api/Controllers/TeachersController.cs:25 | POST | /api/academies/{academyId:guid}/teachers | Task<ActionResult<TeacherSummary>> Create(Guid academyId, CreateTeacherRequest request, CancellationToken cancellationToken) | AcademyAccessFilter + action checks | no catalog mapping | Academies; Branches; Teachers | 1 |
| apps/api/Controllers/TeachersController.cs:39 | PUT | /api/academies/{academyId:guid}/teachers/{teacherId:guid} | Task<ActionResult<TeacherSummary>> Update(Guid academyId, Guid teacherId, UpdateTeacherRequest request, CancellationToken token) | AcademyAccessFilter + action checks | no catalog mapping | Teachers | 1 |
| apps/api/Controllers/WeatherForecastController.cs:14 | GET | /WeatherForecast | IEnumerable<WeatherForecast> Get() | Action/middleware checks | no catalog mapping |  | 0 |

## Action branch and response traces

### AcademicGovernanceController.Schemes (GET /api/academies/{academyId:guid}/academic-governance/grading-schemes)

Source: apps/api/Controllers/AcademicGovernanceController.cs:3

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### AcademicGovernanceController.AddScheme (POST /api/academies/{academyId:guid}/academic-governance/grading-schemes)

Source: apps/api/Controllers/AcademicGovernanceController.cs:3

| Validation/guard expressions |
| --- |
| [HttpPost("grading-schemes")]public async Task<ActionResult> AddScheme(Guid academyId,GradingSchemeRequest r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Name)\|\|r.PassingPercent<0\|\|r.PassingPercent>100)return BadRequest();try{System.Text.Json.JsonDocument.Parse(r.BandsJson??"[]");}catch{return BadRequest(new{message="Grade bands must be valid JSON."});}db.GradingSchemes.Add(new GradingScheme{AcademyId=academyId,Name=r.Name.Trim(),PassingPercent=r.PassingPercent,BandsJson=r.BandsJson??"[]"});await db.SaveChangesAsync(t);return Ok();} |

| Return / serialization expressions |
| --- |
| [HttpPost("grading-schemes")]public async Task<ActionResult> AddScheme(Guid academyId,GradingSchemeRequest r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Name)\|\|r.PassingPercent<0\|\|r.PassingPercent>100)return BadRequest();try{System.Text.Json.JsonDocument.Parse(r.BandsJson??"[]");}catch{return BadRequest(new{message="Grade bands must be valid JSON."});}db.GradingSchemes.Add(new GradingScheme{AcademyId=academyId,Name=r.Name.Trim(),PassingPercent=r.PassingPercent,BandsJson=r.BandsJson??"[]"});await db.SaveChangesAsync(t);return Ok();} |

### AcademicGovernanceController.Prerequisites (GET /api/academies/{academyId:guid}/academic-governance/prerequisites)

Source: apps/api/Controllers/AcademicGovernanceController.cs:3

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### AcademicGovernanceController.AddPrerequisite (POST /api/academies/{academyId:guid}/academic-governance/prerequisites)

Source: apps/api/Controllers/AcademicGovernanceController.cs:3

| Validation/guard expressions |
| --- |
| [HttpPost("prerequisites")]public async Task<ActionResult> AddPrerequisite(Guid academyId,PrerequisiteRequest r,CancellationToken t){if(r.CourseId==r.RequiredCourseId\|\|!await db.Courses.AnyAsync(x=>x.AcademyId==academyId&&x.Id==r.CourseId,t)\|\|!await db.Courses.AnyAsync(x=>x.AcademyId==academyId&&x.Id==r.RequiredCourseId,t))return BadRequest();if(await db.CoursePrerequisites.AnyAsync(x=>x.AcademyId==academyId&&x.CourseId==r.CourseId&&x.RequiredCourseId==r.RequiredCourseId,t))return Conflict(new{message="This prerequisite is already configured."});db.CoursePrerequisites.Add(new CoursePrerequisite{AcademyId=academyId,CourseId=r.CourseId,RequiredCourseId=r.RequiredCourseId});await db.SaveChangesAsync(t);return Ok();}} |

| Return / serialization expressions |
| --- |
| [HttpPost("prerequisites")]public async Task<ActionResult> AddPrerequisite(Guid academyId,PrerequisiteRequest r,CancellationToken t){if(r.CourseId==r.RequiredCourseId\|\|!await db.Courses.AnyAsync(x=>x.AcademyId==academyId&&x.Id==r.CourseId,t)\|\|!await db.Courses.AnyAsync(x=>x.AcademyId==academyId&&x.Id==r.RequiredCourseId,t))return BadRequest();if(await db.CoursePrerequisites.AnyAsync(x=>x.AcademyId==academyId&&x.CourseId==r.CourseId&&x.RequiredCourseId==r.RequiredCourseId,t))return Conflict(new{message="This prerequisite is already configured."});db.CoursePrerequisites.Add(new CoursePrerequisite{AcademyId=academyId,CourseId=r.CourseId,RequiredCourseId=r.RequiredCourseId});await db.SaveChangesAsync(t);return Ok();}} |

### AcademicPeriodsController.List (GET /api/academies/{academyId:guid}/academic-periods)

Source: apps/api/Controllers/AcademicPeriodsController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| return Ok(new AcademicPeriodsSummary(years, terms)); |

### AcademicPeriodsController.CreateYear (POST /api/academies/{academyId:guid}/academic-periods/years)

Source: apps/api/Controllers/AcademicPeriodsController.cs:20

| Validation/guard expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Name) \|\| request.EndDate < request.StartDate) return BadRequest(new { message = "Enter a name and valid academic-year dates." }); |
| if (await db.AcademicYears.AnyAsync(x => x.AcademyId == academyId && x.Name == request.Name.Trim(), token)) return Conflict(new { message = "That academic year already exists." }); |
| if (request.IsCurrent) { var current = await db.AcademicYears.Where(x => x.AcademyId == academyId && x.IsCurrent).ToListAsync(token); current.ForEach(x => x.IsCurrent = false); } |

| Return / serialization expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Name) \|\| request.EndDate < request.StartDate) return BadRequest(new { message = "Enter a name and valid academic-year dates." }); |
| var year = new AcademicYear { AcademyId = academyId, Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate, IsCurrent = request.IsCurrent }; db.AcademicYears.Add(year); await db.SaveChangesAsync(token); return Ok(new AcademicYearSummary(year.Id, year.Name, year.StartDate, year.EndDate, year.IsCurrent, year.IsClosed)); |

### AcademicPeriodsController.CreateTerm (POST /api/academies/{academyId:guid}/academic-periods/terms)

Source: apps/api/Controllers/AcademicPeriodsController.cs:29

| Validation/guard expressions |
| --- |
| if (year is null) return BadRequest(new { message = "Select an academic year in this academy." }); |
| if (string.IsNullOrWhiteSpace(request.Name) \|\| request.EndDate < request.StartDate \|\| request.StartDate < year.StartDate \|\| request.EndDate > year.EndDate) return BadRequest(new { message = "Term dates must be within the academic year." }); |

| Return / serialization expressions |
| --- |
| if (year is null) return BadRequest(new { message = "Select an academic year in this academy." }); |
| if (string.IsNullOrWhiteSpace(request.Name) \|\| request.EndDate < request.StartDate \|\| request.StartDate < year.StartDate \|\| request.EndDate > year.EndDate) return BadRequest(new { message = "Term dates must be within the academic year." }); |
| var term = new AcademicTerm { AcademyId = academyId, AcademicYearId = year.Id, Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate }; db.AcademicTerms.Add(term); await db.SaveChangesAsync(token); return Ok(new AcademicTermSummary(term.Id, term.AcademicYearId, term.Name, term.StartDate, term.EndDate, term.IsClosed)); |

### AcademicPeriodsController.CloseYear (PATCH /api/academies/{academyId:guid}/academic-periods/years/{yearId:guid}/close)

Source: apps/api/Controllers/AcademicPeriodsController.cs:38

| Validation/guard expressions |
| --- |
| if (year is null) return NotFound(); |
| if (await db.AcademicTerms.AnyAsync(item => item.AcademicYearId == yearId && !item.IsClosed, token)) return Conflict(new { message = "Close all terms before closing the academic year." }); |

| Return / serialization expressions |
| --- |
| if (year is null) return NotFound(); |
| year.IsClosed = true; year.IsCurrent = false; await db.SaveChangesAsync(token); return Ok(); |

### AcademicPeriodsController.CloseTerm (PATCH /api/academies/{academyId:guid}/academic-periods/terms/{termId:guid}/close)

Source: apps/api/Controllers/AcademicPeriodsController.cs:47

| Validation/guard expressions |
| --- |
| if (term is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (term is null) return NotFound(); |
| term.IsClosed = true; await db.SaveChangesAsync(token); return Ok(); |

### AcademiesController.List (GET /api/academies)

Source: apps/api/Controllers/AcademiesController.cs:19

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null) return Ok(Array.Empty<AcademySummary>()); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null) return Ok(Array.Empty<AcademySummary>()); |
| return Ok(academies); |

### AcademiesController.Create (POST /api/academies)

Source: apps/api/Controllers/AcademiesController.cs:35

| Validation/guard expressions |
| --- |
| if (user is null) return Unauthorized(); |
| if (user.AcademyId is not null) |
| if (string.IsNullOrWhiteSpace(request.Name)) |
| return BadRequest(new { message = "Academy name is required." }); |
| if (!await roleManager.RoleExistsAsync(ownerRole)) |
| if (!roleResult.Succeeded) return Problem("The Owner role could not be created."); |

| Return / serialization expressions |
| --- |
| if (user is null) return Unauthorized(); |
| return BadRequest(new { message = "Academy name is required." }); |
| return CreatedAtAction(nameof(List), new { id = academy.Id }, response); |

### AcademiesController.Update (PUT /api/academies/{academyId:guid})

Source: apps/api/Controllers/AcademiesController.cs:83

| Validation/guard expressions |
| --- |
| { var user=await userManager.GetUserAsync(User); if(user?.AcademyId!=academyId)return Forbid(); var x=await dbContext.Academies.SingleOrDefaultAsync(v=>v.Id==academyId,token); if(x is null)return NotFound(); if(string.IsNullOrWhiteSpace(request.Name))return BadRequest(new{message="Academy name is required."}); x.Name=request.Name.Trim();x.LegalName=string.IsNullOrWhiteSpace(request.LegalName)?null:request.LegalName.Trim();x.CountryCode=string.IsNullOrWhiteSpace(request.CountryCode)?x.CountryCode:request.CountryCode.Trim().ToUpperInvariant();x.TimeZone=string.IsNullOrWhiteSpace(request.TimeZone)?x.TimeZone:request.TimeZone.Trim(); await dbContext.SaveChangesAsync(token); return Ok(new AcademySummary(x.Id,x.Name,x.LegalName,x.CountryCode,x.TimeZone,x.IsActive,x.SubscriptionPlan,x.SubscriptionStatus,x.EnabledModulesJson)); } |

| Return / serialization expressions |
| --- |
| { var user=await userManager.GetUserAsync(User); if(user?.AcademyId!=academyId)return Forbid(); var x=await dbContext.Academies.SingleOrDefaultAsync(v=>v.Id==academyId,token); if(x is null)return NotFound(); if(string.IsNullOrWhiteSpace(request.Name))return BadRequest(new{message="Academy name is required."}); x.Name=request.Name.Trim();x.LegalName=string.IsNullOrWhiteSpace(request.LegalName)?null:request.LegalName.Trim();x.CountryCode=string.IsNullOrWhiteSpace(request.CountryCode)?x.CountryCode:request.CountryCode.Trim().ToUpperInvariant();x.TimeZone=string.IsNullOrWhiteSpace(request.TimeZone)?x.TimeZone:request.TimeZone.Trim(); await dbContext.SaveChangesAsync(token); return Ok(new AcademySummary(x.Id,x.Name,x.LegalName,x.CountryCode,x.TimeZone,x.IsActive,x.SubscriptionPlan,x.SubscriptionStatus,x.EnabledModulesJson)); } |

### AcademiesController.SetActive (PATCH /api/academies/{academyId:guid}/active)

Source: apps/api/Controllers/AcademiesController.cs:86

| Validation/guard expressions |
| --- |
| { var user=await userManager.GetUserAsync(User); if(user?.AcademyId!=academyId)return Forbid(); var x=await dbContext.Academies.SingleOrDefaultAsync(v=>v.Id==academyId,token); if(x is null)return NotFound(); x.IsActive=request.IsActive; await dbContext.SaveChangesAsync(token); return Ok(); } |

| Return / serialization expressions |
| --- |
| { var user=await userManager.GetUserAsync(User); if(user?.AcademyId!=academyId)return Forbid(); var x=await dbContext.Academies.SingleOrDefaultAsync(v=>v.Id==academyId,token); if(x is null)return NotFound(); x.IsActive=request.IsActive; await dbContext.SaveChangesAsync(token); return Ok(); } |

### AcademyExportsController.Export (GET /api/academies/{academyId:guid}/exports/{resource})

Source: apps/api/Controllers/AcademyExportsController.cs:11

| Validation/guard expressions |
| --- |
| if (string.IsNullOrEmpty(csv)) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (string.IsNullOrEmpty(csv)) return NotFound(); |
| return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"academydesk-{resource}.csv"); |

### AcademyPlatformServicesController.BillingInvoices (GET /api/academies/{academyId:guid}/platform-services/billing-invoices)

Source: apps/api/Controllers/AcademyPlatformServicesController.cs:14

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### AcademyPlatformServicesController.SubmitPayment (POST /api/academies/{academyId:guid}/platform-services/billing-invoices/{invoiceId:guid}/payment-submission)

Source: apps/api/Controllers/AcademyPlatformServicesController.cs:21

| Validation/guard expressions |
| --- |
| if (invoice is null) return NotFound(); |
| if (invoice.Status is "Paid" or "Void") return BadRequest(new { message = "This invoice cannot accept a payment submission." }); |

| Return / serialization expressions |
| --- |
| if (invoice is null) return NotFound(); |
| if (invoice.Status is "Paid" or "Void") return BadRequest(new { message = "This invoice cannot accept a payment submission." }); |
| return Ok(new { invoice.Id, invoice.Status, invoice.PaymentReference, invoice.PaymentSubmittedAtUtc }); |

### AcademyPlatformServicesController.SupportCases (GET /api/academies/{academyId:guid}/platform-services/support-cases)

Source: apps/api/Controllers/AcademyPlatformServicesController.cs:35

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### AcademyPlatformServicesController.CreateSupportCase (POST /api/academies/{academyId:guid}/platform-services/support-cases)

Source: apps/api/Controllers/AcademyPlatformServicesController.cs:42

| Validation/guard expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Subject) \|\| string.IsNullOrWhiteSpace(request.Description)) return BadRequest(new { message = "A subject and message are required." }); |

| Return / serialization expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Subject) \|\| string.IsNullOrWhiteSpace(request.Description)) return BadRequest(new { message = "A subject and message are required." }); |
| return Created($"/api/academies/{academyId}/platform-services/support-cases/{item.Id}", item); |

### AcademyPlatformServicesController.RespondToSupportCase (POST /api/academies/{academyId:guid}/platform-services/support-cases/{caseId:guid}/response)

Source: apps/api/Controllers/AcademyPlatformServicesController.cs:52

| Validation/guard expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Message)) return BadRequest(new { message = "Write a response before sending it." }); |
| if (item is null) return NotFound(); |
| if (item.Status is "Resolved" or "Closed") return BadRequest(new { message = "This support case is closed." }); |

| Return / serialization expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Message)) return BadRequest(new { message = "Write a response before sending it." }); |
| if (item is null) return NotFound(); |
| if (item.Status is "Resolved" or "Closed") return BadRequest(new { message = "This support case is closed." }); |
| return Ok(new { item.Id, item.AcademyResponse, item.AcademyRespondedAtUtc }); |

### AcademyRolesController.List (GET /api/academies/{academyId:guid}/roles)

Source: apps/api/Controllers/AcademyRolesController.cs:14

| Validation/guard expressions |
| --- |
| if (!await IsAdmin(academyId)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsAdmin(academyId)) return Forbid(); |
| return Ok(result); |

### AcademyRolesController.Create (POST /api/academies/{academyId:guid}/roles)

Source: apps/api/Controllers/AcademyRolesController.cs:24

| Validation/guard expressions |
| --- |
| if (!await IsAdmin(academyId)) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.Name) \|\| request.Name.Length > 80) return BadRequest(new { message = "Enter a role name of up to 80 characters." }); |
| if (await roles.Roles.AnyAsync(role => role.AcademyId == academyId && role.Name == name)) return Conflict(new { message = "That academy role already exists." }); |
| return result.Succeeded ? Ok(new { role.Id, role.Name, role.PermissionsJson }) : BadRequest(result.Errors); |

| Return / serialization expressions |
| --- |
| if (!await IsAdmin(academyId)) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.Name) \|\| request.Name.Length > 80) return BadRequest(new { message = "Enter a role name of up to 80 characters." }); |

### AcademyRolesController.Assign (PUT /api/academies/{academyId:guid}/roles/staff/{staffId:guid}/assignment)

Source: apps/api/Controllers/AcademyRolesController.cs:36

| Validation/guard expressions |
| --- |
| if (!await IsAdmin(academyId)) return Forbid(); |
| if (staff?.AcademyId != academyId \|\| role is null \|\| (role.AcademyId is not null && role.AcademyId != academyId)) return NotFound(); |
| if (role.Name is null) return BadRequest(); |
| if (mutable.Length > 0) await users.RemoveFromRolesAsync(staff, mutable); |
| return result.Succeeded ? Ok(new { staff.Id, role = role.Name }) : BadRequest(result.Errors); |

| Return / serialization expressions |
| --- |
| if (!await IsAdmin(academyId)) return Forbid(); |
| if (staff?.AcademyId != academyId \|\| role is null \|\| (role.AcademyId is not null && role.AcademyId != academyId)) return NotFound(); |
| if (role.Name is null) return BadRequest(); |

### AccessGrantsController.List (GET /api/academies/{academyId:guid}/access-grants)

Source: apps/api/Controllers/AccessGrantsController.cs:16

| Validation/guard expressions |
| --- |
| if (!await IsAdmin(academyId)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsAdmin(academyId)) return Forbid(); |
| return Ok(grants.Select(x => new |

### AccessGrantsController.Create (POST /api/academies/{academyId:guid}/access-grants)

Source: apps/api/Controllers/AccessGrantsController.cs:33

| Validation/guard expressions |
| --- |
| if (admin is null) return Forbid(); |
| if (person?.AcademyId != academyId \|\| !person.IsActive) return NotFound(new { message = "Choose an active academy staff member." }); |
| if (permissions.Length == 0) return BadRequest(new { message = "Choose at least one permitted function." }); |
| if (!request.IsPermanent && expiry is null) return BadRequest(new { message = "Choose an expiry date or select permanent access." }); |
| if (!request.IsPermanent && expiry <= DateTimeOffset.UtcNow) return BadRequest(new { message = "The expiry date must be in the future." }); |

| Return / serialization expressions |
| --- |
| if (admin is null) return Forbid(); |
| if (person?.AcademyId != academyId \|\| !person.IsActive) return NotFound(new { message = "Choose an active academy staff member." }); |
| if (permissions.Length == 0) return BadRequest(new { message = "Choose at least one permitted function." }); |
| if (!request.IsPermanent && expiry is null) return BadRequest(new { message = "Choose an expiry date or select permanent access." }); |
| if (!request.IsPermanent && expiry <= DateTimeOffset.UtcNow) return BadRequest(new { message = "The expiry date must be in the future." }); |
| return Ok(new { grant.Id }); |

### AccessGrantsController.Revoke (PATCH /api/academies/{academyId:guid}/access-grants/{grantId:guid}/revoke)

Source: apps/api/Controllers/AccessGrantsController.cs:51

| Validation/guard expressions |
| --- |
| if (admin is null) return Forbid(); |
| if (grant is null) return NotFound(); |
| if (grant.RevokedAtUtc is null) { grant.RevokedAtUtc = DateTimeOffset.UtcNow; grant.RevokedByUserId = admin.Id; await db.SaveChangesAsync(token); } |

| Return / serialization expressions |
| --- |
| if (admin is null) return Forbid(); |
| if (grant is null) return NotFound(); |
| return NoContent(); |

### AccessReviewsController.List (GET /api/academies/{academyId:guid}/access-reviews)

Source: apps/api/Controllers/AccessReviewsController.cs:3

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### AccessReviewsController.SignOff (POST /api/academies/{academyId:guid}/access-reviews)

Source: apps/api/Controllers/AccessReviewsController.cs:3

| Validation/guard expressions |
| --- |
| [HttpPost]public async Task<ActionResult> SignOff(Guid academyId,AccessReviewRequest r,CancellationToken t){db.AccessReviews.Add(new AccessReview{AcademyId=academyId,Notes=r.Notes?.Trim()});if(r.CreateFollowUp)db.AdminWorkItems.Add(new AdminWorkItem{AcademyId=academyId,Type="AccessReview",Title="Resolve access review actions",Description=r.Notes?.Trim(),Priority="High"});await db.SaveChangesAsync(t);return Ok();}} |

| Return / serialization expressions |
| --- |
| [HttpPost]public async Task<ActionResult> SignOff(Guid academyId,AccessReviewRequest r,CancellationToken t){db.AccessReviews.Add(new AccessReview{AcademyId=academyId,Notes=r.Notes?.Trim()});if(r.CreateFollowUp)db.AdminWorkItems.Add(new AdminWorkItem{AcademyId=academyId,Type="AccessReview",Title="Resolve access review actions",Description=r.Notes?.Trim(),Priority="High"});await db.SaveChangesAsync(t);return Ok();}} |

### AdminIntelligenceController.Get (GET /api/academies/{academyId:guid}/admin-intelligence)

Source: apps/api/Controllers/AdminIntelligenceController.cs:10

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| return Ok(new { occupancy, collections = invoices.Select(x => new { x.InvoiceNumber, x.TotalAmount, x.DueDate, DaysOverdue = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - x.DueDate.DayNumber }), workload, attendanceRisk, activeEnrolments = active.Count }); |

### AdminWorkItemsController.List (GET /api/academies/{academyId:guid}/admin-work-items)

Source: apps/api/Controllers/AdminWorkItemsController.cs:5

| Validation/guard expressions |
| --- |
| [HttpGet] public async Task<ActionResult> List(Guid academyId,string? status,string? type,CancellationToken t){var q=db.AdminWorkItems.AsNoTracking().Where(x=>x.AcademyId==academyId);if(!string.IsNullOrWhiteSpace(status))q=q.Where(x=>x.Status==status);if(!string.IsNullOrWhiteSpace(type))q=q.Where(x=>x.Type==type);return Ok(await q.OrderBy(x=>x.DueAtUtc).ThenByDescending(x=>x.Priority).ToListAsync(t));} |

| Return / serialization expressions |
| --- |
| [HttpGet] public async Task<ActionResult> List(Guid academyId,string? status,string? type,CancellationToken t){var q=db.AdminWorkItems.AsNoTracking().Where(x=>x.AcademyId==academyId);if(!string.IsNullOrWhiteSpace(status))q=q.Where(x=>x.Status==status);if(!string.IsNullOrWhiteSpace(type))q=q.Where(x=>x.Type==type);return Ok(await q.OrderBy(x=>x.DueAtUtc).ThenByDescending(x=>x.Priority).ToListAsync(t));} |

### AdminWorkItemsController.Create (POST /api/academies/{academyId:guid}/admin-work-items)

Source: apps/api/Controllers/AdminWorkItemsController.cs:6

| Validation/guard expressions |
| --- |
| [HttpPost] public async Task<ActionResult> Create(Guid academyId,CreateWorkItem r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Type)\|\|string.IsNullOrWhiteSpace(r.Title))return BadRequest();if(r.Priority is not (null or "Low" or "Normal" or "High" or "Critical"))return BadRequest("Invalid priority.");var x=new AdminWorkItem{AcademyId=academyId,Type=r.Type.Trim(),Title=r.Title.Trim(),Description=r.Description?.Trim(),Priority=string.IsNullOrWhiteSpace(r.Priority)?"Normal":r.Priority.Trim(),EntityType=r.EntityType?.Trim(),EntityId=r.EntityId,AssignedUserId=r.AssignedUserId,DueAtUtc=r.DueAtUtc};db.AdminWorkItems.Add(x);await db.SaveChangesAsync(t);return Ok(x);} |

| Return / serialization expressions |
| --- |
| [HttpPost] public async Task<ActionResult> Create(Guid academyId,CreateWorkItem r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Type)\|\|string.IsNullOrWhiteSpace(r.Title))return BadRequest();if(r.Priority is not (null or "Low" or "Normal" or "High" or "Critical"))return BadRequest("Invalid priority.");var x=new AdminWorkItem{AcademyId=academyId,Type=r.Type.Trim(),Title=r.Title.Trim(),Description=r.Description?.Trim(),Priority=string.IsNullOrWhiteSpace(r.Priority)?"Normal":r.Priority.Trim(),EntityType=r.EntityType?.Trim(),EntityId=r.EntityId,AssignedUserId=r.AssignedUserId,DueAtUtc=r.DueAtUtc};db.AdminWorkItems.Add(x);await db.SaveChangesAsync(t);return Ok(x);} |

### AdminWorkItemsController.Status (PATCH /api/academies/{academyId:guid}/admin-work-items/{id:guid}/status)

Source: apps/api/Controllers/AdminWorkItemsController.cs:7

| Validation/guard expressions |
| --- |
| [HttpPatch("{id:guid}/status")] public async Task<ActionResult> Status(Guid academyId,Guid id,WorkStatus r,CancellationToken t){var x=await db.AdminWorkItems.SingleOrDefaultAsync(x=>x.AcademyId==academyId&&x.Id==id,t);if(x is null)return NotFound();if(r.Status is not("Open"or"InProgress"or"Completed"or"Cancelled"))return BadRequest();x.Status=r.Status;x.CompletedAtUtc=r.Status=="Completed"?DateTime.UtcNow:null;await db.SaveChangesAsync(t);return Ok(x);} |

| Return / serialization expressions |
| --- |
| [HttpPatch("{id:guid}/status")] public async Task<ActionResult> Status(Guid academyId,Guid id,WorkStatus r,CancellationToken t){var x=await db.AdminWorkItems.SingleOrDefaultAsync(x=>x.AcademyId==academyId&&x.Id==id,t);if(x is null)return NotFound();if(r.Status is not("Open"or"InProgress"or"Completed"or"Cancelled"))return BadRequest();x.Status=r.Status;x.CompletedAtUtc=r.Status=="Completed"?DateTime.UtcNow:null;await db.SaveChangesAsync(t);return Ok(x);} |

### AdminWorkItemsController.CollectionState (PATCH /api/academies/{academyId:guid}/admin-work-items/{id:guid}/collections)

Source: apps/api/Controllers/AdminWorkItemsController.cs:8

| Validation/guard expressions |
| --- |
| [HttpPatch("{id:guid}/collections")] public async Task<ActionResult> CollectionState(Guid academyId,Guid id,CollectionStateRequest r,CancellationToken t){var x=await db.AdminWorkItems.SingleOrDefaultAsync(x=>x.AcademyId==academyId&&x.Id==id&&x.Type=="Collections",t);if(x is null)return NotFound();if(r.EscalationStage is not("Initial"or"Reminder"or"ManagerReview"or"FinalNotice"))return BadRequest();x.EscalationStage=r.EscalationStage;x.PromisedPaymentDate=r.PromisedPaymentDate;await db.SaveChangesAsync(t);return Ok(x);}} |

| Return / serialization expressions |
| --- |
| [HttpPatch("{id:guid}/collections")] public async Task<ActionResult> CollectionState(Guid academyId,Guid id,CollectionStateRequest r,CancellationToken t){var x=await db.AdminWorkItems.SingleOrDefaultAsync(x=>x.AcademyId==academyId&&x.Id==id&&x.Type=="Collections",t);if(x is null)return NotFound();if(r.EscalationStage is not("Initial"or"Reminder"or"ManagerReview"or"FinalNotice"))return BadRequest();x.EscalationStage=r.EscalationStage;x.PromisedPaymentDate=r.PromisedPaymentDate;await db.SaveChangesAsync(t);return Ok(x);}} |

### AssessmentsController.List (GET /api/academies/{academyId:guid}/assessments)

Source: apps/api/Controllers/AssessmentsController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### AssessmentsController.Create (POST /api/academies/{academyId:guid}/assessments)

Source: apps/api/Controllers/AssessmentsController.cs:15

| Validation/guard expressions |
| --- |
| if (!await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected batch does not belong to this academy." }); |
| if (string.IsNullOrWhiteSpace(request.Title) \|\| request.MaxScore <= 0) return BadRequest(new { message = "Title and a positive maximum score are required." }); |
| if (request.GradingSchemeId.HasValue && !await dbContext.GradingSchemes.AnyAsync(x => x.Id == request.GradingSchemeId && x.AcademyId == academyId && x.IsActive, cancellationToken)) return BadRequest(new { message = "The grading scheme does not belong to this academy." }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected batch does not belong to this academy." }); |
| if (string.IsNullOrWhiteSpace(request.Title) \|\| request.MaxScore <= 0) return BadRequest(new { message = "Title and a positive maximum score are required." }); |
| if (request.GradingSchemeId.HasValue && !await dbContext.GradingSchemes.AnyAsync(x => x.Id == request.GradingSchemeId && x.AcademyId == academyId && x.IsActive, cancellationToken)) return BadRequest(new { message = "The grading scheme does not belong to this academy." }); |
| return Created($"/api/academies/{academyId}/assessments/{assessment.Id}", new AssessmentSummary(assessment.Id, assessment.BatchId, assessment.Title, assessment.Type, assessment.MaxScore, assessment.ScheduledAtUtc, assessment.IsPublished)); |

### AssessmentsController.Publish (PATCH /api/academies/{academyId:guid}/assessments/{assessmentId:guid}/publish)

Source: apps/api/Controllers/AssessmentsController.cs:25

| Validation/guard expressions |
| --- |
| { var x=await dbContext.Assessments.SingleOrDefaultAsync(v=>v.Id==assessmentId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); x.IsPublished=request.IsPublished; await dbContext.SaveChangesAsync(token); return Ok(); } |

| Return / serialization expressions |
| --- |
| { var x=await dbContext.Assessments.SingleOrDefaultAsync(v=>v.Id==assessmentId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); x.IsPublished=request.IsPublished; await dbContext.SaveChangesAsync(token); return Ok(); } |

### AssessmentResultsController.List (GET /api/academies/{academyId:guid}/assessments/{assessmentId:guid}/results)

Source: apps/api/Controllers/AssessmentsController.cs:34

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### AssessmentResultsController.Upsert (POST /api/academies/{academyId:guid}/assessments/{assessmentId:guid}/results)

Source: apps/api/Controllers/AssessmentsController.cs:37

| Validation/guard expressions |
| --- |
| if (assessment is null) return NotFound(); |
| if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The student does not belong to this academy." }); |
| if (request.Score < 0 \|\| request.Score > assessment.MaxScore) return BadRequest(new { message = "Score must be within the assessment range." }); |
| if (assessment.GradingSchemeId.HasValue) { var scheme = await dbContext.GradingSchemes.AsNoTracking().SingleAsync(x => x.Id == assessment.GradingSchemeId && x.AcademyId == academyId, cancellationToken); result.Grade = request.Grade?.Trim() ?? (request.Score * 100m / assessment.MaxScore >= scheme.PassingPercent ? "Pass" : "Fail"); } |
| if (isNew) dbContext.AssessmentResults.Add(result); await dbContext.SaveChangesAsync(cancellationToken); |

| Return / serialization expressions |
| --- |
| if (assessment is null) return NotFound(); |
| if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The student does not belong to this academy." }); |
| if (request.Score < 0 \|\| request.Score > assessment.MaxScore) return BadRequest(new { message = "Score must be within the assessment range." }); |
| return Ok(new AssessmentResultSummary(result.Id, result.StudentId, result.Score, result.Grade, result.Remarks, result.IsPublished)); |

### AssignmentsController.List (GET /api/academies/{academyId:guid}/assignments)

Source: apps/api/Controllers/AssignmentsController.cs:12

| Validation/guard expressions |
| --- |
| if (batchId.HasValue) query = query.Where(x => x.BatchId == batchId.Value); |

| Return / serialization expressions |
| --- |
| return Ok(assignments); |

### AssignmentsController.Create (POST /api/academies/{academyId:guid}/assignments)

Source: apps/api/Controllers/AssignmentsController.cs:21

| Validation/guard expressions |
| --- |
| if (!await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected batch does not belong to this academy." }); |
| if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Assignment title is required." }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected batch does not belong to this academy." }); |
| if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Assignment title is required." }); |
| return Created($"/api/academies/{academyId}/assignments/{assignment.Id}", new AssignmentSummary(assignment.Id, assignment.BatchId, assignment.Title, assignment.Description, assignment.DueAtUtc, assignment.Type, assignment.IsPublished)); |

### AssignmentsController.Publish (PATCH /api/academies/{academyId:guid}/assignments/{assignmentId:guid}/publish)

Source: apps/api/Controllers/AssignmentsController.cs:30

| Validation/guard expressions |
| --- |
| { var x=await dbContext.Assignments.SingleOrDefaultAsync(v=>v.Id==assignmentId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); x.IsPublished=request.IsPublished; await dbContext.SaveChangesAsync(token); return Ok(); } |

| Return / serialization expressions |
| --- |
| { var x=await dbContext.Assignments.SingleOrDefaultAsync(v=>v.Id==assignmentId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); x.IsPublished=request.IsPublished; await dbContext.SaveChangesAsync(token); return Ok(); } |

### AssignmentSubmissionsController.List (GET /api/academies/{academyId:guid}/assignment-submissions)

Source: apps/api/Controllers/AssignmentSubmissionsController.cs:4

| Validation/guard expressions |
| --- |
| [HttpGet]public async Task<ActionResult<IReadOnlyList<AssignmentSubmission>>>List(Guid academyId,Guid? assignmentId,CancellationToken t){var q=db.AssignmentSubmissions.AsNoTracking().Where(x=>x.AcademyId==academyId);if(assignmentId.HasValue)q=q.Where(x=>x.AssignmentId==assignmentId);return Ok(await q.OrderByDescending(x=>x.SubmittedAtUtc).ToListAsync(t));} |

| Return / serialization expressions |
| --- |
| [HttpGet]public async Task<ActionResult<IReadOnlyList<AssignmentSubmission>>>List(Guid academyId,Guid? assignmentId,CancellationToken t){var q=db.AssignmentSubmissions.AsNoTracking().Where(x=>x.AcademyId==academyId);if(assignmentId.HasValue)q=q.Where(x=>x.AssignmentId==assignmentId);return Ok(await q.OrderByDescending(x=>x.SubmittedAtUtc).ToListAsync(t));} |

### AssignmentSubmissionsController.Submit (POST /api/academies/{academyId:guid}/assignment-submissions)

Source: apps/api/Controllers/AssignmentSubmissionsController.cs:4

| Validation/guard expressions |
| --- |
| [HttpPost]public async Task<ActionResult<AssignmentSubmission>>Submit(Guid academyId,AssignmentSubmission x,CancellationToken t){if(!await db.Assignments.AnyAsync(a=>a.Id==x.AssignmentId&&a.AcademyId==academyId,t))return BadRequest();x.Id=Guid.NewGuid();x.AcademyId=academyId;db.AssignmentSubmissions.Add(x);await db.SaveChangesAsync(t);return Ok(x);} |

| Return / serialization expressions |
| --- |
| [HttpPost]public async Task<ActionResult<AssignmentSubmission>>Submit(Guid academyId,AssignmentSubmission x,CancellationToken t){if(!await db.Assignments.AnyAsync(a=>a.Id==x.AssignmentId&&a.AcademyId==academyId,t))return BadRequest();x.Id=Guid.NewGuid();x.AcademyId=academyId;db.AssignmentSubmissions.Add(x);await db.SaveChangesAsync(t);return Ok(x);} |

### AssignmentSubmissionsController.Review (PATCH /api/academies/{academyId:guid}/assignment-submissions/{id:guid}/review)

Source: apps/api/Controllers/AssignmentSubmissionsController.cs:4

| Validation/guard expressions |
| --- |
| [HttpPatch("{id:guid}/review")]public async Task<ActionResult<AssignmentSubmission>>Review(Guid academyId,Guid id,ReviewRequest r,CancellationToken t){var x=await db.AssignmentSubmissions.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();x.TeacherFeedback=r.Feedback;x.Status="Reviewed";await db.SaveChangesAsync(t);return Ok(x);}} |

| Return / serialization expressions |
| --- |
| [HttpPatch("{id:guid}/review")]public async Task<ActionResult<AssignmentSubmission>>Review(Guid academyId,Guid id,ReviewRequest r,CancellationToken t){var x=await db.AssignmentSubmissions.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();x.TeacherFeedback=r.Feedback;x.Status="Reviewed";await db.SaveChangesAsync(t);return Ok(x);}} |

### AttendanceController.List (GET /api/academies/{academyId:guid}/sessions/{sessionId:guid}/attendance)

Source: apps/api/Controllers/AttendanceController.cs:14

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| return Ok(records); |

### AttendanceController.Mark (POST /api/academies/{academyId:guid}/sessions/{sessionId:guid}/attendance)

Source: apps/api/Controllers/AttendanceController.cs:21

| Validation/guard expressions |
| --- |
| if (session is null) return NotFound(); |
| if (!AllowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Status must be Present, Absent, Late, Excused, or Online." }); |
| if (!enrolled) return BadRequest(new { message = "The student is not actively enrolled in this session's batch." }); |
| if (record is null) { record = new AttendanceRecord { AcademyId = academyId, ClassSessionId = sessionId, StudentId = request.StudentId }; dbContext.AttendanceRecords.Add(record); } |

| Return / serialization expressions |
| --- |
| if (session is null) return NotFound(); |
| if (!AllowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Status must be Present, Absent, Late, Excused, or Online." }); |
| if (!enrolled) return BadRequest(new { message = "The student is not actively enrolled in this session's batch." }); |
| return Ok(new AttendanceSummary(record.Id, record.StudentId, record.Status, record.MarkedAtUtc, record.Notes)); |

### AuditLogsController.List (GET /api/academies/{academyId:guid}/audit-logs)

Source: apps/api/Controllers/AuditLogsController.cs:11

| Validation/guard expressions |
| --- |
| if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action); |
| if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType); |
| if (fromUtc.HasValue) query = query.Where(x => x.OccurredAtUtc >= fromUtc.Value); |
| if (toUtc.HasValue) query = query.Where(x => x.OccurredAtUtc < toUtc.Value); |

| Return / serialization expressions |
| --- |
| return Ok(await query.OrderByDescending(x => x.OccurredAtUtc).Take(200).Select(x => new AuditLogSummary(x.Id, x.ActorUserId, x.Action, x.EntityType, x.EntityId, x.MetadataJson, x.IpAddress, x.OccurredAtUtc)).ToListAsync(cancellationToken)); |

### AuditLogsController.ExportCsv (GET /api/academies/{academyId:guid}/audit-logs/export.csv)

Source: apps/api/Controllers/AuditLogsController.cs:21

| Validation/guard expressions |
| --- |
| { var query=dbContext.AuditLogs.AsNoTracking().Where(x=>x.AcademyId==academyId); if(!string.IsNullOrWhiteSpace(action))query=query.Where(x=>x.Action==action); if(!string.IsNullOrWhiteSpace(entityType))query=query.Where(x=>x.EntityType==entityType); if(fromUtc.HasValue)query=query.Where(x=>x.OccurredAtUtc>=fromUtc); if(toUtc.HasValue)query=query.Where(x=>x.OccurredAtUtc<toUtc); var rows=await query.OrderByDescending(x=>x.OccurredAtUtc).Take(200).ToListAsync(token); static string Q(string? v)=>$"\"{(v??"").Replace("\"","\"\"")}\""; var csv="OccurredAtUtc,Action,EntityType,EntityId,ActorUserId,Metadata\r\n"+string.Join("\r\n",rows.Select(x=>string.Join(",",Q(x.OccurredAtUtc.ToString("O")),Q(x.Action),Q(x.EntityType),Q(x.EntityId?.ToString()),Q(x.ActorUserId?.ToString()),Q(x.MetadataJson)))); return File(System.Text.Encoding.UTF8.GetBytes(csv),"text/csv","academydesk-audit-log.csv"); } |

| Return / serialization expressions |
| --- |
| { var query=dbContext.AuditLogs.AsNoTracking().Where(x=>x.AcademyId==academyId); if(!string.IsNullOrWhiteSpace(action))query=query.Where(x=>x.Action==action); if(!string.IsNullOrWhiteSpace(entityType))query=query.Where(x=>x.EntityType==entityType); if(fromUtc.HasValue)query=query.Where(x=>x.OccurredAtUtc>=fromUtc); if(toUtc.HasValue)query=query.Where(x=>x.OccurredAtUtc<toUtc); var rows=await query.OrderByDescending(x=>x.OccurredAtUtc).Take(200).ToListAsync(token); static string Q(string? v)=>$"\"{(v??"").Replace("\"","\"\"")}\""; var csv="OccurredAtUtc,Action,EntityType,EntityId,ActorUserId,Metadata\r\n"+string.Join("\r\n",rows.Select(x=>string.Join(",",Q(x.OccurredAtUtc.ToString("O")),Q(x.Action),Q(x.EntityType),Q(x.EntityId?.ToString()),Q(x.ActorUserId?.ToString()),Q(x.MetadataJson)))); return File(System.Text.Encoding.UTF8.GetBytes(csv),"text/csv","academydesk-audit-log.csv"); } |

### AuthSessionController.Current (GET /api/auth/session)

Source: apps/api/Controllers/AuthSessionController.cs:13

| Validation/guard expressions |
| --- |
| if (user is null) return Unauthorized(); |

| Return / serialization expressions |
| --- |
| if (user is null) return Unauthorized(); |
| return Ok(new SessionSummary(user.DisplayName, user.Email, user.PhoneNumber, roles.ToArray(), user.AcademyId, user.IsPlatformOwner, workspace, user.ProfileImageUrl)); |

### AuthSessionController.UpdateProfile (PUT /api/auth/session/profile)

Source: apps/api/Controllers/AuthSessionController.cs:24

| Validation/guard expressions |
| --- |
| if (user is null) return Unauthorized(); |
| if (string.IsNullOrWhiteSpace(request.DisplayName) \|\| request.DisplayName.Trim().Length > 200) |
| return BadRequest(new { message = "Enter a display name of up to 200 characters." }); |
| if (!update.Succeeded) return BadRequest(new { message = string.Join(" ", update.Errors.Select(x => x.Description)) }); |

| Return / serialization expressions |
| --- |
| if (user is null) return Unauthorized(); |
| return BadRequest(new { message = "Enter a display name of up to 200 characters." }); |
| if (!update.Succeeded) return BadRequest(new { message = string.Join(" ", update.Errors.Select(x => x.Description)) }); |
| return Ok(new SessionSummary(user.DisplayName, user.Email, user.PhoneNumber, roles.ToArray(), user.AcademyId, user.IsPlatformOwner, workspace, user.ProfileImageUrl)); |

### AuthSessionController.ChangePassword (POST /api/auth/session/change-password)

Source: apps/api/Controllers/AuthSessionController.cs:42

| Validation/guard expressions |
| --- |
| if (user is null) return Unauthorized(); |
| if (string.IsNullOrWhiteSpace(request.NewPassword) \|\| request.NewPassword.Length < 6) |
| return BadRequest(new { message = "Use a password of at least 6 characters." }); |
| if (!result.Succeeded) return BadRequest(new { message = "The current password is incorrect or the new password does not meet the security rules." }); |

| Return / serialization expressions |
| --- |
| if (user is null) return Unauthorized(); |
| return BadRequest(new { message = "Use a password of at least 6 characters." }); |
| if (!result.Succeeded) return BadRequest(new { message = "The current password is incorrect or the new password does not meet the security rules." }); |
| return Ok(new { message = "Password changed." }); |

### AuthSessionController.UploadProfileImage (POST /api/auth/session/profile-image)

Source: apps/api/Controllers/AuthSessionController.cs:55

| Validation/guard expressions |
| --- |
| if (user is null) return Unauthorized(); |
| if (image.Length is <= 0 or > 2_000_000) |
| return BadRequest(new { message = "Choose an image smaller than 2 MB." }); |
| if (extension.Length == 0) |
| return BadRequest(new { message = "Use a JPG, PNG, or WebP image." }); |
| if (!string.IsNullOrWhiteSpace(user.ProfileImageUrl) && user.ProfileImageUrl.StartsWith("/uploads/profile-images/", StringComparison.Ordinal)) |
| if (System.IO.File.Exists(previousPath)) System.IO.File.Delete(previousPath); |
| if (!update.Succeeded) |
| if (System.IO.File.Exists(diskPath)) System.IO.File.Delete(diskPath); |
| return BadRequest(new { message = "Profile image could not be saved." }); |

| Return / serialization expressions |
| --- |
| if (user is null) return Unauthorized(); |
| return BadRequest(new { message = "Choose an image smaller than 2 MB." }); |
| return BadRequest(new { message = "Use a JPG, PNG, or WebP image." }); |
| return BadRequest(new { message = "Profile image could not be saved." }); |
| return Ok(new ProfileImageSummary(user.ProfileImageUrl)); |

### BatchesController.List (GET /api/academies/{academyId:guid}/batches)

Source: apps/api/Controllers/BatchesController.cs:13

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| return Ok(rows); |

### BatchesController.Create (POST /api/academies/{academyId:guid}/batches)

Source: apps/api/Controllers/BatchesController.cs:22

| Validation/guard expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound(); |
| var problem = await Validate(academyId, request, token); if (problem is not null) return BadRequest(new { message = problem }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound(); |
| var problem = await Validate(academyId, request, token); if (problem is not null) return BadRequest(new { message = problem }); |
| var batch = new Batch { AcademyId = academyId, Name = request.Name.Trim() }; Apply(batch, request); dbContext.Batches.Add(batch); await dbContext.SaveChangesAsync(token); return Created($"/api/academies/{academyId}/batches/{batch.Id}", Summary(batch, 0)); |

### BatchesController.Update (PUT /api/academies/{academyId:guid}/batches/{batchId:guid})

Source: apps/api/Controllers/BatchesController.cs:30

| Validation/guard expressions |
| --- |
| var batch = await dbContext.Batches.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == batchId, token); if (batch is null) return NotFound(); |
| var problem = await Validate(academyId, request, token, batchId); if (problem is not null) return BadRequest(new { message = problem }); |
| var active = await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == batchId && x.Status == "Active", token); if (request.Capacity < active) return BadRequest(new { message = $"Capacity cannot be lower than the {active} active enrolments." }); |
| if (string.IsNullOrWhiteSpace(r.Name) \|\| r.Capacity is < 1 or > 1000 \|\| r.WaitlistCapacity is < 0 or > 1000) return "Enter a batch name, capacity between 1 and 1000, and a valid waitlist capacity."; |
| if (r.EndDate.HasValue && r.StartDate.HasValue && r.EndDate < r.StartDate) return "End date cannot be earlier than the start date."; |
| if (!new[] { "InPerson", "Online", "Hybrid" }.Contains((r.DeliveryMode ?? "InPerson").Replace(" ", ""), StringComparer.OrdinalIgnoreCase)) return "Delivery mode must be InPerson, Online, or Hybrid."; |
| if ((r.DeliveryMode ?? "").Replace(" ", "") is "Online" or "Hybrid" && string.IsNullOrWhiteSpace(r.MeetingLink)) return "A meeting link is required for online and hybrid classes."; |
| if (!string.IsNullOrWhiteSpace(r.MeetingDaysJson)) |
| if (sessions is null \|\| sessions.Count == 0 \|\| sessions.Any(x => !validDays.Contains(x.Day, StringComparer.OrdinalIgnoreCase) \|\| !TimeOnly.TryParse(x.StartTime, out _))) |
| if (!new[] { "Open", "Waitlist", "Closed" }.Contains((r.EnrollmentStatus ?? "Open").Trim(), StringComparer.OrdinalIgnoreCase)) return "Enrolment status must be Open, Waitlist, or Closed."; |
| if (!await dbContext.Courses.AnyAsync(x => x.Id == r.CourseId && x.AcademyId == academyId, token)) return "The selected course does not belong to this academy."; |
| if (r.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == r.BranchId && x.AcademyId == academyId, token)) return "The selected branch does not belong to this academy."; |
| if (r.TeacherId.HasValue && !await dbContext.Teachers.AnyAsync(x => x.Id == r.TeacherId && x.AcademyId == academyId, token)) return "The selected teacher does not belong to this academy."; |
| var code = Clean(r.BatchCode); if (code is not null && await dbContext.Batches.AnyAsync(x => x.AcademyId == academyId && x.Id != ignore && x.BatchCode == code, token)) return "That batch code is already in use."; |

| Return / serialization expressions |
| --- |
| var batch = await dbContext.Batches.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == batchId, token); if (batch is null) return NotFound(); |
| var problem = await Validate(academyId, request, token, batchId); if (problem is not null) return BadRequest(new { message = problem }); |
| var active = await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == batchId && x.Status == "Active", token); if (request.Capacity < active) return BadRequest(new { message = $"Capacity cannot be lower than the {active} active enrolments." }); |
| await dbContext.SaveChangesAsync(token); return Ok(Summary(batch, active)); |

### BatchPromotionsController.List (GET /api/academies/{academyId:guid}/batch-promotions)

Source: apps/api/Controllers/BatchPromotionsController.cs:3

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### BatchPromotionsController.Create (POST /api/academies/{academyId:guid}/batch-promotions)

Source: apps/api/Controllers/BatchPromotionsController.cs:3

| Validation/guard expressions |
| --- |
| [HttpPost]public async Task<ActionResult> Create(Guid academyId,PromotionRequest r,CancellationToken t){if(r.SourceBatchId==r.TargetBatchId\|\|!await db.Enrollments.AnyAsync(x=>x.AcademyId==academyId&&x.StudentId==r.StudentId&&x.BatchId==r.SourceBatchId&&x.Status=="Active",t)\|\|!await db.Batches.AnyAsync(x=>x.AcademyId==academyId&&x.Id==r.TargetBatchId,t))return BadRequest();db.BatchPromotions.Add(new BatchPromotion{AcademyId=academyId,StudentId=r.StudentId,SourceBatchId=r.SourceBatchId,TargetBatchId=r.TargetBatchId,EffectiveDate=r.EffectiveDate,Notes=r.Notes?.Trim()});await db.SaveChangesAsync(t);return Ok();} |

| Return / serialization expressions |
| --- |
| [HttpPost]public async Task<ActionResult> Create(Guid academyId,PromotionRequest r,CancellationToken t){if(r.SourceBatchId==r.TargetBatchId\|\|!await db.Enrollments.AnyAsync(x=>x.AcademyId==academyId&&x.StudentId==r.StudentId&&x.BatchId==r.SourceBatchId&&x.Status=="Active",t)\|\|!await db.Batches.AnyAsync(x=>x.AcademyId==academyId&&x.Id==r.TargetBatchId,t))return BadRequest();db.BatchPromotions.Add(new BatchPromotion{AcademyId=academyId,StudentId=r.StudentId,SourceBatchId=r.SourceBatchId,TargetBatchId=r.TargetBatchId,EffectiveDate=r.EffectiveDate,Notes=r.Notes?.Trim()});await db.SaveChangesAsync(t);return Ok();} |

### BatchPromotionsController.Decide (PATCH /api/academies/{academyId:guid}/batch-promotions/{id:guid}/decision)

Source: apps/api/Controllers/BatchPromotionsController.cs:3

| Validation/guard expressions |
| --- |
| [HttpPatch("{id:guid}/decision")]public async Task<ActionResult> Decide(Guid academyId,Guid id,PromotionDecision r,CancellationToken t){var x=await db.BatchPromotions.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();if(r.Status is not("Approved"or"Rejected"))return BadRequest();if(r.Status=="Approved"){var source=await db.Enrollments.SingleAsync(e=>e.AcademyId==academyId&&e.StudentId==x.StudentId&&e.BatchId==x.SourceBatchId&&e.Status=="Active",t);source.Status="Completed";source.EndDate=x.EffectiveDate;source.LifecycleReason="Promoted to target batch";db.Enrollments.Add(new Enrollment{AcademyId=academyId,StudentId=x.StudentId,BatchId=x.TargetBatchId,StartDate=x.EffectiveDate,Status="Active"});}x.Status=r.Status;x.Notes=r.Notes?.Trim()??x.Notes;await db.SaveChangesAsync(t);return Ok();}} |

| Return / serialization expressions |
| --- |
| [HttpPatch("{id:guid}/decision")]public async Task<ActionResult> Decide(Guid academyId,Guid id,PromotionDecision r,CancellationToken t){var x=await db.BatchPromotions.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();if(r.Status is not("Approved"or"Rejected"))return BadRequest();if(r.Status=="Approved"){var source=await db.Enrollments.SingleAsync(e=>e.AcademyId==academyId&&e.StudentId==x.StudentId&&e.BatchId==x.SourceBatchId&&e.Status=="Active",t);source.Status="Completed";source.EndDate=x.EffectiveDate;source.LifecycleReason="Promoted to target batch";db.Enrollments.Add(new Enrollment{AcademyId=academyId,StudentId=x.StudentId,BatchId=x.TargetBatchId,StartDate=x.EffectiveDate,Status="Active"});}x.Status=r.Status;x.Notes=r.Notes?.Trim()??x.Notes;await db.SaveChangesAsync(t);return Ok();}} |

### BranchesController.List (GET /api/academies/{academyId:guid}/branches)

Source: apps/api/Controllers/BranchesController.cs:12

| Validation/guard expressions |
| --- |
| if (!academyExists) |
| return NotFound(); |

| Return / serialization expressions |
| --- |
| return NotFound(); |
| return Ok(branches); |

### BranchesController.Create (POST /api/academies/{academyId:guid}/branches)

Source: apps/api/Controllers/BranchesController.cs:33

| Validation/guard expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) |
| return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.Name)) |
| return BadRequest(new { message = "Branch name is required." }); |

| Return / serialization expressions |
| --- |
| return NotFound(); |
| return BadRequest(new { message = "Branch name is required." }); |
| return CreatedAtAction(nameof(List), new { academyId }, response); |

### BranchesController.Update (PUT /api/academies/{academyId:guid}/branches/{branchId:guid})

Source: apps/api/Controllers/BranchesController.cs:72

| Validation/guard expressions |
| --- |
| public async Task<ActionResult<BranchSummary>> Update(Guid academyId,Guid branchId,UpdateBranchRequest request,CancellationToken token){var x=await dbContext.Branches.SingleOrDefaultAsync(b=>b.Id==branchId&&b.AcademyId==academyId,token);if(x is null)return NotFound();if(string.IsNullOrWhiteSpace(request.Name))return BadRequest();x.Name=request.Name.Trim();x.AddressLine1=request.AddressLine1?.Trim();x.City=request.City?.Trim();x.State=request.State?.Trim();x.PostalCode=request.PostalCode?.Trim();x.IsActive=request.IsActive;await dbContext.SaveChangesAsync(token);return Ok(new BranchSummary(x.Id,x.Name,x.City,x.State,x.PostalCode,x.IsActive));} |

| Return / serialization expressions |
| --- |
| public async Task<ActionResult<BranchSummary>> Update(Guid academyId,Guid branchId,UpdateBranchRequest request,CancellationToken token){var x=await dbContext.Branches.SingleOrDefaultAsync(b=>b.Id==branchId&&b.AcademyId==academyId,token);if(x is null)return NotFound();if(string.IsNullOrWhiteSpace(request.Name))return BadRequest();x.Name=request.Name.Trim();x.AddressLine1=request.AddressLine1?.Trim();x.City=request.City?.Trim();x.State=request.State?.Trim();x.PostalCode=request.PostalCode?.Trim();x.IsActive=request.IsActive;await dbContext.SaveChangesAsync(token);return Ok(new BranchSummary(x.Id,x.Name,x.City,x.State,x.PostalCode,x.IsActive));} |

### CertificatesController.List (GET /api/academies/{academyId:guid}/certificates)

Source: apps/api/Controllers/CertificatesController.cs:24

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| Ok(await dbContext.Certificates.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.IssuedDate) |

### CertificatesController.GetBranding (GET /api/academies/{academyId:guid}/certificates/branding)

Source: apps/api/Controllers/CertificatesController.cs:29

| Validation/guard expressions |
| --- |
| return academy is null ? NotFound() : Ok(ToBranding(academy)); |

| Return / serialization expressions |
| --- |


### CertificatesController.UpdateBranding (PUT /api/academies/{academyId:guid}/certificates/branding)

Source: apps/api/Controllers/CertificatesController.cs:36

| Validation/guard expressions |
| --- |
| if (academy is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (academy is null) return NotFound(); |
| return Ok(ToBranding(academy)); |

### CertificatesController.UploadLogo (POST /api/academies/{academyId:guid}/certificates/branding/logo)

Source: apps/api/Controllers/CertificatesController.cs:47

| Validation/guard expressions |
| --- |
| if (academy is null) return NotFound(); |
| if (logo is null \|\| logo.Length == 0 \|\| logo.Length > 2_500_000) |
| return BadRequest(new { message = "Upload a PNG, JPG or WebP logo smaller than 2.5 MB." }); |
| if (!expectedTypes.TryGetValue(extension, out var expectedType) \|\| !string.Equals(logo.ContentType, expectedType, StringComparison.OrdinalIgnoreCase)) |
| return BadRequest(new { message = "Only PNG, JPG and WebP logo files are supported." }); |

| Return / serialization expressions |
| --- |
| if (academy is null) return NotFound(); |
| return BadRequest(new { message = "Upload a PNG, JPG or WebP logo smaller than 2.5 MB." }); |
| return BadRequest(new { message = "Only PNG, JPG and WebP logo files are supported." }); |
| return Ok(ToBranding(academy)); |

### CertificatesController.Issue (POST /api/academies/{academyId:guid}/certificates)

Source: apps/api/Controllers/CertificatesController.cs:71

| Validation/guard expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Certificate title is required." }); |
| if (!TemplateKeys.Contains(request.TemplateKey ?? "music-recital")) return BadRequest(new { message = "Select a supported certificate theme." }); |
| if (!DesignKeys.Contains(request.DesignKey ?? "none")) return BadRequest(new { message = "Certificate artwork is no longer supported." }); |
| if (!InRange(request.ArtworkX, 0, 100) \|\| !InRange(request.ArtworkY, 0, 100) \|\| !InRange(request.ArtworkSize, 36, 180)) return BadRequest(new { message = "Artwork placement is outside the certificate canvas." }); |
| if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The student does not belong to this academy." }); |
| if (request.BatchId.HasValue && !await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The class or batch does not belong to this academy." }); |

| Return / serialization expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Certificate title is required." }); |
| if (!TemplateKeys.Contains(request.TemplateKey ?? "music-recital")) return BadRequest(new { message = "Select a supported certificate theme." }); |
| if (!DesignKeys.Contains(request.DesignKey ?? "none")) return BadRequest(new { message = "Certificate artwork is no longer supported." }); |
| if (!InRange(request.ArtworkX, 0, 100) \|\| !InRange(request.ArtworkY, 0, 100) \|\| !InRange(request.ArtworkSize, 36, 180)) return BadRequest(new { message = "Artwork placement is outside the certificate canvas." }); |
| if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The student does not belong to this academy." }); |
| if (request.BatchId.HasValue && !await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The class or batch does not belong to this academy." }); |
| return Created($"/api/academies/{academyId}/certificates/{certificate.Id}", ToSummary(certificate)); |

### CertificatesController.UpdateStatus (PATCH /api/academies/{academyId:guid}/certificates/{certificateId:guid}/status)

Source: apps/api/Controllers/CertificatesController.cs:94

| Validation/guard expressions |
| --- |
| if (certificate is null) return NotFound(); |
| if (!new[] { "Issued", "Revoked", "Replaced" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(); |

| Return / serialization expressions |
| --- |
| if (certificate is null) return NotFound(); |
| if (!new[] { "Issued", "Revoked", "Replaced" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(); |
| return Ok(); |

### CertificateVerificationController.Verify (GET /api/certificates/verify/{verificationCode})

Source: apps/api/Controllers/CertificateVerificationController.cs:13

| Validation/guard expressions |
| --- |
| return certificate is null ? NotFound(new { message = "Certificate verification code was not found." }) : Ok(certificate); |

| Return / serialization expressions |
| --- |


### ClassSessionsController.List (GET /api/academies/{academyId:guid}/sessions)

Source: apps/api/Controllers/ClassSessionsController.cs:12

| Validation/guard expressions |
| --- |
| if (fromUtc.HasValue) query = query.Where(x => x.StartUtc >= fromUtc.Value); |
| if (toUtc.HasValue) query = query.Where(x => x.StartUtc < toUtc.Value); |

| Return / serialization expressions |
| --- |
| return Ok(sessions); |

### ClassSessionsController.Create (POST /api/academies/{academyId:guid}/sessions)

Source: apps/api/Controllers/ClassSessionsController.cs:22

| Validation/guard expressions |
| --- |
| if (new[] { "Online", "Hybrid" }.Contains(request.DeliveryMode, StringComparer.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.RoomName)) return BadRequest(new { message = "A meeting link is required for online and hybrid classes." }); |
| if (batch is null) return BadRequest(new { message = "The selected batch does not belong to this academy." }); |
| if (request.EndUtc <= request.StartUtc) return BadRequest(new { message = "End time must be after start time." }); |
| if (request.TeacherId.HasValue && !await dbContext.Teachers.AnyAsync(x => x.Id == request.TeacherId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected teacher does not belong to this academy." }); |
| if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected branch does not belong to this academy." }); |
| if (request.TeacherId.HasValue && await overlaps.AnyAsync(x => x.TeacherId == request.TeacherId, cancellationToken)) return Conflict(new { message = "Teacher clash: this teacher already has a session during the selected time." }); |
| if (!string.IsNullOrWhiteSpace(request.RoomName) && await overlaps.AnyAsync(x => x.BranchId == request.BranchId && x.RoomName == request.RoomName.Trim(), cancellationToken)) return Conflict(new { message = "Room clash: this room is already scheduled during the selected time." }); |

| Return / serialization expressions |
| --- |
| if (new[] { "Online", "Hybrid" }.Contains(request.DeliveryMode, StringComparer.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.RoomName)) return BadRequest(new { message = "A meeting link is required for online and hybrid classes." }); |
| if (batch is null) return BadRequest(new { message = "The selected batch does not belong to this academy." }); |
| if (request.EndUtc <= request.StartUtc) return BadRequest(new { message = "End time must be after start time." }); |
| if (request.TeacherId.HasValue && !await dbContext.Teachers.AnyAsync(x => x.Id == request.TeacherId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected teacher does not belong to this academy." }); |
| if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected branch does not belong to this academy." }); |
| return Created($"/api/academies/{academyId}/sessions/{session.Id}", new ClassSessionSummary(session.Id, session.BatchId, session.TeacherId, session.BranchId, session.StartUtc, session.EndUtc, session.DeliveryMode, session.RoomName, session.Status)); |

### ClassSessionsController.Update (PUT /api/academies/{academyId:guid}/sessions/{sessionId:guid})

Source: apps/api/Controllers/ClassSessionsController.cs:40

| Validation/guard expressions |
| --- |
| if (new[] { "Online", "Hybrid" }.Contains(request.DeliveryMode, StringComparer.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.RoomName)) return BadRequest(new { message = "A meeting link is required for online and hybrid classes." }); |
| if (session is null) return NotFound(); |
| if (request.EndUtc <= request.StartUtc) return BadRequest(new { message = "End time must be after the start time." }); |
| if (!new[] { "Scheduled", "Completed", "Cancelled", "NoShow" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid session status." }); |

| Return / serialization expressions |
| --- |
| if (new[] { "Online", "Hybrid" }.Contains(request.DeliveryMode, StringComparer.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.RoomName)) return BadRequest(new { message = "A meeting link is required for online and hybrid classes." }); |
| if (session is null) return NotFound(); |
| if (request.EndUtc <= request.StartUtc) return BadRequest(new { message = "End time must be after the start time." }); |
| if (!new[] { "Scheduled", "Completed", "Cancelled", "NoShow" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid session status." }); |
| return Ok(new ClassSessionSummary(session.Id, session.BatchId, session.TeacherId, session.BranchId, session.StartUtc, session.EndUtc, session.DeliveryMode, session.RoomName, session.Status)); |

### CommunicationPreferencesController.List (GET /api/academies/{academyId:guid}/communication-preferences)

Source: apps/api/Controllers/CommunicationPreferencesController.cs:18

| Validation/guard expressions |
| --- |
| if (!await CanManage(academyId)) return Forbid(); |
| if (!string.IsNullOrWhiteSpace(recipientType)) query = query.Where(x => x.RecipientType == recipientType); |

| Return / serialization expressions |
| --- |
| if (!await CanManage(academyId)) return Forbid(); |
| return Ok(await query.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).Select(x => new CommunicationPreferenceSummary(x.Id, x.RecipientId, x.RecipientType, x.EmailAllowed, x.WhatsAppAllowed, x.MarketingAllowed, x.EmailOptedInAtUtc, x.WhatsAppOptedInAtUtc, x.OptedOutAtUtc, x.Notes)).ToListAsync(cancellationToken)); |

### CommunicationPreferencesController.Save (PUT /api/academies/{academyId:guid}/communication-preferences/{recipientType}/{recipientId:guid})

Source: apps/api/Controllers/CommunicationPreferencesController.cs:27

| Validation/guard expressions |
| --- |
| if (!await CanManage(academyId)) return Forbid(); |
| if (canonicalType is null) return BadRequest(new { message = "Preferences currently support Student and Guardian recipients." }); |
| if (!exists) return NotFound(); |
| if (preference is null) { preference = new CommunicationPreference { AcademyId = academyId, RecipientType = canonicalType, RecipientId = recipientId }; dbContext.CommunicationPreferences.Add(preference); } |

| Return / serialization expressions |
| --- |
| if (!await CanManage(academyId)) return Forbid(); |
| if (canonicalType is null) return BadRequest(new { message = "Preferences currently support Student and Guardian recipients." }); |
| if (!exists) return NotFound(); |
| return Ok(ToSummary(preference)); |

### CommunicationSettingsController.List (GET /api/academies/{academyId:guid}/communication-settings)

Source: apps/api/Controllers/CommunicationSettingsController.cs:22

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| return Ok(items.Select(ToSummary).ToList()); |

### CommunicationSettingsController.Save (PUT /api/academies/{academyId:guid}/communication-settings/{channel})

Source: apps/api/Controllers/CommunicationSettingsController.cs:34

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (canonicalChannel is null) return BadRequest(new { message = "Channel must be Email, WhatsApp, or Meeting." }); |
| if (string.IsNullOrWhiteSpace(request.Provider)) return BadRequest(new { message = "Select a provider." }); |
| if (status is null) return BadRequest(new { message = "Status is invalid." }); |
| if (canonicalChannel == "Email" && status == "Configured" && string.IsNullOrWhiteSpace(request.SenderAddress)) |
| return BadRequest(new { message = "A sender email address is required before enabling email." }); |
| if (canonicalChannel == "WhatsApp" && status == "Configured" && string.IsNullOrWhiteSpace(request.PhoneNumber)) |
| return BadRequest(new { message = "A dedicated WhatsApp number is required before enabling WhatsApp." }); |
| if (canonicalChannel == "Meeting" && status == "Configured" && (string.IsNullOrWhiteSpace(request.SenderAddress) \|\| string.IsNullOrWhiteSpace(request.ExternalAccountReference))) |
| return BadRequest(new { message = "An organizer email and provider application or tenant reference are required before enabling meetings." }); |
| if (item is null) |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (canonicalChannel is null) return BadRequest(new { message = "Channel must be Email, WhatsApp, or Meeting." }); |
| if (string.IsNullOrWhiteSpace(request.Provider)) return BadRequest(new { message = "Select a provider." }); |
| if (status is null) return BadRequest(new { message = "Status is invalid." }); |
| return BadRequest(new { message = "A sender email address is required before enabling email." }); |
| return BadRequest(new { message = "A dedicated WhatsApp number is required before enabling WhatsApp." }); |
| return BadRequest(new { message = "An organizer email and provider application or tenant reference are required before enabling meetings." }); |
| return Ok(ToSummary(item)); |

### CommunicationTemplatesController.List (GET /api/academies/{academyId:guid}/communication-templates)

Source: apps/api/Controllers/CommunicationTemplatesController.cs:23

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| return Ok(await dbContext.CommunicationTemplates.AsNoTracking().Where(x => x.AcademyId == academyId) |

### CommunicationTemplatesController.Create (POST /api/academies/{academyId:guid}/communication-templates)

Source: apps/api/Controllers/CommunicationTemplatesController.cs:33

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (!TryValidate(request, out var error)) return BadRequest(new { message = error }); |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (!TryValidate(request, out var error)) return BadRequest(new { message = error }); |
| return Created($"/api/academies/{academyId}/communication-templates/{template.Id}", ToSummary(template)); |

### CommunicationTemplatesController.StarterTemplateCatalogue (GET /api/academies/{academyId:guid}/communication-templates/starter-templates)

Source: apps/api/Controllers/CommunicationTemplatesController.cs:45

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| return Ok(StarterTemplates.Select(x => new StarterTemplateDefinition(x.Id, x.Channel, x.TemplateGroup, x.Name, x.TemplateKey, x.Category, x.Subject, x.Body)).ToList()); |

### CommunicationTemplatesController.AddStarterTemplates (POST /api/academies/{academyId:guid}/communication-templates/starter-templates)

Source: apps/api/Controllers/CommunicationTemplatesController.cs:52

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (request.TemplateIds is null \|\| request.TemplateIds.Count == 0) return BadRequest(new { message = "Select at least one starter template." }); |
| if (selectedTemplates.Count == 0) return BadRequest(new { message = "The selected templates are not valid." }); |
| if (additions.Count > 0) |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (request.TemplateIds is null \|\| request.TemplateIds.Count == 0) return BadRequest(new { message = "Select at least one starter template." }); |
| if (selectedTemplates.Count == 0) return BadRequest(new { message = "The selected templates are not valid." }); |
| return Ok(new StarterTemplateResult(additions.Count, additions.Count == 0 ? "All starter templates already exist." : $"Added {additions.Count} starter template(s). Review each one before use.")); |

### CommunicationTemplatesController.Update (PUT /api/academies/{academyId:guid}/communication-templates/{templateId:guid})

Source: apps/api/Controllers/CommunicationTemplatesController.cs:91

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (!TryValidate(request, out var error)) return BadRequest(new { message = error }); |
| if (template is null) return NotFound(); |
| if (Canonical(Channels, request.Channel) is null) { error = "Channel must be Email or WhatsApp."; return false; } |
| if (Canonical(Categories, request.Category) is null) { error = "Choose a valid template category."; return false; } |
| if (Canonical(Statuses, request.Status) is null) { error = "Choose a valid template status."; return false; } |
| if (string.IsNullOrWhiteSpace(request.Name) \|\| string.IsNullOrWhiteSpace(request.TemplateKey) \|\| string.IsNullOrWhiteSpace(request.Body)) { error = "Name, template key, and message body are required."; return false; } |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (!TryValidate(request, out var error)) return BadRequest(new { message = error }); |
| if (template is null) return NotFound(); |
| return Ok(ToSummary(template)); |

### ComplianceController.Documents (GET /api/academies/{academyId:guid}/compliance/documents)

Source: apps/api/Controllers/ComplianceController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| Ok(await db.PersonDocuments.AsNoTracking().Where(document => document.AcademyId == academyId) |

### ComplianceController.CreateReviewTask (POST /api/academies/{academyId:guid}/compliance/documents/{documentId:guid}/review-task)

Source: apps/api/Controllers/ComplianceController.cs:17

| Validation/guard expressions |
| --- |
| if (document is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (document is null) return NotFound(); |
| return Ok(); |

### ComplianceController.AddDocument (POST /api/academies/{academyId:guid}/compliance/documents)

Source: apps/api/Controllers/ComplianceController.cs:27

| Validation/guard expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.DocumentType) \|\| string.IsNullOrWhiteSpace(request.FileName)) |
| return BadRequest("Document type and file name are required."); |

| Return / serialization expressions |
| --- |
| return BadRequest("Document type and file name are required."); |
| return Ok(document); |

### ComplianceController.ReviewDocument (PATCH /api/academies/{academyId:guid}/compliance/documents/{documentId:guid}/review)

Source: apps/api/Controllers/ComplianceController.cs:45

| Validation/guard expressions |
| --- |
| if (document is null) return NotFound(); |
| if (!allowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest("Invalid review status."); |

| Return / serialization expressions |
| --- |
| if (document is null) return NotFound(); |
| if (!allowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest("Invalid review status."); |
| return Ok(document); |

### ComplianceController.Consents (GET /api/academies/{academyId:guid}/compliance/consents)

Source: apps/api/Controllers/ComplianceController.cs:58

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| Ok(await db.ConsentRecords.AsNoTracking().Where(consent => consent.AcademyId == academyId) |

### ComplianceController.AddConsent (POST /api/academies/{academyId:guid}/compliance/consents)

Source: apps/api/Controllers/ComplianceController.cs:63

| Validation/guard expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.ConsentType) \|\| (request.StudentId is null && request.GuardianId is null)) |
| return BadRequest("A consent type and student or guardian are required."); |

| Return / serialization expressions |
| --- |
| return BadRequest("A consent type and student or guardian are required."); |
| return Ok(consent); |

### ComplianceController.WithdrawConsent (PATCH /api/academies/{academyId:guid}/compliance/consents/{consentId:guid}/withdraw)

Source: apps/api/Controllers/ComplianceController.cs:79

| Validation/guard expressions |
| --- |
| if (consent is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (consent is null) return NotFound(); |
| return Ok(consent); |

### CourseModulesController.List (GET /api/academies/{academyId:guid}/course-modules)

Source: apps/api/Controllers/CourseModulesController.cs:2

| Validation/guard expressions |
| --- |
| [HttpGet]public async Task<ActionResult> List(Guid academyId,Guid? courseId,CancellationToken t){var q=db.CourseModules.AsNoTracking().Where(x=>x.AcademyId==academyId);if(courseId.HasValue)q=q.Where(x=>x.CourseId==courseId);return Ok(await q.OrderBy(x=>x.Sequence).Select(x=>new{x.Id,x.CourseId,x.Title,x.Description,x.Sequence,x.IsPublished}).ToListAsync(t));} |

| Return / serialization expressions |
| --- |
| [HttpGet]public async Task<ActionResult> List(Guid academyId,Guid? courseId,CancellationToken t){var q=db.CourseModules.AsNoTracking().Where(x=>x.AcademyId==academyId);if(courseId.HasValue)q=q.Where(x=>x.CourseId==courseId);return Ok(await q.OrderBy(x=>x.Sequence).Select(x=>new{x.Id,x.CourseId,x.Title,x.Description,x.Sequence,x.IsPublished}).ToListAsync(t));} |

### CourseModulesController.Create (POST /api/academies/{academyId:guid}/course-modules)

Source: apps/api/Controllers/CourseModulesController.cs:2

| Validation/guard expressions |
| --- |
| [HttpPost]public async Task<ActionResult> Create(Guid academyId,CreateModule r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Title)\|\|!await db.Courses.AnyAsync(x=>x.Id==r.CourseId&&x.AcademyId==academyId,t))return BadRequest(new{message="Valid course and title required."});db.CourseModules.Add(new CourseModule{AcademyId=academyId,CourseId=r.CourseId,Title=r.Title.Trim(),Description=r.Description?.Trim(),Sequence=r.Sequence,IsPublished=false});await db.SaveChangesAsync(t);return Ok();} |

| Return / serialization expressions |
| --- |
| [HttpPost]public async Task<ActionResult> Create(Guid academyId,CreateModule r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Title)\|\|!await db.Courses.AnyAsync(x=>x.Id==r.CourseId&&x.AcademyId==academyId,t))return BadRequest(new{message="Valid course and title required."});db.CourseModules.Add(new CourseModule{AcademyId=academyId,CourseId=r.CourseId,Title=r.Title.Trim(),Description=r.Description?.Trim(),Sequence=r.Sequence,IsPublished=false});await db.SaveChangesAsync(t);return Ok();} |

### CourseModulesController.SetPublication (PATCH /api/academies/{academyId:guid}/course-modules/{moduleId:guid}/publication)

Source: apps/api/Controllers/CourseModulesController.cs:2

| Validation/guard expressions |
| --- |
| [HttpPatch("{moduleId:guid}/publication")]public async Task<ActionResult> SetPublication(Guid academyId,Guid moduleId,PublicationRequest r,CancellationToken t){var module=await db.CourseModules.SingleOrDefaultAsync(x=>x.Id==moduleId&&x.AcademyId==academyId,t);if(module is null)return NotFound();module.IsPublished=r.IsPublished;await db.SaveChangesAsync(t);return Ok(new{module.Id,module.IsPublished});}} |

| Return / serialization expressions |
| --- |
| [HttpPatch("{moduleId:guid}/publication")]public async Task<ActionResult> SetPublication(Guid academyId,Guid moduleId,PublicationRequest r,CancellationToken t){var module=await db.CourseModules.SingleOrDefaultAsync(x=>x.Id==moduleId&&x.AcademyId==academyId,t);if(module is null)return NotFound();module.IsPublished=r.IsPublished;await db.SaveChangesAsync(t);return Ok(new{module.Id,module.IsPublished});}} |

### CourseModuleStatusController.Publish (PATCH /api/academies/{academyId:guid}/course-modules/{moduleId:guid}/publish)

Source: apps/api/Controllers/CourseModuleStatusController.cs:11

| Validation/guard expressions |
| --- |
| if (module is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (module is null) return NotFound(); |
| return Ok(); |

### CoursesController.List (GET /api/academies/{academyId:guid}/courses)

Source: apps/api/Controllers/CoursesController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### CoursesController.Create (POST /api/academies/{academyId:guid}/courses)

Source: apps/api/Controllers/CoursesController.cs:15

| Validation/guard expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound(); |
| if (problem is not null) return BadRequest(new { message = problem }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound(); |
| if (problem is not null) return BadRequest(new { message = problem }); |
| return Created($"/api/academies/{academyId}/courses/{course.Id}", ToSummary(course)); |

### CoursesController.Update (PUT /api/academies/{academyId:guid}/courses/{courseId:guid})

Source: apps/api/Controllers/CoursesController.cs:26

| Validation/guard expressions |
| --- |
| var course = await dbContext.Courses.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == courseId, token); if (course is null) return NotFound(); |
| if (problem is not null) return BadRequest(new { message = problem }); |
| if (string.IsNullOrWhiteSpace(name)) return "Course name is required."; |
| if (!new[] { "Music", "Tuition", "Coaching" }.Contains((type ?? "Music").Trim(), StringComparer.OrdinalIgnoreCase)) return "Academy type must be Music, Tuition, or Coaching."; |
| if (weeklySessions is < 1 or > 14 \|\| sessionMinutes is < 15 or > 480) return "Weekly sessions must be 1–14 and session duration 15–480 minutes."; |
| if (minimumAge is < 0 or > 120 \|\| maximumAge is < 0 or > 120 \|\| (minimumAge.HasValue && maximumAge.HasValue && minimumAge > maximumAge)) return "Check the minimum and maximum age range."; |
| var code = Clean(courseCode); if (code is not null && await dbContext.Courses.AnyAsync(x => x.AcademyId == academyId && x.Id != ignore && x.CourseCode == code, token)) return "That course code is already in use."; |

| Return / serialization expressions |
| --- |
| var course = await dbContext.Courses.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == courseId, token); if (course is null) return NotFound(); |
| if (problem is not null) return BadRequest(new { message = problem }); |
| Apply(course, request); course.IsActive = request.IsActive; await dbContext.SaveChangesAsync(token); return Ok(ToSummary(course)); |

### DashboardController.Summary (GET /api/academies/{academyId:guid}/dashboard)

Source: apps/api/Controllers/DashboardController.cs:15

| Validation/guard expressions |
| --- |
| if (user?.AcademyId != academyId) return Forbid(); |
| if (academy is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId != academyId) return Forbid(); |
| if (academy is null) return NotFound(); |
| return Ok(new DashboardSummary( |

### EnrollmentsController.List (GET /api/academies/{academyId:guid}/enrollments)

Source: apps/api/Controllers/EnrollmentsController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| return Ok(enrollments); |

### EnrollmentsController.Create (POST /api/academies/{academyId:guid}/enrollments)

Source: apps/api/Controllers/EnrollmentsController.cs:19

| Validation/guard expressions |
| --- |
| if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected student does not belong to this academy." }); |
| if (batch is null) return BadRequest(new { message = "The selected batch does not belong to this academy." }); |
| if (prerequisites.Count > 0) |
| if (prerequisites.Any(prerequisite => !completedCourseIds.Contains(prerequisite))) return Conflict(new { message = "The learner has not completed all course prerequisites for this batch." }); |
| if (!batch.IsActive \|\| !string.Equals(batch.EnrollmentStatus, "Open", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "This batch is not open for enrolment." }); |
| if (requestedStatus == "Active" && activeCount >= batch.Capacity) return Conflict(new { message = "This batch has reached its active enrolment capacity. Add the learner to the waitlist or select another batch." }); |
| if (requestedStatus == "Waitlisted" && (batch.WaitlistCapacity <= 0 \|\| waitlistCount >= batch.WaitlistCapacity)) return Conflict(new { message = "This batch waitlist is full or not enabled." }); |
| if (await dbContext.Enrollments.AnyAsync(x => x.AcademyId == academyId && x.StudentId == request.StudentId && x.BatchId == request.BatchId && x.Status == "Active", cancellationToken)) return Conflict(new { message = "The student is already enrolled in this batch." }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected student does not belong to this academy." }); |
| if (batch is null) return BadRequest(new { message = "The selected batch does not belong to this academy." }); |
| if (!batch.IsActive \|\| !string.Equals(batch.EnrollmentStatus, "Open", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "This batch is not open for enrolment." }); |
| return Created($"/api/academies/{academyId}/enrollments/{enrollment.Id}", new EnrollmentSummary(enrollment.Id, enrollment.StudentId, enrollment.BatchId, enrollment.StartDate, enrollment.EndDate, enrollment.Status)); |

### EnrollmentsController.Update (PUT /api/academies/{academyId:guid}/enrollments/{enrollmentId:guid})

Source: apps/api/Controllers/EnrollmentsController.cs:42

| Validation/guard expressions |
| --- |
| if (enrollment is null) return NotFound(); |
| if (!new[] { "Active", "Waitlisted", "Completed", "Withdrawn", "Cancelled", "Paused" }.Contains(status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Status must be Active, Waitlisted, Completed, Withdrawn, Cancelled, or Paused." }); |
| if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.LifecycleReason)) return BadRequest(new { message = "A lifecycle reason is required when changing an enrolment from Active." }); |

| Return / serialization expressions |
| --- |
| if (enrollment is null) return NotFound(); |
| if (!new[] { "Active", "Waitlisted", "Completed", "Withdrawn", "Cancelled", "Paused" }.Contains(status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Status must be Active, Waitlisted, Completed, Withdrawn, Cancelled, or Paused." }); |
| if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.LifecycleReason)) return BadRequest(new { message = "A lifecycle reason is required when changing an enrolment from Active." }); |
| return Ok(new EnrollmentSummary(enrollment.Id, enrollment.StudentId, enrollment.BatchId, enrollment.StartDate, enrollment.EndDate, enrollment.Status)); |

### EnrollmentsController.Transfer (POST /api/academies/{academyId:guid}/enrollments/{enrollmentId:guid}/transfer)

Source: apps/api/Controllers/EnrollmentsController.cs:55

| Validation/guard expressions |
| --- |
| if (source is null \|\| target is null) return NotFound(); |
| if (source.Status != "Active" \|\| !target.IsActive \|\| !string.Equals(target.EnrollmentStatus, "Open", StringComparison.OrdinalIgnoreCase)) return Conflict(new { message = "Only active enrolments can transfer to an open batch." }); |
| if (await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == target.Id && x.Status == "Active", token) >= target.Capacity) return Conflict(new { message = "Target batch is at capacity." }); |

| Return / serialization expressions |
| --- |
| if (source is null \|\| target is null) return NotFound(); |
| return Ok(new EnrollmentSummary(replacement.Id, replacement.StudentId, replacement.BatchId, replacement.StartDate, replacement.EndDate, replacement.Status)); |

### EventsController.List (GET /api/academies/{academyId:guid}/events)

Source: apps/api/Controllers/EventsController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### EventsController.Create (POST /api/academies/{academyId:guid}/events)

Source: apps/api/Controllers/EventsController.cs:14

| Validation/guard expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Title) \|\| request.EndUtc <= request.StartUtc) return BadRequest(new { message = "Title and valid event times are required." }); |
| if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Invalid branch." }); |

| Return / serialization expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Title) \|\| request.EndUtc <= request.StartUtc) return BadRequest(new { message = "Title and valid event times are required." }); |
| if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Invalid branch." }); |
| dbContext.AcademyEvents.Add(item); await dbContext.SaveChangesAsync(token); return Created($"/api/academies/{academyId}/events/{item.Id}", new EventSummary(item.Id, item.Title, item.Type, item.BranchId, item.StartUtc, item.EndUtc, item.Venue, item.Capacity, item.Status, item.Notes)); |

### EventsController.UpdateStatus (PATCH /api/academies/{academyId:guid}/events/{eventId:guid}/status)

Source: apps/api/Controllers/EventsController.cs:22

| Validation/guard expressions |
| --- |
| { var x=await dbContext.AcademyEvents.SingleOrDefaultAsync(v=>v.Id==eventId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(!new[]{"Planned","Published","Completed","Cancelled"}.Contains(request.Status,StringComparer.OrdinalIgnoreCase))return BadRequest(); x.Status=request.Status.Trim(); await dbContext.SaveChangesAsync(token); return Ok(); } |

| Return / serialization expressions |
| --- |
| { var x=await dbContext.AcademyEvents.SingleOrDefaultAsync(v=>v.Id==eventId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(!new[]{"Planned","Published","Completed","Cancelled"}.Contains(request.Status,StringComparer.OrdinalIgnoreCase))return BadRequest(); x.Status=request.Status.Trim(); await dbContext.SaveChangesAsync(token); return Ok(); } |

### ExpensesController.List (GET /api/academies/{academyId:guid}/expenses)

Source: apps/api/Controllers/ExpensesController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### ExpensesController.Create (POST /api/academies/{academyId:guid}/expenses)

Source: apps/api/Controllers/ExpensesController.cs:15

| Validation/guard expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.Description) \|\| request.Amount <= 0) return BadRequest(new { message = "Description and a positive amount are required." }); |
| if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected branch does not belong to this academy." }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.Description) \|\| request.Amount <= 0) return BadRequest(new { message = "Description and a positive amount are required." }); |
| if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected branch does not belong to this academy." }); |
| return Created($"/api/academies/{academyId}/expenses/{expense.Id}", new ExpenseSummary(expense.Id, expense.Description, expense.Amount, expense.Currency, expense.Category, expense.BranchId, expense.ExpenseDate, expense.Status)); |

### ExpensesController.Update (PUT /api/academies/{academyId:guid}/expenses/{expenseId:guid})

Source: apps/api/Controllers/ExpensesController.cs:25

| Validation/guard expressions |
| --- |
| { var x=await dbContext.Expenses.SingleOrDefaultAsync(v=>v.Id==expenseId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(string.IsNullOrWhiteSpace(request.Description)\|\|request.Amount<=0)return BadRequest(); x.Description=request.Description.Trim();x.Amount=request.Amount;x.Category=request.Category?.Trim()??"General";x.BranchId=request.BranchId;x.ExpenseDate=request.ExpenseDate;x.Status=request.Status?.Trim()??x.Status;await dbContext.SaveChangesAsync(token);return Ok(); } |

| Return / serialization expressions |
| --- |
| { var x=await dbContext.Expenses.SingleOrDefaultAsync(v=>v.Id==expenseId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(string.IsNullOrWhiteSpace(request.Description)\|\|request.Amount<=0)return BadRequest(); x.Description=request.Description.Trim();x.Amount=request.Amount;x.Category=request.Category?.Trim()??"General";x.BranchId=request.BranchId;x.ExpenseDate=request.ExpenseDate;x.Status=request.Status?.Trim()??x.Status;await dbContext.SaveChangesAsync(token);return Ok(); } |

### FeePlansController.List (GET /api/academies/{academyId:guid}/fee-plans)

Source: apps/api/Controllers/FeePlansController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### FeePlansController.Create (POST /api/academies/{academyId:guid}/fee-plans)

Source: apps/api/Controllers/FeePlansController.cs:15

| Validation/guard expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.Name) \|\| request.Amount <= 0) return BadRequest(new { message = "Name and a positive amount are required." }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.Name) \|\| request.Amount <= 0) return BadRequest(new { message = "Name and a positive amount are required." }); |
| return Created($"/api/academies/{academyId}/fee-plans/{plan.Id}", new FeePlanSummary(plan.Id, plan.Name, plan.Amount, plan.Currency, plan.Frequency, plan.IsActive)); |

### FeePlansController.Update (PUT /api/academies/{academyId:guid}/fee-plans/{feePlanId:guid})

Source: apps/api/Controllers/FeePlansController.cs:24

| Validation/guard expressions |
| --- |
| { var x=await dbContext.FeePlans.SingleOrDefaultAsync(v=>v.Id==feePlanId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(string.IsNullOrWhiteSpace(request.Name)\|\|request.Amount<=0)return BadRequest(); x.Name=request.Name.Trim();x.Amount=request.Amount;x.Frequency=request.Frequency?.Trim()??"Monthly";x.IsActive=request.IsActive;await dbContext.SaveChangesAsync(token);return Ok(); } |

| Return / serialization expressions |
| --- |
| { var x=await dbContext.FeePlans.SingleOrDefaultAsync(v=>v.Id==feePlanId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(string.IsNullOrWhiteSpace(request.Name)\|\|request.Amount<=0)return BadRequest(); x.Name=request.Name.Trim();x.Amount=request.Amount;x.Frequency=request.Frequency?.Trim()??"Monthly";x.IsActive=request.IsActive;await dbContext.SaveChangesAsync(token);return Ok(); } |

### FeeRemindersController.Queue (POST /api/academies/{academyId:guid}/fee-reminders)

Source: apps/api/Controllers/FeeRemindersController.cs:12

| Validation/guard expressions |
| --- |
| if (request.InvoiceId.HasValue) invoices = invoices.Where(x => x.Id == request.InvoiceId.Value); |
| if (invoiceList.Count == 0) return Ok(new FeeReminderResult(0, "No matching invoices need a reminder.")); |
| if (balance <= 0) continue; |

| Return / serialization expressions |
| --- |
| if (invoiceList.Count == 0) return Ok(new FeeReminderResult(0, "No matching invoices need a reminder.")); |
| return Ok(new FeeReminderResult(queued, $"Queued {queued} fee reminder(s).")); |

### FinanceAdjustmentsController.List (GET /api/academies/{academyId:guid}/finance-adjustments)

Source: apps/api/Controllers/FinanceAdjustmentsController.cs:12

| Validation/guard expressions |
| --- |
| if (invoiceId.HasValue) query = query.Where(x => x.InvoiceId == invoiceId); |

| Return / serialization expressions |
| --- |
| return Ok(await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => new FinanceAdjustmentSummary(x.Id, x.InvoiceId, x.Type, x.Amount, x.Currency, x.Reason, x.Status, x.ApprovedAtUtc, x.ApprovalNotes)).ToListAsync(token)); |

### FinanceAdjustmentsController.Create (POST /api/academies/{academyId:guid}/finance-adjustments)

Source: apps/api/Controllers/FinanceAdjustmentsController.cs:19

| Validation/guard expressions |
| --- |
| if (invoice is null) return NotFound(); |
| if (request.Amount <= 0 \|\| string.IsNullOrWhiteSpace(request.Reason) \|\| !AllowedTypes.Contains(request.Type ?? "")) return BadRequest(new { message = "Enter a valid type, positive amount, and reason." }); |

| Return / serialization expressions |
| --- |
| if (invoice is null) return NotFound(); |
| if (request.Amount <= 0 \|\| string.IsNullOrWhiteSpace(request.Reason) \|\| !AllowedTypes.Contains(request.Type ?? "")) return BadRequest(new { message = "Enter a valid type, positive amount, and reason." }); |
| db.FinanceAdjustments.Add(adjustment); await db.SaveChangesAsync(token); return Ok(ToSummary(adjustment)); |

### FinanceAdjustmentsController.Decide (PATCH /api/academies/{academyId:guid}/finance-adjustments/{adjustmentId:guid}/approval)

Source: apps/api/Controllers/FinanceAdjustmentsController.cs:28

| Validation/guard expressions |
| --- |
| var item = await db.FinanceAdjustments.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == adjustmentId, token); if (item is null) return NotFound(); |
| if (item.Status != "PendingApproval") return Conflict(new { message = "This adjustment has already been decided." }); |
| if (request.Approve) |

| Return / serialization expressions |
| --- |
| var item = await db.FinanceAdjustments.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == adjustmentId, token); if (item is null) return NotFound(); |
| await db.SaveChangesAsync(token); return Ok(ToSummary(item)); |

### FinanceGovernanceController.Get (GET /api/academies/{academyId:guid}/finance-governance/settings)

Source: apps/api/Controllers/FinanceGovernanceController.cs:11

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| [HttpGet("settings")] public async Task<ActionResult<FinanceSettingsSummary>> Get(Guid academyId,CancellationToken t){var x=await db.AcademyFinanceSettings.AsNoTracking().SingleOrDefaultAsync(x=>x.AcademyId==academyId,t);return Ok(x is null?new FinanceSettingsSummary("","GST",0,7,false,null,null,null,null,"Classic","Standard"):new(x.TaxRegistrationNumber,x.TaxLabel,x.TaxRatePercent,x.DefaultPaymentTermsDays,x.TaxInclusivePricing,x.InvoiceLogoUrl,x.InvoiceAuthorityName,x.InvoiceAuthorityTitle,x.InvoiceSignatureUrl,x.InvoiceTemplateKey,x.PayslipTemplateKey));} |

### FinanceGovernanceController.Save (PUT /api/academies/{academyId:guid}/finance-governance/settings)

Source: apps/api/Controllers/FinanceGovernanceController.cs:12

| Validation/guard expressions |
| --- |
| [HttpPut("settings")] public async Task<ActionResult<FinanceSettingsSummary>> Save(Guid academyId,FinanceSettingsRequest r,CancellationToken t){if(r.TaxRatePercent<0\|\|r.TaxRatePercent>100\|\|r.DefaultPaymentTermsDays<0\|\|r.DefaultPaymentTermsDays>365)return BadRequest(new{message="Check tax rate and payment terms."});if(!new[]{"Classic","Modern","Minimal","Formal"}.Contains(r.InvoiceTemplateKey??"Classic")\|\|!new[]{"Standard","Compact","Professional"}.Contains(r.PayslipTemplateKey??"Standard"))return BadRequest(new{message="Choose a supported document theme."});var x=await db.AcademyFinanceSettings.SingleOrDefaultAsync(x=>x.AcademyId==academyId,t);if(x is null){x=new AcademyFinanceSettings{AcademyId=academyId};db.AcademyFinanceSettings.Add(x);}x.TaxRegistrationNumber=r.TaxRegistrationNumber?.Trim()??"";x.TaxLabel=string.IsNullOrWhiteSpace(r.TaxLabel)?"GST":r.TaxLabel.Trim();x.TaxRatePercent=r.TaxRatePercent;x.DefaultPaymentTermsDays=r.DefaultPaymentTermsDays;x.TaxInclusivePricing=r.TaxInclusivePricing;x.InvoiceLogoUrl=Clean(r.InvoiceLogoUrl);x.InvoiceAuthorityName=Clean(r.InvoiceAuthorityName);x.InvoiceAuthorityTitle=Clean(r.InvoiceAuthorityTitle);x.InvoiceSignatureUrl=Clean(r.InvoiceSignatureUrl);x.InvoiceTemplateKey=r.InvoiceTemplateKey??"Classic";x.PayslipTemplateKey=r.PayslipTemplateKey??"Standard";await db.SaveChangesAsync(t);return Ok(new FinanceSettingsSummary(x.TaxRegistrationNumber,x.TaxLabel,x.TaxRatePercent,x.DefaultPaymentTermsDays,x.TaxInclusivePricing,x.InvoiceLogoUrl,x.InvoiceAuthorityName,x.InvoiceAuthorityTitle,x.InvoiceSignatureUrl,x.InvoiceTemplateKey,x.PayslipTemplateKey));} |

| Return / serialization expressions |
| --- |
| [HttpPut("settings")] public async Task<ActionResult<FinanceSettingsSummary>> Save(Guid academyId,FinanceSettingsRequest r,CancellationToken t){if(r.TaxRatePercent<0\|\|r.TaxRatePercent>100\|\|r.DefaultPaymentTermsDays<0\|\|r.DefaultPaymentTermsDays>365)return BadRequest(new{message="Check tax rate and payment terms."});if(!new[]{"Classic","Modern","Minimal","Formal"}.Contains(r.InvoiceTemplateKey??"Classic")\|\|!new[]{"Standard","Compact","Professional"}.Contains(r.PayslipTemplateKey??"Standard"))return BadRequest(new{message="Choose a supported document theme."});var x=await db.AcademyFinanceSettings.SingleOrDefaultAsync(x=>x.AcademyId==academyId,t);if(x is null){x=new AcademyFinanceSettings{AcademyId=academyId};db.AcademyFinanceSettings.Add(x);}x.TaxRegistrationNumber=r.TaxRegistrationNumber?.Trim()??"";x.TaxLabel=string.IsNullOrWhiteSpace(r.TaxLabel)?"GST":r.TaxLabel.Trim();x.TaxRatePercent=r.TaxRatePercent;x.DefaultPaymentTermsDays=r.DefaultPaymentTermsDays;x.TaxInclusivePricing=r.TaxInclusivePricing;x.InvoiceLogoUrl=Clean(r.InvoiceLogoUrl);x.InvoiceAuthorityName=Clean(r.InvoiceAuthorityName);x.InvoiceAuthorityTitle=Clean(r.InvoiceAuthorityTitle);x.InvoiceSignatureUrl=Clean(r.InvoiceSignatureUrl);x.InvoiceTemplateKey=r.InvoiceTemplateKey??"Classic";x.PayslipTemplateKey=r.PayslipTemplateKey??"Standard";await db.SaveChangesAsync(t);return Ok(new FinanceSettingsSummary(x.TaxRegistrationNumber,x.TaxLabel,x.TaxRatePercent,x.DefaultPaymentTermsDays,x.TaxInclusivePricing,x.InvoiceLogoUrl,x.InvoiceAuthorityName,x.InvoiceAuthorityTitle,x.InvoiceSignatureUrl,x.InvoiceTemplateKey,x.PayslipTemplateKey));} |

### FinanceGovernanceController.Collections (GET /api/academies/{academyId:guid}/finance-governance/collections)

Source: apps/api/Controllers/FinanceGovernanceController.cs:13

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### FinanceGovernanceController.Summary (GET /api/academies/{academyId:guid}/finance-governance/summary)

Source: apps/api/Controllers/FinanceGovernanceController.cs:14

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| [HttpGet("summary")] public async Task<ActionResult> Summary(Guid academyId,CancellationToken t){var invoices=await db.Invoices.Where(x=>x.AcademyId==academyId).ToListAsync(t);var payments=await db.Payments.Where(x=>x.AcademyId==academyId&&x.Status!="Voided").ToListAsync(t);return Ok(new{GrossBilled=invoices.Sum(x=>x.TotalAmount),ApprovedAdjustments=invoices.Sum(x=>x.AdjustedAmount),Collected=payments.Sum(x=>x.Amount),Outstanding=Math.Max(0,invoices.Sum(x=>x.TotalAmount-x.AdjustedAmount)-payments.Sum(x=>x.Amount)),Reconciled=payments.Where(x=>x.Status=="Reconciled").Sum(x=>x.Amount),OverdueInvoices=invoices.Count(x=>x.Status!="Paid"&&x.Status!="Cancelled"&&x.DueDate<DateOnly.FromDateTime(DateTime.UtcNow))});} |

### FinanceGovernanceController.CreateFollowUp (POST /api/academies/{academyId:guid}/finance-governance/collections/{invoiceId:guid}/follow-up)

Source: apps/api/Controllers/FinanceGovernanceController.cs:15

| Validation/guard expressions |
| --- |
| [HttpPost("collections/{invoiceId:guid}/follow-up")] public async Task<ActionResult> CreateFollowUp(Guid academyId,Guid invoiceId,CollectionFollowUpRequest r,CancellationToken t){var invoice=await db.Invoices.SingleOrDefaultAsync(x=>x.Id==invoiceId&&x.AcademyId==academyId,t);if(invoice is null)return NotFound();if(invoice.Status is "Paid" or "Cancelled")return Conflict(new{message="This invoice no longer requires collections follow-up."});var item=new AdminWorkItem{AcademyId=academyId,Type="Collections",Title=$"Collect {invoice.InvoiceNumber}",Description=string.IsNullOrWhiteSpace(r.Note)?$"Overdue invoice {invoice.InvoiceNumber}.":r.Note.Trim(),Priority=string.IsNullOrWhiteSpace(r.Priority)?"High":r.Priority.Trim(),AssignedUserId=r.AssignedUserId,EntityType="Invoice",EntityId=invoice.Id,DueAtUtc=r.DueAtUtc};db.AdminWorkItems.Add(item);await db.SaveChangesAsync(t);return Ok(item);} |

| Return / serialization expressions |
| --- |
| [HttpPost("collections/{invoiceId:guid}/follow-up")] public async Task<ActionResult> CreateFollowUp(Guid academyId,Guid invoiceId,CollectionFollowUpRequest r,CancellationToken t){var invoice=await db.Invoices.SingleOrDefaultAsync(x=>x.Id==invoiceId&&x.AcademyId==academyId,t);if(invoice is null)return NotFound();if(invoice.Status is "Paid" or "Cancelled")return Conflict(new{message="This invoice no longer requires collections follow-up."});var item=new AdminWorkItem{AcademyId=academyId,Type="Collections",Title=$"Collect {invoice.InvoiceNumber}",Description=string.IsNullOrWhiteSpace(r.Note)?$"Overdue invoice {invoice.InvoiceNumber}.":r.Note.Trim(),Priority=string.IsNullOrWhiteSpace(r.Priority)?"High":r.Priority.Trim(),AssignedUserId=r.AssignedUserId,EntityType="Invoice",EntityId=invoice.Id,DueAtUtc=r.DueAtUtc};db.AdminWorkItems.Add(item);await db.SaveChangesAsync(t);return Ok(item);} |

### GradingSchemeLifecycleController.Active (GET /api/academies/{academyId:guid}/grading-schemes/active)

Source: apps/api/Controllers/GradingSchemeLifecycleController.cs:4

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### GradingSchemeLifecycleController.Status (PATCH /api/academies/{academyId:guid}/grading-schemes/{schemeId:guid}/status)

Source: apps/api/Controllers/GradingSchemeLifecycleController.cs:4

| Validation/guard expressions |
| --- |
| [HttpPatch("{schemeId:guid}/status")]public async Task<ActionResult> Status(Guid academyId,Guid schemeId,SchemeStatusRequest r,CancellationToken t){var scheme=await db.GradingSchemes.SingleOrDefaultAsync(x=>x.Id==schemeId&&x.AcademyId==academyId,t);if(scheme is null)return NotFound();scheme.IsActive=r.IsActive;await db.SaveChangesAsync(t);return Ok(new{scheme.Id,scheme.IsActive});}} |

| Return / serialization expressions |
| --- |
| [HttpPatch("{schemeId:guid}/status")]public async Task<ActionResult> Status(Guid academyId,Guid schemeId,SchemeStatusRequest r,CancellationToken t){var scheme=await db.GradingSchemes.SingleOrDefaultAsync(x=>x.Id==schemeId&&x.AcademyId==academyId,t);if(scheme is null)return NotFound();scheme.IsActive=r.IsActive;await db.SaveChangesAsync(t);return Ok(new{scheme.Id,scheme.IsActive});}} |

### GuardiansController.List (GET /api/academies/{academyId:guid}/guardians)

Source: apps/api/Controllers/GuardiansController.cs:14

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| return Ok(guardians); |

### GuardiansController.Create (POST /api/academies/{academyId:guid}/guardians)

Source: apps/api/Controllers/GuardiansController.cs:25

| Validation/guard expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.FirstName) \|\| string.IsNullOrWhiteSpace(request.LastName)) |
| return BadRequest(new { message = "First name and last name are required." }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| return BadRequest(new { message = "First name and last name are required." }); |
| return Created($"/api/academies/{academyId}/guardians/{guardian.Id}", new GuardianSummary(guardian.Id, guardian.FirstName, guardian.LastName, guardian.Email, guardian.Phone, guardian.IsActive)); |

### GuardiansController.Update (PUT /api/academies/{academyId:guid}/guardians/{guardianId:guid})

Source: apps/api/Controllers/GuardiansController.cs:37

| Validation/guard expressions |
| --- |
| if (guardian is null) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.FirstName) \|\| string.IsNullOrWhiteSpace(request.LastName)) |
| return BadRequest(new { message = "First and last name are required." }); |
| if (!string.IsNullOrWhiteSpace(guardian.Email)) account.Email = guardian.Email; |
| if (!update.Succeeded) |
| return BadRequest(new { message = "Parent record saved, but the linked portal account could not be updated." }); |

| Return / serialization expressions |
| --- |
| if (guardian is null) return NotFound(); |
| return BadRequest(new { message = "First and last name are required." }); |
| return BadRequest(new { message = "Parent record saved, but the linked portal account could not be updated." }); |
| return Ok(new GuardianSummary(guardian.Id, guardian.FirstName, guardian.LastName, guardian.Email, guardian.Phone, guardian.IsActive)); |

### HolidayDeleteController.Delete (DELETE /api/academies/{academyId:guid}/holidays/{holidayId:guid})

Source: apps/api/Controllers/HolidayDeleteController.cs:11

| Validation/guard expressions |
| --- |
| if (holiday is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (holiday is null) return NotFound(); |
| return NoContent(); |

### HolidaysController.List (GET /api/academies/{academyId:guid}/holidays)

Source: apps/api/Controllers/HolidaysController.cs:9

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### HolidaysController.Create (POST /api/academies/{academyId:guid}/holidays)

Source: apps/api/Controllers/HolidaysController.cs:10

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| [HttpPost] public async Task<ActionResult<AcademyHoliday>> Create(Guid academyId,AcademyHoliday x,CancellationToken t){x.Id=Guid.NewGuid();x.AcademyId=academyId;db.Add(x);await db.SaveChangesAsync(t);return Ok(x);} |

### HolidaysController.India (POST /api/academies/{academyId:guid}/holidays/india-2026-defaults)

Source: apps/api/Controllers/HolidaysController.cs:11

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| [HttpPost("india-2026-defaults")] public async Task<ActionResult> India(Guid academyId,CancellationToken t){var h=new[]{("Republic Day",1,26),("Id-ul-Fitr",3,21),("Mahavir Jayanti",3,31),("Good Friday",4,3),("Buddha Purnima",5,1),("Id-ul-Zuha",5,27),("Muharram",6,26),("Independence Day",8,15),("Id-e-Milad",8,26),("Gandhi Jayanti",10,2),("Dussehra",10,20),("Diwali",11,8),("Guru Nanak Jayanti",11,24),("Christmas Day",12,25)};var e=await db.AcademyHolidays.Where(x=>x.AcademyId==academyId).Select(x=>x.HolidayDate).ToListAsync(t);db.AddRange(h.Where(x=>!e.Contains(new DateOnly(2026,x.Item2,x.Item3))).Select(x=>new AcademyHoliday{AcademyId=academyId,Name=x.Item1,HolidayDate=new DateOnly(2026,x.Item2,x.Item3),Scope="National",StateOrUt="All India"}));await db.SaveChangesAsync(t);return Ok();} |

### HolidaysController.Delete (DELETE /api/academies/{academyId:guid}/holidays/{id:guid})

Source: apps/api/Controllers/HolidaysController.cs:12

| Validation/guard expressions |
| --- |
| [HttpDelete("{id:guid}")] public async Task<ActionResult> Delete(Guid academyId,Guid id,CancellationToken t){var x=await db.AcademyHolidays.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();db.Remove(x);await db.SaveChangesAsync(t);return NoContent();} |

| Return / serialization expressions |
| --- |
| [HttpDelete("{id:guid}")] public async Task<ActionResult> Delete(Guid academyId,Guid id,CancellationToken t){var x=await db.AcademyHolidays.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();db.Remove(x);await db.SaveChangesAsync(t);return NoContent();} |

### InvoicesController.List (GET /api/academies/{academyId:guid}/invoices)

Source: apps/api/Controllers/InvoicesController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### InvoicesController.Create (POST /api/academies/{academyId:guid}/invoices)

Source: apps/api/Controllers/InvoicesController.cs:15

| Validation/guard expressions |
| --- |
| if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The student does not belong to this academy." }); |
| if (request.FeePlanId.HasValue) plan = await dbContext.FeePlans.SingleOrDefaultAsync(x => x.Id == request.FeePlanId && x.AcademyId == academyId && x.IsActive, cancellationToken); |
| if (request.FeePlanId.HasValue && plan is null) return BadRequest(new { message = "The fee plan does not belong to this academy." }); |
| if (amount <= 0) return BadRequest(new { message = "A positive invoice amount is required." }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The student does not belong to this academy." }); |
| if (request.FeePlanId.HasValue && plan is null) return BadRequest(new { message = "The fee plan does not belong to this academy." }); |
| if (amount <= 0) return BadRequest(new { message = "A positive invoice amount is required." }); |
| return Created($"/api/academies/{academyId}/invoices/{invoice.Id}", new InvoiceSummary(invoice.Id, invoice.InvoiceNumber, invoice.StudentId, invoice.FeePlanId, invoice.TotalAmount, invoice.AdjustedAmount, 0, invoice.Currency, invoice.IssuedDate, invoice.DueDate, invoice.Status)); |

### InvoicesController.UpdateStatus (PATCH /api/academies/{academyId:guid}/invoices/{invoiceId:guid}/status)

Source: apps/api/Controllers/InvoicesController.cs:29

| Validation/guard expressions |
| --- |
| { var x=await dbContext.Invoices.SingleOrDefaultAsync(v=>v.Id==invoiceId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(!new[]{"Issued","PartiallyPaid","Paid","Overdue","Cancelled"}.Contains(request.Status,StringComparer.OrdinalIgnoreCase))return BadRequest(); x.Status=request.Status.Trim(); await dbContext.SaveChangesAsync(token); return Ok(); } |

| Return / serialization expressions |
| --- |
| { var x=await dbContext.Invoices.SingleOrDefaultAsync(v=>v.Id==invoiceId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(!new[]{"Issued","PartiallyPaid","Paid","Overdue","Cancelled"}.Contains(request.Status,StringComparer.OrdinalIgnoreCase))return BadRequest(); x.Status=request.Status.Trim(); await dbContext.SaveChangesAsync(token); return Ok(); } |

### LeadsController.List (GET /api/academies/{academyId:guid}/leads)

Source: apps/api/Controllers/LeadsController.cs:14

| Validation/guard expressions |
| --- |
| if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(x => x.Stage == stage); |

| Return / serialization expressions |
| --- |
| return Ok(await query.OrderBy(x => x.FollowUpAtUtc ?? DateTime.MaxValue).ThenByDescending(x => x.CreatedAtUtc) |

### LeadsController.Create (POST /api/academies/{academyId:guid}/leads)

Source: apps/api/Controllers/LeadsController.cs:24

| Validation/guard expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.FullName)) return BadRequest(new { message = "Lead name is required." }); |

| Return / serialization expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.FullName)) return BadRequest(new { message = "Lead name is required." }); |
| return Created($"/api/academies/{academyId}/leads/{lead.Id}", ToSummary(lead)); |

### LeadsController.UpdateStage (PATCH /api/academies/{academyId:guid}/leads/{leadId:guid}/stage)

Source: apps/api/Controllers/LeadsController.cs:34

| Validation/guard expressions |
| --- |
| if (!Stages.Contains(request.Stage, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid lead stage." }); |
| if (lead is null) return NotFound(); |
| if (lead.ConvertedStudentId.HasValue) return Conflict(new { message = "A converted lead cannot be moved back through the pipeline." }); |

| Return / serialization expressions |
| --- |
| if (!Stages.Contains(request.Stage, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid lead stage." }); |
| if (lead is null) return NotFound(); |
| return Ok(ToSummary(lead)); |

### LeadsController.UpdateNotes (PATCH /api/academies/{academyId:guid}/leads/{leadId:guid}/notes)

Source: apps/api/Controllers/LeadsController.cs:46

| Validation/guard expressions |
| --- |
| { var lead=await dbContext.Leads.SingleOrDefaultAsync(x=>x.Id==leadId&&x.AcademyId==academyId,token); if(lead is null)return NotFound(); lead.Notes=string.IsNullOrWhiteSpace(request.Notes)?null:request.Notes.Trim(); await dbContext.SaveChangesAsync(token); return Ok(ToSummary(lead)); } |

| Return / serialization expressions |
| --- |
| { var lead=await dbContext.Leads.SingleOrDefaultAsync(x=>x.Id==leadId&&x.AcademyId==academyId,token); if(lead is null)return NotFound(); lead.Notes=string.IsNullOrWhiteSpace(request.Notes)?null:request.Notes.Trim(); await dbContext.SaveChangesAsync(token); return Ok(ToSummary(lead)); } |

### LeadsController.Convert (POST /api/academies/{academyId:guid}/leads/{leadId:guid}/convert)

Source: apps/api/Controllers/LeadsController.cs:50

| Validation/guard expressions |
| --- |
| if (lead is null) return NotFound(); |
| if (lead.ConvertedStudentId.HasValue) return Conflict(new { message = "This lead has already been converted." }); |
| if (string.IsNullOrWhiteSpace(lastName)) lastName = "Student"; |

| Return / serialization expressions |
| --- |
| if (lead is null) return NotFound(); |
| return Ok(new LeadConversionSummary(lead.Id, student.Id, student.FirstName, student.LastName)); |

### LearningResourcesController.List (GET /api/academies/{academyId:guid}/resources)

Source: apps/api/Controllers/LearningResourcesController.cs:6

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### LearningResourcesController.Create (POST /api/academies/{academyId:guid}/resources)

Source: apps/api/Controllers/LearningResourcesController.cs:7

| Validation/guard expressions |
| --- |
| [HttpPost]public async Task<ActionResult> Create(Guid academyId,CreateResourceRequest r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Title)\|\|!Uri.TryCreate(r.Url,UriKind.Absolute,out _))return BadRequest(new{message="Title and a valid full URL are required."});var problem=await ValidateScope(academyId,r.BatchId,r.CourseId,t);if(problem is not null)return BadRequest(new{message=problem});var x=new LearningResource{AcademyId=academyId,Title=r.Title.Trim(),Description=r.Description?.Trim(),Type=string.IsNullOrWhiteSpace(r.Type)?"Link":r.Type.Trim(),Url=r.Url.Trim(),BatchId=r.BatchId,CourseId=r.CourseId,IsPublished=r.IsPublished};db.LearningResources.Add(x);await db.SaveChangesAsync(t);return Ok();} |

| Return / serialization expressions |
| --- |
| [HttpPost]public async Task<ActionResult> Create(Guid academyId,CreateResourceRequest r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Title)\|\|!Uri.TryCreate(r.Url,UriKind.Absolute,out _))return BadRequest(new{message="Title and a valid full URL are required."});var problem=await ValidateScope(academyId,r.BatchId,r.CourseId,t);if(problem is not null)return BadRequest(new{message=problem});var x=new LearningResource{AcademyId=academyId,Title=r.Title.Trim(),Description=r.Description?.Trim(),Type=string.IsNullOrWhiteSpace(r.Type)?"Link":r.Type.Trim(),Url=r.Url.Trim(),BatchId=r.BatchId,CourseId=r.CourseId,IsPublished=r.IsPublished};db.LearningResources.Add(x);await db.SaveChangesAsync(t);return Ok();} |

### LearningResourcesController.Upload (POST /api/academies/{academyId:guid}/resources/upload)

Source: apps/api/Controllers/LearningResourcesController.cs:8

| Validation/guard expressions |
| --- |
| [HttpPost("upload")][RequestSizeLimit(50_000_000)]public async Task<ActionResult> Upload(Guid academyId,[FromForm]UploadResourceRequest r,IWebHostEnvironment environment,CancellationToken t){if(r.File is null\|\|r.File.Length==0)return BadRequest(new{message="Choose a file to upload."});var problem=await ValidateScope(academyId,r.BatchId,r.CourseId,t);if(problem is not null)return BadRequest(new{message=problem});var allowed=new[]{".pdf",".doc",".docx",".jpg",".jpeg",".png",".webp",".mp3",".wav",".mp4"};var ext=Path.GetExtension(r.File.FileName).ToLowerInvariant();if(!allowed.Contains(ext))return BadRequest(new{message="Upload a PDF, document, image, audio, or video file."});var folder=Path.Combine(environment.WebRootPath,"uploads","learning-resources");Directory.CreateDirectory(folder);var fileName=$"{Guid.NewGuid():N}{ext}";await using(var stream=System.IO.File.Create(Path.Combine(folder,fileName))){await r.File.CopyToAsync(stream,t);}db.LearningResources.Add(new LearningResource{AcademyId=academyId,Title=string.IsNullOrWhiteSpace(r.Title)?Path.GetFileNameWithoutExtension(r.File.FileName):r.Title.Trim(),Description=r.Description?.Trim(),Type=string.IsNullOrWhiteSpace(r.Type)?"Document":r.Type.Trim(),Url=$"/uploads/learning-resources/{fileName}",BatchId=r.BatchId,CourseId=r.CourseId,IsPublished=r.IsPublished});await db.SaveChangesAsync(t);return Ok();} |

| Return / serialization expressions |
| --- |
| [HttpPost("upload")][RequestSizeLimit(50_000_000)]public async Task<ActionResult> Upload(Guid academyId,[FromForm]UploadResourceRequest r,IWebHostEnvironment environment,CancellationToken t){if(r.File is null\|\|r.File.Length==0)return BadRequest(new{message="Choose a file to upload."});var problem=await ValidateScope(academyId,r.BatchId,r.CourseId,t);if(problem is not null)return BadRequest(new{message=problem});var allowed=new[]{".pdf",".doc",".docx",".jpg",".jpeg",".png",".webp",".mp3",".wav",".mp4"};var ext=Path.GetExtension(r.File.FileName).ToLowerInvariant();if(!allowed.Contains(ext))return BadRequest(new{message="Upload a PDF, document, image, audio, or video file."});var folder=Path.Combine(environment.WebRootPath,"uploads","learning-resources");Directory.CreateDirectory(folder);var fileName=$"{Guid.NewGuid():N}{ext}";await using(var stream=System.IO.File.Create(Path.Combine(folder,fileName))){await r.File.CopyToAsync(stream,t);}db.LearningResources.Add(new LearningResource{AcademyId=academyId,Title=string.IsNullOrWhiteSpace(r.Title)?Path.GetFileNameWithoutExtension(r.File.FileName):r.Title.Trim(),Description=r.Description?.Trim(),Type=string.IsNullOrWhiteSpace(r.Type)?"Document":r.Type.Trim(),Url=$"/uploads/learning-resources/{fileName}",BatchId=r.BatchId,CourseId=r.CourseId,IsPublished=r.IsPublished});await db.SaveChangesAsync(t);return Ok();} |

### LearningResourcesController.Publish (PATCH /api/academies/{academyId:guid}/resources/{resourceId:guid}/publish)

Source: apps/api/Controllers/LearningResourcesController.cs:9

| Validation/guard expressions |
| --- |
| [HttpPatch("{resourceId:guid}/publish")]public async Task<ActionResult> Publish(Guid academyId,Guid resourceId,PublishResourceRequest r,CancellationToken t){var x=await db.LearningResources.SingleOrDefaultAsync(v=>v.Id==resourceId&&v.AcademyId==academyId,t);if(x is null)return NotFound();x.IsPublished=r.IsPublished;await db.SaveChangesAsync(t);return Ok();} |
| private async Task<string?> ValidateScope(Guid academyId,Guid? batchId,Guid? courseId,CancellationToken t){if(batchId.HasValue&&!await db.Batches.AnyAsync(x=>x.Id==batchId&&x.AcademyId==academyId,t))return "Invalid batch.";if(courseId.HasValue&&!await db.Courses.AnyAsync(x=>x.Id==courseId&&x.AcademyId==academyId,t))return "Invalid subject.";return null;} |

| Return / serialization expressions |
| --- |
| [HttpPatch("{resourceId:guid}/publish")]public async Task<ActionResult> Publish(Guid academyId,Guid resourceId,PublishResourceRequest r,CancellationToken t){var x=await db.LearningResources.SingleOrDefaultAsync(v=>v.Id==resourceId&&v.AcademyId==academyId,t);if(x is null)return NotFound();x.IsPublished=r.IsPublished;await db.SaveChangesAsync(t);return Ok();} |

### LeaveRequestsController.List (GET /api/academies/{academyId:guid}/leave-requests)

Source: apps/api/Controllers/LeaveRequestsController.cs:10

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### LeaveRequestsController.Create (POST /api/academies/{academyId:guid}/leave-requests)

Source: apps/api/Controllers/LeaveRequestsController.cs:11

| Validation/guard expressions |
| --- |
| [HttpPost] public async Task<ActionResult> Create(Guid academyId,CreateLeaveRequest r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Reason)\|\|r.EndDate<r.StartDate)return BadRequest(new{message="Reason and valid dates are required."});if(r.RequesterType=="Student"&&(!r.StudentId.HasValue\|\|!await db.Students.AnyAsync(x=>x.Id==r.StudentId&&x.AcademyId==academyId,t)))return BadRequest(new{message="Select a valid student."});if(r.RequesterType=="Teacher"&&(!r.TeacherId.HasValue\|\|!await db.Teachers.AnyAsync(x=>x.Id==r.TeacherId&&x.AcademyId==academyId,t)))return BadRequest(new{message="Select a valid teacher."});var x=new LeaveRequest{AcademyId=academyId,RequesterType=r.RequesterType,StudentId=r.StudentId,TeacherId=r.TeacherId,StartDate=r.StartDate,EndDate=r.EndDate,Reason=r.Reason.Trim()};db.LeaveRequests.Add(x);await db.SaveChangesAsync(t);return Ok();} |

| Return / serialization expressions |
| --- |
| [HttpPost] public async Task<ActionResult> Create(Guid academyId,CreateLeaveRequest r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Reason)\|\|r.EndDate<r.StartDate)return BadRequest(new{message="Reason and valid dates are required."});if(r.RequesterType=="Student"&&(!r.StudentId.HasValue\|\|!await db.Students.AnyAsync(x=>x.Id==r.StudentId&&x.AcademyId==academyId,t)))return BadRequest(new{message="Select a valid student."});if(r.RequesterType=="Teacher"&&(!r.TeacherId.HasValue\|\|!await db.Teachers.AnyAsync(x=>x.Id==r.TeacherId&&x.AcademyId==academyId,t)))return BadRequest(new{message="Select a valid teacher."});var x=new LeaveRequest{AcademyId=academyId,RequesterType=r.RequesterType,StudentId=r.StudentId,TeacherId=r.TeacherId,StartDate=r.StartDate,EndDate=r.EndDate,Reason=r.Reason.Trim()};db.LeaveRequests.Add(x);await db.SaveChangesAsync(t);return Ok();} |

### LeaveRequestsController.Decide (PATCH /api/academies/{academyId:guid}/leave-requests/{id:guid})

Source: apps/api/Controllers/LeaveRequestsController.cs:12

| Validation/guard expressions |
| --- |
| [HttpPatch("{id:guid}")] public async Task<ActionResult> Decide(Guid academyId,Guid id,DecideLeaveRequest r,CancellationToken t){if(r.Status is not("Approved" or "Rejected"))return BadRequest();var x=await db.LeaveRequests.SingleOrDefaultAsync(v=>v.Id==id&&v.AcademyId==academyId,t);if(x is null)return NotFound();x.Status=r.Status;x.DecisionNotes=r.Notes?.Trim();await db.SaveChangesAsync(t);return Ok();} |

| Return / serialization expressions |
| --- |
| [HttpPatch("{id:guid}")] public async Task<ActionResult> Decide(Guid academyId,Guid id,DecideLeaveRequest r,CancellationToken t){if(r.Status is not("Approved" or "Rejected"))return BadRequest();var x=await db.LeaveRequests.SingleOrDefaultAsync(v=>v.Id==id&&v.AcademyId==academyId,t);if(x is null)return NotFound();x.Status=r.Status;x.DecisionNotes=r.Notes?.Trim();await db.SaveChangesAsync(t);return Ok();} |

### LessonPlansController.List (GET /api/academies/{academyId:guid}/lesson-plans)

Source: apps/api/Controllers/LessonPlansController.cs:2

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### LessonPlansController.Create (POST /api/academies/{academyId:guid}/lesson-plans)

Source: apps/api/Controllers/LessonPlansController.cs:2

| Validation/guard expressions |
| --- |
| [HttpPost]public async Task<ActionResult> Create(Guid academyId,CreateLessonPlan r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Title)\|\|!await db.Batches.AnyAsync(x=>x.Id==r.BatchId&&x.AcademyId==academyId,t))return BadRequest(new{message="Valid batch and title required."});db.LessonPlans.Add(new LessonPlan{AcademyId=academyId,BatchId=r.BatchId,CourseModuleId=r.CourseModuleId,ClassSessionId=r.ClassSessionId,Title=r.Title.Trim(),Objectives=r.Objectives?.Trim()});await db.SaveChangesAsync(t);return Ok();} |

| Return / serialization expressions |
| --- |
| [HttpPost]public async Task<ActionResult> Create(Guid academyId,CreateLessonPlan r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Title)\|\|!await db.Batches.AnyAsync(x=>x.Id==r.BatchId&&x.AcademyId==academyId,t))return BadRequest(new{message="Valid batch and title required."});db.LessonPlans.Add(new LessonPlan{AcademyId=academyId,BatchId=r.BatchId,CourseModuleId=r.CourseModuleId,ClassSessionId=r.ClassSessionId,Title=r.Title.Trim(),Objectives=r.Objectives?.Trim()});await db.SaveChangesAsync(t);return Ok();} |

### LessonPlansController.Status (PATCH /api/academies/{academyId:guid}/lesson-plans/{id:guid})

Source: apps/api/Controllers/LessonPlansController.cs:2

| Validation/guard expressions |
| --- |
| [HttpPatch("{id:guid}")]public async Task<ActionResult> Status(Guid academyId,Guid id,UpdateLessonStatus r,CancellationToken t){if(r.Status is not("Planned" or "Delivered" or "Skipped" or "MakeupNeeded"))return BadRequest();var x=await db.LessonPlans.SingleOrDefaultAsync(v=>v.Id==id&&v.AcademyId==academyId,t);if(x is null)return NotFound();x.Status=r.Status;await db.SaveChangesAsync(t);return Ok();}} |

| Return / serialization expressions |
| --- |
| [HttpPatch("{id:guid}")]public async Task<ActionResult> Status(Guid academyId,Guid id,UpdateLessonStatus r,CancellationToken t){if(r.Status is not("Planned" or "Delivered" or "Skipped" or "MakeupNeeded"))return BadRequest();var x=await db.LessonPlans.SingleOrDefaultAsync(v=>v.Id==id&&v.AcademyId==academyId,t);if(x is null)return NotFound();x.Status=r.Status;await db.SaveChangesAsync(t);return Ok();}} |

### MakeupClassesController.List (GET /api/academies/{academyId:guid}/makeup-classes)

Source: apps/api/Controllers/MakeupClassesController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### MakeupClassesController.Create (POST /api/academies/{academyId:guid}/makeup-classes)

Source: apps/api/Controllers/MakeupClassesController.cs:15

| Validation/guard expressions |
| --- |
| if (batch is null \|\| !await db.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Student or class/batch is invalid." }); |
| if (!new[] { "Online", "Offline", "Hybrid" }.Contains(deliveryMode, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Venue must be Online, Offline, or Hybrid." }); |
| if (request.UseNextScheduledClass) |
| if (session is null) return BadRequest(new { message = "There is no upcoming scheduled class for this batch. Select a make-up date and time instead." }); |
| else { if (!request.StartUtc.HasValue) return BadRequest(new { message = "Select a make-up date and time." }); startUtc = request.StartUtc.Value; endUtc = startUtc.AddMinutes(batch.SessionMinutes is > 0 ? batch.SessionMinutes : 60); } |
| if (deliveryMode is "Online" or "Hybrid" && string.IsNullOrWhiteSpace(meetingLink)) return BadRequest(new { message = "A meeting link is required for online and hybrid make-up classes." }); |
| if (teacherId.HasValue && !await db.Teachers.AnyAsync(x => x.Id == teacherId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Teacher is invalid." }); |

| Return / serialization expressions |
| --- |
| if (batch is null \|\| !await db.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Student or class/batch is invalid." }); |
| if (!new[] { "Online", "Offline", "Hybrid" }.Contains(deliveryMode, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Venue must be Online, Offline, or Hybrid." }); |
| if (session is null) return BadRequest(new { message = "There is no upcoming scheduled class for this batch. Select a make-up date and time instead." }); |
| else { if (!request.StartUtc.HasValue) return BadRequest(new { message = "Select a make-up date and time." }); startUtc = request.StartUtc.Value; endUtc = startUtc.AddMinutes(batch.SessionMinutes is > 0 ? batch.SessionMinutes : 60); } |
| if (deliveryMode is "Online" or "Hybrid" && string.IsNullOrWhiteSpace(meetingLink)) return BadRequest(new { message = "A meeting link is required for online and hybrid make-up classes." }); |
| if (teacherId.HasValue && !await db.Teachers.AnyAsync(x => x.Id == teacherId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Teacher is invalid." }); |
| return Ok(new MakeupSummary(makeup.Id, makeup.StudentId, makeup.BatchId, makeup.TeacherId, makeup.StartUtc, makeup.EndUtc, makeup.DeliveryMode, makeup.Venue, makeup.MeetingLink, makeup.UsesNextScheduledClass, makeup.Status, makeup.Notes)); |

### MakeupClassesController.UpdateStatus (PATCH /api/academies/{academyId:guid}/makeup-classes/{id:guid})

Source: apps/api/Controllers/MakeupClassesController.cs:37

| Validation/guard expressions |
| --- |
| { if (request.Status is not ("Scheduled" or "Completed" or "Cancelled")) return BadRequest(); var makeup = await db.MakeupClasses.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academyId, token); if (makeup is null) return NotFound(); makeup.Status = request.Status; await db.SaveChangesAsync(token); return Ok(); } |

| Return / serialization expressions |
| --- |
| { if (request.Status is not ("Scheduled" or "Completed" or "Cancelled")) return BadRequest(); var makeup = await db.MakeupClasses.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academyId, token); if (makeup is null) return NotFound(); makeup.Status = request.Status; await db.SaveChangesAsync(token); return Ok(); } |

### MusicPiecesController.List (GET /api/academies/{academyId:guid}/music-pieces)

Source: apps/api/Controllers/MusicPiecesController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### MusicPiecesController.Create (POST /api/academies/{academyId:guid}/music-pieces)

Source: apps/api/Controllers/MusicPiecesController.cs:15

| Validation/guard expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Piece title is required." }); |

| Return / serialization expressions |
| --- |
| if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Piece title is required." }); |
| return Created($"/api/academies/{academyId}/music-pieces/{piece.Id}", new MusicPieceSummary(piece.Id, piece.Title, piece.Composer, piece.Instrument, piece.Genre, piece.Difficulty, piece.DurationMinutes)); |

### MusicPiecesController.SetActive (PATCH /api/academies/{academyId:guid}/music-pieces/{pieceId:guid}/active)

Source: apps/api/Controllers/MusicPiecesController.cs:23

| Validation/guard expressions |
| --- |
| { var x=await dbContext.MusicPieces.SingleOrDefaultAsync(v=>v.Id==pieceId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); x.IsActive=request.IsActive; await dbContext.SaveChangesAsync(token); return Ok(); } |

| Return / serialization expressions |
| --- |
| { var x=await dbContext.MusicPieces.SingleOrDefaultAsync(v=>v.Id==pieceId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); x.IsActive=request.IsActive; await dbContext.SaveChangesAsync(token); return Ok(); } |

### MusicProgressController.List (GET /api/academies/{academyId:guid}/music-progress)

Source: apps/api/Controllers/MusicProgressController.cs:13

| Validation/guard expressions |
| --- |
| if (studentId.HasValue) query = query.Where(x => x.StudentId == studentId); |

| Return / serialization expressions |
| --- |
| return Ok(await query.OrderByDescending(x => x.UpdatedAtUtc).Select(x => new MusicProgressSummary(x.Id, x.StudentId, x.MusicPieceId, x.Status, x.TargetDate, x.Score, x.Notes)).ToListAsync(token)); |

### MusicProgressController.Assign (POST /api/academies/{academyId:guid}/music-progress)

Source: apps/api/Controllers/MusicProgressController.cs:20

| Validation/guard expressions |
| --- |
| if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token) \|\| !await dbContext.MusicPieces.AnyAsync(x => x.Id == request.MusicPieceId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Student or music piece is invalid." }); |
| if (await dbContext.StudentMusicProgress.AnyAsync(x => x.AcademyId == academyId && x.StudentId == request.StudentId && x.MusicPieceId == request.MusicPieceId, token)) return Conflict(new { message = "This piece is already assigned to the student." }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token) \|\| !await dbContext.MusicPieces.AnyAsync(x => x.Id == request.MusicPieceId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Student or music piece is invalid." }); |
| dbContext.StudentMusicProgress.Add(progress); await dbContext.SaveChangesAsync(token); return Ok(new MusicProgressSummary(progress.Id, progress.StudentId, progress.MusicPieceId, progress.Status, progress.TargetDate, progress.Score, progress.Notes)); |

### MusicProgressController.Update (PATCH /api/academies/{academyId:guid}/music-progress/{progressId:guid})

Source: apps/api/Controllers/MusicProgressController.cs:28

| Validation/guard expressions |
| --- |
| if (!Statuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid progress status." }); |
| var progress = await dbContext.StudentMusicProgress.SingleOrDefaultAsync(x => x.Id == progressId && x.AcademyId == academyId, token); if (progress is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (!Statuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid progress status." }); |
| var progress = await dbContext.StudentMusicProgress.SingleOrDefaultAsync(x => x.Id == progressId && x.AcademyId == academyId, token); if (progress is null) return NotFound(); |
| progress.Status = Statuses.Single(x => x.Equals(request.Status, StringComparison.OrdinalIgnoreCase)); progress.Score = request.Score; progress.Notes = request.Notes?.Trim(); progress.TargetDate = request.TargetDate; await dbContext.SaveChangesAsync(token); return Ok(new MusicProgressSummary(progress.Id, progress.StudentId, progress.MusicPieceId, progress.Status, progress.TargetDate, progress.Score, progress.Notes)); |

### NotificationsController.List (GET /api/academies/{academyId:guid}/notifications)

Source: apps/api/Controllers/NotificationsController.cs:14

| Validation/guard expressions |
| --- |
| if (recipientId.HasValue) query = query.Where(x => x.RecipientId == recipientId.Value); |

| Return / serialization expressions |
| --- |
| return Ok(await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => new NotificationSummary(x.Id, x.RecipientId, x.RecipientType, x.Title, x.Message, x.Channel, x.Status, x.TemplateId, x.VariablesJson, x.FailureReason, x.ScheduledAtUtc, x.SentAtUtc)).ToListAsync(cancellationToken)); |

### NotificationsController.Create (POST /api/academies/{academyId:guid}/notifications)

Source: apps/api/Controllers/NotificationsController.cs:22

| Validation/guard expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| if (request.TemplateId.HasValue) |
| if (template is null) return BadRequest(new { message = "The selected template is unavailable." }); |
| if (string.IsNullOrWhiteSpace(title) \|\| string.IsNullOrWhiteSpace(body)) return BadRequest(new { message = "A title and message are required." }); |
| if (string.Equals(request.RecipientType, "Academy", StringComparison.OrdinalIgnoreCase) && request.IsImportant) |
| if (request.DisplayHours is < 1 or > 168) return BadRequest(new { message = "Important announcements must be displayed for 1 to 168 hours." }); |
| if (channel is "Email" or "WhatsApp") |
| if (!consent) { status = "BlockedConsent"; failureReason = $"No recorded {channel} consent for this recipient."; } |
| else if (!await dbContext.CommunicationChannels.AnyAsync(x => x.AcademyId == academyId && x.Channel == channel && x.MessagesEnabled && x.HasSecureConnection, cancellationToken)) { status = "AwaitingConnection"; failureReason = $"{channel} is not securely connected for this academy."; } |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| if (template is null) return BadRequest(new { message = "The selected template is unavailable." }); |
| if (string.IsNullOrWhiteSpace(title) \|\| string.IsNullOrWhiteSpace(body)) return BadRequest(new { message = "A title and message are required." }); |
| if (request.DisplayHours is < 1 or > 168) return BadRequest(new { message = "Important announcements must be displayed for 1 to 168 hours." }); |
| return Created($"/api/academies/{academyId}/notifications/{notification.Id}", new NotificationSummary(notification.Id, notification.RecipientId, notification.RecipientType, notification.Title, notification.Message, notification.Channel, notification.Status, notification.TemplateId, notification.VariablesJson, notification.FailureReason, notification.ScheduledAtUtc, notification.SentAtUtc)); |

### NotificationsController.UpdateStatus (PATCH /api/academies/{academyId:guid}/notifications/{notificationId:guid}/status)

Source: apps/api/Controllers/NotificationsController.cs:66

| Validation/guard expressions |
| --- |
| { var x=await dbContext.Notifications.SingleOrDefaultAsync(v=>v.Id==notificationId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(!new[]{"Queued","Cancelled","RetryRequested"}.Contains(request.Status,StringComparer.OrdinalIgnoreCase))return BadRequest(); x.Status=request.Status.Trim(); x.FailureReason=request.Status=="RetryRequested"?null:x.FailureReason; await dbContext.SaveChangesAsync(token); return Ok(); } |

| Return / serialization expressions |
| --- |
| { var x=await dbContext.Notifications.SingleOrDefaultAsync(v=>v.Id==notificationId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(!new[]{"Queued","Cancelled","RetryRequested"}.Contains(request.Status,StringComparer.OrdinalIgnoreCase))return BadRequest(); x.Status=request.Status.Trim(); x.FailureReason=request.Status=="RetryRequested"?null:x.FailureReason; await dbContext.SaveChangesAsync(token); return Ok(); } |

### PaymentsController.List (GET /api/academies/{academyId:guid}/payments)

Source: apps/api/Controllers/PaymentsController.cs:12

| Validation/guard expressions |
| --- |
| if (invoiceId.HasValue) query = query.Where(x => x.InvoiceId == invoiceId.Value); |

| Return / serialization expressions |
| --- |
| return Ok(await query.OrderByDescending(x => x.PaidAtUtc).Select(x => new PaymentSummary(x.Id, x.InvoiceId, x.Amount, x.Currency, x.Method, x.Status, x.Reference, x.PaidAtUtc, x.ReconciliationReference, x.ReconciledAtUtc)).ToListAsync(cancellationToken)); |

### PaymentsController.Create (POST /api/academies/{academyId:guid}/payments)

Source: apps/api/Controllers/PaymentsController.cs:20

| Validation/guard expressions |
| --- |
| if (invoice is null) return NotFound(); |
| if (request.Amount <= 0) return BadRequest(new { message = "Payment amount must be positive." }); |
| if (paid + request.Amount > invoice.TotalAmount) return BadRequest(new { message = "Payment exceeds the invoice balance." }); |

| Return / serialization expressions |
| --- |
| if (invoice is null) return NotFound(); |
| if (request.Amount <= 0) return BadRequest(new { message = "Payment amount must be positive." }); |
| if (paid + request.Amount > invoice.TotalAmount) return BadRequest(new { message = "Payment exceeds the invoice balance." }); |
| return Created($"/api/academies/{academyId}/payments/{payment.Id}", new PaymentSummary(payment.Id, payment.InvoiceId, payment.Amount, payment.Currency, payment.Method, payment.Status, payment.Reference, payment.PaidAtUtc, payment.ReconciliationReference, payment.ReconciledAtUtc)); |

### PaymentsController.UpdateStatus (PATCH /api/academies/{academyId:guid}/payments/{paymentId:guid}/status)

Source: apps/api/Controllers/PaymentsController.cs:33

| Validation/guard expressions |
| --- |
| { var x=await dbContext.Payments.SingleOrDefaultAsync(v=>v.Id==paymentId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(request.Status is not("Completed" or "Reconciled" or "Voided"))return BadRequest(new { message = "Status must be Completed, Reconciled, or Voided." }); x.Status=request.Status; await dbContext.SaveChangesAsync(token); return Ok(); } |

| Return / serialization expressions |
| --- |
| { var x=await dbContext.Payments.SingleOrDefaultAsync(v=>v.Id==paymentId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(request.Status is not("Completed" or "Reconciled" or "Voided"))return BadRequest(new { message = "Status must be Completed, Reconciled, or Voided." }); x.Status=request.Status; await dbContext.SaveChangesAsync(token); return Ok(); } |

### PaymentsController.Reconcile (PATCH /api/academies/{academyId:guid}/payments/{paymentId:guid}/reconcile)

Source: apps/api/Controllers/PaymentsController.cs:36

| Validation/guard expressions |
| --- |
| { var payment=await dbContext.Payments.SingleOrDefaultAsync(x=>x.Id==paymentId&&x.AcademyId==academyId,token);if(payment is null)return NotFound();if(string.IsNullOrWhiteSpace(request.Reference))return BadRequest(new{message="A bank/cash reconciliation reference is required."});payment.Status="Reconciled";payment.ReconciledAtUtc=DateTime.UtcNow;payment.ReconciliationReference=request.Reference.Trim();await dbContext.SaveChangesAsync(token);return Ok(); } |

| Return / serialization expressions |
| --- |
| { var payment=await dbContext.Payments.SingleOrDefaultAsync(x=>x.Id==paymentId&&x.AcademyId==academyId,token);if(payment is null)return NotFound();if(string.IsNullOrWhiteSpace(request.Reference))return BadRequest(new{message="A bank/cash reconciliation reference is required."});payment.Status="Reconciled";payment.ReconciledAtUtc=DateTime.UtcNow;payment.ReconciliationReference=request.Reference.Trim();await dbContext.SaveChangesAsync(token);return Ok(); } |

### PayrollController.Profiles (GET /api/academies/{academyId:guid}/payroll/profiles)

Source: apps/api/Controllers/PayrollController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| Ok(await db.PayrollProfiles.AsNoTracking().Where(x => x.AcademyId == academyId).OrderBy(x => x.WorkerName) |

### PayrollController.CreateProfile (POST /api/academies/{academyId:guid}/payroll/profiles)

Source: apps/api/Controllers/PayrollController.cs:17

| Validation/guard expressions |
| --- |
| if (!Valid(request, out var message)) return BadRequest(new { message }); |
| if (request.TeacherId.HasValue && !await db.Teachers.AnyAsync(x => x.Id == request.TeacherId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The selected teacher is not part of this academy." }); |

| Return / serialization expressions |
| --- |
| if (!Valid(request, out var message)) return BadRequest(new { message }); |
| if (request.TeacherId.HasValue && !await db.Teachers.AnyAsync(x => x.Id == request.TeacherId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The selected teacher is not part of this academy." }); |
| return Created($"/api/academies/{academyId}/payroll/profiles/{profile.Id}", Summary(profile)); |

### PayrollController.UpdateProfile (PUT /api/academies/{academyId:guid}/payroll/profiles/{profileId:guid})

Source: apps/api/Controllers/PayrollController.cs:27

| Validation/guard expressions |
| --- |
| if (!Valid(request, out var message)) return BadRequest(new { message }); |
| var profile = await db.PayrollProfiles.SingleOrDefaultAsync(x => x.Id == profileId && x.AcademyId == academyId, token); if (profile is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (!Valid(request, out var message)) return BadRequest(new { message }); |
| var profile = await db.PayrollProfiles.SingleOrDefaultAsync(x => x.Id == profileId && x.AcademyId == academyId, token); if (profile is null) return NotFound(); |
| await db.SaveChangesAsync(token); return Ok(Summary(profile)); |

### PayrollController.Payouts (GET /api/academies/{academyId:guid}/payroll/payouts)

Source: apps/api/Controllers/PayrollController.cs:36

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| Ok(await db.PayrollPayouts.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.PaidAtUtc) |

### PayrollController.Pay (POST /api/academies/{academyId:guid}/payroll/payouts)

Source: apps/api/Controllers/PayrollController.cs:41

| Validation/guard expressions |
| --- |
| var profile = await db.PayrollProfiles.SingleOrDefaultAsync(x => x.Id == request.PayrollProfileId && x.AcademyId == academyId && x.IsActive, token); if (profile is null) return BadRequest(new { message = "Choose an active payroll profile." }); |
| if (string.IsNullOrWhiteSpace(request.PeriodLabel) \|\| request.Deductions < 0) return BadRequest(new { message = "Enter a pay period and valid deductions." }); |
| if (gross <= 0 \|\| (profile.PaymentModel == "SessionBlock" && (!sessions.HasValue \|\| sessions <= 0))) return BadRequest(new { message = "Enter the completed sessions and a positive payout amount." }); |
| private static bool Valid(SavePayrollProfileRequest r, out string message) { message = ""; if (r.WorkerType is not ("Teacher" or "Staff")) { message = "Choose Teacher or Staff."; return false; } if (string.IsNullOrWhiteSpace(r.WorkerName)) { message = "Enter the worker name."; return false; } if (r.PaymentModel is not ("Monthly" or "SessionBlock")) { message = "Choose monthly or session-block payment."; return false; } if (r.PaymentModel == "Monthly" && r.MonthlyAmount is not > 0) { message = "Enter a positive monthly salary."; return false; } if (r.PaymentModel == "SessionBlock" && (r.AmountPerCycle is not > 0 \|\| r.SessionsPerCycle is not > 0)) { message = "Enter sessions per cycle and payout amount."; return false; } return true; } |

| Return / serialization expressions |
| --- |
| var profile = await db.PayrollProfiles.SingleOrDefaultAsync(x => x.Id == request.PayrollProfileId && x.AcademyId == academyId && x.IsActive, token); if (profile is null) return BadRequest(new { message = "Choose an active payroll profile." }); |
| if (string.IsNullOrWhiteSpace(request.PeriodLabel) \|\| request.Deductions < 0) return BadRequest(new { message = "Enter a pay period and valid deductions." }); |
| if (gross <= 0 \|\| (profile.PaymentModel == "SessionBlock" && (!sessions.HasValue \|\| sessions <= 0))) return BadRequest(new { message = "Enter the completed sessions and a positive payout amount." }); |
| return Created($"/api/academies/{academyId}/payroll/payouts/{payout.Id}", new PayrollPayoutSummary(payout.Id, payout.PayrollProfileId, profile.WorkerName, profile.WorkerType, payout.PayslipNumber, payout.PeriodLabel, payout.SessionsCovered, payout.GrossAmount, payout.Deductions, payout.NetAmount, payout.Currency, payout.Status, payout.PaymentMethod, payout.Reference, payout.PaidAtUtc)); |

### PlatformAcademiesController.List (GET /api/platform/academies)

Source: apps/api/Controllers/PlatformAcademiesController.cs:18

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| return Ok(await db.Academies.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.LegalName, x.CountryCode, x.TimeZone, x.IsActive, x.CreatedAtUtc, x.SubscriptionPlan, x.SubscriptionStatus, x.SubscriptionEndsAtUtc, x.StudentLimit, x.StaffLimit, x.EnabledModulesJson, Branches = db.Branches.Count(b => b.AcademyId == x.Id), Students = db.Students.Count(s => s.AcademyId == x.Id && s.IsActive), Staff = db.Teachers.Count(t => t.AcademyId == x.Id && t.IsActive) }).ToListAsync(token)); |

### PlatformAcademiesController.Onboard (POST /api/platform/academies)

Source: apps/api/Controllers/PlatformAcademiesController.cs:25

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.AcademyName) \|\| string.IsNullOrWhiteSpace(request.AdminUserName) \|\| string.IsNullOrWhiteSpace(request.Password)) return BadRequest(new { message = "Academy name, admin user name, and temporary password are required." }); |
| if (await users.FindByEmailAsync(requestedEmail) is not null) return Conflict(new { message = "That academy admin user name already exists." }); |
| if (!await roles.RoleExistsAsync("AcademyAdmin")) await roles.CreateAsync(new ApplicationRole { Name = "AcademyAdmin" }); |
| if (!result.Succeeded) { db.Academies.Remove(academy); await db.SaveChangesAsync(token); return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) }); } |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.AcademyName) \|\| string.IsNullOrWhiteSpace(request.AdminUserName) \|\| string.IsNullOrWhiteSpace(request.Password)) return BadRequest(new { message = "Academy name, admin user name, and temporary password are required." }); |
| if (!result.Succeeded) { db.Academies.Remove(academy); await db.SaveChangesAsync(token); return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) }); } |
| return Created($"/api/platform/academies/{academy.Id}", new { academy.Id, academy.Name, admin.UserName, admin.DisplayName }); |

### PlatformAcademiesController.SetStatus (PATCH /api/platform/academies/{academyId:guid}/status)

Source: apps/api/Controllers/PlatformAcademiesController.cs:47

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (academy is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (academy is null) return NotFound(); |
| return Ok(new { academy.Id, academy.IsActive }); |

### PlatformAcademiesController.Configure (PUT /api/platform/academies/{academyId:guid}/configuration)

Source: apps/api/Controllers/PlatformAcademiesController.cs:59

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (academy is null) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.SubscriptionPlan) \|\| string.IsNullOrWhiteSpace(request.SubscriptionStatus)) return BadRequest(new { message = "Subscription plan and status are required." }); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (academy is null) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.SubscriptionPlan) \|\| string.IsNullOrWhiteSpace(request.SubscriptionStatus)) return BadRequest(new { message = "Subscription plan and status are required." }); |
| return Ok(new { academy.Id, academy.SubscriptionPlan, academy.SubscriptionStatus, academy.SubscriptionEndsAtUtc, academy.StudentLimit, academy.StaffLimit, academy.EnabledModulesJson }); |

### PlatformControlController.Overview (GET /api/platform/overview)

Source: apps/api/Controllers/PlatformControlController.cs:17

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| return Ok(new |

### PlatformControlController.CreateAnnouncement (POST /api/platform/announcements)

Source: apps/api/Controllers/PlatformControlController.cs:38

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.Title) \|\| string.IsNullOrWhiteSpace(request.Message) \|\| request.DisplayHours is < 1 or > 168) return BadRequest(new { message = "Select an academy, message, and duration from 1 to 168 hours." }); |
| if (!await db.Academies.AnyAsync(x => x.Id == request.AcademyId, token)) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.Title) \|\| string.IsNullOrWhiteSpace(request.Message) \|\| request.DisplayHours is < 1 or > 168) return BadRequest(new { message = "Select an academy, message, and duration from 1 to 168 hours." }); |
| if (!await db.Academies.AnyAsync(x => x.Id == request.AcademyId, token)) return NotFound(); |
| db.Notifications.Add(notification); await Audit("Academy admin announcement published", "Notification", notification.Id, new { request.AcademyId, Audience = "Admin", request.DisplayHours }, token); await db.SaveChangesAsync(token); return Ok(new { notification.Id }); |

### PlatformControlController.ListAnnouncements (GET /api/platform/announcements)

Source: apps/api/Controllers/PlatformControlController.cs:48

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| return Ok(rows.Where(x => HasAdminAudience(x.notification.VariablesJson)).Select(x => new |

### PlatformControlController.GetTenantOnboarding (GET /api/platform/academies/{academyId:guid}/onboarding)

Source: apps/api/Controllers/PlatformControlController.cs:70

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (!await db.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (!await db.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound(); |
| return Ok(await db.TenantOnboardingProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academyId, token)); |

### PlatformControlController.SaveTenantOnboarding (PUT /api/platform/academies/{academyId:guid}/onboarding)

Source: apps/api/Controllers/PlatformControlController.cs:78

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (!await db.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound(); |
| if (profile is null) { profile = new TenantOnboardingProfile { AcademyId = academyId }; db.TenantOnboardingProfiles.Add(profile); } |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (!await db.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound(); |
| await Audit("Tenant onboarding profile saved", "TenantOnboardingProfile", profile.Id, new { academyId, profile.Status, profile.CurrentSection }, token); await db.SaveChangesAsync(token); return Ok(profile); |

### PlatformControlController.GetSettings (GET /api/platform/settings)

Source: apps/api/Controllers/PlatformControlController.cs:94

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| return Ok(await GetSettingsRecord(token)); |

### PlatformControlController.SaveSettings (PUT /api/platform/settings)

Source: apps/api/Controllers/PlatformControlController.cs:101

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.PlatformName) \|\| request.DefaultTrialDays < 1 \|\| request.DataRetentionDays < 30) return BadRequest(new { message = "Platform name, a positive trial period, and at least 30 days retention are required." }); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.PlatformName) \|\| request.DefaultTrialDays < 1 \|\| request.DataRetentionDays < 30) return BadRequest(new { message = "Platform name, a positive trial period, and at least 30 days retention are required." }); |
| await Audit("Platform settings updated", "PlatformSettings", settings.Id, new { settings.MaintenanceMode }, token); await db.SaveChangesAsync(token); return Ok(settings); |

### PlatformControlController.Admins (GET /api/platform/admins)

Source: apps/api/Controllers/PlatformControlController.cs:111

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| return Ok(admins.OrderBy(x => x.DisplayName).Select(x => new { x.Id, x.DisplayName, x.UserName, x.Email, x.IsActive, x.AcademyId, AcademyName = x.AcademyId.HasValue && academyNames.TryGetValue(x.AcademyId.Value, out var name) ? name : null })); |

### PlatformControlController.SetAdminActive (PATCH /api/platform/admins/{userId:guid}/active)

Source: apps/api/Controllers/PlatformControlController.cs:120

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| var admin = await users.FindByIdAsync(userId.ToString()); if (admin is null \|\| !await users.IsInRoleAsync(admin, "AcademyAdmin")) return NotFound(); |
| admin.IsActive = request.IsActive; var result = await users.UpdateAsync(admin); if (!result.Succeeded) return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) }); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| var admin = await users.FindByIdAsync(userId.ToString()); if (admin is null \|\| !await users.IsInRoleAsync(admin, "AcademyAdmin")) return NotFound(); |
| admin.IsActive = request.IsActive; var result = await users.UpdateAsync(admin); if (!result.Succeeded) return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) }); |
| await Audit(request.IsActive ? "Academy admin activated" : "Academy admin deactivated", "ApplicationUser", admin.Id, new { admin.AcademyId, admin.DisplayName }, token); await db.SaveChangesAsync(token); return Ok(new { admin.Id, admin.IsActive }); |

### PlatformControlController.ResetAdminPassword (POST /api/platform/admins/{userId:guid}/reset-password)

Source: apps/api/Controllers/PlatformControlController.cs:129

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.NewPassword) \|\| request.NewPassword.Length < 8) return BadRequest(new { message = "Use a password of at least eight characters." }); |
| var admin = await users.FindByIdAsync(userId.ToString()); if (admin is null \|\| !await users.IsInRoleAsync(admin, "AcademyAdmin")) return NotFound(); |
| var reset = await users.ResetPasswordAsync(admin, await users.GeneratePasswordResetTokenAsync(admin), request.NewPassword); if (!reset.Succeeded) return BadRequest(new { message = string.Join(" ", reset.Errors.Select(x => x.Description)) }); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.NewPassword) \|\| request.NewPassword.Length < 8) return BadRequest(new { message = "Use a password of at least eight characters." }); |
| var admin = await users.FindByIdAsync(userId.ToString()); if (admin is null \|\| !await users.IsInRoleAsync(admin, "AcademyAdmin")) return NotFound(); |
| var reset = await users.ResetPasswordAsync(admin, await users.GeneratePasswordResetTokenAsync(admin), request.NewPassword); if (!reset.Succeeded) return BadRequest(new { message = string.Join(" ", reset.Errors.Select(x => x.Description)) }); |
| await Audit("Academy admin password reset", "ApplicationUser", admin.Id, new { admin.AcademyId, admin.DisplayName }, token); await db.SaveChangesAsync(token); return Ok(new { message = "Password reset." }); |

### PlatformControlController.ListSupportCases (GET /api/platform/support-cases)

Source: apps/api/Controllers/PlatformControlController.cs:139

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| return Ok(await (from item in db.PlatformSupportCases.AsNoTracking() join academy in db.Academies.AsNoTracking() on item.AcademyId equals academy.Id orderby item.CreatedAtUtc descending select new { item.Id, item.AcademyId, AcademyName = academy.Name, item.Subject, item.Priority, item.Status, item.Description, item.AcademyResponse, item.CreatedAtUtc, item.AcademyRespondedAtUtc, item.ResolvedAtUtc }).ToListAsync(token)); |

### PlatformControlController.CreateSupportCase (POST /api/platform/support-cases)

Source: apps/api/Controllers/PlatformControlController.cs:146

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.Subject) \|\| !await db.Academies.AnyAsync(x => x.Id == request.AcademyId, token)) return BadRequest(new { message = "A valid academy and subject are required." }); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.Subject) \|\| !await db.Academies.AnyAsync(x => x.Id == request.AcademyId, token)) return BadRequest(new { message = "A valid academy and subject are required." }); |
| var item = new PlatformSupportCase { AcademyId = request.AcademyId, Subject = request.Subject.Trim(), Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority.Trim(), Description = request.Description?.Trim() }; db.PlatformSupportCases.Add(item); await Audit("Support case created", "PlatformSupportCase", item.Id, new { item.AcademyId, item.Subject }, token); await db.SaveChangesAsync(token); return Created($"/api/platform/support-cases/{item.Id}", item); |

### PlatformControlController.UpdateSupportCase (PATCH /api/platform/support-cases/{caseId:guid})

Source: apps/api/Controllers/PlatformControlController.cs:154

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| var item = await db.PlatformSupportCases.SingleOrDefaultAsync(x => x.Id == caseId, token); if (item is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| var item = await db.PlatformSupportCases.SingleOrDefaultAsync(x => x.Id == caseId, token); if (item is null) return NotFound(); |
| item.Status = request.Status.Trim(); item.Priority = request.Priority.Trim(); item.ResolvedAtUtc = item.Status is "Resolved" or "Closed" ? DateTime.UtcNow : null; item.UpdatedAtUtc = DateTime.UtcNow; await Audit("Support case updated", "PlatformSupportCase", item.Id, new { item.Status, item.Priority }, token); await db.SaveChangesAsync(token); return Ok(item); |

### PlatformControlController.ListInvoices (GET /api/platform/billing-invoices)

Source: apps/api/Controllers/PlatformControlController.cs:162

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| return Ok(await (from invoice in db.PlatformBillingInvoices.AsNoTracking() join academy in db.Academies.AsNoTracking() on invoice.AcademyId equals academy.Id orderby invoice.DueDate descending select new { invoice.Id, invoice.AcademyId, AcademyName = academy.Name, invoice.InvoiceNumber, invoice.Amount, invoice.Currency, invoice.Status, invoice.PeriodStart, invoice.PeriodEnd, invoice.DueDate, invoice.PaidAtUtc, invoice.PaymentReference, invoice.PaymentSubmittedAtUtc }).ToListAsync(token)); |

### PlatformControlController.CreateInvoice (POST /api/platform/billing-invoices)

Source: apps/api/Controllers/PlatformControlController.cs:169

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (request.Amount < 0 \|\| string.IsNullOrWhiteSpace(request.InvoiceNumber) \|\| !await db.Academies.AnyAsync(x => x.Id == request.AcademyId, token)) return BadRequest(new { message = "Academy, invoice number, and non-negative amount are required." }); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (request.Amount < 0 \|\| string.IsNullOrWhiteSpace(request.InvoiceNumber) \|\| !await db.Academies.AnyAsync(x => x.Id == request.AcademyId, token)) return BadRequest(new { message = "Academy, invoice number, and non-negative amount are required." }); |
| var item = new PlatformBillingInvoice { AcademyId = request.AcademyId, InvoiceNumber = request.InvoiceNumber.Trim(), Amount = request.Amount, Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim().ToUpperInvariant(), Status = "Draft", PeriodStart = request.PeriodStart, PeriodEnd = request.PeriodEnd, DueDate = request.DueDate }; db.PlatformBillingInvoices.Add(item); await Audit("Platform invoice created", "PlatformBillingInvoice", item.Id, new { item.AcademyId, item.InvoiceNumber, item.Amount }, token); await db.SaveChangesAsync(token); return Created($"/api/platform/billing-invoices/{item.Id}", item); |

### PlatformControlController.UpdateInvoiceStatus (PATCH /api/platform/billing-invoices/{invoiceId:guid}/status)

Source: apps/api/Controllers/PlatformControlController.cs:177

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| var invoice = await db.PlatformBillingInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId, token); if (invoice is null) return NotFound(); invoice.Status = request.Status.Trim(); invoice.PaidAtUtc = invoice.Status == "Paid" ? DateTime.UtcNow : null; invoice.UpdatedAtUtc = DateTime.UtcNow; await Audit("Platform invoice status updated", "PlatformBillingInvoice", invoice.Id, new { invoice.Status }, token); await db.SaveChangesAsync(token); return Ok(invoice); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| var invoice = await db.PlatformBillingInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId, token); if (invoice is null) return NotFound(); invoice.Status = request.Status.Trim(); invoice.PaidAtUtc = invoice.Status == "Paid" ? DateTime.UtcNow : null; invoice.UpdatedAtUtc = DateTime.UtcNow; await Audit("Platform invoice status updated", "PlatformBillingInvoice", invoice.Id, new { invoice.Status }, token); await db.SaveChangesAsync(token); return Ok(invoice); |

### PlatformControlController.AuditLog (GET /api/platform/audit)

Source: apps/api/Controllers/PlatformControlController.cs:184

| Validation/guard expressions |
| --- |
| public async Task<ActionResult> AuditLog(CancellationToken token) { if (!await IsPlatformOwner()) return Forbid(); return Ok(await db.PlatformAuditEntries.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(250).ToListAsync(token)); } |

| Return / serialization expressions |
| --- |
| public async Task<ActionResult> AuditLog(CancellationToken token) { if (!await IsPlatformOwner()) return Forbid(); return Ok(await db.PlatformAuditEntries.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(250).ToListAsync(token)); } |

### PlatformControlController.ActivityLogs (GET /api/platform/activity-logs)

Source: apps/api/Controllers/PlatformControlController.cs:187

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (fromUtc.HasValue) { platformQuery = platformQuery.Where(x => x.OccurredAtUtc >= fromUtc); adminQuery = adminQuery.Where(x => x.OccurredAtUtc >= fromUtc); } |
| if (toUtc.HasValue) { platformQuery = platformQuery.Where(x => x.OccurredAtUtc < toUtc); adminQuery = adminQuery.Where(x => x.OccurredAtUtc < toUtc); } |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| return Ok(rows); |

### PlatformControlController.DeleteActivityLogs (DELETE /api/platform/activity-logs)

Source: apps/api/Controllers/PlatformControlController.cs:209

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (request.ToUtc <= request.FromUtc) return BadRequest(new { message = "Choose a valid start and end date." }); |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); |
| if (request.ToUtc <= request.FromUtc) return BadRequest(new { message = "Choose a valid start and end date." }); |
| return Ok(new { deleted = platform.Count + admin.Count }); |

### PlatformControlController.Health (GET /api/platform/health)

Source: apps/api/Controllers/PlatformControlController.cs:224

| Validation/guard expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); var settings = await GetSettingsRecord(token); return Ok(new { Api = "Operational", Database = await db.Database.CanConnectAsync(token) ? "Operational" : "Unavailable", BackgroundJobs = "Not configured", CommunicationProviders = "Not configured", MaintenanceMode = settings.MaintenanceMode, StatusMessage = settings.StatusMessage }); |
| if (records.Count > 0) |
| if (records.Count > 1) |
| if (string.IsNullOrWhiteSpace(variablesJson)) return false; |
| if (string.IsNullOrWhiteSpace(variablesJson)) return null; |

| Return / serialization expressions |
| --- |
| if (!await IsPlatformOwner()) return Forbid(); var settings = await GetSettingsRecord(token); return Ok(new { Api = "Operational", Database = await db.Database.CanConnectAsync(token) ? "Operational" : "Unavailable", BackgroundJobs = "Not configured", CommunicationProviders = "Not configured", MaintenanceMode = settings.MaintenanceMode, StatusMessage = settings.StatusMessage }); |

### PortalAccountsController.Create (POST /api/academies/{academyId:guid}/portal-accounts)

Source: apps/api/Controllers/PortalAccountsController.cs:15

| Validation/guard expressions |
| --- |
| if (owner?.AcademyId != academyId \|\| (!await users.IsInRoleAsync(owner, "Owner") && !await users.IsInRoleAsync(owner, "AcademyAdmin"))) return Forbid(); |
| if (role is null \|\| string.IsNullOrWhiteSpace(request.Email) \|\| string.IsNullOrWhiteSpace(request.Password)) return BadRequest(new { message = "Student, Parent, or Teacher role, email, and temporary password are required." }); |
| if (role == "Student" && (!request.StudentId.HasValue \|\| !await db.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid student." }); |
| if (role == "Guardian" && (!request.GuardianId.HasValue \|\| !await db.Guardians.AnyAsync(x => x.Id == request.GuardianId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid guardian." }); |
| if (role == "Teacher" && (!request.TeacherId.HasValue \|\| !await db.Teachers.AnyAsync(x => x.Id == request.TeacherId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid teacher." }); |
| if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new ApplicationRole { Name = role }); |
| if (!create.Succeeded) return BadRequest(new { message = string.Join(" ", create.Errors.Select(x => x.Description)) }); |

| Return / serialization expressions |
| --- |
| if (owner?.AcademyId != academyId \|\| (!await users.IsInRoleAsync(owner, "Owner") && !await users.IsInRoleAsync(owner, "AcademyAdmin"))) return Forbid(); |
| if (role is null \|\| string.IsNullOrWhiteSpace(request.Email) \|\| string.IsNullOrWhiteSpace(request.Password)) return BadRequest(new { message = "Student, Parent, or Teacher role, email, and temporary password are required." }); |
| if (role == "Student" && (!request.StudentId.HasValue \|\| !await db.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid student." }); |
| if (role == "Guardian" && (!request.GuardianId.HasValue \|\| !await db.Guardians.AnyAsync(x => x.Id == request.GuardianId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid guardian." }); |
| if (role == "Teacher" && (!request.TeacherId.HasValue \|\| !await db.Teachers.AnyAsync(x => x.Id == request.TeacherId && x.AcademyId == academyId, token))) return BadRequest(new { message = "Select a valid teacher." }); |
| if (!create.Succeeded) return BadRequest(new { message = string.Join(" ", create.Errors.Select(x => x.Description)) }); |
| return Ok(new { user.Id, user.Email, role }); |

### PortalController.Me (GET /api/portal/me)

Source: apps/api/Controllers/PortalController.cs:19

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null) return Forbid(); |
| if (user.StudentId.HasValue) |
| if (student is null \|\| !student.IsActive) return Forbid(); |
| if (user.GuardianId.HasValue) |
| if (parent is null) return Forbid(); |
| return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null) return Forbid(); |
| if (student is null \|\| !student.IsActive) return Forbid(); |
| return Ok(new { role = "Student", displayName = $"{student.FirstName} {student.LastName}", studentId = student.Id, enrollmentCount, invoiceCount }); |
| if (parent is null) return Forbid(); |
| return Ok(new { role = "Parent", displayName = user.DisplayName, parentId = user.GuardianId, parentEmail = parent.Email, parentPhone = parent.Phone, children }); |
| return Forbid(); |

### PortalController.ChangePassword (POST /api/portal/change-password)

Source: apps/api/Controllers/PortalController.cs:42

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| (!user.StudentId.HasValue && !user.GuardianId.HasValue)) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.CurrentPassword) \|\| string.IsNullOrWhiteSpace(request.NewPassword)) |
| return BadRequest(new { message = "Current and new passwords are required." }); |
| if (request.NewPassword.Length < 8) |
| return BadRequest(new { message = "The new password must be at least 8 characters." }); |
| if (request.CurrentPassword == request.NewPassword) |
| return BadRequest(new { message = "The new password must be different from the current password." }); |
| if (!result.Succeeded) |
| return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) }); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| (!user.StudentId.HasValue && !user.GuardianId.HasValue)) return Forbid(); |
| return BadRequest(new { message = "Current and new passwords are required." }); |
| return BadRequest(new { message = "The new password must be at least 8 characters." }); |
| return BadRequest(new { message = "The new password must be different from the current password." }); |
| return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) }); |
| return Ok(new { message = "Password changed successfully." }); |

### PortalController.Notifications (GET /api/portal/notifications)

Source: apps/api/Controllers/PortalController.cs:60

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null) return Forbid(); |
| if (!recipientId.HasValue) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null) return Forbid(); |
| if (!recipientId.HasValue) return Forbid(); |
| return Ok(notifications); |

### PortalController.Announcements (GET /api/portal/announcements)

Source: apps/api/Controllers/PortalController.cs:76

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null) return Forbid(); |
| return Ok(announcements); |

### PortalController.MarkNotificationRead (PATCH /api/portal/notifications/{notificationId:guid}/read)

Source: apps/api/Controllers/PortalController.cs:93

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null) return Forbid(); |
| if (!recipientId.HasValue) return Forbid(); |
| if (notification is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null) return Forbid(); |
| if (!recipientId.HasValue) return Forbid(); |
| if (notification is null) return NotFound(); |
| return Ok(new { notification.Id, notification.Status }); |

### PortalController.UpcomingEvents (GET /api/portal/events)

Source: apps/api/Controllers/PortalController.cs:108

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| (!user.StudentId.HasValue && !user.GuardianId.HasValue)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| (!user.StudentId.HasValue && !user.GuardianId.HasValue)) return Forbid(); |
| return Ok(events); |

### PortalController.GuardianChildren (GET /api/portal/guardians/{guardianId:guid}/children)

Source: apps/api/Controllers/PortalController.cs:122

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.GuardianId != guardianId) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.GuardianId != guardianId) return Forbid(); |
| return Ok(children); |

### PortalController.Student (GET /api/portal/students/{studentId:guid})

Source: apps/api/Controllers/PortalController.cs:136

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null) return Forbid(); |
| if (!await CanAccessStudent(user, studentId, token)) return Forbid(); |
| if (student is null) return NotFound(); |
| if (completed > 0 && inCycle == 0) inCycle = cycleTotal; |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null) return Forbid(); |
| if (!await CanAccessStudent(user, studentId, token)) return Forbid(); |
| if (student is null) return NotFound(); |
| return Ok(new PortalStudentDetails($"{student.FirstName} {student.LastName}", student.Email, student.Phone, student.FirstName, student.LastName, student.PreferredName, student.Gender, student.DateOfBirth, student.AddressLine1, student.City, student.State, student.PostalCode, student.EmergencyContactName, student.EmergencyContactPhone, |

### PortalController.SubmitAssignment (POST /api/portal/students/{studentId:guid}/assignments/{assignmentId:guid}/submit)

Source: apps/api/Controllers/PortalController.cs:228

| Validation/guard expressions |
| --- |
| var user = await users.GetUserAsync(User); if (user?.AcademyId is null) return Forbid(); |
| if (!await CanAccessStudent(user, studentId, token) \|\| (user.GuardianId.HasValue && !(await ParentAccess(user, studentId, token))!.CanViewAcademicProgress)) return Forbid(); |
| var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId && x.AcademyId == user.AcademyId && x.IsPublished, token); if (assignment is null) return NotFound(); |
| if (!isAssigned) return Forbid(); |
| if (item is null) { item = new AcademyDesk.Api.Domain.Entities.AssignmentSubmission { AcademyId = user.AcademyId.Value, AssignmentId = assignmentId, StudentId = studentId }; db.AssignmentSubmissions.Add(item); } |
| if (string.IsNullOrWhiteSpace(request.ResponseText) && request.File is null) return BadRequest(new { message = "Write a response or attach a practice recording, photo, or file." }); |
| if (request.File is { Length: > 0 }) |
| if (request.File.Length > 50_000_000) return BadRequest(new { message = "Files must be 50 MB or smaller." }); |
| if (!allowed.Contains(extension)) return BadRequest(new { message = "Upload a photo, PDF, audio, video, or document." }); |
| if (teacherId.HasValue) |

| Return / serialization expressions |
| --- |
| var user = await users.GetUserAsync(User); if (user?.AcademyId is null) return Forbid(); |
| if (!await CanAccessStudent(user, studentId, token) \|\| (user.GuardianId.HasValue && !(await ParentAccess(user, studentId, token))!.CanViewAcademicProgress)) return Forbid(); |
| var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId && x.AcademyId == user.AcademyId && x.IsPublished, token); if (assignment is null) return NotFound(); |
| if (!isAssigned) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.ResponseText) && request.File is null) return BadRequest(new { message = "Write a response or attach a practice recording, photo, or file." }); |
| if (request.File.Length > 50_000_000) return BadRequest(new { message = "Files must be 50 MB or smaller." }); |
| if (!allowed.Contains(extension)) return BadRequest(new { message = "Upload a photo, PDF, audio, video, or document." }); |
| await db.SaveChangesAsync(token); return Ok(item); |

### PortalController.DownloadInvoice (GET /api/portal/students/{studentId:guid}/invoices/{invoiceId:guid}/download)

Source: apps/api/Controllers/PortalController.cs:264

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| !await CanAccessStudent(user, studentId, token)) return Forbid(); |
| if (invoice is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| !await CanAccessStudent(user, studentId, token)) return Forbid(); |
| if (invoice is null) return NotFound(); |
| return File(Encoding.UTF8.GetBytes(document), "text/html", $"{invoice.InvoiceNumber}.html"); |

### PortalController.DownloadCertificate (GET /api/portal/students/{studentId:guid}/certificates/{certificateNumber}/download)

Source: apps/api/Controllers/PortalController.cs:276

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| !await CanAccessStudent(user, studentId, token)) return Forbid(); |
| if (certificate is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| !await CanAccessStudent(user, studentId, token)) return Forbid(); |
| if (certificate is null) return NotFound(); |
| return File(Encoding.UTF8.GetBytes(document), "text/html", $"{certificate.CertificateNumber}.html"); |

### PortalController.StudentLeaveRequests (GET /api/portal/students/{studentId:guid}/leave-requests)

Source: apps/api/Controllers/PortalController.cs:287

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| !await CanManageLeave(user, studentId, token)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| !await CanManageLeave(user, studentId, token)) return Forbid(); |
| return Ok(requests); |

### PortalController.RequestStudentLeave (POST /api/portal/students/{studentId:guid}/leave-requests)

Source: apps/api/Controllers/PortalController.cs:300

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| !await CanManageLeave(user, studentId, token)) return Forbid(); |
| if (request.EndDate < request.StartDate \|\| string.IsNullOrWhiteSpace(request.Reason)) |
| return BadRequest(new { message = "Reason and valid dates are required." }); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| !await CanManageLeave(user, studentId, token)) return Forbid(); |
| return BadRequest(new { message = "Reason and valid dates are required." }); |
| return Ok(new PortalLeaveSummary(leave.Id, leave.StartDate, leave.EndDate, leave.Reason, leave.Status, leave.DecisionNotes)); |

### PortalController.LogPractice (POST /api/portal/students/{studentId:guid}/practice-logs)

Source: apps/api/Controllers/PortalController.cs:321

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| !await CanAccessStudent(user, studentId, token) \|\| (user.GuardianId.HasValue && !(await ParentAccess(user, studentId, token))!.CanViewAcademicProgress)) return Forbid(); |
| if (request.MinutesPracticed is < 1 or > 1440) |
| return BadRequest(new { message = "Practice time must be between 1 and 1440 minutes." }); |
| if (request.PracticeDate > DateOnly.FromDateTime(DateTime.UtcNow)) |
| return BadRequest(new { message = "Practice date cannot be in the future." }); |
| if (string.IsNullOrWhiteSpace(variablesJson)) return false; |
| if (!root.TryGetProperty("important", out var important) \|\| !string.Equals(important.GetString(), "true", StringComparison.OrdinalIgnoreCase)) return false; |
| if (root.TryGetProperty("startsAtUtc", out var start) && DateTime.TryParse(start.GetString(), out var startsAtUtc) && startsAtUtc.ToUniversalTime() > now) return false; |
| try { using var document = JsonDocument.Parse(variablesJson ?? "{}"); if (!document.RootElement.TryGetProperty("audiences", out var targets)) return true; return targets.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries \| StringSplitOptions.TrimEntries).Any(x => string.Equals(x, audience, StringComparison.OrdinalIgnoreCase) \|\| string.Equals(x, "Both", StringComparison.OrdinalIgnoreCase)) == true; } |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| !await CanAccessStudent(user, studentId, token) \|\| (user.GuardianId.HasValue && !(await ParentAccess(user, studentId, token))!.CanViewAcademicProgress)) return Forbid(); |
| return BadRequest(new { message = "Practice time must be between 1 and 1440 minutes." }); |
| return BadRequest(new { message = "Practice date cannot be in the future." }); |
| return Ok(new { log.Id, log.PracticeDate, log.MinutesPracticed, log.FocusArea, log.Notes, log.Status }); |

### PortalController.UpdateProfile (PUT /api/portal/students/{studentId:guid}/profile)

Source: apps/api/Controllers/PortalController.cs:379

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.StudentId != studentId \|\| !await IsActiveStudent(user.AcademyId.Value, studentId, token)) return Forbid(); |
| if (student is null) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.FirstName) \|\| string.IsNullOrWhiteSpace(request.LastName)) return BadRequest(new { message = "First and last names are required." }); |
| if (!string.IsNullOrWhiteSpace(student.Email)) user.Email = student.Email; |
| if (!identityUpdate.Succeeded) return BadRequest(new { message = string.Join(" ", identityUpdate.Errors.Select(x => x.Description)) }); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.StudentId != studentId \|\| !await IsActiveStudent(user.AcademyId.Value, studentId, token)) return Forbid(); |
| if (student is null) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.FirstName) \|\| string.IsNullOrWhiteSpace(request.LastName)) return BadRequest(new { message = "First and last names are required." }); |
| if (!identityUpdate.Succeeded) return BadRequest(new { message = string.Join(" ", identityUpdate.Errors.Select(x => x.Description)) }); |
| return Ok(new { message = "Profile saved." }); |

### PortalController.UpdateGuardianProfile (PUT /api/portal/guardians/{guardianId:guid}/profile)

Source: apps/api/Controllers/PortalController.cs:398

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.GuardianId != guardianId) return Forbid(); |
| if (guardian is null) return NotFound(); |
| if (!string.IsNullOrWhiteSpace(guardian.Email)) user.Email = guardian.Email; |
| if (!identityUpdate.Succeeded) return BadRequest(new { message = string.Join(" ", identityUpdate.Errors.Select(x => x.Description)) }); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.GuardianId != guardianId) return Forbid(); |
| if (guardian is null) return NotFound(); |
| if (!identityUpdate.Succeeded) return BadRequest(new { message = string.Join(" ", identityUpdate.Errors.Select(x => x.Description)) }); |
| return Ok(new { guardian.Id, guardian.Email, guardian.Phone }); |

### PracticeLogsController.List (GET /api/academies/{academyId:guid}/practice-logs)

Source: apps/api/Controllers/PracticeLogsController.cs:4

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### PracticeLogsController.Create (POST /api/academies/{academyId:guid}/practice-logs)

Source: apps/api/Controllers/PracticeLogsController.cs:4

| Validation/guard expressions |
| --- |
| [HttpPost] public async Task<ActionResult<PracticeLog>> Create(Guid academyId,PracticeLog x,CancellationToken t){if(x.MinutesPracticed<1)return BadRequest(new{message="Minutes must be greater than zero."});x.Id=Guid.NewGuid();x.AcademyId=academyId;db.PracticeLogs.Add(x);await db.SaveChangesAsync(t);return Ok(x);} |

| Return / serialization expressions |
| --- |
| [HttpPost] public async Task<ActionResult<PracticeLog>> Create(Guid academyId,PracticeLog x,CancellationToken t){if(x.MinutesPracticed<1)return BadRequest(new{message="Minutes must be greater than zero."});x.Id=Guid.NewGuid();x.AcademyId=academyId;db.PracticeLogs.Add(x);await db.SaveChangesAsync(t);return Ok(x);} |

### PracticeLogsController.Review (PATCH /api/academies/{academyId:guid}/practice-logs/{id:guid}/review)

Source: apps/api/Controllers/PracticeLogsController.cs:4

| Validation/guard expressions |
| --- |
| [HttpPatch("{id:guid}/review")] public async Task<ActionResult<PracticeLog>> Review(Guid academyId,Guid id,ReviewPracticeLogRequest r,CancellationToken t){var x=await db.PracticeLogs.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();x.TeacherFeedback=r.TeacherFeedback?.Trim();x.Status="Reviewed";x.ReviewedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync(t);return Ok(x);} } |

| Return / serialization expressions |
| --- |
| [HttpPatch("{id:guid}/review")] public async Task<ActionResult<PracticeLog>> Review(Guid academyId,Guid id,ReviewPracticeLogRequest r,CancellationToken t){var x=await db.PracticeLogs.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();x.TeacherFeedback=r.TeacherFeedback?.Trim();x.Status="Reviewed";x.ReviewedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync(t);return Ok(x);} } |

### ProfilesController.Guardian (GET /api/academies/{academyId:guid}/guardians/{guardianId:guid}/profile)

Source: apps/api/Controllers/ProfilesController.cs:12

| Validation/guard expressions |
| --- |
| if (guardian is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (guardian is null) return NotFound(); |
| return Ok(new GuardianProfileSummary(guardian.Id, guardian.FirstName + " " + guardian.LastName, guardian.Email, guardian.Phone, guardian.PreferredName, guardian.AddressLine1, guardian.City, guardian.State, guardian.PostalCode, guardian.PreferredLanguage, students, invoices, communications)); |

### ProfilesController.Student (GET /api/academies/{academyId:guid}/students/{studentId:guid}/profile)

Source: apps/api/Controllers/ProfilesController.cs:26

| Validation/guard expressions |
| --- |
| if (student is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (student is null) return NotFound(); |
| return Ok(new StudentProfileSummary(student.Id, student.FirstName + " " + student.LastName, student.Email, student.Phone, student.StudentNumber, student.PreferredName, student.Gender, student.DateOfBirth, student.AdmissionDate, student.AddressLine1, student.City, student.State, student.PostalCode, student.EmergencyContactName, student.EmergencyContactPhone, student.MedicalOrAccessibilityNotes, student.AdminNotes, guardians, enrolments, attendance, currentMonthAttendance, invoices, progress, practice, communications)); |

### ProfilesController.Teacher (GET /api/academies/{academyId:guid}/teachers/{teacherId:guid}/profile)

Source: apps/api/Controllers/ProfilesController.cs:67

| Validation/guard expressions |
| --- |
| if (teacher is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (teacher is null) return NotFound(); |
| return Ok(new TeacherProfileSummary(teacher.Id, teacher.FirstName + " " + teacher.LastName, teacher.Email, teacher.Phone, teacher.Specialties, teacher.EmployeeCode, teacher.PreferredName, teacher.EmploymentType, teacher.DateOfBirth, teacher.JoiningDate, teacher.Qualifications, teacher.AddressLine1, teacher.City, teacher.State, teacher.PostalCode, teacher.EmergencyContactName, teacher.EmergencyContactPhone, teacher.AdminNotes, subjects, students, ReadAvailability(teacher.AvailabilityJson), batches, classes, leave)); |

### ProfilesController.UpdateStudentProfile (PUT /api/academies/{academyId:guid}/students/{studentId:guid}/profile)

Source: apps/api/Controllers/ProfilesController.cs:109

| Validation/guard expressions |
| --- |
| if (student is null) return NotFound(); |
| if (studentNumber is not null && await dbContext.Students.AnyAsync(x => x.AcademyId == academyId && x.Id != studentId && x.StudentNumber == studentNumber, token)) |

| Return / serialization expressions |
| --- |
| if (student is null) return NotFound(); |

### ProfilesController.UpdateTeacherProfile (PUT /api/academies/{academyId:guid}/teachers/{teacherId:guid}/profile)

Source: apps/api/Controllers/ProfilesController.cs:134

| Validation/guard expressions |
| --- |
| if (teacher is null) return NotFound(); |
| if (employeeCode is not null && await dbContext.Teachers.AnyAsync(x => x.AcademyId == academyId && x.Id != teacherId && x.EmployeeCode == employeeCode, token)) |

| Return / serialization expressions |
| --- |
| if (teacher is null) return NotFound(); |

### ProfilesController.UpdateGuardianProfile (PUT /api/academies/{academyId:guid}/guardians/{guardianId:guid}/profile)

Source: apps/api/Controllers/ProfilesController.cs:161

| Validation/guard expressions |
| --- |
| if (guardian is null) return NotFound(); |
| if (string.IsNullOrWhiteSpace(json)) return []; |

| Return / serialization expressions |
| --- |
| if (guardian is null) return NotFound(); |

### SalesMarketingController.Campaigns (GET /api/academies/{academyId:guid}/sales-marketing/campaigns)

Source: apps/api/Controllers/SalesMarketingController.cs:15

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| Ok(await db.SalesCampaigns.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.StartDate).ToListAsync(t)); |

### SalesMarketingController.CreateCampaign (POST /api/academies/{academyId:guid}/sales-marketing/campaigns)

Source: apps/api/Controllers/SalesMarketingController.cs:19

| Validation/guard expressions |
| --- |
| if (string.IsNullOrWhiteSpace(r.Name) \|\| r.Budget < 0 \|\| !CampaignStatuses.Contains(r.Status ?? "Draft")) |
| return BadRequest(new { message = "Provide a campaign name, valid budget and status." }); |

| Return / serialization expressions |
| --- |
| return BadRequest(new { message = "Provide a campaign name, valid budget and status." }); |
| return Ok(campaign); |

### SalesMarketingController.UpdateCampaign (PATCH /api/academies/{academyId:guid}/sales-marketing/campaigns/{id:guid})

Source: apps/api/Controllers/SalesMarketingController.cs:30

| Validation/guard expressions |
| --- |
| if (campaign is null) return NotFound(); |
| if (!CampaignStatuses.Contains(r.Status) \|\| r.Budget < 0) return BadRequest(new { message = "Invalid campaign update." }); |

| Return / serialization expressions |
| --- |
| if (campaign is null) return NotFound(); |
| if (!CampaignStatuses.Contains(r.Status) \|\| r.Budget < 0) return BadRequest(new { message = "Invalid campaign update." }); |
| return Ok(campaign); |

### SalesMarketingController.Trials (GET /api/academies/{academyId:guid}/sales-marketing/trials)

Source: apps/api/Controllers/SalesMarketingController.cs:41

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| Ok(await db.TrialClassBookings.AsNoTracking().Where(x => x.AcademyId == academyId).OrderBy(x => x.ScheduledAtUtc).ToListAsync(t)); |

### SalesMarketingController.CreateTrial (POST /api/academies/{academyId:guid}/sales-marketing/trials)

Source: apps/api/Controllers/SalesMarketingController.cs:45

| Validation/guard expressions |
| --- |
| if (r.ScheduledAtUtc == default \|\| !await db.Leads.AnyAsync(x => x.Id == r.LeadId && x.AcademyId == academyId, t)) return BadRequest(new { message = "Select a valid lead and scheduled time." }); |
| if (r.BatchId.HasValue && !await db.Batches.AnyAsync(x => x.Id == r.BatchId && x.AcademyId == academyId, t)) return BadRequest(new { message = "Select a valid class or batch." }); |
| if (r.TeacherId.HasValue && !await db.Teachers.AnyAsync(x => x.Id == r.TeacherId && x.AcademyId == academyId, t)) return BadRequest(new { message = "Select a valid teacher." }); |
| var lead = await db.Leads.FindAsync([r.LeadId], t); if (lead is not null) lead.Stage = "TrialBooked"; |

| Return / serialization expressions |
| --- |
| if (r.ScheduledAtUtc == default \|\| !await db.Leads.AnyAsync(x => x.Id == r.LeadId && x.AcademyId == academyId, t)) return BadRequest(new { message = "Select a valid lead and scheduled time." }); |
| if (r.BatchId.HasValue && !await db.Batches.AnyAsync(x => x.Id == r.BatchId && x.AcademyId == academyId, t)) return BadRequest(new { message = "Select a valid class or batch." }); |
| if (r.TeacherId.HasValue && !await db.Teachers.AnyAsync(x => x.Id == r.TeacherId && x.AcademyId == academyId, t)) return BadRequest(new { message = "Select a valid teacher." }); |
| return Ok(trial); |

### SalesMarketingController.UpdateTrialStatus (PATCH /api/academies/{academyId:guid}/sales-marketing/trials/{id:guid}/status)

Source: apps/api/Controllers/SalesMarketingController.cs:58

| Validation/guard expressions |
| --- |
| if (!TrialStatuses.Contains(r.Status)) return BadRequest(new { message = "Invalid trial status." }); |
| if (trial is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (!TrialStatuses.Contains(r.Status)) return BadRequest(new { message = "Invalid trial status." }); |
| if (trial is null) return NotFound(); |
| return Ok(trial); |

### StaffController.List (GET /api/academies/{academyId:guid}/staff)

Source: apps/api/Controllers/StaffController.cs:17

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (roles.Any(role => role is "Student" or "Guardian" or "Teacher")) continue; |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| return Ok(result); |

### StaffController.Create (POST /api/academies/{academyId:guid}/staff)

Source: apps/api/Controllers/StaffController.cs:38

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (!AllowedRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase)) |
| return BadRequest(new { message = "Choose a supported staff role." }); |
| if (string.IsNullOrWhiteSpace(request.Email) \|\| string.IsNullOrWhiteSpace(request.DisplayName) \|\| string.IsNullOrWhiteSpace(request.Password)) |
| return BadRequest(new { message = "Email, display name, and an initial password are required." }); |
| if (!await roleManager.RoleExistsAsync(role)) |
| if (!roleResult.Succeeded) return Problem("The staff role could not be created."); |
| if (!createResult.Succeeded) |
| return BadRequest(new { message = string.Join(" ", createResult.Errors.Select(x => x.Description)) }); |
| if (!addRoleResult.Succeeded) |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| return BadRequest(new { message = "Choose a supported staff role." }); |
| return BadRequest(new { message = "Email, display name, and an initial password are required." }); |
| return BadRequest(new { message = string.Join(" ", createResult.Errors.Select(x => x.Description)) }); |
| return Created($"/api/academies/{academyId}/staff/{user.Id}", |

### StaffController.UpdateStatus (PATCH /api/academies/{academyId:guid}/staff/{staffId:guid}/status)

Source: apps/api/Controllers/StaffController.cs:78

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (current?.Id == staffId) return BadRequest(new { message = "The owner account cannot be deactivated here." }); |
| if (staff?.AcademyId != academyId) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (current?.Id == staffId) return BadRequest(new { message = "The owner account cannot be deactivated here." }); |
| if (staff?.AcademyId != academyId) return NotFound(); |
| return Ok(new { staff.Id, staff.IsActive }); |

### StaffController.UpdateRole (PATCH /api/academies/{academyId:guid}/staff/{staffId:guid}/role)

Source: apps/api/Controllers/StaffController.cs:91

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (!AllowedRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase)) |
| return BadRequest(new { message = "Choose a supported staff role." }); |
| if (staff?.AcademyId != academyId) return NotFound(); |
| if (removableRoles.Length > 0) |
| if (!removeResult.Succeeded) return Problem("The existing staff role could not be updated."); |
| if (!addResult.Succeeded) return Problem("The new staff role could not be assigned."); |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| return BadRequest(new { message = "Choose a supported staff role." }); |
| if (staff?.AcademyId != academyId) return NotFound(); |
| return Ok(new { staff.Id, Roles = new[] { requestedRole } }); |

### StaffController.ResetPassword (PATCH /api/academies/{academyId:guid}/staff/{staffId:guid}/password)

Source: apps/api/Controllers/StaffController.cs:114

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.NewPassword) \|\| request.NewPassword.Length < 8) |
| return BadRequest(new { message = "The new password must be at least 8 characters." }); |
| if (staff?.AcademyId != academyId) return NotFound(); |
| if (!result.Succeeded) return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) }); |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| return BadRequest(new { message = "The new password must be at least 8 characters." }); |
| if (staff?.AcademyId != academyId) return NotFound(); |
| if (!result.Succeeded) return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) }); |
| return Ok(new { message = "Staff password reset successfully." }); |

### StaffController.Offboard (POST /api/academies/{academyId:guid}/staff/{staffId:guid}/offboard)

Source: apps/api/Controllers/StaffController.cs:128

| Validation/guard expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (current?.Id == staffId) return BadRequest(new { message = "The signed-in administrator cannot offboard their own account." }); |
| if (staff?.AcademyId != academyId) return NotFound(); |
| if (!result.Succeeded) return Problem("Staff offboarding could not be completed."); |

| Return / serialization expressions |
| --- |
| if (!await IsOwner(academyId)) return Forbid(); |
| if (current?.Id == staffId) return BadRequest(new { message = "The signed-in administrator cannot offboard their own account." }); |
| if (staff?.AcademyId != academyId) return NotFound(); |
| return Ok(new { message = "Staff access revoked and active sessions invalidated." }); |

### StudentFeeArrangementsController.AdmissionFee (GET /api/academies/{academyId:guid}/students/{studentId:guid}/fee-arrangements/admission-fee)

Source: apps/api/Controllers/StudentFeeArrangementsController.cs:12

| Validation/guard expressions |
| --- |
| return student is null ? NotFound() : Ok(new AdmissionFeeSummary(student.AdmissionFeeAmount, student.AdmissionFeeDueDate)); |

| Return / serialization expressions |
| --- |


### StudentFeeArrangementsController.UpdateAdmissionFee (PUT /api/academies/{academyId:guid}/students/{studentId:guid}/fee-arrangements/admission-fee)

Source: apps/api/Controllers/StudentFeeArrangementsController.cs:19

| Validation/guard expressions |
| --- |
| if (request.Amount is < 0 \|\| (request.DueDate is not null && request.Amount is null)) return BadRequest(new { message = "Enter a non-negative admission fee before setting its due date." }); |
| if (student is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (request.Amount is < 0 \|\| (request.DueDate is not null && request.Amount is null)) return BadRequest(new { message = "Enter a non-negative admission fee before setting its due date." }); |
| if (student is null) return NotFound(); |
| return Ok(new AdmissionFeeSummary(student.AdmissionFeeAmount, student.AdmissionFeeDueDate)); |

### StudentFeeArrangementsController.List (GET /api/academies/{academyId:guid}/students/{studentId:guid}/fee-arrangements)

Source: apps/api/Controllers/StudentFeeArrangementsController.cs:31

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### StudentFeeArrangementsController.Create (POST /api/academies/{academyId:guid}/students/{studentId:guid}/fee-arrangements)

Source: apps/api/Controllers/StudentFeeArrangementsController.cs:34

| Validation/guard expressions |
| --- |
| if (!await db.Students.AnyAsync(x => x.Id == studentId && x.AcademyId == academyId, token)) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.SubjectName) \|\| request.Amount <= 0 \|\| !new[] { "Monthly", "Quarterly", "HalfYearly", "Annual" }.Contains(request.Frequency)) return BadRequest(new { message = "Subject, amount, and a valid billing frequency are required." }); |
| if (request.CourseId.HasValue && !await db.Courses.AnyAsync(x => x.Id == request.CourseId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The course does not belong to this academy." }); |

| Return / serialization expressions |
| --- |
| if (!await db.Students.AnyAsync(x => x.Id == studentId && x.AcademyId == academyId, token)) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.SubjectName) \|\| request.Amount <= 0 \|\| !new[] { "Monthly", "Quarterly", "HalfYearly", "Annual" }.Contains(request.Frequency)) return BadRequest(new { message = "Subject, amount, and a valid billing frequency are required." }); |
| if (request.CourseId.HasValue && !await db.Courses.AnyAsync(x => x.Id == request.CourseId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The course does not belong to this academy." }); |
| return Ok(new FeeArrangementSummary(arrangement.Id, arrangement.CourseId, arrangement.SubjectName, arrangement.Amount, arrangement.Frequency, arrangement.EffectiveFrom, arrangement.EffectiveTo, arrangement.IsActive)); |

### StudentGuardiansController.List (GET /api/academies/{academyId:guid}/students/{studentId:guid}/guardians)

Source: apps/api/Controllers/StudentGuardiansController.cs:12

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| return Ok(links); |

### StudentGuardiansController.Link (POST /api/academies/{academyId:guid}/students/{studentId:guid}/guardians)

Source: apps/api/Controllers/StudentGuardiansController.cs:25

| Validation/guard expressions |
| --- |
| if (student is null \|\| !validGuardian) return NotFound(); |
| if (await dbContext.StudentGuardians.AnyAsync(x => x.AcademyId == academyId && x.StudentId == studentId && x.GuardianId == request.GuardianId, cancellationToken)) |
| if (request.IsPrimary) |

| Return / serialization expressions |
| --- |
| if (student is null \|\| !validGuardian) return NotFound(); |
| return Created($"/api/academies/{academyId}/students/{studentId}/guardians/{guardian.Id}", Summary(guardian, studentGuardian)); |

### StudentGuardiansController.SetPortalAccess (PATCH /api/academies/{academyId:guid}/students/{studentId:guid}/guardians/{guardianId:guid}/portal-access)

Source: apps/api/Controllers/StudentGuardiansController.cs:48

| Validation/guard expressions |
| --- |
| if (link?.Guardian is null \|\| link.Student is null) return NotFound(); |

| Return / serialization expressions |
| --- |
| if (link?.Guardian is null \|\| link.Student is null) return NotFound(); |
| return Ok(Summary(link.Guardian, link)); |

### StudentGuardiansController.Unlink (DELETE /api/academies/{academyId:guid}/students/{studentId:guid}/guardians/{guardianId:guid})

Source: apps/api/Controllers/StudentGuardiansController.cs:67

| Validation/guard expressions |
| --- |
| { var link=await dbContext.StudentGuardians.SingleOrDefaultAsync(x=>x.AcademyId==academyId&&x.StudentId==studentId&&x.GuardianId==guardianId,token); if(link is null)return NotFound(); dbContext.StudentGuardians.Remove(link); await dbContext.SaveChangesAsync(token); return NoContent(); } |

| Return / serialization expressions |
| --- |
| { var link=await dbContext.StudentGuardians.SingleOrDefaultAsync(x=>x.AcademyId==academyId&&x.StudentId==studentId&&x.GuardianId==guardianId,token); if(link is null)return NotFound(); dbContext.StudentGuardians.Remove(link); await dbContext.SaveChangesAsync(token); return NoContent(); } |

### StudentImportsController.Validate (POST /api/academies/{academyId:guid}/imports/students/validate)

Source: apps/api/Controllers/StudentImportsController.cs:11

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |


### StudentImportsController.Import (POST /api/academies/{academyId:guid}/imports/students)

Source: apps/api/Controllers/StudentImportsController.cs:13

| Validation/guard expressions |
| --- |
| var result = ValidateRows(request.Rows); if (result.Errors.Count > 0) return BadRequest(result); |
| if (existing.Count > 0) return Conflict(new { message = "Some email addresses already exist in this academy.", emails = existing }); |
| var errors = new List<object>(); if (rows is null \|\| rows.Count == 0) errors.Add(new { row = 0, message = "Provide at least one student row." }); |
| if (rows is not null) for (var index = 0; index < rows.Count; index++) { var row = rows[index]; if (string.IsNullOrWhiteSpace(row.FirstName) \|\| string.IsNullOrWhiteSpace(row.LastName)) errors.Add(new { row = index + 1, message = "First name and last name are required." }); if (!string.IsNullOrWhiteSpace(row.Email) && !row.Email.Contains('@')) errors.Add(new { row = index + 1, message = "Email address is invalid." }); } |

| Return / serialization expressions |
| --- |
| var result = ValidateRows(request.Rows); if (result.Errors.Count > 0) return BadRequest(result); |
| return Ok(new { imported = rows.Count }); |

### StudentOnboardingController.Create (POST /api/academies/{academyId:guid}/student-onboarding)

Source: apps/api/Controllers/StudentOnboardingController.cs:16

| Validation/guard expressions |
| --- |
| if (actor?.AcademyId != academyId \|\| (!await users.IsInRoleAsync(actor, "Owner") && !await users.IsInRoleAsync(actor, "AcademyAdmin"))) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.StudentFirstName) \|\| string.IsNullOrWhiteSpace(request.StudentLastName) \|\| request.DateOfBirth is null) return BadRequest(new { message = "Student name and date of birth are required." }); |
| if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow)) return BadRequest(new { message = "Date of birth cannot be in the future." }); |
| if (isMinor && (string.IsNullOrWhiteSpace(request.ParentFirstName) \|\| string.IsNullOrWhiteSpace(request.ParentLastName) \|\| string.IsNullOrWhiteSpace(request.ParentEmail))) return BadRequest(new { message = "A parent name and email are required for a minor student." }); |
| if (hasParentDetails && (string.IsNullOrWhiteSpace(request.ParentFirstName) \|\| string.IsNullOrWhiteSpace(request.ParentLastName))) |
| return BadRequest(new { message = "Enter both the parent first name and last name, or clear the optional parent details." }); |
| if (hasParentDetails) |
| if (parent is not null) { var parentAccess = isMinor \|\| request.AllowParentPortalAccess; db.StudentGuardians.Add(new StudentGuardian { AcademyId = academyId, StudentId = student.Id, GuardianId = parent.Id, Relationship = string.IsNullOrWhiteSpace(request.Relationship) ? "Parent" : request.Relationship.Trim(), IsPrimary = true, CanAccessPortal = parentAccess, CanViewAcademicProgress = parentAccess && request.AllowAcademicProgress, CanViewFinance = parentAccess && request.AllowFinance, CanViewDocuments = parentAccess && request.AllowDocuments, CanManageLeave = parentAccess && request.AllowLeave, AccessGrantedAtUtc = parentAccess ? DateTime.UtcNow : null }); } |
| if (!await roles.RoleExistsAsync("Student")) await roles.CreateAsync(new ApplicationRole { Name = "Student" }); |
| if (!await roles.RoleExistsAsync("Guardian")) await roles.CreateAsync(new ApplicationRole { Name = "Guardian" }); |
| if (!string.IsNullOrWhiteSpace(request.StudentUserName) && !string.IsNullOrWhiteSpace(request.StudentTemporaryPassword)) await CreateAccount(request.StudentUserName, request.StudentEmail ?? $"{request.StudentUserName}@academydesk.local", request.StudentTemporaryPassword, student.FirstName + " " + student.LastName, academyId, "Student", student.Id, null, token); |
| if (parent is not null && !string.IsNullOrWhiteSpace(request.ParentUserName) && !string.IsNullOrWhiteSpace(request.ParentTemporaryPassword)) await CreateAccount(request.ParentUserName, parent.Email!, request.ParentTemporaryPassword, parent.FirstName + " " + parent.LastName, academyId, "Guardian", null, parent.Id, token); |
| return BadRequest(new { message = "Student onboarding could not be saved. Check that the student number is unique and the entered values are valid." }); |
| return BadRequest(new { message = exception.Message }); |
| if (await users.FindByNameAsync(userName) is not null \|\| await users.FindByEmailAsync(email) is not null) throw new InvalidOperationException("Student or Parent username/email is already in use."); |
| if (!created.Succeeded) throw new InvalidOperationException(string.Join(" ", created.Errors.Select(x => x.Description))); |

| Return / serialization expressions |
| --- |
| if (actor?.AcademyId != academyId \|\| (!await users.IsInRoleAsync(actor, "Owner") && !await users.IsInRoleAsync(actor, "AcademyAdmin"))) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.StudentFirstName) \|\| string.IsNullOrWhiteSpace(request.StudentLastName) \|\| request.DateOfBirth is null) return BadRequest(new { message = "Student name and date of birth are required." }); |
| if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow)) return BadRequest(new { message = "Date of birth cannot be in the future." }); |
| if (isMinor && (string.IsNullOrWhiteSpace(request.ParentFirstName) \|\| string.IsNullOrWhiteSpace(request.ParentLastName) \|\| string.IsNullOrWhiteSpace(request.ParentEmail))) return BadRequest(new { message = "A parent name and email are required for a minor student." }); |
| return BadRequest(new { message = "Enter both the parent first name and last name, or clear the optional parent details." }); |
| return Ok(new { student.Id, student.FirstName, student.LastName, IsMinor = isMinor, ParentId = parent?.Id, StudentAccountCreated = !string.IsNullOrWhiteSpace(request.StudentUserName), ParentAccountCreated = parent is not null && !string.IsNullOrWhiteSpace(request.ParentUserName) }); |
| return BadRequest(new { message = "Student onboarding could not be saved. Check that the student number is unique and the entered values are valid." }); |
| return BadRequest(new { message = exception.Message }); |

### StudentsController.Overview (GET /api/academies/{academyId:guid}/students/overview)

Source: apps/api/Controllers/StudentsController.cs:14

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| return Ok(new StudentOverviewSummary( |

### StudentsController.List (GET /api/academies/{academyId:guid}/students)

Source: apps/api/Controllers/StudentsController.cs:32

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| return Ok(students); |

### StudentsController.Create (POST /api/academies/{academyId:guid}/students)

Source: apps/api/Controllers/StudentsController.cs:43

| Validation/guard expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.FirstName) \|\| string.IsNullOrWhiteSpace(request.LastName)) |
| return BadRequest(new { message = "First name and last name are required." }); |
| if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken)) |
| return BadRequest(new { message = "The selected branch does not belong to this academy." }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| return BadRequest(new { message = "First name and last name are required." }); |
| return BadRequest(new { message = "The selected branch does not belong to this academy." }); |
| return Created($"/api/academies/{academyId}/students/{student.Id}", response); |

### StudentsController.Update (PUT /api/academies/{academyId:guid}/students/{studentId:guid})

Source: apps/api/Controllers/StudentsController.cs:67

| Validation/guard expressions |
| --- |
| if (student is null) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.FirstName) \|\| string.IsNullOrWhiteSpace(request.LastName)) |
| return BadRequest(new { message = "First and last name are required." }); |
| if (!string.IsNullOrWhiteSpace(student.Email)) account.Email = student.Email; |
| if (!update.Succeeded) |
| return BadRequest(new { message = "Student record saved, but the linked portal account could not be updated." }); |

| Return / serialization expressions |
| --- |
| if (student is null) return NotFound(); |
| return BadRequest(new { message = "First and last name are required." }); |
| return BadRequest(new { message = "Student record saved, but the linked portal account could not be updated." }); |
| return Ok(new StudentSummary(student.Id, student.FirstName, student.LastName, student.Email, student.Phone, student.BranchId, student.IsActive)); |

### TeacherCompensationController.Get (GET /api/academies/{academyId:guid}/teachers/{teacherId:guid}/compensation)

Source: apps/api/Controllers/TeacherCompensationController.cs:12

| Validation/guard expressions |
| --- |
| return teacher is null ? NotFound() : Ok(Read(teacherId, teacher.CompensationJson)); |

| Return / serialization expressions |
| --- |


### TeacherCompensationController.Save (PUT /api/academies/{academyId:guid}/teachers/{teacherId:guid}/compensation)

Source: apps/api/Controllers/TeacherCompensationController.cs:19

| Validation/guard expressions |
| --- |
| if (teacher is null) return NotFound(); |
| if (request.Model is not ("Monthly" or "Hourly")) return BadRequest(new { message = "Choose monthly salary or hourly rates." }); |
| if (request.Model == "Monthly" && request.MonthlySalary is null) return BadRequest(new { message = "Enter the monthly salary." }); |
| if (request.Model == "Hourly" && request.StandardHourlyRate is null) return BadRequest(new { message = "Enter the standard hourly rate." }); |
| if (new[] { request.MonthlySalary, request.StandardHourlyRate, request.BeginnerHourlyRate, request.IntermediateHourlyRate, request.AdvancedHourlyRate }.Any(value => value is < 0)) return BadRequest(new { message = "Payment rates cannot be negative." }); |
| if (string.IsNullOrWhiteSpace(compensationJson)) return new TeacherCompensationSummary(teacherId, "", null, null, null, null, null, null); |
| if (names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))) return property.Value; |

| Return / serialization expressions |
| --- |
| if (teacher is null) return NotFound(); |
| if (request.Model is not ("Monthly" or "Hourly")) return BadRequest(new { message = "Choose monthly salary or hourly rates." }); |
| if (request.Model == "Monthly" && request.MonthlySalary is null) return BadRequest(new { message = "Enter the monthly salary." }); |
| if (request.Model == "Hourly" && request.StandardHourlyRate is null) return BadRequest(new { message = "Enter the standard hourly rate." }); |
| if (new[] { request.MonthlySalary, request.StandardHourlyRate, request.BeginnerHourlyRate, request.IntermediateHourlyRate, request.AdvancedHourlyRate }.Any(value => value is < 0)) return BadRequest(new { message = "Payment rates cannot be negative." }); |
| return Ok(summary); |

### TeacherPortalController.Me (GET /api/teacher/me)

Source: apps/api/Controllers/TeacherPortalController.cs:21

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (teacher is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (teacher is null) return Forbid(); |
| return Ok(new TeacherPortalSummary(teacher.FirstName, teacher.LastName, batches, sessions)); |

### TeacherPortalController.Calendar (GET /api/teacher/calendar)

Source: apps/api/Controllers/TeacherPortalController.cs:47

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null \|\| month is < 1 or > 12 \|\| year is < 2020 or > 2100) return BadRequest(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null \|\| month is < 1 or > 12 \|\| year is < 2020 or > 2100) return BadRequest(); |
| return Ok(new TeacherCalendarSummary(sessions, holidays)); |

### TeacherPortalController.BatchProgress (GET /api/teacher/batch-progress)

Source: apps/api/Controllers/TeacherPortalController.cs:67

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return Ok(result); |

### TeacherPortalController.Profile (GET /api/teacher/profile)

Source: apps/api/Controllers/TeacherPortalController.cs:105

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return teacher is null ? Forbid() : Ok(new TeacherPortalProfileSummary(teacher.FirstName, teacher.LastName, teacher.PreferredName, teacher.Email, teacher.Phone, teacher.AddressLine1, teacher.City, teacher.State, teacher.PostalCode, teacher.EmergencyContactName, teacher.EmergencyContactPhone)); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |

### TeacherPortalController.UpdateProfile (PUT /api/teacher/profile)

Source: apps/api/Controllers/TeacherPortalController.cs:115

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (teacher is null) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.FirstName) \|\| string.IsNullOrWhiteSpace(request.LastName)) |
| return BadRequest(new { message = "First and last name are required." }); |
| if (!string.IsNullOrWhiteSpace(teacher.Email)) user.Email = teacher.Email; |
| if (!identityUpdate.Succeeded) return BadRequest(new { message = string.Join(" ", identityUpdate.Errors.Select(x => x.Description)) }); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (teacher is null) return Forbid(); |
| return BadRequest(new { message = "First and last name are required." }); |
| if (!identityUpdate.Succeeded) return BadRequest(new { message = string.Join(" ", identityUpdate.Errors.Select(x => x.Description)) }); |
| return Ok(new TeacherPortalProfileSummary(teacher.FirstName, teacher.LastName, teacher.PreferredName, teacher.Email, teacher.Phone, teacher.AddressLine1, teacher.City, teacher.State, teacher.PostalCode, teacher.EmergencyContactName, teacher.EmergencyContactPhone)); |

### TeacherPortalController.LeaveRequests (GET /api/teacher/leave-requests)

Source: apps/api/Controllers/TeacherPortalController.cs:145

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return Ok(requests); |

### TeacherPortalController.RequestLeave (POST /api/teacher/leave-requests)

Source: apps/api/Controllers/TeacherPortalController.cs:158

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (request.EndDate < request.StartDate \|\| string.IsNullOrWhiteSpace(request.Reason)) |
| return BadRequest(new { message = "Reason and valid dates are required." }); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return BadRequest(new { message = "Reason and valid dates are required." }); |
| return Ok(new TeacherLeaveSummary(leave.Id, leave.StartDate, leave.EndDate, leave.Reason, leave.Status, leave.DecisionNotes)); |

### TeacherPortalController.PracticeLogs (GET /api/teacher/practice-logs)

Source: apps/api/Controllers/TeacherPortalController.cs:179

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return Ok(logs); |

### TeacherPortalController.ReviewPracticeLog (PATCH /api/teacher/practice-logs/{practiceLogId:guid}/review)

Source: apps/api/Controllers/TeacherPortalController.cs:196

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (!permitted) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (!permitted) return Forbid(); |
| return Ok(new { log.Id, log.Status, log.TeacherFeedback, log.ReviewedAtUtc }); |

### TeacherPortalController.Assignments (GET /api/teacher/assignments)

Source: apps/api/Controllers/TeacherPortalController.cs:213

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return Ok(items); |

### TeacherPortalController.CreateAssignment (POST /api/teacher/assignments)

Source: apps/api/Controllers/TeacherPortalController.cs:226

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.Title) \|\| !await OwnsBatch(user, request.BatchId, cancellationToken)) |
| return BadRequest(new { message = "Select a batch you teach and provide an assignment title." }); |
| if (request.StudentId.HasValue && !await dbContext.Enrollments.AnyAsync(x => x.AcademyId == user.AcademyId && x.BatchId == request.BatchId && x.StudentId == request.StudentId && x.Status == "Active", cancellationToken)) |
| return BadRequest(new { message = "The selected student is not active in this batch." }); |
| if (assignment.IsPublished) |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return BadRequest(new { message = "Select a batch you teach and provide an assignment title." }); |
| return BadRequest(new { message = "The selected student is not active in this batch." }); |
| return Ok(new TeacherAssignmentSummary(assignment.Id, assignment.BatchId, assignment.StudentId, assignment.Title, assignment.Description, assignment.DueAtUtc, assignment.Type, assignment.IsPublished)); |

### TeacherPortalController.PublishAssignment (PATCH /api/teacher/assignments/{assignmentId:guid}/publish)

Source: apps/api/Controllers/TeacherPortalController.cs:253

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (assignment is null \|\| !await OwnsBatch(user, assignment.BatchId, cancellationToken)) return Forbid(); |
| if (assignment.IsPublished) |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (assignment is null \|\| !await OwnsBatch(user, assignment.BatchId, cancellationToken)) return Forbid(); |
| return Ok(new { assignment.Id, assignment.IsPublished }); |

### TeacherPortalController.Submissions (GET /api/teacher/assignments/{assignmentId:guid}/submissions)

Source: apps/api/Controllers/TeacherPortalController.cs:267

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (assignment is null \|\| !await OwnsBatch(user, assignment.BatchId, cancellationToken)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (assignment is null \|\| !await OwnsBatch(user, assignment.BatchId, cancellationToken)) return Forbid(); |
| return Ok(submissions); |

### TeacherPortalController.ReviewSubmission (PATCH /api/teacher/submissions/{submissionId:guid}/review)

Source: apps/api/Controllers/TeacherPortalController.cs:282

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (submission is null) return NotFound(); |
| if (assignment is null \|\| !await OwnsBatch(user, assignment.BatchId, cancellationToken)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (submission is null) return NotFound(); |
| if (assignment is null \|\| !await OwnsBatch(user, assignment.BatchId, cancellationToken)) return Forbid(); |
| return Ok(new { submission.Id, submission.Status, submission.TeacherFeedback }); |

### TeacherPortalController.LessonPlans (GET /api/teacher/lesson-plans)

Source: apps/api/Controllers/TeacherPortalController.cs:297

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return Ok(plans); |

### TeacherPortalController.CreateLessonPlan (POST /api/teacher/lesson-plans)

Source: apps/api/Controllers/TeacherPortalController.cs:310

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.Title) \|\| !await OwnsBatch(user, request.BatchId, cancellationToken)) |
| return BadRequest(new { message = "Select a batch you teach and provide a lesson title." }); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return BadRequest(new { message = "Select a batch you teach and provide a lesson title." }); |
| return Ok(new TeacherLessonPlanSummary(plan.Id, plan.BatchId, plan.CourseModuleId, plan.ClassSessionId, plan.Title, plan.Objectives, plan.Status)); |

### TeacherPortalController.UpdateLessonPlanStatus (PATCH /api/teacher/lesson-plans/{lessonPlanId:guid}/status)

Source: apps/api/Controllers/TeacherPortalController.cs:323

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (request.Status is not ("Planned" or "Delivered" or "Skipped" or "MakeupNeeded")) return BadRequest(new { message = "Invalid lesson-plan status." }); |
| if (plan is null \|\| !await OwnsBatch(user, plan.BatchId, cancellationToken)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (request.Status is not ("Planned" or "Delivered" or "Skipped" or "MakeupNeeded")) return BadRequest(new { message = "Invalid lesson-plan status." }); |
| if (plan is null \|\| !await OwnsBatch(user, plan.BatchId, cancellationToken)) return Forbid(); |
| return Ok(new { plan.Id, plan.Status }); |

### TeacherPortalController.Assessments (GET /api/teacher/assessments)

Source: apps/api/Controllers/TeacherPortalController.cs:336

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return Ok(items); |

### TeacherPortalController.CreateAssessment (POST /api/teacher/assessments)

Source: apps/api/Controllers/TeacherPortalController.cs:349

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (string.IsNullOrWhiteSpace(request.Title) \|\| request.MaxScore <= 0 \|\| !await OwnsBatch(user, request.BatchId, cancellationToken)) |
| return BadRequest(new { message = "Select a batch you teach, provide a title, and use a positive maximum score." }); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return BadRequest(new { message = "Select a batch you teach, provide a title, and use a positive maximum score." }); |
| return Ok(new TeacherAssessmentSummary(assessment.Id, assessment.BatchId, assessment.Title, assessment.Type, assessment.MaxScore, assessment.ScheduledAtUtc, assessment.IsPublished)); |

### TeacherPortalController.PublishAssessment (PATCH /api/teacher/assessments/{assessmentId:guid}/publish)

Source: apps/api/Controllers/TeacherPortalController.cs:371

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (assessment is null \|\| !await OwnsBatch(user, assessment.BatchId, cancellationToken)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (assessment is null \|\| !await OwnsBatch(user, assessment.BatchId, cancellationToken)) return Forbid(); |
| return Ok(new { assessment.Id, assessment.IsPublished }); |

### TeacherPortalController.AssessmentResults (GET /api/teacher/assessments/{assessmentId:guid}/results)

Source: apps/api/Controllers/TeacherPortalController.cs:383

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (assessment is null \|\| !await OwnsBatch(user, assessment.BatchId, cancellationToken)) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (assessment is null \|\| !await OwnsBatch(user, assessment.BatchId, cancellationToken)) return Forbid(); |
| return Ok(results); |

### TeacherPortalController.RecordAssessmentResult (POST /api/teacher/assessments/{assessmentId:guid}/results)

Source: apps/api/Controllers/TeacherPortalController.cs:398

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (assessment is null \|\| !await OwnsBatch(user, assessment.BatchId, cancellationToken)) return Forbid(); |
| if (request.Score < 0 \|\| request.Score > assessment.MaxScore \|\| !await dbContext.Enrollments.AnyAsync(x => |
| return BadRequest(new { message = "Student must be actively enrolled and score must be within the assessment range." }); |
| if (isNew) dbContext.AssessmentResults.Add(result); |
| if (result.IsPublished) |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| if (assessment is null \|\| !await OwnsBatch(user, assessment.BatchId, cancellationToken)) return Forbid(); |
| return BadRequest(new { message = "Student must be actively enrolled and score must be within the assessment range." }); |
| return Ok(new TeacherAssessmentResultSummary(result.Id, result.StudentId, student.FirstName + " " + student.LastName, result.Score, result.Grade, result.Remarks, result.IsPublished)); |

### TeacherPortalController.Resources (GET /api/teacher/resources)

Source: apps/api/Controllers/TeacherPortalController.cs:420

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return Ok(items); |

### TeacherPortalController.ClassroomActivity (GET /api/teacher/classroom-activity)

Source: apps/api/Controllers/TeacherPortalController.cs:433

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null \|\| !await OwnsBatch(user, batchId, cancellationToken)) return Forbid(); |
| if (studentId.HasValue && !await dbContext.Enrollments.AnyAsync(x => x.AcademyId == user.AcademyId && x.BatchId == batchId && x.StudentId == studentId && x.Status == "Active", cancellationToken)) |
| return BadRequest(new { message = "The selected student is not active in this batch." }); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null \|\| !await OwnsBatch(user, batchId, cancellationToken)) return Forbid(); |
| return BadRequest(new { message = "The selected student is not active in this batch." }); |
| return Ok(new TeacherClassroomActivitySummary(resources, homework)); |

### TeacherPortalController.CreateClassNote (POST /api/teacher/resources/note)

Source: apps/api/Controllers/TeacherPortalController.cs:451

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null \|\| string.IsNullOrWhiteSpace(request.Title) \|\| string.IsNullOrWhiteSpace(request.Notes) \|\| !await OwnsBatch(user, request.BatchId, cancellationToken)) |
| return BadRequest(new { message = "Select one of your batches and provide a title and note." }); |
| if (request.StudentId.HasValue && !await dbContext.Enrollments.AnyAsync(x => x.AcademyId == user.AcademyId && x.BatchId == request.BatchId && x.StudentId == request.StudentId && x.Status == "Active", cancellationToken)) |
| return BadRequest(new { message = "The selected student is not active in this batch." }); |

| Return / serialization expressions |
| --- |
| return BadRequest(new { message = "Select one of your batches and provide a title and note." }); |
| return BadRequest(new { message = "The selected student is not active in this batch." }); |
| return Ok(ResourceSummary(resource)); |

### TeacherPortalController.UploadClassMaterial (POST /api/teacher/resources/upload)

Source: apps/api/Controllers/TeacherPortalController.cs:466

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null \|\| request.File is null \|\| request.File.Length == 0 \|\| !await OwnsBatch(user, request.BatchId, cancellationToken)) |
| return BadRequest(new { message = "Select one of your batches and a file to upload." }); |
| if (request.File.Length > 50_000_000) return BadRequest(new { message = "Files must be 50 MB or smaller." }); |
| if (request.StudentId.HasValue && !await dbContext.Enrollments.AnyAsync(x => x.AcademyId == user.AcademyId && x.BatchId == request.BatchId && x.StudentId == request.StudentId && x.Status == "Active", cancellationToken)) |
| return BadRequest(new { message = "The selected student is not active in this batch." }); |
| if (!allowed.Contains(extension)) return BadRequest(new { message = "Upload a photo, PDF, audio, video, or document." }); |

| Return / serialization expressions |
| --- |
| return BadRequest(new { message = "Select one of your batches and a file to upload." }); |
| if (request.File.Length > 50_000_000) return BadRequest(new { message = "Files must be 50 MB or smaller." }); |
| return BadRequest(new { message = "The selected student is not active in this batch." }); |
| if (!allowed.Contains(extension)) return BadRequest(new { message = "Upload a photo, PDF, audio, video, or document." }); |
| return Ok(ResourceSummary(resource)); |

### TeacherPortalController.Progress (GET /api/teacher/progress)

Source: apps/api/Controllers/TeacherPortalController.cs:490

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return Forbid(); |
| return Ok(new TeacherProgressSummary(completed, upcoming, attendance.Count, present)); |

### TeacherPortalController.Payments (GET /api/teacher/payments)

Source: apps/api/Controllers/TeacherPortalController.cs:503

| Validation/guard expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null \|\| (year.HasValue != month.HasValue) \|\| (month.HasValue && month is < 1 or > 12) \|\| (year.HasValue && year is < 2020 or > 2100)) return BadRequest(); |
| if (year.HasValue && month.HasValue) |
| if (batch is null) return; |

| Return / serialization expressions |
| --- |
| if (user?.AcademyId is null \|\| user.TeacherId is null \|\| (year.HasValue != month.HasValue) \|\| (month.HasValue && month is < 1 or > 12) \|\| (year.HasValue && year is < 2020 or > 2100)) return BadRequest(); |
| return Ok(new TeacherPaymentSummary(profile?.PaymentModel, profile?.MonthlyAmount, profile?.AmountPerCycle, profile?.SessionsPerCycle, payouts)); |

### TeacherPortalController.Roster (GET /api/teacher/sessions/{sessionId:guid}/roster)

Source: apps/api/Controllers/TeacherPortalController.cs:532

| Validation/guard expressions |
| --- |
| if (context is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (context is null) return Forbid(); |
| return Ok(students); |

### TeacherPortalController.MarkAttendance (POST /api/teacher/sessions/{sessionId:guid}/attendance)

Source: apps/api/Controllers/TeacherPortalController.cs:548

| Validation/guard expressions |
| --- |
| if (context is null) return Forbid(); |
| if (!AttendanceStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) |
| return BadRequest(new { message = "Invalid attendance status." }); |
| if (!enrolled) return NotFound(); |
| if (record is null) |

| Return / serialization expressions |
| --- |
| if (context is null) return Forbid(); |
| return BadRequest(new { message = "Invalid attendance status." }); |
| if (!enrolled) return NotFound(); |
| return Ok(new TeacherAttendanceSummary(record.StudentId, record.Status, record.Notes)); |

### TeacherPortalController.Attendance (GET /api/teacher/sessions/{sessionId:guid}/attendance)

Source: apps/api/Controllers/TeacherPortalController.cs:584

| Validation/guard expressions |
| --- |
| if (context is null) return Forbid(); |

| Return / serialization expressions |
| --- |
| if (context is null) return Forbid(); |
| return Ok(records); |

### TeacherPortalController.UpdateSessionStatus (PATCH /api/teacher/sessions/{sessionId:guid}/status)

Source: apps/api/Controllers/TeacherPortalController.cs:597

| Validation/guard expressions |
| --- |
| if (context is null) return Forbid(); |
| if (!new[] { "Scheduled", "InProgress", "Completed", "Cancelled", "Rescheduled" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) |
| return BadRequest(new { message = "Invalid session status." }); |

| Return / serialization expressions |
| --- |
| if (context is null) return Forbid(); |
| return BadRequest(new { message = "Invalid session status." }); |
| return Ok(new { context.Session.Id, context.Session.Status }); |

### TeacherPortalController.MarkAttendanceBulk (POST /api/teacher/sessions/{sessionId:guid}/attendance/bulk)

Source: apps/api/Controllers/TeacherPortalController.cs:609

| Validation/guard expressions |
| --- |
| if (context is null) return Forbid(); |
| if (request.Records.Count == 0) return BadRequest(new { message = "At least one attendance record is required." }); |
| if (request.Records.Any(x => !AttendanceStatuses.Contains(x.Status, StringComparer.OrdinalIgnoreCase))) |
| return BadRequest(new { message = "One or more attendance statuses are invalid." }); |
| if (enrolled.Count != studentIds.Length) return BadRequest(new { message = "Every student must be actively enrolled in this batch." }); |
| if (!existing.TryGetValue(item.StudentId, out var record)) |

| Return / serialization expressions |
| --- |
| if (context is null) return Forbid(); |
| if (request.Records.Count == 0) return BadRequest(new { message = "At least one attendance record is required." }); |
| return BadRequest(new { message = "One or more attendance statuses are invalid." }); |
| if (enrolled.Count != studentIds.Length) return BadRequest(new { message = "Every student must be actively enrolled in this batch." }); |
| return Ok(new { updated = request.Records.Count }); |

### TeacherPortalController.MarkTeacherAttendance (PUT /api/teacher/sessions/{sessionId:guid}/teacher-attendance)

Source: apps/api/Controllers/TeacherPortalController.cs:635

| Validation/guard expressions |
| --- |
| if (context is null) return Forbid(); |
| if (!AttendanceStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid attendance status." }); |
| if (user?.AcademyId is null \|\| user.TeacherId is null) return null; |

| Return / serialization expressions |
| --- |
| if (context is null) return Forbid(); |
| if (!AttendanceStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid attendance status." }); |
| return Ok(new { context.Session.Id, context.Session.TeacherAttendanceStatus }); |

### TeachersController.List (GET /api/academies/{academyId:guid}/teachers)

Source: apps/api/Controllers/TeachersController.cs:14

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
| return Ok(teachers); |

### TeachersController.Create (POST /api/academies/{academyId:guid}/teachers)

Source: apps/api/Controllers/TeachersController.cs:25

| Validation/guard expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.FirstName) \|\| string.IsNullOrWhiteSpace(request.LastName)) |
| return BadRequest(new { message = "First name and last name are required." }); |
| if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken)) |
| return BadRequest(new { message = "The selected branch does not belong to this academy." }); |

| Return / serialization expressions |
| --- |
| if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound(); |
| return BadRequest(new { message = "First name and last name are required." }); |
| return BadRequest(new { message = "The selected branch does not belong to this academy." }); |
| return Created($"/api/academies/{academyId}/teachers/{teacher.Id}", new TeacherSummary(teacher.Id, teacher.FirstName, teacher.LastName, teacher.Email, teacher.Phone, teacher.Specialties, teacher.BranchId, teacher.IsActive)); |

### TeachersController.Update (PUT /api/academies/{academyId:guid}/teachers/{teacherId:guid})

Source: apps/api/Controllers/TeachersController.cs:39

| Validation/guard expressions |
| --- |
| if (teacher is null) return NotFound(); |
| if (string.IsNullOrWhiteSpace(request.FirstName) \|\| string.IsNullOrWhiteSpace(request.LastName)) |
| return BadRequest(new { message = "First and last name are required." }); |
| if (!string.IsNullOrWhiteSpace(teacher.Email)) account.Email = teacher.Email; |
| if (!update.Succeeded) |
| return BadRequest(new { message = "Teacher record saved, but the linked portal account could not be updated." }); |

| Return / serialization expressions |
| --- |
| if (teacher is null) return NotFound(); |
| return BadRequest(new { message = "First and last name are required." }); |
| return BadRequest(new { message = "Teacher record saved, but the linked portal account could not be updated." }); |
| return Ok(new TeacherSummary(teacher.Id, teacher.FirstName, teacher.LastName, teacher.Email, teacher.Phone, teacher.Specialties, teacher.BranchId, teacher.IsActive)); |

### WeatherForecastController.Get (GET /WeatherForecast)

Source: apps/api/Controllers/WeatherForecastController.cs:14

| Validation/guard expressions |
| --- |


| Return / serialization expressions |
| --- |
