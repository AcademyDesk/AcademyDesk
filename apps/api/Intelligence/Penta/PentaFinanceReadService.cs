using System.Data;
using System.Security.Claims;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.ReadModels;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaFinanceOverview(FinanceCurrencyTotals[] ByCurrency, DateTimeOffset AsOfUtc)
{
    public bool HasMixedCurrencies => ByCurrency.Length > 1;
}

public sealed record PentaFinanceInvoice(InvoiceSummary Invoice, DateTimeOffset AsOfUtc);
public sealed record PentaFinanceCollections(CollectionInvoiceSummary[] Invoices, bool HasMore, DateTimeOffset AsOfUtc);
public sealed record PentaReconciliationPayment(Guid Id, decimal Amount, string Currency, string Status, DateTime PaidAtUtc,
    DateTime? ReconciledAtUtc, bool CountsTowardInvoiceBalance);
public sealed record PentaFinanceReconciliation(InvoiceSummary Invoice, PentaReconciliationPayment[] Payments, bool HasMore, DateTimeOffset AsOfUtc);
internal sealed record PentaFinanceReadOutcome(int Status, object? Value = null);
internal enum PentaFinanceReadKind { Overview, Invoice, Collections, Reconciliation }

/// <summary>Testing-only finance reads through the same ledger projections as Manual.</summary>
public sealed class PentaFinanceReadService(AcademyDeskDbContext db, IdentityDbContext identity, PentaFinancePolicy policy,
    PentaPilotPolicy pilot, UserManager<ApplicationUser> users, IConfiguration configuration, IHostEnvironment environment, TimeProvider clock)
{
    private const int PreviewSize = 20;
    public bool Enabled => environment.IsEnvironment("Testing") && pilot.IsAvailable && configuration.GetValue<bool>("Penta:FinanceReadEnabled");

    internal async Task<PentaFinanceReadOutcome> ReadAsync(ClaimsPrincipal principal, Guid academy, PentaFinanceReadKind kind,
        Guid? invoice, CancellationToken token)
    {
        if (!Enabled) return new(404);
        if (kind is PentaFinanceReadKind.Invoice or PentaFinanceReadKind.Reconciliation && (invoice is null || invoice == Guid.Empty))
            return new(404);
        var stamp = principal.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        if (string.IsNullOrEmpty(stamp) || !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ||
            !await policy.AllowsAsync(principal, academy, token)) return new(403);

        var original = AcademyIdentityTransaction.Validate(db, identity);
        identity.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academy}", token);
        await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, original, token);
        var current = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actor, token);
        if (current?.SecurityStamp != stamp || !await policy.AllowsAsync(principal, academy, token)) return new(403);

        if (kind is PentaFinanceReadKind.Overview or PentaFinanceReadKind.Collections)
        {
            var modules = await db.Academies.AsNoTracking().Where(x => x.Id == academy)
                .Select(x => x.EnabledModulesJson).SingleAsync(token);
            if (!SubscriptionPlanCatalog.Allows(modules, "FinanceControls")) return new(403);
        }

        var now = clock.GetUtcNow();
        object? result = kind switch
        {
            PentaFinanceReadKind.Overview => new PentaFinanceOverview(await AcademyFinanceReadModel.LoadAsync(db, academy, token), now),
            PentaFinanceReadKind.Collections => await CollectionsAsync(academy, now, token),
            PentaFinanceReadKind.Invoice or PentaFinanceReadKind.Reconciliation =>
                await ReadInvoiceAsync(academy, invoice!.Value, kind, now, token),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (result is null) return new(404);

        db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityId = invoice ?? academy,
            EntityType = invoice.HasValue ? "Invoice" : "Academy", Action = $"PentaFinance{kind}Read", OccurredAtUtc = now.UtcDateTime });
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return new(200, result);
    }

    private async Task<PentaFinanceCollections> CollectionsAsync(Guid academy, DateTimeOffset now, CancellationToken token)
    {
        var rows = await CollectionsReadModel.LoadAsync(db, academy, DateOnly.FromDateTime(now.UtcDateTime), token, PreviewSize + 1);
        return new(rows.Take(PreviewSize).ToArray(), rows.Count > PreviewSize, now);
    }

    private async Task<object?> ReadInvoiceAsync(Guid academy, Guid invoiceId, PentaFinanceReadKind kind, DateTimeOffset now, CancellationToken token)
    {
        var invoice = await InvoiceReadModel.Query(db, academy, invoiceId).SingleOrDefaultAsync(token);
        if (invoice is null) return null;
        if (kind == PentaFinanceReadKind.Invoice) return new PentaFinanceInvoice(invoice, now);

        var currency = invoice.Currency.Trim().ToUpperInvariant();
        var rows = await PaymentReadModel.Query(db, academy, invoiceId).Take(PreviewSize + 1)
            .Select(p => new PentaReconciliationPayment(p.Id, p.Amount, p.Currency, p.Status, p.PaidAtUtc, p.ReconciledAtUtc,
                (p.Status == "Completed" || p.Status == "Reconciled") && p.Currency.Trim().ToUpper() == currency))
            .ToArrayAsync(token);
        return new PentaFinanceReconciliation(invoice, rows.Take(PreviewSize).ToArray(), rows.Length > PreviewSize, now);
    }
}
