using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/payroll")]
public sealed class PayrollController(AcademyDeskDbContext db, IdentityDbContext identityDb) : ControllerBase
{
    [HttpGet("profiles")]
    public async Task<ActionResult<IReadOnlyList<PayrollProfileSummary>>> Profiles(Guid academyId, CancellationToken token) =>
        Ok(await db.PayrollProfiles.AsNoTracking().Where(x => x.AcademyId == academyId).OrderBy(x => x.WorkerName)
            .Select(x => new PayrollProfileSummary(x.Id, x.WorkerType, x.TeacherId, x.StaffUserId, x.WorkerName, x.PaymentModel, x.MonthlyAmount, x.AmountPerCycle, x.SessionsPerCycle, x.EffectiveFrom, x.IsActive)).ToListAsync(token));

    [HttpPost("profiles")]
    public async Task<ActionResult<PayrollProfileSummary>> CreateProfile(Guid academyId, SavePayrollProfileRequest request, CancellationToken token)
    {
        if (!Valid(request, out var message)) return BadRequest(new { message });
        if (!await ValidWorker(academyId, request.WorkerType, request.TeacherId, request.StaffUserId, token))
            return BadRequest(new { message = "Choose exactly one valid same-academy worker matching the worker type." });
        var profile = new PayrollProfile { AcademyId = academyId, WorkerType = request.WorkerType, TeacherId = request.TeacherId, StaffUserId = Clean(request.StaffUserId), WorkerName = request.WorkerName.Trim(), PaymentModel = request.PaymentModel, MonthlyAmount = request.PaymentModel == "Monthly" ? request.MonthlyAmount : null, AmountPerCycle = request.PaymentModel == "SessionBlock" ? request.AmountPerCycle : null, SessionsPerCycle = request.PaymentModel == "SessionBlock" ? request.SessionsPerCycle : null, EffectiveFrom = request.EffectiveFrom ?? DateOnly.FromDateTime(DateTime.UtcNow) };
        db.PayrollProfiles.Add(profile); await db.SaveChangesAsync(token);
        return Created($"/api/academies/{academyId}/payroll/profiles/{profile.Id}", Summary(profile));
    }

    [HttpPut("profiles/{profileId:guid}")]
    public async Task<ActionResult<PayrollProfileSummary>> UpdateProfile(Guid academyId, Guid profileId, SavePayrollProfileRequest request, CancellationToken token)
    {
        if (!Valid(request, out var message)) return BadRequest(new { message });
        await using var ownedTransaction = db.Database.IsSqlServer() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(token) : null;
        var profile = await LockedProfile(academyId, profileId).SingleOrDefaultAsync(token); if (profile is null) return NotFound();
        if (!await ValidWorker(academyId, request.WorkerType, request.TeacherId, request.StaffUserId, token))
            return BadRequest(new { message = "Choose exactly one valid same-academy worker matching the worker type." });
        profile.WorkerType = request.WorkerType; profile.TeacherId = request.TeacherId; profile.StaffUserId = Clean(request.StaffUserId); profile.WorkerName = request.WorkerName.Trim(); profile.PaymentModel = request.PaymentModel; profile.MonthlyAmount = request.PaymentModel == "Monthly" ? request.MonthlyAmount : null; profile.AmountPerCycle = request.PaymentModel == "SessionBlock" ? request.AmountPerCycle : null; profile.SessionsPerCycle = request.PaymentModel == "SessionBlock" ? request.SessionsPerCycle : null; profile.EffectiveFrom = request.EffectiveFrom ?? profile.EffectiveFrom; profile.IsActive = request.IsActive;
        await db.SaveChangesAsync(token);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
        return Ok(Summary(profile));
    }

    [HttpGet("payouts")]
    public async Task<ActionResult<IReadOnlyList<PayrollPayoutSummary>>> Payouts(Guid academyId, CancellationToken token) =>
        Ok(await db.PayrollPayouts.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.PaidAtUtc)
            .Select(x => new PayrollPayoutSummary(x.Id, x.PayrollProfileId,
                x.IdentityRecordedAtUtc != null && x.WorkerNameSnapshot != null && x.WorkerTypeSnapshot != null ? x.WorkerNameSnapshot : "Unavailable worker",
                x.IdentityRecordedAtUtc != null && x.WorkerNameSnapshot != null && x.WorkerTypeSnapshot != null ? x.WorkerTypeSnapshot : "Unknown",
                x.PayslipNumber, x.PeriodLabel, x.SessionsCovered, x.GrossAmount, x.Deductions, x.NetAmount, x.Currency, x.Status, x.PaymentMethod, x.Reference, x.PaidAtUtc == null ? null : DateTime.SpecifyKind(x.PaidAtUtc.Value, DateTimeKind.Utc),
                x.IdentityRecordedAtUtc != null && x.WorkerNameSnapshot != null && x.WorkerTypeSnapshot != null ? "PayoutSnapshot" : "LegacyUnverified",
                x.IdentityRecordedAtUtc == null ? null : DateTime.SpecifyKind(x.IdentityRecordedAtUtc.Value, DateTimeKind.Utc))).ToListAsync(token));

    [HttpPost("payouts")]
    public async Task<ActionResult<PayrollPayoutSummary>> Pay(Guid academyId, CreatePayrollPayoutRequest request, CancellationToken token)
    {
        // Share the profile-first lock with edits; identity and effective pay values
        // come from the same serialized profile version. The finance filter owns
        // normal API transaction/audit; direct/platform callers own this boundary.
        await using var ownedTransaction = db.Database.IsSqlServer() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(token) : null;
        // Serialize all regular payouts before taking a profile lock: alternate
        // profiles for one worker must not admit the same period concurrently.
        if (!await TryLockRegularPayroll(academyId, token))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Academy payroll is being updated. Refresh the register before trying again." });
        var profile = await LockedProfile(academyId, request.PayrollProfileId).SingleOrDefaultAsync(token); if (profile is null || !profile.IsActive) return BadRequest(new { message = "Choose an active payroll profile." });
        if (!await ValidWorker(academyId, profile.WorkerType, profile.TeacherId, profile.StaffUserId, token))
            return BadRequest(new { message = "Correct this profile's worker link before recording payroll." });
        if (string.IsNullOrWhiteSpace(request.PeriodLabel) || request.PeriodLabel.Length > 120 || request.Deductions < 0) return BadRequest(new { message = "Enter a pay period of at most 120 characters and valid deductions." });
        var gross = profile.PaymentModel == "Monthly" ? profile.MonthlyAmount ?? 0 : request.GrossAmount ?? profile.AmountPerCycle ?? 0;
        var sessions = profile.PaymentModel == "SessionBlock" ? request.SessionsCovered ?? profile.SessionsPerCycle : null;
        if (gross <= 0 || (profile.PaymentModel == "SessionBlock" && (!sessions.HasValue || sessions <= 0))) return BadRequest(new { message = "Enter the completed sessions and a positive payout amount." });
        if (!StoredMoney(gross) || !StoredMoney(request.Deductions))
            return BadRequest(new { message = "Payroll amounts must fit within 16 whole digits and two decimal places; no rounding is applied." });
        if (request.Deductions >= gross)
            return BadRequest(new { message = "Deductions must be less than gross pay. The net payout must be positive." });
        // Recorded identity is authoritative; never relabel historical statements
        // from today's mutable profile. Unknown legacy identity is guarded only
        // within its original profile, without inferring a worker across profiles.
        var periodKey = RegularPeriodKey(request.PeriodLabel);
        var priorPeriods = await db.PayrollPayouts.AsNoTracking().Where(x => x.AcademyId == academyId &&
            ((x.IdentityRecordedAtUtc != null && x.WorkerTypeSnapshot == profile.WorkerType &&
              (profile.WorkerType == "Teacher" ? x.TeacherIdSnapshot == profile.TeacherId : x.StaffUserIdSnapshot == profile.StaffUserId)) ||
             ((x.IdentityRecordedAtUtc == null || x.WorkerTypeSnapshot == null ||
               (x.WorkerTypeSnapshot == "Teacher" && x.TeacherIdSnapshot == null) ||
               (x.WorkerTypeSnapshot == "Staff" && x.StaffUserIdSnapshot == null)) && x.PayrollProfileId == profile.Id)))
            .OrderBy(x => x.Id).Select(x => x.PeriodLabel).Take(10001).ToListAsync(token);
        if (priorPeriods.Count > 10000)
            return Conflict(new { message = "This worker's payroll history requires review before recording another regular statement." });
        if (priorPeriods.Any(label => RegularPeriodKey(label) == periodKey))
            return Conflict(new { message = "A regular payroll statement already exists for this worker and pay period. Additional amounts require a separately approved payroll adjustment." });
        var payout = new PayrollPayout { AcademyId = academyId, PayrollProfileId = profile.Id, PayslipNumber = $"PS-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}", PeriodLabel = request.PeriodLabel.Trim(), SessionsCovered = sessions, GrossAmount = gross, Deductions = request.Deductions, NetAmount = gross - request.Deductions, PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "BankTransfer" : request.PaymentMethod.Trim(), Reference = Clean(request.Reference), PaidAtUtc = request.PaidAtUtc ?? DateTime.UtcNow };
        payout.WorkerNameSnapshot = profile.WorkerName;
        payout.WorkerTypeSnapshot = profile.WorkerType;
        payout.TeacherIdSnapshot = profile.TeacherId;
        payout.StaffUserIdSnapshot = profile.StaffUserId;
        payout.IdentityRecordedAtUtc = DateTime.UtcNow;
        db.PayrollPayouts.Add(payout); await db.SaveChangesAsync(token);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
        return Created($"/api/academies/{academyId}/payroll/payouts/{payout.Id}", new PayrollPayoutSummary(payout.Id, payout.PayrollProfileId, payout.WorkerNameSnapshot, payout.WorkerTypeSnapshot, payout.PayslipNumber, payout.PeriodLabel, payout.SessionsCovered, payout.GrossAmount, payout.Deductions, payout.NetAmount, payout.Currency, payout.Status, payout.PaymentMethod, payout.Reference, payout.PaidAtUtc, "PayoutSnapshot", payout.IdentityRecordedAtUtc));
    }

    private static bool Valid(SavePayrollProfileRequest r, out string message)
    {
        message = "";
        if (r.WorkerType is not ("Teacher" or "Staff")) { message = "Choose Teacher or Staff."; return false; }
        if (string.IsNullOrWhiteSpace(r.WorkerName)) { message = "Enter the worker name."; return false; }
        if (r.PaymentModel is not ("Monthly" or "SessionBlock")) { message = "Choose monthly or session-block payment."; return false; }
        if (r.PaymentModel == "Monthly" && r.MonthlyAmount is not > 0) { message = "Enter a positive monthly salary."; return false; }
        if (r.PaymentModel == "SessionBlock" && (r.AmountPerCycle is not > 0 || r.SessionsPerCycle is not > 0)) { message = "Enter sessions per cycle and payout amount."; return false; }
        var amount = r.PaymentModel == "Monthly" ? r.MonthlyAmount!.Value : r.AmountPerCycle!.Value;
        if (!StoredMoney(amount)) { message = "Payroll amounts must fit within 16 whole digits and two decimal places; no rounding is applied."; return false; }
        return true;
    }
    // Validate the existing SQL decimal(18,2) contract, never silently round.
    private static bool StoredMoney(decimal amount) => amount >= 0 && amount <= 9999999999999999.99m && decimal.Round(amount, 2) == amount;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private async Task<bool> ValidWorker(Guid academyId, string type, Guid? teacherId, string? staffUserId, CancellationToken token)
    {
        if (type == "Teacher") return teacherId is not null && teacherId != Guid.Empty && string.IsNullOrWhiteSpace(staffUserId) &&
            await db.Teachers.AnyAsync(x => x.Id == teacherId && x.AcademyId == academyId, token);
        if (type != "Staff" || teacherId is not null || !Guid.TryParse(Clean(staffUserId), out var id) || id == Guid.Empty) return false;
        var member = await identityDb.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academyId, token);
        if (member is null || member.TeacherId is not null || member.StudentId is not null || member.GuardianId is not null) return false;
        return !await (from assigned in identityDb.UserRoles join role in identityDb.Roles on assigned.RoleId equals role.Id
            where assigned.UserId == id && (role.Name == "Teacher" || role.Name == "Student" || role.Name == "Guardian") select assigned.UserId).AnyAsync(token);
    }
    private static string RegularPeriodKey(string label)
    {
        var normalized = string.Join(' ', label.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (DateTime.TryParseExact(normalized, ["yyyy-MM", "yyyy/MM", "MMM yyyy", "MMMM yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
            return "MONTH:" + month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        return "LABEL:" + normalized.ToUpperInvariant();
    }

    private async Task<bool> TryLockRegularPayroll(Guid academyId, CancellationToken token)
    {
        // InMemory is unit-test coverage only, never SQL concurrency evidence.
        if (!db.Database.IsRelational()) return true;
        if (!db.Database.IsSqlServer() || db.Database.CurrentTransaction is null) return false;
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; SELECT @result;";
        var resource = command.CreateParameter(); resource.ParameterName = "@resource";
        resource.Value = $"AcademyDesk:RegularPayroll:{academyId:D}"; command.Parameters.Add(resource);
        return await command.ExecuteScalarAsync(token) is int result && result >= 0;
    }

    private IQueryable<PayrollProfile> LockedProfile(Guid academyId, Guid profileId) => db.Database.IsSqlServer()
        ? db.PayrollProfiles.FromSqlInterpolated($"SELECT * FROM [PayrollProfiles] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademyId] = {academyId} AND [Id] = {profileId}")
        : db.PayrollProfiles.Where(x => x.AcademyId == academyId && x.Id == profileId);
    private static PayrollProfileSummary Summary(PayrollProfile p) => new(p.Id, p.WorkerType, p.TeacherId, p.StaffUserId, p.WorkerName, p.PaymentModel, p.MonthlyAmount, p.AmountPerCycle, p.SessionsPerCycle, p.EffectiveFrom, p.IsActive);
}

public sealed record SavePayrollProfileRequest(string WorkerType, Guid? TeacherId, string? StaffUserId, string WorkerName, string PaymentModel, decimal? MonthlyAmount, decimal? AmountPerCycle, int? SessionsPerCycle, DateOnly? EffectiveFrom, bool IsActive = true);
public sealed record CreatePayrollPayoutRequest(Guid PayrollProfileId, string PeriodLabel, int? SessionsCovered, decimal? GrossAmount, decimal Deductions, string? PaymentMethod, string? Reference, DateTime? PaidAtUtc);
public sealed record PayrollProfileSummary(Guid Id, string WorkerType, Guid? TeacherId, string? StaffUserId, string WorkerName, string PaymentModel, decimal? MonthlyAmount, decimal? AmountPerCycle, int? SessionsPerCycle, DateOnly EffectiveFrom, bool IsActive)
{
    public string? MonthlyAmountExact => MonthlyAmount?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    public string? AmountPerCycleExact => AmountPerCycle?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
}
public sealed record PayrollPayoutSummary(Guid Id, Guid PayrollProfileId, string WorkerName, string WorkerType, string PayslipNumber, string PeriodLabel, int? SessionsCovered, decimal GrossAmount, decimal Deductions, decimal NetAmount, string Currency, string Status, string PaymentMethod, string? Reference, DateTime? PaidAtUtc, string IdentitySource = "LegacyUnverified", DateTime? IdentityRecordedAtUtc = null)
{
    public string GrossAmountExact => GrossAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    public string DeductionsExact => Deductions.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    public string NetAmountExact => NetAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    public string SettlementKind => Status == "NoCashDue" ? "NoCashDue" : NetAmount == 0m ? "LegacyZeroNet" : "RecordedPayout";
}
