using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure;

// Stage, conversion and trial creation share one lead lock. The action's
// AtomicAcademyMutation transaction retains it through the required actor audit.
internal static class LeadMutationBoundary
{
    internal static bool IsConverted(Lead lead) => lead.ConvertedStudentId.HasValue ||
        string.Equals(lead.Stage.Trim(), "Converted", StringComparison.OrdinalIgnoreCase);

    internal static async Task<Lead?> ReadAsync(AcademyDeskDbContext db, Guid academyId,
        Guid leadId, CancellationToken token)
    {
        if (!db.Database.IsSqlServer())
            return await db.Leads.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == leadId, token);
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Lead mutations require the academy mutation/audit transaction.");
        var cached = db.ChangeTracker.Entries<Lead>().FirstOrDefault(x => x.Entity.Id == leadId && x.Entity.AcademyId == academyId);
        if (cached is not null && cached.State != EntityState.Unchanged)
            throw new InvalidOperationException("Enter the lead boundary before changing a tracked lead.");
        var lead = await db.Leads.FromSqlInterpolated($"SELECT * FROM [Leads] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademyId] = {academyId} AND [Id] = {leadId}")
            .SingleOrDefaultAsync(token);
        if (cached is not null && lead is not null) await db.Entry(lead).ReloadAsync(token);
        return lead;
    }
}
