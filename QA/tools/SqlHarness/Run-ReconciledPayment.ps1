# Fresh local owned SQL only. Real auth/API and strict selected finance regression.
param([ValidateSet('InvoiceIntegrity','InvoiceStatus','InvoiceStatusBaseline','MusicProgress','FinancePolicyTheme','TeacherPublication','TeacherCompensation','StudentImports','StudentImportsBaseline','WorkItemIntegrity','ExpenseIntegrity','MetadataShape','HolidayRoute','OnboardingNumbers','OnboardingAccounts','OnboardingAtomic','OnboardingDates','TeacherAvailability','EnrollmentLifecycle','GuardianRevocation','TeacherLifecycle','PentaFoundation','Reconciliation','Adjustment','Payroll','Transition','TransitionRace','ReconcileVoidRace','ApprovalRace','Consumers','Access','AuditBaseline','AuditFixed','PeopleBaseline','PeopleFixed','LinkedBaseline','LinkedFixed','BranchBaseline','BranchFixed','Lookups','Governance','CollectionsBaseline','Collections','InvoiceSettingsBaseline','InvoiceSettings','BranchAddressBaseline','BranchAddress','BatchTimesBaseline','BatchTimes','YearClosure','CoursePrerequisites','CoursePreservation','PromotionDecisions','AssessmentGrades','AssessmentRoster','AssessmentAccess','AssessmentOptions','AttendanceNotes','CalendarDetails','ScheduleDefaults','LeaveIdentity','MakeupLocation','RoleReplacement','RoleRevocation','SessionRevocationRepair','SessionRevocation','RefreshConcurrency','SessionRefresh','BrowserPayments','BrowserApproval','BrowserCertificate','BrowserSession','BrowserProvisioning','CollectionRace','AdjustmentRace','ProvisioningConflict','ProvisioningConcurrency','AcademyProvisioning','PortalProvisioning','PlatformProvisioning','TrialDuration','TenantPlan','PlatformBilling','ActivityDeletion','AnnouncementAudience','GuardianFlags','CertificateFamily','CertificateEnrollment','ComplianceIdentity','ResourceScope','PracticeIdentity','SubmissionIdentity','ReviewContext','MarketingConsent','TemplateState','PreferenceAccess','InboxLifecycle','NotificationChannel','CommunicationRoundtrip','MeetingReadiness','BatchPreservationBaseline','BatchPreservation')][string]$Module = 'Reconciliation')
$ErrorActionPreference = 'Stop'
if ($env:QA_ENROLLMENT_SHARED -and ($env:QA_ENROLLMENT_SHARED -ne '1' -or $Module -ne 'EnrollmentLifecycle')) {
    throw 'Enrollment shared-service checks require EnrollmentLifecycle and value1 before creating SQL.'
}
if ($env:QA_PENTA_TEACHER_CHAT_EXPORT -and ($env:QA_PENTA_TEACHER_CHAT_EXPORT -notmatch '^[0-9a-f]{32}$' -or $env:QA_PENTA_TEACHER_CHAT_EXPORT -ne $env:QA_PENTA_TEACHER_BROWSER_EXPORT -or $Module -ne 'TeacherAvailability' -or $env:QA_PENTA_TEACHER_START -ne '1' -or $env:QA_PENTA_TEACHER_CONVERSATION -ne '1' -or $env:QA_PENTA_TEACHER_CORE -ne '1' -or $env:QA_PENTA_TEACHER_WORKFLOW -ne '1' -or $env:QA_TEACHER_CREATION_SERVICE -ne '1')) {
    throw 'Teacher chat browser requires the same reviewed export and all retained startup/conversation/Core/workflow/service gates before SQL.'
}
if ($env:QA_PENTA_TEACHER_START -and ($env:QA_PENTA_TEACHER_START -ne '1' -or $Module -ne 'TeacherAvailability' -or $env:QA_PENTA_TEACHER_CONVERSATION -ne '1' -or $env:QA_PENTA_TEACHER_CORE -ne '1' -or $env:QA_PENTA_TEACHER_WORKFLOW -ne '1' -or $env:QA_TEACHER_CREATION_SERVICE -ne '1')) {
    throw 'Teacher startup checks require TeacherAvailability and retained conversation/Core/workflow/service gates before creating SQL.'
}
if ($env:QA_PENTA_TEACHER_CONVERSATION -and ($env:QA_PENTA_TEACHER_CONVERSATION -ne '1' -or $Module -ne 'TeacherAvailability' -or $env:QA_PENTA_TEACHER_CORE -ne '1' -or $env:QA_PENTA_TEACHER_WORKFLOW -ne '1' -or $env:QA_TEACHER_CREATION_SERVICE -ne '1')) {
    throw 'Teacher conversation checks require TeacherAvailability and retained Core/workflow/service gates before creating SQL.'
}
if ($env:QA_PENTA_TEACHER_BROWSER_EXPORT -and ($env:QA_PENTA_TEACHER_BROWSER_EXPORT -notmatch '^[0-9a-f]{32}$' -or $Module -ne 'TeacherAvailability' -or $env:QA_PENTA_TEACHER_CORE -ne '1' -or $env:QA_PENTA_TEACHER_WORKFLOW -ne '1' -or $env:QA_TEACHER_CREATION_SERVICE -ne '1')) {
    throw 'Teacher browser checks require an exact export and retained Core/workflow/service gates before creating SQL.'
}
if ($env:QA_PENTA_TEACHER_CORE -and ($env:QA_PENTA_TEACHER_CORE -ne '1' -or $Module -ne 'TeacherAvailability' -or $env:QA_PENTA_TEACHER_WORKFLOW -ne '1' -or $env:QA_TEACHER_CREATION_SERVICE -ne '1')) {
    throw 'Teacher Core checks require the retained guarded teacher workflow and shared-service checks before creating SQL.'
}
if ($env:QA_PENTA_TEACHER_WORKFLOW -and ($env:QA_PENTA_TEACHER_WORKFLOW -ne '1' -or $Module -ne 'TeacherAvailability' -or $env:QA_TEACHER_CREATION_SERVICE -ne '1')) {
    throw 'Teacher workflow checks require TeacherAvailability and retained shared-service checks before creating SQL.'
}
if ($env:QA_TEACHER_CREATION_SERVICE -and ($env:QA_TEACHER_CREATION_SERVICE -ne '1' -or $Module -ne 'TeacherAvailability')) {
    throw 'Teacher creation shared-service checks require TeacherAvailability and value1 before creating SQL.'
}
if ($env:QA_ONBOARDING_START_RECOVERY_BROWSER -and ($env:QA_ONBOARDING_START_RECOVERY_BROWSER -ne '1' -or $Module -ne 'OnboardingAtomic' -or $env:QA_ONBOARDING_START_EXPORT -notmatch '^[0-9a-f]{32}$')) {
    throw 'Start recovery browser requires OnboardingAtomic, value1 and an exact owned export build before creating SQL.'
}
if ($env:QA_ONBOARDING_SESSION_BROWSER -and ($env:QA_ONBOARDING_SESSION_BROWSER -ne '1' -or $Module -ne 'OnboardingAtomic' -or $env:QA_ONBOARDING_REVIEW_BROWSER -ne '1')) {
    throw 'Onboarding session browser requires OnboardingAtomic, retained review browser and value1 before creating SQL.'
}
if ($env:QA_REFRESH_LOCK_BROWSER -and ($env:QA_REFRESH_LOCK_BROWSER -ne '1' -or $Module -ne 'RefreshConcurrency')) {
    throw 'Native refresh-lock browser checks require RefreshConcurrency and value1 before creating SQL.'
}
if ($env:QA_ONBOARDING_REVIEW_BROWSER -and ($env:QA_ONBOARDING_REVIEW_BROWSER -ne '1' -or $Module -ne 'OnboardingAtomic')) {
    throw 'Passive onboarding review browser is available only with OnboardingAtomic and value1.'
}
$qaRepo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
if ($env:QA_IDENTITY_CHALLENGE_CHECK) {
    if ($env:QA_IDENTITY_CHALLENGE_CHECK -ne '1' -or $Module -ne 'SessionRevocationRepair') { throw 'Identity challenge checks require SessionRevocationRepair and value1 before creating SQL.' }
    if (!(Test-Path -LiteralPath (Join-Path $qaRepo '.build-check/identity-challenge-sql/bin/SqlHarness/debug/SqlHarness.dll'))) { throw 'Build the isolated identity-challenge-sql harness first.' }
}
if ($env:QA_CSV_EXPORT_CHECK) {
    if ($env:QA_CSV_EXPORT_CHECK -ne '1' -or $Module -ne 'Access') { throw 'CSV export checks require Access and value1 before creating SQL.' }
    if (!(Test-Path -LiteralPath (Join-Path $qaRepo '.build-check/csv-export-sql/bin/SqlHarness/debug/SqlHarness.dll'))) { throw 'Build the isolated csv-export-sql harness first.' }
}
if ($env:QA_ONBOARDING_REVIEW_BROWSER -eq '1' -and !(Test-Path -LiteralPath (Join-Path $qaRepo 'apps/web/out/penta/onboarding.html'))) {
    throw 'Build the onboarding review product export before creating disposable SQL.'
}
if ($env:QA_LIFECYCLE_BROWSER -and ($env:QA_LIFECYCLE_BROWSER -ne '1' -or $Module -ne 'TeacherLifecycle' -or $env:QA_LIFECYCLE_OVERLAP -or $env:QA_TEACHER_LIFECYCLE_BASELINE -or $env:QA_GUARDIAN_REVOCATION_BASELINE)) {
    throw 'Lifecycle browser requires TeacherLifecycle, QA_LIFECYCLE_BROWSER=1, no overlap or baseline flags before creating SQL.'
}
if ($env:QA_LIFECYCLE_OVERLAP -and ($env:QA_LIFECYCLE_OVERLAP -ne '1' -or $Module -notin @('TeacherLifecycle','GuardianRevocation') -or $env:QA_TEACHER_LIFECYCLE_BASELINE -or $env:QA_GUARDIAN_REVOCATION_BASELINE)) {
    throw 'Lifecycle overlap requires TeacherLifecycle/GuardianRevocation, QA_LIFECYCLE_OVERLAP=1 and no baseline flags before creating SQL.'
}
if ($env:QA_PAYROLL_DOCUMENT_BROWSER -and ($env:QA_PAYROLL_DOCUMENT_BROWSER -ne '1' -or $Module -ne 'Payroll' -or $env:QA_PAYROLL_HISTORY -ne 'guarded' -or $env:QA_BATCH1 -ne 'guarded')) {
    throw 'Payroll document browser requires Payroll, guarded Batch1/history and QA_PAYROLL_DOCUMENT_BROWSER=1 before creating SQL.'
}
if ($env:QA_BATCH1 -and ($env:QA_BATCH1 -notin @('baseline','guarded') -or $Module -ne 'Payroll' -or $env:QA_PAYROLL_HISTORY -ne 'guarded')) {
    throw 'Batch1 requires baseline/guarded mode, Payroll and guarded history before creating SQL.'
}
if ($env:QA_BATCH1_FINANCE -and ($env:QA_BATCH1_FINANCE -ne '1' -or $Module -ne 'Payroll' -or $env:QA_BATCH1 -ne 'guarded')) {
    throw 'Batch1 finance add-on requires Payroll, guarded Batch1 and QA_BATCH1_FINANCE=1 before creating SQL.'
}
if ($env:QA_PAYROLL_BROWSER -and ($env:QA_PAYROLL_BROWSER -ne '1' -or $Module -ne 'Payroll' -or $env:QA_PAYROLL_HISTORY -ne 'guarded')) {
    throw 'Payroll browser requires QA_PAYROLL_BROWSER=1, Payroll module and guarded history before creating SQL.'
}
if ($env:QA_PAYROLL_RACE -and ($env:QA_PAYROLL_RACE -ne '1' -or $Module -ne 'Payroll' -or $env:QA_PAYROLL_HISTORY -ne 'guarded')) {
    throw 'Payroll race requires QA_PAYROLL_RACE=1, Payroll module and guarded history before creating SQL.'
}
if ($Module -eq 'Payroll' -and $env:QA_PAYROLL_HISTORY -and $env:QA_PAYROLL_HISTORY -notin @('baseline','guarded')) {
    throw 'QA_PAYROLL_HISTORY must be baseline or guarded before creating SQL.'
}
if (($Module -eq 'PentaFoundation' -and $env:QA_PENTA_BROWSER -eq '1') -or ($Module -eq 'CalendarDetails' -and $env:QA_CALENDAR_BROWSER -eq '1')) {
    $qaBrowserPort = 0
    [Uri]$qaBrowserOrigin = $null
    if (![int]::TryParse($env:QA_BROWSER_API_PORT, [ref]$qaBrowserPort) -or $qaBrowserPort -lt 1024 -or $qaBrowserPort -gt 65535 -or
        ![Uri]::TryCreate($env:QA_BROWSER_ORIGIN, [UriKind]::Absolute, [ref]$qaBrowserOrigin) -or
        $qaBrowserOrigin.Scheme -ne 'http' -or $qaBrowserOrigin.Host -ne '127.0.0.1' -or
        $qaBrowserOrigin.Port -lt 1024 -or $qaBrowserOrigin.Port -gt 65535 -or $qaBrowserOrigin.Port -eq $qaBrowserPort -or
        $qaBrowserOrigin.AbsolutePath -ne '/' -or $qaBrowserOrigin.Query -ne '' -or $qaBrowserOrigin.Fragment -ne '' -or $qaBrowserOrigin.UserInfo -ne '') {
        throw 'Browser requires exact distinct loopback API port and origin before creating SQL.'
    }
}
$qaRun = [Guid]::NewGuid().ToString('N')
$qaContainer = "academydesk-qa-$qaRun"
$qaPreviousSa = $env:QA_SQL_SA_PASSWORD
$qaPreviousMssql = $env:MSSQL_SA_PASSWORD
$qaPreviousInvoiceBaseline = $env:QA_INVOICE_STATUS_BASELINE
$qaStarted = [DateTime]::UtcNow
$qaSwitch = switch ($Module) { 'YearClosure' { '--audit-year-closure' }; 'CoursePrerequisites' { '--audit-course-prerequisites' }; 'CoursePreservation' { '--audit-course-preservation' }; 'PromotionDecisions' { '--audit-promotion-decisions' }; 'Adjustment' { '--finance-adjustment' }; 'Payroll' { '--payroll-net' }; 'Transition' { '--payment-transition' }; 'Consumers' { '--finance-consumers' }; 'Access' { '--finance-access' }; 'Lookups' { '--finance-lookups' }; 'Governance' { '--finance-governance' }; 'CollectionsBaseline' { '--collections-baseline' }; 'Collections' { '--collections-balance' }; 'InvoiceSettingsBaseline' { '--invoice-settings-baseline' }; 'InvoiceSettings' { '--invoice-settings' }; 'BatchTimesBaseline' { '--audit-batch-times-baseline' }; 'BatchTimes' { '--audit-batch-times-fixed' }; 'BatchPreservationBaseline' { '--audit-batch-preservation-baseline' }; 'BatchPreservation' { '--audit-batch-preservation-fixed' }; 'BranchAddressBaseline' { '--audit-branch-address-baseline' }; 'BranchAddress' { '--audit-branch-address-fixed' }; 'AuditBaseline' { '--audit-baseline' }; 'AuditFixed' { '--audit-fixed' }; 'PeopleBaseline' { '--audit-people-baseline' }; 'PeopleFixed' { '--audit-people-fixed' }; 'LinkedBaseline' { '--audit-linked-baseline' }; 'LinkedFixed' { '--audit-linked-fixed' }; 'BranchBaseline' { '--audit-branch-baseline' }; 'BranchFixed' { '--audit-branch-fixed' }; default { '--finance-reconciliation' } }
try {
    if ($Module -eq 'InvoiceIntegrity') { $qaSwitch = '--audit-invoice-integrity' }
    if ($Module -in @('InvoiceStatus','InvoiceStatusBaseline')) { $qaSwitch = '--audit-invoice-status'; $env:QA_INVOICE_STATUS_BASELINE = if ($Module -eq 'InvoiceStatusBaseline') { '1' } else { '0' } }
    if ($Module -eq 'MusicProgress') { $qaSwitch = '--audit-music-progress' }
    if ($Module -eq 'FinancePolicyTheme') { $qaSwitch = '--audit-finance-policy-theme' }
    if ($Module -eq 'TeacherPublication') { $qaSwitch = '--audit-teacher-publication' }
    if ($Module -eq 'TeacherCompensation') { $qaSwitch = '--audit-teacher-compensation' }
    if ($Module -eq 'StudentImports') { $qaSwitch = '--audit-student-imports' }
    if ($Module -eq 'StudentImportsBaseline') { $qaSwitch = '--audit-student-imports-baseline' }
    if ($Module -eq 'WorkItemIntegrity') { $qaSwitch = '--audit-work-item-integrity' }
    if ($Module -eq 'ExpenseIntegrity') { $qaSwitch = '--audit-expense-integrity' }
    if ($Module -eq 'MetadataShape') { $qaSwitch = '--audit-metadata-shape' }
    if ($Module -eq 'HolidayRoute') { $qaSwitch = '--audit-holiday-route' }
    if ($Module -eq 'OnboardingNumbers') { $qaSwitch = '--audit-onboarding-numbers' }
    if ($Module -eq 'OnboardingAccounts') { $qaSwitch = '--audit-onboarding-accounts' }
    if ($Module -eq 'OnboardingAtomic') { $qaSwitch = '--audit-onboarding-atomic' }
    if ($Module -eq 'OnboardingDates') { $qaSwitch = '--audit-onboarding-dates' }
    if ($Module -eq 'TeacherAvailability') { $qaSwitch = '--audit-teacher-availability' }
    if ($Module -eq 'AssessmentGrades') { $qaSwitch = '--audit-assessment-grades' }
    if ($Module -eq 'MeetingReadiness') { $qaSwitch = '--audit-meeting-readiness' }
    if ($Module -eq 'PentaFoundation') { $qaSwitch = '--penta-foundation' }
    if ($Module -eq 'AssessmentRoster') { $qaSwitch = '--audit-assessment-roster' }
    if ($Module -eq 'AssessmentAccess') { $qaSwitch = '--audit-assessment-access' }
    if ($Module -eq 'AssessmentOptions') { $qaSwitch = '--audit-assessment-options' }
    if ($Module -eq 'AttendanceNotes') { $qaSwitch = '--audit-attendance-notes' }
    if ($Module -eq 'CalendarDetails') { $qaSwitch = '--audit-calendar-details' }
    if ($Module -eq 'ScheduleDefaults') { $qaSwitch = '--audit-schedule-defaults' }
    if ($Module -eq 'LeaveIdentity') { $qaSwitch = '--audit-leave-identity' }
    if ($Module -eq 'MakeupLocation') { $qaSwitch = '--audit-makeup-location' }
    if ($Module -eq 'PreferenceAccess') { $qaSwitch = '--audit-preference-access' }
    if ($Module -eq 'TemplateState') { $qaSwitch = '--audit-template-state' }
    if ($Module -eq 'TeacherLifecycle') { $qaSwitch = '--audit-teacher-lifecycle' }
    if ($Module -eq 'ResourceScope') { $qaSwitch = '--audit-resource-scope' }
    if ($Module -eq 'CertificateFamily') { $qaSwitch = '--audit-certificate-family' }
    if ($Module -eq 'GuardianRevocation') { $qaSwitch = '--audit-guardian-revocation' }
    if ($Module -eq 'GuardianFlags') { $qaSwitch = '--audit-guardian-flags' }
    if ($Module -eq 'AnnouncementAudience') { $qaSwitch = '--audit-announcement-audience' }
    if ($Module -eq 'ActivityDeletion') { $qaSwitch = '--audit-activity-deletion' }
    if ($Module -eq 'PlatformBilling') { $qaSwitch = '--audit-platform-billing' }
    if ($Module -eq 'TenantPlan') { $qaSwitch = '--audit-tenant-plan' }
    if ($Module -eq 'TrialDuration') { $qaSwitch = '--audit-trial-duration' }
    if ($Module -eq 'PlatformProvisioning') { $qaSwitch = '--audit-platform-provisioning' }
    if ($Module -eq 'PortalProvisioning') { $qaSwitch = '--audit-portal-provisioning' }
    if ($Module -eq 'AcademyProvisioning') { $qaSwitch = '--audit-academy-provisioning' }
    if ($Module -eq 'ProvisioningConcurrency') { $qaSwitch = '--audit-provisioning-concurrency' }
    if ($Module -eq 'ProvisioningConflict') { $qaSwitch = '--audit-provisioning-conflict' }
    if ($Module -eq 'CollectionRace') { $qaSwitch = '--finance-collection-race' }
    if ($Module -eq 'AdjustmentRace') { $qaSwitch = '--finance-adjustment-race' }
    if ($Module -eq 'TransitionRace') { $qaSwitch = '--payment-transition-race' }
    if ($Module -eq 'ReconcileVoidRace') { $qaSwitch = '--payment-reconcile-void-race' }
    if ($Module -eq 'ApprovalRace') { $qaSwitch = '--finance-approval-race' }
    if ($Module -eq 'BrowserCertificate') { $qaSwitch = '--browser-certificate'; if (-not $env:QA_BROWSER_API_PORT) { $env:QA_BROWSER_API_PORT = '49202' }; if (-not $env:QA_BROWSER_ORIGIN) { $env:QA_BROWSER_ORIGIN = 'http://127.0.0.1:49201' } }
    if ($Module -eq 'BrowserPayments') { $qaSwitch = '--browser-payments'; if (-not $env:QA_BROWSER_API_PORT) { $env:QA_BROWSER_API_PORT = '49302' }; if (-not $env:QA_BROWSER_ORIGIN) { $env:QA_BROWSER_ORIGIN = 'http://127.0.0.1:49301' } }
    if ($Module -eq 'BrowserApproval') { $qaSwitch = '--browser-approval'; if (-not $env:QA_BROWSER_API_PORT) { $env:QA_BROWSER_API_PORT = '49402' }; if (-not $env:QA_BROWSER_ORIGIN) { $env:QA_BROWSER_ORIGIN = 'http://127.0.0.1:49401' } }
    if ($Module -eq 'BrowserSession') { $qaSwitch = '--browser-session' }
    if ($Module -eq 'BrowserProvisioning') { $qaSwitch = '--browser-provisioning' }
    if ($Module -eq 'RoleReplacement') { $qaSwitch = '--audit-role-replacement' }
    if ($Module -eq 'RoleRevocation') { $qaSwitch = '--audit-role-revocation' }
    if ($Module -eq 'SessionRevocationRepair') { $qaSwitch = '--audit-session-revocation-repair' }
    if ($Module -eq 'SessionRevocation') { $qaSwitch = '--audit-session-revocation' }
    if ($Module -eq 'SessionRefresh') { $qaSwitch = '--audit-session-refresh' }
    if ($Module -eq 'RefreshConcurrency') { $qaSwitch = '--audit-refresh-concurrency' }
    if ($Module -eq 'CertificateEnrollment') { $qaSwitch = '--audit-certificate-enrollment' }
    if ($Module -eq 'EnrollmentLifecycle') { $qaSwitch = '--audit-enrollment-lifecycle' }
    if ($Module -eq 'ComplianceIdentity') { $qaSwitch = '--audit-compliance-identity' }
    if ($Module -eq 'PracticeIdentity') { $qaSwitch = '--audit-practice-identity' }
    if ($Module -eq 'SubmissionIdentity') { $qaSwitch = '--audit-submission-identity' }
    if ($Module -eq 'ReviewContext') { $qaSwitch = '--audit-review-context' }
    if ($Module -eq 'MarketingConsent') { $qaSwitch = '--audit-marketing-consent' }
    if ($Module -eq 'InboxLifecycle') { $qaSwitch = '--audit-inbox-lifecycle' }
    if ($Module -eq 'NotificationChannel') { $qaSwitch = '--audit-notification-channel' }
    if ($Module -eq 'CommunicationRoundtrip') { $qaSwitch = '--audit-communication-roundtrip' }
    $env:QA_SQL_SA_PASSWORD = 'Qa!a9' + [Guid]::NewGuid().ToString('N')
    $env:MSSQL_SA_PASSWORD = $env:QA_SQL_SA_PASSWORD
    # This focused recovery window uses only the already-cached SQL image and
    # the same bounded SQL allocation as the newer image/native runners.
    $qaRecoverySqlLimits = @()
    if ($env:QA_ONBOARDING_START_RECOVERY_BROWSER -eq '1' -or $env:QA_TEACHER_CREATION_SERVICE -eq '1' -or $env:QA_ENROLLMENT_SHARED -eq '1') {
        $qaRecoverySqlLimits = @('--pull=never', '--memory', '2g', '--cpus', '2', '-e', 'MSSQL_MEMORY_LIMIT_MB=1536')
    }
    $qaId = docker run -d @qaRecoverySqlLimits --name $qaContainer --hostname $qaContainer --label "academydesk.qa.run=$qaRun" -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e MSSQL_SA_PASSWORD -p 127.0.0.1::1433 mcr.microsoft.com/mssql/server:2022-latest
    if ($LASTEXITCODE -ne 0) { throw 'Could not create isolated SQL container.' }
    Write-Output "QA $Module run=$qaRun container=$qaContainer started=$($qaStarted.ToString('o'))"
    $qaReady = $false
    for ($qaAttempt=0; $qaAttempt -lt 30; $qaAttempt++) {
        $qaLogs = docker logs $qaContainer 2>&1
        if ($qaLogs -match 'SQL Server is now ready for client connections') {
            # The first startup log can precede completion of SA provisioning.
            # Require an actual authenticated probe; never print the credential.
            $qaProbeErrorAction=$ErrorActionPreference
            try {
                $ErrorActionPreference='Continue'
                docker exec $qaContainer /bin/bash -c 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -Q "SELECT 1"' 2>&1 | Out-Null
                $qaProbeExit=$LASTEXITCODE
            } finally { $ErrorActionPreference=$qaProbeErrorAction }
            if ($qaProbeExit -eq 0) { $qaReady=$true; break }
        }
        Start-Sleep -Seconds 2
    }
    if (!$qaReady) { throw "Readiness failed; retained exact owned container $qaContainer" }
    $qaPort = [int]((docker port $qaContainer 1433/tcp).Trim().Split(':')[-1])
    Write-Output "QA SQL port=$qaPort"
    Push-Location -LiteralPath $qaRepo
    try {
        if ($Module -eq 'SessionRevocationRepair' -and $env:QA_IDENTITY_CHALLENGE_CHECK -eq '1') {
            $qaHarness = Join-Path $qaRepo '.build-check/identity-challenge-sql/bin/SqlHarness/debug/SqlHarness.dll'
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'Access' -and $env:QA_CSV_EXPORT_CHECK -eq '1') {
            $qaHarness = Join-Path $qaRepo '.build-check/csv-export-sql/bin/SqlHarness/debug/SqlHarness.dll'
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -in @('CoursePreservation','CoursePrerequisites','YearClosure','PromotionDecisions','AssessmentGrades','AssessmentRoster','AssessmentAccess','AssessmentOptions','AttendanceNotes','CalendarDetails','ScheduleDefaults','LeaveIdentity','MakeupLocation','RoleReplacement','RoleRevocation','SessionRevocationRepair','SessionRevocation','RefreshConcurrency','SessionRefresh','BrowserCertificate','BrowserSession','BrowserProvisioning','ProvisioningConflict','ProvisioningConcurrency','AcademyProvisioning','PortalProvisioning','PlatformProvisioning','TrialDuration','TenantPlan','PlatformBilling','ActivityDeletion','AnnouncementAudience','GuardianFlags','CertificateFamily','CertificateEnrollment','ComplianceIdentity','ResourceScope','PracticeIdentity','SubmissionIdentity','ReviewContext','MarketingConsent','TemplateState','PreferenceAccess','InboxLifecycle','NotificationChannel','CommunicationRoundtrip')) {
            $qaArtifact = switch ($Module) { 'YearClosure' { 'year-closure-sql' }; 'CoursePrerequisites' { 'prerequisite-sql' }; 'PromotionDecisions' { 'promotion-sql' }; 'AssessmentGrades' { 'assessment-grade-sql' }; 'AssessmentRoster' { 'assessment-roster-sql' }; 'AssessmentAccess' { 'assessment-access-sql' }; 'AssessmentOptions' { 'assessment-options-sql' }; 'AttendanceNotes' { 'attendance-notes-sql' }; 'CalendarDetails' { 'calendar-details-sql' }; 'ScheduleDefaults' { 'schedule-defaults-sql' }; 'LeaveIdentity' { 'leave-identity-sql' }; 'MakeupLocation' { 'makeup-location-sql' }; 'ProvisioningConflict' { 'provisioning-conflict-sql' }; 'ProvisioningConcurrency' { 'provisioning-concurrency-sql' }; 'AcademyProvisioning' { 'academy-provisioning-sql-final' }; 'PortalProvisioning' { 'portal-provisioning-sql-final' }; 'PlatformProvisioning' { 'platform-provisioning-sql' }; 'TrialDuration' { 'trial-duration-sql' }; 'TenantPlan' { 'tenant-plan-sql-baseline' }; 'PlatformBilling' { 'platform-billing-sql-baseline' }; 'ActivityDeletion' { 'activity-deletion-sql-baseline' }; 'AnnouncementAudience' { 'announcement-audience-sql-baseline' }; 'GuardianFlags' { 'guardian-flags-sql-baseline' }; 'CertificateFamily' { 'certificate-family-sql' }; 'CertificateEnrollment' { 'certificate-enrollment-sql-final' }; 'ComplianceIdentity' { 'compliance-identity-sql-final' }; 'ResourceScope' { 'resource-scope-sql' }; 'PracticeIdentity' { 'practice-identity-sql' }; 'SubmissionIdentity' { 'submission-identity-sql' }; 'ReviewContext' { 'review-context-sql' }; 'MarketingConsent' { 'marketing-consent-sql' }; 'TemplateState' { 'template-state-sql' }; 'PreferenceAccess' { 'preference-access-sql' }; 'InboxLifecycle' { 'inbox-lifecycle-sql' }; 'NotificationChannel' { 'notification-channel-sql' }; 'CommunicationRoundtrip' { 'communication-roundtrip-sql' }; default { 'course-sql' } }
            if ($Module -eq 'BrowserCertificate') { $qaArtifact = 'certificate-portal-sql' }
            if ($Module -eq 'BrowserSession') { $qaArtifact = 'session-browser-sql' }
            if ($Module -eq 'BrowserProvisioning') { $qaArtifact = 'provisioning-ui-sql' }
            if ($Module -eq 'RoleReplacement') { $qaArtifact = 'role-replacement-sql' }
            if ($Module -eq 'RoleRevocation') { $qaArtifact = 'role-revocation-sql' }
            if ($Module -eq 'SessionRevocationRepair') { $qaArtifact = 'session-revocation-repair-sql' }
            if ($Module -eq 'SessionRevocation') { $qaArtifact = 'session-revocation-sql' }
            if ($Module -eq 'SessionRefresh') { $qaArtifact = 'session-refresh-sql' }
            if ($Module -eq 'RefreshConcurrency') { $qaArtifact = 'refresh-concurrency-sql' }
            if ($Module -eq 'ProvisioningConflict') { $qaArtifact = if ($env:QA_PROVISIONING_CONFLICT_BASELINE -eq '1') { 'provisioning-conflict-sql-classified' } else { 'provisioning-conflict-sql-final' } }
            if ($Module -eq 'CertificateFamily' -and $env:QA_CERTIFICATE_FAMILY_BASELINE -ne '1') { $qaArtifact = 'certificate-family-sql-final' }
            if ($Module -eq 'GuardianFlags' -and $env:QA_GUARDIAN_FLAGS_BASELINE -ne '1') { $qaArtifact = 'guardian-flags-sql-final' }
            if ($Module -eq 'AnnouncementAudience' -and $env:QA_ANNOUNCEMENT_AUDIENCE_BASELINE -ne '1') { $qaArtifact = 'announcement-audience-sql-final' }
            if ($Module -eq 'ActivityDeletion' -and $env:QA_ACTIVITY_DELETION_BASELINE -ne '1') { $qaArtifact = 'activity-deletion-sql-final' }
            if ($Module -eq 'PlatformBilling' -and $env:QA_PLATFORM_BILLING_BASELINE -ne '1') { $qaArtifact = 'platform-billing-sql-final' }
            if ($Module -eq 'TenantPlan' -and $env:QA_TENANT_PLAN_BASELINE -ne '1') { $qaArtifact = 'tenant-plan-sql-final' }
            $qaHarness = Join-Path $qaRepo ".build-check/$qaArtifact/bin/SqlHarness/debug/SqlHarness.dll"
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the isolated Course SQL harness first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'Consumers' -and $env:QA_INVOICE_CONSUMER_BROWSER -eq '1') {
            $qaHarness = Join-Path $qaRepo '.build-check/invoice-consumers-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build Consumers with --artifacts-path .build-check/invoice-consumers-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'Payroll' -and $env:QA_PAYROLL_HISTORY -in @('baseline','guarded')) {
            $qaPayrollArtifact = if ($env:QA_BATCH1_FINANCE -eq '1') { '.build-check/batch1-retained-finance/bin/SqlHarness/debug/SqlHarness.dll' } else { '.build-check/payroll-history-sql/bin/SqlHarness/debug/SqlHarness.dll' }
            $qaHarness = Join-Path $qaRepo $qaPayrollArtifact
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build Payroll history with --artifacts-path .build-check/payroll-history-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'InvoiceIntegrity') {
            $qaHarness = Join-Path $qaRepo '.build-check/invoice-integrity-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build InvoiceIntegrity with --artifacts-path .build-check/invoice-integrity-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -in @('InvoiceStatus','InvoiceStatusBaseline')) {
            $qaArtifact = if ($Module -eq 'InvoiceStatusBaseline') { 'invoice-status-baseline' } else { 'invoice-status-sql' }
            $qaHarness = Join-Path $qaRepo ".build-check/$qaArtifact/bin/SqlHarness/debug/SqlHarness.dll"
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the selected isolated InvoiceStatus harness first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'MusicProgress') {
            $qaHarness = Join-Path $qaRepo '.build-check/music-progress-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build MusicProgress with --artifacts-path .build-check/music-progress-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'FinancePolicyTheme') {
            $qaHarness = Join-Path $qaRepo '.build-check/finance-policy-theme-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build FinancePolicyTheme with --artifacts-path .build-check/finance-policy-theme-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'TeacherPublication') {
            $qaHarness = Join-Path $qaRepo '.build-check/teacher-publication-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build TeacherPublication with --artifacts-path .build-check/teacher-publication-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'TeacherCompensation') {
            $qaHarness = Join-Path $qaRepo '.build-check/teacher-compensation-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build TeacherCompensation with --artifacts-path .build-check/teacher-compensation-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -in @('StudentImports','StudentImportsBaseline')) {
            $qaHarness = Join-Path $qaRepo '.build-check/student-import-integrity-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build StudentImports with --artifacts-path .build-check/student-import-integrity-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'WorkItemIntegrity') {
            $qaHarness = Join-Path $qaRepo '.build-check/work-item-integrity-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build WorkItemIntegrity with --artifacts-path .build-check/work-item-integrity-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'ExpenseIntegrity') {
            $qaHarness = Join-Path $qaRepo '.build-check/expense-integrity-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build ExpenseIntegrity with --artifacts-path .build-check/expense-integrity-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'MetadataShape') {
            $qaHarness = Join-Path $qaRepo '.build-check/metadata-shape-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build MetadataShape with --artifacts-path .build-check/metadata-shape-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'HolidayRoute') {
            $qaHarness = Join-Path $qaRepo '.build-check/holiday-route-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build HolidayRoute with --artifacts-path .build-check/holiday-route-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'TeacherLifecycle' -and $env:QA_LIFECYCLE_BROWSER -eq '1') {
            $qaHarness = Join-Path $qaRepo '.build-check/lifecycle-browser-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build lifecycle browser harness first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -in @('GuardianRevocation','TeacherLifecycle') -and $env:QA_LIFECYCLE_OVERLAP -eq '1') {
            $qaHarness = Join-Path $qaRepo '.build-check/lifecycle-overlap-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the lifecycle overlap harness first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'GuardianRevocation') {
            $qaHarness = Join-Path $qaRepo '.build-check/guardian-revocation-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the GuardianRevocation harness with --artifacts-path .build-check/guardian-revocation-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -in @('OnboardingAtomic','OnboardingAccounts','OnboardingNumbers')) {
            $qaHarness = Join-Path $qaRepo '.build-check/onboarding-atomic-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the isolated onboarding atomic harness first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'OnboardingDates') {
            $qaHarness = Join-Path $qaRepo '.build-check/onboarding-dates-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the isolated onboarding dates harness first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'TeacherAvailability') {
            $qaHarness = Join-Path $qaRepo '.build-check/teacher-availability-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the isolated teacher availability harness first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'EnrollmentLifecycle') {
            $qaHarness = Join-Path $qaRepo '.build-check/enrollment-lifecycle-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the isolated enrollment lifecycle harness first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'TeacherLifecycle') {
            $qaHarness = Join-Path $qaRepo '.build-check/teacher-lifecycle-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the TeacherLifecycle harness with --artifacts-path .build-check/teacher-lifecycle-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'PentaFoundation') {
            $qaHarness = Join-Path $qaRepo '.build-check/penta-mini-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the isolated PENTA harness with --artifacts-path .build-check/penta-mini-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'MeetingReadiness') {
            $qaHarness = Join-Path $qaRepo '.build-check/meeting-readiness-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build MeetingReadiness with --artifacts-path .build-check/meeting-readiness-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } else { $qaCommand = @('run', '--project', 'QA/tools/SqlHarness/SqlHarness.csproj', '--no-build', '--', $qaRun, $qaPort, $qaContainer, $qaSwitch) }
        # Windows PowerShell must not turn native stderr into a terminating
        # error before the regression's exit code and stack trace are captured.
        $qaPriorErrorAction = $ErrorActionPreference
        try {
        $ErrorActionPreference = 'Continue'
        $qaUnhandled = $false
        $qaHarnessCompleted = $false
        dotnet @qaCommand 2>&1 | ForEach-Object {
            $qaLine=$_.ToString()
            if ($qaLine -match '^REFRESHLOCK') { Write-Output $qaLine; return }
            if ($qaLine -match '^REVOCATION') { Write-Output $qaLine; return }
            if ($qaLine -match '^IDENTITYCHALLENGE') { Write-Output $qaLine; return }
            if ($qaLine -match '^Unhandled exception') { $qaUnhandled = $true }
            if ($qaLine -match '^PASS: application migrations=\d+, identity migrations=\d+, scoped runtime login verified; run-owned database and login removed\.$') { $qaHarnessCompleted = $true }
            if ($qaLine -match '^(MEETING|GUARDIANREVOKE|TEACHERLIFE|PENTA|GRADE|ROSTER|ACADEMICACCESS|ACADEMICOPTIONS|ATTENDANCENOTES|CALENDARDETAILS|SCHEDULEDEFAULTS|LEAVEIDENTITY|MAKEUPLOCATION|CHANNELROUNDTRIP|CHANNELCHOICE|INBOXLIFE|CONSENTACCESS|TEMPLATESTATE|MARKETING|REVIEWCONTEXT|SUBMISSIONIDENTITY|PRACTICEIDENTITY|RESOURCESCOPE|COMPLIANCE|CERTIFICATE|CERTFAMILY|GUARDIANFLAGS|ANNOUNCEMENTAUDIENCE|ACTIVITYDELETE|PLATFORMBILLING|TENANTPLAN|TRIALDURATION|PLATFORMPROVISION|PORTALPROVISION|ACADEMYPROVISION|PROVISIONRACE|PROVISIONCONFLICT|ROLEREPLACE|ROLESESSION|PROVISIONUI|CERTPORTAL|PAYPORTAL|SESSIONBROWSER|SESSIONREVOCATION|SESSIONREFRESH|REFRESHFLIGHT)') { Write-Output $qaLine; return }
            if ($qaLine -match '^(SECURITY-FILE|CLASSMATERIAL|Runtime inventory|Created run-owned|HTTP controls|Tenant controls|FINANCE|ADJUSTMENT|PAYROLL|BATCH1|TRANSITION|CONSUMER|ACCESS|AUDIT|PEOPLE|LINKED|BRANCH|BATCHPRESERVE|BATCHTIMES|COURSEPRESERVE|PREREQUISITE|YEARCLOSURE|PROMOTION|LOOKUP|GOVERNANCE|COLLECTIONS|INVOICESETTINGS|Negative cleanup|PASS:|Unhandled exception)' -or ($qaUnhandled -and $qaLine -match '^\s+at SqlHarnessEntryPoint\.') -or $_ -is [System.Management.Automation.ErrorRecord]) { Write-Output $qaLine }
        }
        $qaExit=$LASTEXITCODE
        } finally { $ErrorActionPreference = $qaPriorErrorAction }
    } finally { Pop-Location }
    Write-Output "QA $Module exit=$qaExit elapsedSeconds=$([Math]::Round(([DateTime]::UtcNow-$qaStarted).TotalSeconds,1))"
    if ($qaExit -ne 0) { throw "Regression failed; retained exact owned container $qaContainer" }
    if (!$qaHarnessCompleted) { throw "Harness did not confirm final database/login teardown; retained exact owned container $qaContainer" }
    $qaInspect=docker inspect $qaContainer | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $qaInspect[0].Name -ne "/$qaContainer" -or $qaInspect[0].Config.Labels.'academydesk.qa.run' -ne $qaRun -or $qaInspect[0].NetworkSettings.Ports.'1433/tcp'[0].HostIp -ne '127.0.0.1') { throw 'Refused container cleanup: ownership mismatch.' }
    docker stop $qaContainer
    if ($LASTEXITCODE -ne 0) { throw 'Owned container stop failed.' }
    docker rm $qaContainer
    if ($LASTEXITCODE -ne 0) { throw 'Owned container removal failed.' }
} finally {
    $env:QA_SQL_SA_PASSWORD=$qaPreviousSa
    $env:MSSQL_SA_PASSWORD=$qaPreviousMssql
    $env:QA_INVOICE_STATUS_BASELINE=$qaPreviousInvoiceBaseline
}
