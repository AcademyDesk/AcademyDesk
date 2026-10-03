using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
private static async Task Main(string[] args)
{
if ((args.Length != 3 && (args.Length != 4 || (args[3] != "--payment-reconcile-void-race" && args[3] != "--payment-transition-race" && args[3] != "--finance-adjustment-race" && args[3] != "--audit-role-replacement" && args[3] != "--audit-role-revocation" && args[3] != "--audit-session-revocation-repair" && args[3] != "--audit-session-revocation" && args[3] != "--audit-refresh-concurrency" && args[3] != "--audit-session-refresh" && args[3] != "--browser-payments" && args[3] != "--browser-certificate" && args[3] != "--browser-session" && args[3] != "--browser-provisioning" && args[3] != "--audit-provisioning-conflict" && args[3] != "--audit-provisioning-concurrency" && args[3] != "--audit-academy-provisioning" && args[3] != "--audit-portal-provisioning" && args[3] != "--audit-platform-provisioning" && args[3] != "--audit-trial-duration" && args[3] != "--audit-tenant-plan" && args[3] != "--audit-platform-billing" && args[3] != "--audit-activity-deletion" && args[3] != "--audit-announcement-audience" && args[3] != "--audit-guardian-flags" && args[3] != "--audit-certificate-family" && args[3] != "--audit-certificate-enrollment" && args[3] != "--audit-compliance-identity" && args[3] != "--audit-resource-scope" && args[3] != "--audit-practice-identity" && args[3] != "--audit-submission-identity" && args[3] != "--audit-review-context" && args[3] != "--audit-marketing-consent" && args[3] != "--audit-template-state" && args[3] != "--audit-preference-access" && args[3] != "--audit-inbox-lifecycle" && args[3] != "--audit-notification-channel" && args[3] != "--audit-communication-roundtrip" && args[3] != "--audit-makeup-location" && args[3] != "--audit-leave-identity" && args[3] != "--audit-schedule-defaults" && args[3] != "--audit-calendar-details" && args[3] != "--audit-attendance-notes" && args[3] != "--audit-assessment-options" && args[3] != "--audit-assessment-access" && args[3] != "--audit-assessment-roster" && args[3] != "--audit-assessment-grades" && args[3] != "--audit-year-closure" && args[3] != "--audit-course-prerequisites" && args[3] != "--audit-course-preservation" && args[3] != "--audit-promotion-decisions" && args[3] != "--class-media" && args[3] != "--blob-media" && args[3] != "--browser-media" && args[3] != "--finance-collection-race" && args[3] != "--finance-reconciliation" && args[3] != "--finance-adjustment" && args[3] != "--payroll-net" && args[3] != "--payment-transition" && args[3] != "--finance-consumers" && args[3] != "--finance-access" && args[3] != "--finance-lookups" && args[3] != "--finance-governance" && args[3] != "--collections-baseline" && args[3] != "--collections-balance" && args[3] != "--invoice-settings-baseline" && args[3] != "--invoice-settings" && args[3] != "--audit-batch-times-baseline" && args[3] != "--audit-batch-times-fixed" && args[3] != "--audit-batch-preservation-baseline" && args[3] != "--audit-batch-preservation-fixed" && args[3] != "--audit-branch-address-baseline" && args[3] != "--audit-branch-address-fixed" && args[3] != "--audit-baseline" && args[3] != "--audit-fixed" && args[3] != "--audit-people-baseline" && args[3] != "--audit-people-fixed" && args[3] != "--audit-linked-baseline" && args[3] != "--audit-linked-fixed" && args[3] != "--audit-branch-baseline" && args[3] != "--audit-branch-fixed"))) || !Guid.TryParseExact(args[0], "N", out var runId) ||
    !int.TryParse(args[1], out var port) || string.IsNullOrWhiteSpace(args[2]))
    throw new InvalidOperationException("Supply the QA run ID, Docker host port, and exact container name.");

var saPassword = Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD");
if (string.IsNullOrWhiteSpace(saPassword))
    throw new InvalidOperationException("The ephemeral QA SQL administrator credential is required.");

var manifest = QaRunManifest.CreateForDocker(runId, port);
var containerName = $"academydesk-qa-{runId:N}";
if (!string.Equals(args[2], containerName, StringComparison.Ordinal))
    throw new InvalidOperationException("The QA container name does not match the run ID.");

await VerifyDockerTargetAsync(containerName, manifest);
var blobFixture = args.Length == 4 && (args[3] == "--blob-media" || args[3] == "--browser-media") ? await QaBlobFixture.CreateAsync(runId) : null;
var runtimeConnection = Connection(manifest.SqlServer, manifest.Database, manifest.RuntimeLogin, manifest.RuntimePassword);
QaRunGuard.Validate(manifest, new QaHostConfiguration(
    "Testing", manifest.Token, runtimeConnection, runtimeConnection,
    manifest.ContentRoot, manifest.WebRoot, manifest.StorageRoot, manifest.DataProtectionRoot,
    false, null, null));

var masterConnection = Connection(manifest.SqlServer, "master", "sa", saPassword);
var adminDatabaseConnection = Connection(manifest.SqlServer, manifest.Database, "sa", saPassword);
await using (var master = new SqlConnection(masterConnection))
{
    await master.OpenAsync();
    var serverName = (string?)await ScalarAsync(master, "SELECT @@SERVERNAME");
    if (!string.Equals(serverName, containerName, StringComparison.Ordinal))
        throw new InvalidOperationException("SQL Server identity does not match the QA container.");
    if (await DatabaseExistsAsync(master, manifest.Database))
        throw new InvalidOperationException("The generated QA database already exists; refusing to reuse it.");
    await ExecuteAsync(master, $"CREATE DATABASE [{manifest.Database}]");
}

Console.WriteLine($"Created run-owned database {manifest.Database} on {manifest.SqlServer}.");
// If setup fails before the ownership marker is written, leave the isolated
// container for investigation. An unmarked database is never auto-dropped.
await using (var db = new SqlConnection(adminDatabaseConnection))
{
    await db.OpenAsync();
    await ExecuteAsync(db, "CREATE TABLE dbo.QaRunOwnership (RunId uniqueidentifier NOT NULL PRIMARY KEY, TargetAddress varchar(64) NOT NULL, TokenDigest char(64) NOT NULL, CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME())");
    await using var markerCommand = new SqlCommand("INSERT INTO dbo.QaRunOwnership (RunId, TargetAddress, TokenDigest) VALUES (@runId, @target, @digest)", db);
    markerCommand.Parameters.AddWithValue("@runId", manifest.RunId);
    markerCommand.Parameters.AddWithValue("@target", manifest.SqlServer);
    markerCommand.Parameters.AddWithValue("@digest", manifest.TokenDigest);
    await markerCommand.ExecuteNonQueryAsync();
}

await using (var master = new SqlConnection(masterConnection))
{
    await master.OpenAsync();
    // These identifiers and the password are generated in-process with fixed safe
    // alphabets. The SQL text and secrets are never printed or stored in a report.
    await ExecuteAsync(master, $"CREATE LOGIN [{manifest.RuntimeLogin}] WITH PASSWORD = '{manifest.RuntimePassword}', CHECK_POLICY = ON, DEFAULT_DATABASE = [{manifest.Database}]");
}
await using (var db = new SqlConnection(adminDatabaseConnection))
{
    await db.OpenAsync();
    await ExecuteAsync(db, $"CREATE USER [{manifest.RuntimeLogin}] FOR LOGIN [{manifest.RuntimeLogin}]");
    await ExecuteAsync(db, $"ALTER ROLE db_datareader ADD MEMBER [{manifest.RuntimeLogin}]");
    await ExecuteAsync(db, $"ALTER ROLE db_datawriter ADD MEMBER [{manifest.RuntimeLogin}]");
}

Console.WriteLine("Created database-scoped runtime login; applying both EF migration sets.");
await using (var application = new AcademyDeskDbContext(
    new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(adminDatabaseConnection).Options))
{
    if (args.Length == 4 && args[3] == "--audit-assessment-grades")
        await VerifyAssessmentGradeMigrationAsync(application);
    else if (args.Length == 4 && args[3] == "--audit-inbox-lifecycle" && Environment.GetEnvironmentVariable("QA_INBOX_BASELINE") != "1")
        await VerifyInboxReceiptMigrationAsync(application);
    else await application.Database.MigrateAsync();
}
await using (var identity = new IdentityDbContext(
    new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(adminDatabaseConnection).Options))
    await identity.Database.MigrateAsync();

var (applicationMigrations, identityMigrations) = await VerifyMigrationsAsync(adminDatabaseConnection);
await VerifyRuntimePrivilegesAsync(runtimeConnection, manifest);
if (args.Length == 4 && args[3] == "--browser-payments")
    await ServeBrowserPaymentsAsync(manifest);
else if (args.Length == 4 && args[3] == "--browser-certificate")
    await ServeBrowserCertificateAsync(manifest);
else if (args.Length == 4 && args[3] == "--browser-session")
    await ServeBrowserSessionAsync(manifest);
else if (args.Length == 4 && args[3] == "--browser-provisioning")
    await ServeBrowserProvisioningAsync(manifest);
else if (args.Length == 4 && args[3] == "--browser-media")
    await ServeBrowserMediaAsync(manifest, blobFixture ?? throw new InvalidOperationException("Private emulator required."));
else
    await VerifyRealHttpAsync(manifest, classMediaOnly: args.Length == 4, blobFixture, financeReconciliationOnly: args.Length == 4 && args[3] == "--finance-reconciliation", financeAdjustmentOnly: args.Length == 4 && args[3] == "--finance-adjustment", payrollNetOnly: args.Length == 4 && args[3] == "--payroll-net", paymentTransitionOnly: args.Length == 4 && args[3] == "--payment-transition", financeConsumersOnly: args.Length == 4 && args[3] == "--finance-consumers", financeAccessOnly: args.Length == 4 && args[3] == "--finance-access", financeLookupsOnly: args.Length == 4 && args[3] == "--finance-lookups", financeGovernanceOnly: args.Length == 4 && args[3] == "--finance-governance", collectionsMode: args.Length == 4 && args[3].StartsWith("--collections-", StringComparison.Ordinal) ? args[3] : null, invoiceSettingsMode: args.Length == 4 && args[3].StartsWith("--invoice-settings", StringComparison.Ordinal) ? args[3] : null, auditMode: args.Length == 4 && args[3].StartsWith("--audit-", StringComparison.Ordinal) ? args[3] : null, financeCollectionRaceOnly: args.Length == 4 && args[3] == "--finance-collection-race", financeAdjustmentRaceOnly: args.Length == 4 && args[3] == "--finance-adjustment-race", paymentTransitionRaceOnly: args.Length == 4 && args[3] == "--payment-transition-race", paymentReconcileVoidRaceOnly: args.Length == 4 && args[3] == "--payment-reconcile-void-race");
if (blobFixture is not null) await blobFixture.CleanupAsync();
QaOwnershipMarker marker;
await using (var db = new SqlConnection(adminDatabaseConnection))
{
    await db.OpenAsync();
    marker = await ReadMarkerAsync(db, manifest);
}

var rejectedCleanupCalls = 0;
var rejectedBadMarker = false;
try
{
    QaRunGuard.Cleanup(manifest, marker with { TokenDigest = new string('0', 64) }, _ => { rejectedCleanupCalls++; return true; });
}
catch (InvalidOperationException)
{
    rejectedBadMarker = true;
}
if (!rejectedBadMarker || rejectedCleanupCalls != 0)
    throw new InvalidOperationException("A false ownership marker authorized cleanup.");
Console.WriteLine("Negative cleanup check refused a mismatched database marker.");

SqlConnection.ClearAllPools();
await QaRunGuard.Cleanup(manifest, marker, async _ =>
{
    await using var master = new SqlConnection(masterConnection);
    await master.OpenAsync();
    await ExecuteAsync(master, $"ALTER DATABASE [{manifest.Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
    await ExecuteAsync(master, $"DROP DATABASE [{manifest.Database}]");
    await ExecuteAsync(master, $"DROP LOGIN [{manifest.RuntimeLogin}]");
    if (await DatabaseExistsAsync(master, manifest.Database))
        throw new InvalidOperationException("QA database still exists after cleanup.");
    await using var loginCheck = new SqlCommand("SELECT SUSER_ID(@name)", master);
    loginCheck.Parameters.AddWithValue("@name", manifest.RuntimeLogin);
    var remainingLogin = await loginCheck.ExecuteScalarAsync();
    if (remainingLogin is not null && remainingLogin is not DBNull)
        throw new InvalidOperationException("QA runtime login still exists after cleanup.");
    return true;
});

Console.WriteLine($"PASS: application migrations={applicationMigrations}, identity migrations={identityMigrations}, scoped runtime login verified; run-owned database and login removed.");
}

static string Connection(string server, string database, string login, string password) => new SqlConnectionStringBuilder
{
    DataSource = server, InitialCatalog = database, UserID = login, Password = password,
    IntegratedSecurity = false, Encrypt = true, TrustServerCertificate = true,
    ConnectTimeout = 5
}.ConnectionString;

static async Task VerifyDockerTargetAsync(string containerName, QaRunManifest manifest)
{
    using var process = new Process { StartInfo = new ProcessStartInfo("docker")
    {
        RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
    } };
    process.StartInfo.ArgumentList.Add("inspect");
    process.StartInfo.ArgumentList.Add(containerName);
    process.Start();
    var json = await process.StandardOutput.ReadToEndAsync();
    _ = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    if (process.ExitCode != 0) throw new InvalidOperationException("QA Docker container was not found.");
    using var document = JsonDocument.Parse(json);
    var item = document.RootElement[0];
    var config = item.GetProperty("Config");
    var bindings = item.GetProperty("NetworkSettings").GetProperty("Ports").GetProperty("1433/tcp");
    if (item.GetProperty("State").GetProperty("Running").GetBoolean() != true ||
        !string.Equals(item.GetProperty("Name").GetString(), "/" + containerName, StringComparison.Ordinal) ||
        !string.Equals(config.GetProperty("Hostname").GetString(), containerName, StringComparison.Ordinal) ||
        !string.Equals(config.GetProperty("Image").GetString(), "mcr.microsoft.com/mssql/server:2022-latest", StringComparison.Ordinal) ||
        !string.Equals(config.GetProperty("Labels").GetProperty("academydesk.qa.run").GetString(), manifest.RunId.ToString("N"), StringComparison.Ordinal) ||
        bindings.GetArrayLength() != 1 ||
        !string.Equals(bindings[0].GetProperty("HostIp").GetString(), "127.0.0.1", StringComparison.Ordinal) ||
        !string.Equals(bindings[0].GetProperty("HostPort").GetString(), manifest.SqlPort.ToString(), StringComparison.Ordinal))
        throw new InvalidOperationException("Docker identity or loopback port does not match the QA run.");
}

static async Task<bool> DatabaseExistsAsync(SqlConnection master, string name)
{
    await using var command = new SqlCommand("SELECT DB_ID(@name)", master);
    command.Parameters.AddWithValue("@name", name);
    var result = await command.ExecuteScalarAsync();
    return result is not null && result is not DBNull;
}

static async Task<object?> ScalarAsync(SqlConnection connection, string sql)
{
    await using var command = new SqlCommand(sql, connection);
    return await command.ExecuteScalarAsync();
}

static async Task ExecuteAsync(SqlConnection connection, string sql)
{
    await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
    await command.ExecuteNonQueryAsync();
}

static async Task<QaOwnershipMarker> ReadMarkerAsync(SqlConnection db, QaRunManifest manifest)
{
    await using var command = new SqlCommand("SELECT DB_NAME(), RunId, TargetAddress, TokenDigest FROM dbo.QaRunOwnership", db);
    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) throw new InvalidOperationException("QA database ownership marker is missing.");
    var marker = new QaOwnershipMarker(reader.GetGuid(1), reader.GetString(2), reader.GetString(0), reader.GetString(3));
    if (await reader.ReadAsync()) throw new InvalidOperationException("QA database has multiple ownership markers.");
    return marker;
}

static async Task<(int Application, int Identity)> VerifyMigrationsAsync(string adminConnection)
{
    await using var application = new AcademyDeskDbContext(
        new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(adminConnection).Options);
    await using var identity = new IdentityDbContext(
        new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(adminConnection).Options);
    var expectedApp = application.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
    var expectedIdentity = identity.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
    var applied = (await application.Database.GetAppliedMigrationsAsync()).ToHashSet(StringComparer.Ordinal);
    if (expectedApp.Count == 0 || expectedIdentity.Count == 0 ||
        !expectedApp.IsSubsetOf(applied) || !expectedIdentity.IsSubsetOf(applied) ||
        (await application.Database.GetPendingMigrationsAsync()).Any() ||
        (await identity.Database.GetPendingMigrationsAsync()).Any())
        throw new InvalidOperationException("Both EF migration sets were not applied to the QA database.");
    await using var versionConnection = new SqlConnection(adminConnection);
    await versionConnection.OpenAsync();
    await using var versionCommand = new SqlCommand("SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128)), CAST(SERVERPROPERTY('Edition') AS nvarchar(128))", versionConnection);
    await using var versionReader = await versionCommand.ExecuteReaderAsync();
    if (!await versionReader.ReadAsync()) throw new InvalidOperationException("SQL version evidence was unavailable.");
    Console.WriteLine("QA SCHEMA " + JsonSerializer.Serialize(new
    {
        sqlVersion = versionReader.GetString(0), edition = versionReader.GetString(1),
        application = expectedApp.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
        identity = expectedIdentity.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
        applied = applied.OrderBy(x => x, StringComparer.Ordinal).ToArray()
    }));
    return (expectedApp.Count, expectedIdentity.Count);
}

static async Task VerifyRuntimePrivilegesAsync(string runtimeConnection, QaRunManifest manifest)
{
    await using var db = new SqlConnection(runtimeConnection);
    await db.OpenAsync();
    await using var command = new SqlCommand("SELECT DB_NAME(), IS_SRVROLEMEMBER('sysadmin'), HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'ALTER'), HAS_PERMS_BY_NAME(NULL, NULL, 'CREATE ANY DATABASE')", db);
    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync() ||
        !string.Equals(reader.GetString(0), manifest.Database, StringComparison.Ordinal) ||
        reader.GetInt32(1) != 0 || reader.GetInt32(2) != 0 || reader.GetInt32(3) != 0)
        throw new InvalidOperationException("QA runtime login has broader permissions than allowed.");
}

static async Task VerifyRealHttpAsync(QaRunManifest manifest, bool classMediaOnly, QaBlobFixture? blobFixture = null, bool financeReconciliationOnly = false, bool financeAdjustmentOnly = false, bool payrollNetOnly = false, bool paymentTransitionOnly = false, bool financeConsumersOnly = false, bool financeAccessOnly = false, bool financeLookupsOnly = false, bool financeGovernanceOnly = false, string? collectionsMode = null, string? invoiceSettingsMode = null, string? auditMode = null, bool financeCollectionRaceOnly = false, bool financeAdjustmentRaceOnly = false, bool paymentTransitionRaceOnly = false, bool paymentReconcileVoidRaceOnly = false)
{
    try
    {
        var linkedFault = auditMode is "--audit-linked-baseline" or "--audit-linked-fixed" or "--audit-branch-fixed" ? new LinkedIdentitySqlFault() : null;
        using var factory = new QaApiFactory(manifest, isolatedIdentityInterceptor: linkedFault);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        if (!factory.PreflightPassed)
            throw new InvalidOperationException("The HTTP host did not pass the QA preflight.");

        var health = await client.GetAsync("/health");
        if (health.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"The real HTTP/SQL health control returned {(int)health.StatusCode}.");
        RecordRuntimeEndpoints(factory, manifest);

        var anonymous = await client.GetAsync("/api/academies");
        if (anonymous.StatusCode != HttpStatusCode.Unauthorized)
            throw new InvalidOperationException($"Anonymous academy access returned {(int)anonymous.StatusCode}.");

        const string email = "qa-owner@example.invalid";
        const string password = "Synthetic!39Ab";
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var created = await users.CreateAsync(new ApplicationUser
            {
                UserName = email, Email = email, EmailConfirmed = true,
                DisplayName = "Synthetic QA Owner", IsActive = true
            }, password);
            if (!created.Succeeded)
                throw new InvalidOperationException("Synthetic Identity user creation failed: " +
                    string.Join(", ", created.Errors.Select(error => error.Code)));
        }

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        if (login.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"Real Identity login returned {(int)login.StatusCode}.");
        using var payload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = payload.RootElement.GetProperty("accessToken").GetString();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Real Identity login returned no access token.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var authorized = await client.GetAsync("/api/academies");
        if (authorized.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"Authorized academy request returned {(int)authorized.StatusCode}.");
        var body = await authorized.Content.ReadAsStringAsync();
        if (body.Trim() != "[]")
            throw new InvalidOperationException("The fresh QA database did not return the expected empty academy list.");

        Console.WriteLine("HTTP controls PASS: /health 200, anonymous protected request 401, real Identity login 200, bearer request 200 against SQL.");
        await VerifyTenantIsolationAsync(factory, client);
        if (financeCollectionRaceOnly || financeAdjustmentRaceOnly)
        {
            await VerifyCollectionRaceAsync(factory, client, financeAdjustmentRaceOnly);
            return;
        }
        if (paymentTransitionRaceOnly)
        {
            await VerifyPaymentVoidRaceAsync(factory, client);
            return;
        }
        if (paymentReconcileVoidRaceOnly)
        {
            await VerifyReconcileVoidRaceAsync(factory, client);
            return;
        }
        if (auditMode is not null)
        {
            if (auditMode == "--audit-certificate-family") { await VerifyCertificateFamilyAsync(factory, client); return; }
            if (auditMode == "--audit-guardian-flags") { await VerifyGuardianFlagsAsync(factory, client); return; }
            if (auditMode == "--audit-announcement-audience") { await VerifyAnnouncementAudienceAsync(factory, client); return; }
            if (auditMode == "--audit-activity-deletion") { await VerifyActivityDeletionAsync(factory, client); return; }
            if (auditMode == "--audit-platform-billing") { await VerifyPlatformBillingAsync(factory, client); return; }
            if (auditMode == "--audit-tenant-plan") { await VerifyTenantPlanAsync(factory, client); return; }
            if (auditMode == "--audit-trial-duration") { await VerifyTrialDurationAsync(factory, client); return; }
            if (auditMode == "--audit-platform-provisioning") { await VerifyPlatformProvisioningAsync(factory, client, manifest); return; }
            if (auditMode == "--audit-portal-provisioning") { await VerifyPortalProvisioningAsync(factory, client, manifest); return; }
            if (auditMode == "--audit-academy-provisioning") { await VerifyAcademyProvisioningAsync(factory, client, manifest); return; }
            if (auditMode == "--audit-provisioning-concurrency") { await VerifyProvisioningConcurrencyAsync(factory, client, manifest); return; }
            if (auditMode == "--audit-provisioning-conflict") { await VerifyProvisioningConflictAsync(factory, client, manifest); return; }
            if (auditMode == "--audit-role-replacement") { await VerifyRoleReplacementAsync(factory, client, manifest); return; }
            if (auditMode == "--audit-role-revocation") { await VerifyRoleRevocationAsync(factory, client); return; }
            if (auditMode == "--audit-session-revocation-repair") { await VerifyPrivateResourceAccessAsync(factory, client); await VerifySessionRevocationRepairAsync(factory, client); return; }
            if (auditMode == "--audit-session-revocation") { await VerifySessionRevocationAsync(factory, client); return; }
            if (auditMode == "--audit-session-refresh") { await VerifySessionRefreshAsync(factory, client, manifest); return; }
            if (auditMode == "--audit-refresh-concurrency") { await VerifySessionRefreshAsync(factory, client, manifest, concurrency: true); return; }
            if (auditMode == "--audit-certificate-enrollment") { await VerifyCertificateEnrollmentAsync(factory, client); return; }
            if (auditMode == "--audit-compliance-identity") { await VerifyComplianceIdentityAsync(factory, client); return; }
            if (auditMode == "--audit-resource-scope") { await VerifyResourceScopeAsync(factory, client); return; }
            if (auditMode == "--audit-practice-identity") { await VerifyPracticeIdentityAsync(factory, client); return; }
            if (auditMode == "--audit-submission-identity") { await VerifySubmissionIdentityAsync(factory, client); return; }
            if (auditMode == "--audit-review-context") { await VerifyReviewContextAsync(factory, client); return; }
            if (auditMode == "--audit-marketing-consent")
            {
                await VerifyMarketingConsentAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-template-state")
            {
                await VerifyTemplateStateAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-preference-access")
            {
                await VerifyCommunicationPreferenceAccessAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-inbox-lifecycle")
            {
                await VerifyRecipientInboxAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-notification-channel")
            {
                await VerifyNotificationChannelAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-communication-roundtrip")
            {
                await VerifyCommunicationRoundtripAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-makeup-location")
            {
                await VerifyMakeupLocationAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-leave-identity")
            {
                await VerifyLeaveIdentityAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-schedule-defaults")
            {
                await VerifyScheduleDefaultsAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-calendar-details")
            {
                await VerifyCalendarDetailsAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-attendance-notes")
            {
                await VerifyAttendanceNotesAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-assessment-options")
            {
                await VerifyAssessmentOptionsAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-assessment-access")
            {
                await VerifyAssessmentAccessAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-assessment-roster")
            {
                await VerifyAssessmentRosterAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-assessment-grades")
            {
                await VerifyAssessmentGradesAsync(factory, client);
                return;
            }
            if (auditMode == "--audit-year-closure")
            {
                await VerifyAcademicYearClosureAsync(factory, client, manifest);
                return; // Accepted closed-year child creation gap only.
            }
            if (auditMode == "--audit-course-prerequisites")
            {
                await VerifyCoursePrerequisitesAsync(factory, client, manifest);
                return; // Accepted prerequisite graph gap; no repeat audit.
            }
            if (auditMode == "--audit-promotion-decisions")
            {
                await VerifyBatchPromotionDecisionsAsync(factory, client, manifest);
                return; // Selected terminal promotion-decision integrity only.
            }
            if (auditMode == "--audit-course-preservation")
            {
                await VerifyCoursePreservationAsync(factory, client);
                return; // Accepted Course replacement-field preservation; no repeat audit.
            }
            if (auditMode is "--audit-batch-times-baseline" or "--audit-batch-times-fixed")
            {
                await VerifyBatchTimesAsync(factory, client, baseline: auditMode == "--audit-batch-times-baseline");
                return; // Paired accepted teaching-time JSON and null-entry findings.
            }
            if (auditMode is "--audit-batch-preservation-baseline" or "--audit-batch-preservation-fixed")
            {
                await VerifyBatchPreservationAsync(factory, client, baseline: auditMode == "--audit-batch-preservation-baseline");
                return; // Accepted Batch replacement-field preservation gap only.
            }
            if (auditMode is "--audit-branch-address-baseline" or "--audit-branch-address-fixed")
            {
                await VerifyBranchAddressAsync(factory, client, baseline: auditMode == "--audit-branch-address-baseline");
                return; // Bounded branch replacement-field preservation, not a repeated audit.
            }
            if (auditMode is "--audit-branch-baseline" or "--audit-branch-fixed")
            {
                if (auditMode == "--audit-branch-fixed") await VerifyLinkedPeopleAsync(factory, client, manifest, linkedFault!, enforceFixed: true);
                await VerifyPeopleBranchesAsync(factory, client, enforceFixed: auditMode == "--audit-branch-fixed");
                return; // Selected Update branch integrity, not another full audit.
            }
            if (auditMode is "--audit-linked-baseline" or "--audit-linked-fixed")
            {
                if (auditMode == "--audit-linked-fixed")
                {
                    await VerifyAuditSaveAsync(factory, client, manifest, enforceFixed: true);
                    await VerifyPeopleCreationAuditAsync(factory, client, manifest, enforceFixed: true);
                }
                await VerifyLinkedPeopleAsync(factory, client, manifest, linkedFault!, enforceFixed: auditMode == "--audit-linked-fixed");
                return;
            }
            if (auditMode is "--audit-people-baseline" or "--audit-people-fixed")
            {
                if (auditMode == "--audit-people-fixed") await VerifyAuditSaveAsync(factory, client, manifest, enforceFixed: true);
                await VerifyPeopleCreationAuditAsync(factory, client, manifest, enforceFixed: auditMode == "--audit-people-fixed");
                return; // Action-level student/teacher create; Identity-linked update/provisioning is excluded.
            }
            await VerifyAuditSaveAsync(factory, client, manifest, enforceFixed: auditMode == "--audit-fixed");
            if (auditMode == "--audit-fixed") await VerifyFinanceAccessAsync(factory, client);
            return; // Domain finance boundary only; Identity/media and full critical suite remain pending.
        }
        if (invoiceSettingsMode is not null)
        {
            await VerifyInvoiceSettingsAsync(factory, client, baseline: invoiceSettingsMode == "--invoice-settings-baseline");
            return; // Read-only invoice branding under existing Finance gates.
        }
        if (collectionsMode is not null)
        {
            await VerifyCollectionsBalanceAsync(factory, client, baseline: collectionsMode == "--collections-baseline");
            return; // Bounded remaining-balance gate; no currency conversion policy.
        }
        if (financeGovernanceOnly)
        {
            await VerifyFinanceGovernanceAsync(factory, client, manifest);
            return; // Bounded Collections access/update/audit gate, not all governance issues.
        }
        if (financeLookupsOnly)
        {
            await VerifyFinanceLookupsAsync(factory, client);
            return; // Finance-specific student dependency; governance remains separate.
        }
        if (financeAccessOnly)
        {
            await VerifyFinanceAccessAsync(factory, client);
            return; // Current bounded role/tenant rules, not new approval/restoration policy or full critical suite.
        }
        if (financeConsumersOnly)
        {
            await VerifyFinanceConsumersAsync(factory, client);
            return; // Bounded read-view/reminder gate; not critical suite or restoration policy acceptance.
        }
        if (paymentTransitionOnly)
        {
            await VerifyPaymentTransitionsAsync(factory, client, enforceRegression: true);
            return; // Bounded void/evidence gate; restoration policy and critical suite remain pending.
        }
        if (payrollNetOnly)
        {
            await VerifyPayrollBoundariesAsync(factory, client, enforceRegression: true);
            return; // Explicit payroll module, not full critical/release acceptance.
        }
        if (financeAdjustmentOnly)
        {
            await VerifyAdjustedBalanceAsync(factory, client, enforceRegression: true);
            return; // Explicit adjustment module, not the full critical suite.
        }
        if (financeReconciliationOnly)
        {
            await VerifyReconciledBalanceAsync(factory, client, enforceRegression: true);
            return; // Explicit bounded finance gate, not a full critical suite.
        }
        if (blobFixture is not null)
        {
            await VerifyBlobUnavailableAsync(factory, client);
            // A fresh real host/DI graph, same run-owned SQL and authentication.
            using var blobFactory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "http://localhost:3000" }, isolatedMediaStore: blobFixture.Store);
            using var blobClient = blobFactory.CreateClient(new() { AllowAutoRedirect = false });
            await VerifyBlobUploadsAsync(blobFactory, blobClient, blobFixture, manifest);
            return;
        }
        await VerifyPrivateResourceAccessAsync(factory, client);
        if (classMediaOnly) return; // Bounded module run; finance cases are not claimed PASS or suppressed.
        await VerifyReconciledBalanceAsync(factory, client);
        await VerifyAdjustedBalanceAsync(factory, client);
        await VerifyPayrollBoundariesAsync(factory, client);
        await VerifyPaymentTransitionsAsync(factory, client);
    }
    finally
    {
        if (Directory.Exists(manifest.Root))
            QaRunGuard.CleanupHostFolders(manifest, root => { Directory.Delete(root, recursive: true); return true; });
    }
}

static async Task VerifyTenantIsolationAsync(QaApiFactory factory, HttpClient client)
{
    var academyA = Guid.NewGuid();
    var academyB = Guid.NewGuid();
    var teacherId = Guid.NewGuid();
    var courseId = Guid.NewGuid();
    var batchId = Guid.NewGuid();
    using (var scope = factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        db.Academies.AddRange(
            new Academy { Id = academyA, Name = "Synthetic Academy A", EnabledModulesJson = "[\"Certificates\",\"Finance\",\"FinanceControls\"]" },
            new Academy { Id = academyB, Name = "Synthetic Academy B", EnabledModulesJson = "[\"Certificates\"]" });
        db.Students.AddRange(
            new Student { AcademyId = academyA, FirstName = "Isolated-A", LastName = "Fixture" },
            new Student { AcademyId = academyB, FirstName = "Isolated-B", LastName = "Fixture" });
        db.Teachers.Add(new Teacher { Id = teacherId, AcademyId = academyA, FirstName = "Synthetic", LastName = "Teacher" });
        db.Courses.Add(new ProgramCourse { Id = courseId, AcademyId = academyA, Name = "Synthetic Course" });
        db.Batches.Add(new Batch { Id = batchId, AcademyId = academyA, CourseId = courseId, TeacherId = teacherId, Name = "Synthetic Batch" });
        await db.SaveChangesAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var roleResult = await roles.CreateAsync(new ApplicationRole { Name = "AcademyAdmin", IsSystemRole = true });
        if (!roleResult.Succeeded)
            throw new InvalidOperationException("Synthetic academy role creation failed: " +
                string.Join(", ", roleResult.Errors.Select(error => error.Code)));
        var teacherRole = await roles.CreateAsync(new ApplicationRole { Name = "Teacher", IsSystemRole = true });
        if (!teacherRole.Succeeded)
            throw new InvalidOperationException("Synthetic teacher role creation failed: " +
                string.Join(", ", teacherRole.Errors.Select(error => error.Code)));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var (academyId, email) in new[]
        {
            (academyA, "qa-admin-a@example.invalid"),
            (academyB, "qa-admin-b@example.invalid")
        })
        {
            var user = new ApplicationUser
            {
                UserName = email, Email = email, EmailConfirmed = true,
                DisplayName = "Synthetic Academy Admin", AcademyId = academyId, IsActive = true
            };
            var created = await users.CreateAsync(user, "Synthetic!39Ab");
            if (!created.Succeeded)
                throw new InvalidOperationException("Synthetic academy user creation failed: " +
                    string.Join(", ", created.Errors.Select(error => error.Code)));
            var assigned = await users.AddToRoleAsync(user, "AcademyAdmin");
            if (!assigned.Succeeded)
                throw new InvalidOperationException("Synthetic academy role assignment failed: " +
                    string.Join(", ", assigned.Errors.Select(error => error.Code)));
        }
        var teacher = new ApplicationUser
        {
            UserName = "qa-teacher-a@example.invalid", Email = "qa-teacher-a@example.invalid",
            EmailConfirmed = true, DisplayName = "Synthetic Teacher A", AcademyId = academyA, TeacherId = teacherId, IsActive = true
        };
        var teacherCreated = await users.CreateAsync(teacher, "Synthetic!39Ab");
        if (!teacherCreated.Succeeded)
            throw new InvalidOperationException("Synthetic teacher creation failed: " +
                string.Join(", ", teacherCreated.Errors.Select(error => error.Code)));
        var teacherAssigned = await users.AddToRoleAsync(teacher, "Teacher");
        if (!teacherAssigned.Succeeded)
            throw new InvalidOperationException("Synthetic teacher assignment failed: " +
                string.Join(", ", teacherAssigned.Errors.Select(error => error.Code)));
    }

    var tokenA = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
    var tokenB = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
    var ownA = await client.GetAsync($"/api/academies/{academyA}/students");
    await RequireStudentListAsync(ownA, "Isolated-A", "Isolated-B");

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
    var ownB = await client.GetAsync($"/api/academies/{academyB}/students");
    await RequireStudentListAsync(ownB, "Isolated-B", "Isolated-A");

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
    var crossRead = await client.GetAsync($"/api/academies/{academyB}/students");
    if (crossRead.StatusCode != HttpStatusCode.Forbidden ||
        (await crossRead.Content.ReadAsStringAsync()).Contains("Isolated-B", StringComparison.Ordinal))
        throw new InvalidOperationException("Tenant A could read Tenant B student data.");
    var crossWrite = await client.PostAsJsonAsync($"/api/academies/{academyB}/students",
        new { firstName = "Cross-Tenant", lastName = "Rejected" });
    if (crossWrite.StatusCode != HttpStatusCode.Forbidden)
        throw new InvalidOperationException($"Tenant A cross-tenant create returned {(int)crossWrite.StatusCode}.");

    using (var scope = factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var countB = await db.Students.AsNoTracking().CountAsync(student => student.AcademyId == academyB);
        var forbiddenStudent = await db.Students.AsNoTracking().AnyAsync(student => student.AcademyId == academyB && student.FirstName == "Cross-Tenant");
        if (countB != 1 || forbiddenStudent)
            throw new InvalidOperationException("The rejected cross-tenant write changed Tenant B's rows.");
    }
    Console.WriteLine("Tenant controls PASS: two real admin logins; each reads only its own student, cross-tenant GET/POST 403, no cross-tenant SQL write.");
}

static async Task VerifyPrivateResourceAccessAsync(QaApiFactory factory, HttpClient client)
{
    Guid academyA;
    Guid batchId;
    using (var scope = factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        academyA = await db.Academies.AsNoTracking()
            .Where(academy => academy.Name == "Synthetic Academy A")
            .Select(academy => academy.Id).SingleAsync();
        batchId = await db.Batches.AsNoTracking()
            .Where(batch => batch.AcademyId == academyA && batch.Name == "Synthetic Batch")
            .Select(batch => batch.Id).SingleAsync();
    }
    var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
    var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
    var tenantBToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
    var bytes = Encoding.ASCII.GetBytes("%PDF-1.4\nAcademyDesk synthetic QA private resource\n%%EOF\n");
    const string title = "Synthetic teacher class material";

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacherToken);
    using var teacherUpload = await UploadAsync(client, "/api/teacher/resources/upload", title, bytes, batchId);
    if (teacherUpload.StatusCode != HttpStatusCode.OK)
        throw new InvalidOperationException($"Authorized teacher class-material upload returned {(int)teacherUpload.StatusCode}.");
    using var uploaded = JsonDocument.Parse(await teacherUpload.Content.ReadAsStringAsync());
    var resourceId = uploaded.RootElement.GetProperty("id").GetGuid();
    var url = uploaded.RootElement.GetProperty("url").GetString();
    if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/teacher-materials/", StringComparison.Ordinal))
        throw new InvalidOperationException("Teacher upload did not return a local class-material URL.");
    using (var scope = factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var row = await db.LearningResources.AsNoTracking().SingleAsync(resource => resource.Id == resourceId);
        if (row.AcademyId != academyA || row.BatchId != batchId || row.Url != url || !row.IsPublished)
            throw new InvalidOperationException("Teacher material SQL row does not match the uploaded fixture.");
    }
    var diskPath = Path.Combine(factory.Server.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().WebRootPath,
        url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(diskPath))
        throw new InvalidOperationException("Uploaded synthetic file is absent or differs from the owned test payload.");
    var onDiskBytes = await File.ReadAllBytesAsync(diskPath);
    if (!bytes.SequenceEqual(onDiskBytes))
        throw new InvalidOperationException("Uploaded synthetic file is absent or differs from the owned test payload.");

    client.DefaultRequestHeaders.Authorization = null;
    var authorized = await FetchFileAsync(client, url, teacherToken, null, bytes);
    if (authorized.Status != 200 || !authorized.ContentMatches)
        throw new InvalidOperationException("Authorized file control did not retrieve the exact synthetic payload.");
    var anonymous = await FetchFileAsync(client, url, null, null, bytes);
    var tenantB = await FetchFileAsync(client, url, tenantBToken, null, bytes);
    var firstEightBytes = bytes.Take(8).ToArray();
    var anonymousRange = await FetchFileAsync(client, url, null, "bytes=0-7", firstEightBytes);
    var tenantBRange = await FetchFileAsync(client, url, tenantBToken, "bytes=0-7", firstEightBytes);

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    using var unpublish = await client.PatchAsJsonAsync($"/api/academies/{academyA}/resources/{resourceId}/publish",
        new { isPublished = false });
    if (unpublish.StatusCode != HttpStatusCode.OK)
        throw new InvalidOperationException("Resource revocation control did not return 200.");
    using (var scope = factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        if (await db.LearningResources.AsNoTracking().Where(resource => resource.Id == resourceId)
            .Select(resource => resource.IsPublished).SingleAsync())
            throw new InvalidOperationException("Unpublish control did not persist the private state.");
    }
    client.DefaultRequestHeaders.Authorization = null;
    var afterRevocation = await FetchFileAsync(client, url, null, null, bytes);
    var exposed = anonymous.ContentMatches || tenantB.ContentMatches || anonymousRange.ContentMatches ||
        tenantBRange.ContentMatches || afterRevocation.ContentMatches;
    if (exposed || anonymous.Status != 401 || tenantB.Status != 404 || anonymousRange.Status != 401 ||
        tenantBRange.Status != 404 || afterRevocation.Status != 401)
        throw new InvalidOperationException("SECURITY-FILE-001 regression failed: unauthorized class material request was not denied.");
    Console.WriteLine($"SECURITY-FILE-001 {(exposed ? "FAIL — private content exposed" : "PASS — unauthorized content denied")}: teacher upload=200, " +
        $"authorized GET={authorized.Status}/match={authorized.ContentMatches}, anonymous GET={anonymous.Status}/match={anonymous.ContentMatches}, " +
        $"tenant-B GET={tenantB.Status}/match={tenantB.ContentMatches}, anonymous Range={anonymousRange.Status}/match={anonymousRange.ContentMatches}, " +
        $"tenant-B Range={tenantBRange.Status}/match={tenantBRange.ContentMatches}, after unpublish anonymous GET={afterRevocation.Status}/match={afterRevocation.ContentMatches}, " +
        $"payload SHA256={Convert.ToHexString(SHA256.HashData(bytes))}.");
    await VerifyClassMaterialRegressionsAsync(factory, client, academyA, batchId, resourceId, url, bytes,
        teacherToken, adminToken, tenantBToken);
}

static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string path, string title, byte[] bytes, Guid batchId)
{
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(title), "Title");
    form.Add(new StringContent(batchId.ToString()), "BatchId");
    var file = new ByteArrayContent(bytes);
    file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
    form.Add(file, "File", "synthetic-private.pdf");
    return await client.PostAsync(path, form);
}

static async Task<(int Status, bool ContentMatches)> FetchFileAsync(
    HttpClient client, string url, string? token, string? range, byte[] expected)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (range is not null) request.Headers.TryAddWithoutValidation("Range", range);
    using var response = await client.SendAsync(request);
    var actual = await response.Content.ReadAsByteArrayAsync();
    return ((int)response.StatusCode, actual.SequenceEqual(expected));
}

static async Task<string> LoginAsync(HttpClient client, string email, string password)
{
    client.DefaultRequestHeaders.Authorization = null;
    var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
    if (response.StatusCode != HttpStatusCode.OK)
        throw new InvalidOperationException($"Synthetic academy login returned {(int)response.StatusCode}.");
    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    return payload.RootElement.GetProperty("accessToken").GetString() ??
        throw new InvalidOperationException("Synthetic academy login returned no token.");
}

static async Task RequireStudentListAsync(HttpResponseMessage response, string included, string excluded)
{
    if (response.StatusCode != HttpStatusCode.OK)
        throw new InvalidOperationException($"Own-tenant student list returned {(int)response.StatusCode}.");
    var body = await response.Content.ReadAsStringAsync();
    if (!body.Contains(included, StringComparison.Ordinal) || body.Contains(excluded, StringComparison.Ordinal))
        throw new InvalidOperationException("Own-tenant student list did not contain exactly the expected fixture.");
}
}
