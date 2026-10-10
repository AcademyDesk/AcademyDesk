# Fresh local owned SQL only. Real auth/API and strict selected finance regression.
param([ValidateSet('GuardianRevocation','TeacherLifecycle','PentaFoundation','Reconciliation','Adjustment','Payroll','Transition','TransitionRace','ReconcileVoidRace','ApprovalRace','Consumers','Access','AuditBaseline','AuditFixed','PeopleBaseline','PeopleFixed','LinkedBaseline','LinkedFixed','BranchBaseline','BranchFixed','Lookups','Governance','CollectionsBaseline','Collections','InvoiceSettingsBaseline','InvoiceSettings','BranchAddressBaseline','BranchAddress','BatchTimesBaseline','BatchTimes','YearClosure','CoursePrerequisites','CoursePreservation','PromotionDecisions','AssessmentGrades','AssessmentRoster','AssessmentAccess','AssessmentOptions','AttendanceNotes','CalendarDetails','ScheduleDefaults','LeaveIdentity','MakeupLocation','RoleReplacement','RoleRevocation','SessionRevocationRepair','SessionRevocation','RefreshConcurrency','SessionRefresh','BrowserPayments','BrowserApproval','BrowserCertificate','BrowserSession','BrowserProvisioning','CollectionRace','AdjustmentRace','ProvisioningConflict','ProvisioningConcurrency','AcademyProvisioning','PortalProvisioning','PlatformProvisioning','TrialDuration','TenantPlan','PlatformBilling','ActivityDeletion','AnnouncementAudience','GuardianFlags','CertificateFamily','CertificateEnrollment','ComplianceIdentity','ResourceScope','PracticeIdentity','SubmissionIdentity','ReviewContext','MarketingConsent','TemplateState','PreferenceAccess','InboxLifecycle','NotificationChannel','CommunicationRoundtrip','BatchPreservationBaseline','BatchPreservation')][string]$Module = 'Reconciliation')
$ErrorActionPreference = 'Stop'
$qaRepo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
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
$qaStarted = [DateTime]::UtcNow
$qaSwitch = switch ($Module) { 'YearClosure' { '--audit-year-closure' }; 'CoursePrerequisites' { '--audit-course-prerequisites' }; 'CoursePreservation' { '--audit-course-preservation' }; 'PromotionDecisions' { '--audit-promotion-decisions' }; 'Adjustment' { '--finance-adjustment' }; 'Payroll' { '--payroll-net' }; 'Transition' { '--payment-transition' }; 'Consumers' { '--finance-consumers' }; 'Access' { '--finance-access' }; 'Lookups' { '--finance-lookups' }; 'Governance' { '--finance-governance' }; 'CollectionsBaseline' { '--collections-baseline' }; 'Collections' { '--collections-balance' }; 'InvoiceSettingsBaseline' { '--invoice-settings-baseline' }; 'InvoiceSettings' { '--invoice-settings' }; 'BatchTimesBaseline' { '--audit-batch-times-baseline' }; 'BatchTimes' { '--audit-batch-times-fixed' }; 'BatchPreservationBaseline' { '--audit-batch-preservation-baseline' }; 'BatchPreservation' { '--audit-batch-preservation-fixed' }; 'BranchAddressBaseline' { '--audit-branch-address-baseline' }; 'BranchAddress' { '--audit-branch-address-fixed' }; 'AuditBaseline' { '--audit-baseline' }; 'AuditFixed' { '--audit-fixed' }; 'PeopleBaseline' { '--audit-people-baseline' }; 'PeopleFixed' { '--audit-people-fixed' }; 'LinkedBaseline' { '--audit-linked-baseline' }; 'LinkedFixed' { '--audit-linked-fixed' }; 'BranchBaseline' { '--audit-branch-baseline' }; 'BranchFixed' { '--audit-branch-fixed' }; default { '--finance-reconciliation' } }
try {
    if ($Module -eq 'AssessmentGrades') { $qaSwitch = '--audit-assessment-grades' }
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
    $qaId = docker run -d --name $qaContainer --hostname $qaContainer --label "academydesk.qa.run=$qaRun" -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e MSSQL_SA_PASSWORD -p 127.0.0.1::1433 mcr.microsoft.com/mssql/server:2022-latest
    if ($LASTEXITCODE -ne 0) { throw 'Could not create isolated SQL container.' }
    Write-Output "QA $Module run=$qaRun container=$qaContainer started=$($qaStarted.ToString('o'))"
    $qaReady = $false
    for ($qaAttempt=0; $qaAttempt -lt 30; $qaAttempt++) {
        $qaLogs = docker logs $qaContainer 2>&1
        if ($qaLogs -match 'SQL Server is now ready for client connections') { $qaReady=$true; break }
        Start-Sleep -Seconds 2
    }
    if (!$qaReady) { throw "Readiness failed; retained exact owned container $qaContainer" }
    $qaPort = [int]((docker port $qaContainer 1433/tcp).Trim().Split(':')[-1])
    Write-Output "QA SQL port=$qaPort"
    Push-Location -LiteralPath $qaRepo
    try {
        if ($Module -in @('CoursePreservation','CoursePrerequisites','YearClosure','PromotionDecisions','AssessmentGrades','AssessmentRoster','AssessmentAccess','AssessmentOptions','AttendanceNotes','CalendarDetails','ScheduleDefaults','LeaveIdentity','MakeupLocation','RoleReplacement','RoleRevocation','SessionRevocationRepair','SessionRevocation','RefreshConcurrency','SessionRefresh','BrowserCertificate','BrowserSession','BrowserProvisioning','ProvisioningConflict','ProvisioningConcurrency','AcademyProvisioning','PortalProvisioning','PlatformProvisioning','TrialDuration','TenantPlan','PlatformBilling','ActivityDeletion','AnnouncementAudience','GuardianFlags','CertificateFamily','CertificateEnrollment','ComplianceIdentity','ResourceScope','PracticeIdentity','SubmissionIdentity','ReviewContext','MarketingConsent','TemplateState','PreferenceAccess','InboxLifecycle','NotificationChannel','CommunicationRoundtrip')) {
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
        } elseif ($Module -eq 'GuardianRevocation') {
            $qaHarness = Join-Path $qaRepo '.build-check/guardian-revocation-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the GuardianRevocation harness with --artifacts-path .build-check/guardian-revocation-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'TeacherLifecycle') {
            $qaHarness = Join-Path $qaRepo '.build-check/teacher-lifecycle-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the TeacherLifecycle harness with --artifacts-path .build-check/teacher-lifecycle-sql first.' }
            $qaCommand = @($qaHarness, $qaRun, $qaPort, $qaContainer, $qaSwitch)
        } elseif ($Module -eq 'PentaFoundation') {
            $qaHarness = Join-Path $qaRepo '.build-check/penta-mini-sql/bin/SqlHarness/debug/SqlHarness.dll'
            if (!(Test-Path -LiteralPath $qaHarness)) { throw 'Build the isolated PENTA harness with --artifacts-path .build-check/penta-mini-sql first.' }
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
            if ($qaLine -match '^Unhandled exception') { $qaUnhandled = $true }
            if ($qaLine -match '^PASS: application migrations=\d+, identity migrations=\d+, scoped runtime login verified; run-owned database and login removed\.$') { $qaHarnessCompleted = $true }
            if ($qaLine -match '^(GUARDIANREVOKE|TEACHERLIFE|PENTA|GRADE|ROSTER|ACADEMICACCESS|ACADEMICOPTIONS|ATTENDANCENOTES|CALENDARDETAILS|SCHEDULEDEFAULTS|LEAVEIDENTITY|MAKEUPLOCATION|CHANNELROUNDTRIP|CHANNELCHOICE|INBOXLIFE|CONSENTACCESS|TEMPLATESTATE|MARKETING|REVIEWCONTEXT|SUBMISSIONIDENTITY|PRACTICEIDENTITY|RESOURCESCOPE|COMPLIANCE|CERTIFICATE|CERTFAMILY|GUARDIANFLAGS|ANNOUNCEMENTAUDIENCE|ACTIVITYDELETE|PLATFORMBILLING|TENANTPLAN|TRIALDURATION|PLATFORMPROVISION|PORTALPROVISION|ACADEMYPROVISION|PROVISIONRACE|PROVISIONCONFLICT|ROLEREPLACE|ROLESESSION|PROVISIONUI|CERTPORTAL|PAYPORTAL|SESSIONBROWSER|SESSIONREVOCATION|SESSIONREFRESH|REFRESHFLIGHT)') { Write-Output $qaLine; return }
            if ($qaLine -match '^(SECURITY-FILE|CLASSMATERIAL|Runtime inventory|Created run-owned|HTTP controls|Tenant controls|FINANCE|ADJUSTMENT|PAYROLL|TRANSITION|CONSUMER|ACCESS|AUDIT|PEOPLE|LINKED|BRANCH|BATCHPRESERVE|BATCHTIMES|COURSEPRESERVE|PREREQUISITE|YEARCLOSURE|PROMOTION|LOOKUP|GOVERNANCE|COLLECTIONS|INVOICESETTINGS|Negative cleanup|PASS:|Unhandled exception)' -or ($qaUnhandled -and $qaLine -match '^\s+at SqlHarnessEntryPoint\.') -or $_ -is [System.Management.Automation.ErrorRecord]) { Write-Output $qaLine }
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
}
