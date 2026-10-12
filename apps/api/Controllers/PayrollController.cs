using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/payroll")]
public sealed class PayrollController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet("profiles")]
    public async Task<ActionResult<IReadOnlyList<PayrollProfileSummary>>> Profiles(Guid academyId, CancellationToken token) =>
        Ok(await db.PayrollProfiles.AsNoTracking().Where(x => x.AcademyId == academyId).OrderBy(x => x.WorkerName)
            .Select(x => new PayrollProfileSummary(x.Id, x.WorkerType, x.TeacherId, x.StaffUserId, x.WorkerName, x.PaymentModel, x.MonthlyAmount, x.AmountPerCycle, x.SessionsPerCycle, x.EffectiveFrom, x.IsActive)).ToListAsync(token));

    [HttpPost("profiles")]
    public async Task<ActionResult<PayrollProfileSummary>> CreateProfile(Guid academyId, SavePayrollProfileRequest request, CancellationToken token)
    {
        if (!Valid(request, out var message)) return BadRequest(new { message });
        if (request.TeacherId.HasValue && !await db.Teachers.AnyAsync(x => x.Id == request.TeacherId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The selected teacher is not part of this academy." });
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
        profile.WorkerType = request.WorkerType; profile.TeacherId = request.TeacherId; profile.StaffUserId = Clean(request.StaffUserId); profile.WorkerName = request.WorkerName.Trim(); profile.PaymentModel = request.PaymentModel; profile.MonthlyAmount = request.PaymentModel == "Monthly" ? request.MonthlyAmount : null; profile.AmountPerCycle = request.PaymentModel == "SessionBlock" ? request.AmountPerCycle : null; profile.SessionsPerCycle = request.PaymentModel == "SessionBlock" ? request.SessionsPerCycle : null; profile.EffectiveFrom = request.EffectiveFrom ?? profile.EffectiveFrom; profile.IsActive = request.IsActive;
        await db.SaveChangesAsync(token);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
        return Ok(Summary(profile));
    }

    [HttpGet("payouts")]
    public async Task<ActionResult<IReadOnlyList<PayrollPayoutSummary>>> Payouts(Guid academyId, CancellationToken token) =>
        Ok(await db.PayrollPayouts.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.PaidAtUtc)
            .Select(x => new PayrollPayoutSummary(x.Id, x.PayrollProfileId,
                x.IdentityRecordedAtUtc != null && x.WorkerNameSnapshot != null && x.WorkerTypeSnapshot != null ? x.WorkerNameSnapshot : "Historical identity not recorded",
                x.IdentityRecordedAtUtc != null && x.WorkerNameSnapshot != null && x.WorkerTypeSnapshot != null ? x.WorkerTypeSnapshot : "Unknown (legacy)",
                x.PayslipNumber, x.PeriodLabel, x.SessionsCovered, x.GrossAmount, x.Deductions, x.NetAmount, x.Currency, x.Status, x.PaymentMethod, x.Reference, DateTime.SpecifyKind(x.PaidAtUtc, DateTimeKind.Utc),
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
        var profile = await LockedProfile(academyId, request.PayrollProfileId).SingleOrDefaultAsync(token); if (profile is null || !profile.IsActive) return BadRequest(new { message = "Choose an active payroll profile." });
        if (string.IsNullOrWhiteSpace(request.PeriodLabel) || request.Deductions < 0) return BadRequest(new { message = "Enter a pay period and valid deductions." });
        var gross = profile.PaymentModel == "Monthly" ? profile.MonthlyAmount ?? 0 : request.GrossAmount ?? profile.AmountPerCycle ?? 0;
        var sessions = profile.PaymentModel == "SessionBlock" ? request.SessionsCovered ?? profile.SessionsPerCycle : null;
        if (gross <= 0 || (profile.PaymentModel == "SessionBlock" && (!sessions.HasValue || sessions <= 0))) return BadRequest(new { message = "Enter the completed sessions and a positive payout amount." });
        if (request.Deductions > gross) return BadRequest(new { message = "Deductions cannot exceed the gross payout amount." });
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

    private static bool Valid(SavePayrollProfileRequest r, out string message) { message = ""; if (r.WorkerType is not ("Teacher" or "Staff")) { message = "Choose Teacher or Staff."; return false; } if (string.IsNullOrWhiteSpace(r.WorkerName)) { message = "Enter the worker name."; return false; } if (r.PaymentModel is not ("Monthly" or "SessionBlock")) { message = "Choose monthly or session-block payment."; return false; } if (r.PaymentModel == "Monthly" && r.MonthlyAmount is not > 0) { message = "Enter a positive monthly salary."; return false; } if (r.PaymentModel == "SessionBlock" && (r.AmountPerCycle is not > 0 || r.SessionsPerCycle is not > 0)) { message = "Enter sessions per cycle and payout amount."; return false; } return true; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private IQueryable<PayrollProfile> LockedProfile(Guid academyId, Guid profileId) => db.Database.IsSqlServer()
        ? db.PayrollProfiles.FromSqlInterpolated($"SELECT * FROM [PayrollProfiles] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademyId] = {academyId} AND [Id] = {profileId}")
        : db.PayrollProfiles.Where(x => x.AcademyId == academyId && x.Id == profileId);
    private static PayrollProfileSummary Summary(PayrollProfile p) => new(p.Id, p.WorkerType, p.TeacherId, p.StaffUserId, p.WorkerName, p.PaymentModel, p.MonthlyAmount, p.AmountPerCycle, p.SessionsPerCycle, p.EffectiveFrom, p.IsActive);
}

public sealed record SavePayrollProfileRequest(string WorkerType, Guid? TeacherId, string? StaffUserId, string WorkerName, string PaymentModel, decimal? MonthlyAmount, decimal? AmountPerCycle, int? SessionsPerCycle, DateOnly? EffectiveFrom, bool IsActive = true);
public sealed record CreatePayrollPayoutRequest(Guid PayrollProfileId, string PeriodLabel, int? SessionsCovered, decimal? GrossAmount, decimal Deductions, string? PaymentMethod, string? Reference, DateTime? PaidAtUtc);
public sealed record PayrollProfileSummary(Guid Id, string WorkerType, Guid? TeacherId, string? StaffUserId, string WorkerName, string PaymentModel, decimal? MonthlyAmount, decimal? AmountPerCycle, int? SessionsPerCycle, DateOnly EffectiveFrom, bool IsActive);
public sealed record PayrollPayoutSummary(Guid Id, Guid PayrollProfileId, string WorkerName, string WorkerType, string PayslipNumber, string PeriodLabel, int? SessionsCovered, decimal GrossAmount, decimal Deductions, decimal NetAmount, string Currency, string Status, string PaymentMethod, string? Reference, DateTime PaidAtUtc, string IdentitySource = "LegacyUnverified", DateTime? IdentityRecordedAtUtc = null);
