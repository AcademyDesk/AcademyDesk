using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AcademyDesk.Api.Infrastructure;

// All invoice-ledger writers take the invoice lock before loading mutable
// payment/adjustment rows. Reuse the finance/audit transaction when present.
internal sealed class InvoiceWriteScope(Invoice? invoice, IDbContextTransaction? owned) : IAsyncDisposable
{
    public Invoice? Invoice { get; } = invoice;

    public static async Task<InvoiceWriteScope> OpenAsync(AcademyDeskDbContext db, Guid academyId, Guid invoiceId, CancellationToken token)
    {
        var sql = db.Database.IsSqlServer();
        var owned = sql && db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(token) : null;
        try
        {
            var cached = sql ? db.ChangeTracker.Entries<Invoice>().FirstOrDefault(x => x.Entity.Id == invoiceId && x.Entity.AcademyId == academyId) : null;
            if (cached is not null && cached.State != EntityState.Unchanged)
                throw new InvalidOperationException("Enter the invoice write boundary before changing a tracked invoice.");
            var query = sql
                ? db.Invoices.FromSqlInterpolated($"SELECT * FROM [Invoices] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademyId] = {academyId} AND [Id] = {invoiceId}")
                : db.Invoices.Where(x => x.AcademyId == academyId && x.Id == invoiceId);
            var invoice = await query.SingleOrDefaultAsync(token);
            // A request normally starts untracked; direct callers may already
            // have a stale invoice. Refresh only after obtaining the SQL lock.
            if (cached is not null && invoice is not null) await db.Entry(invoice).ReloadAsync(token);
            return new(invoice, owned);
        }
        catch
        {
            if (owned is not null) await owned.DisposeAsync();
            throw;
        }
    }

    public Task CommitAsync(CancellationToken token) => owned?.CommitAsync(token) ?? Task.CompletedTask;
    public ValueTask DisposeAsync() => owned?.DisposeAsync() ?? ValueTask.CompletedTask;
}
