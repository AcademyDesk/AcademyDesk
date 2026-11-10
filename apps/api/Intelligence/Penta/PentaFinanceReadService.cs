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
internal sealed record PentaFinanceReadOutcome(int Status, object? Value = null);

/// <summary>Testing-only finance reads through the same ledger projections as Manual.</summary>
public sealed class PentaFinanceReadService(AcademyDeskDbContext db, IdentityDbContext identity, PentaFinancePolicy policy,
    PentaPilotPolicy pilot, UserManager<ApplicationUser> users, IConfiguration configuration, IHostEnvironment environment, TimeProvider clock)
{
    public bool Enabled => environment.IsEnvironment("Testing") && pilot.IsAvailable && configuration.GetValue<bool>("Penta:FinanceReadEnabled");

    internal async Task<PentaFinanceReadOutcome> ReadAsync(ClaimsPrincipal principal, Guid academy, Guid? invoice, CancellationToken token)
    {
        if (!Enabled) return new(404);
        if (invoice == Guid.Empty) return new(404);
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

        object result;
        if (invoice is { } invoiceId)
        {
            var found = await InvoiceReadModel.Query(db, academy, invoiceId).SingleOrDefaultAsync(token);
            if (found is null) return new(404);
            result = new PentaFinanceInvoice(found, clock.GetUtcNow());
        }
        else
        {
            var modules = await db.Academies.AsNoTracking().Where(x => x.Id == academy)
                .Select(x => x.EnabledModulesJson).SingleAsync(token);
            if (!SubscriptionPlanCatalog.Allows(modules, "FinanceControls")) return new(403);
            var balances = await AcademyFinanceReadModel.LoadAsync(db, academy, token);
            result = new PentaFinanceOverview(balances, clock.GetUtcNow());
        }

        db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityId = invoice ?? academy,
            EntityType = invoice.HasValue ? "Invoice" : "Academy",
            Action = invoice.HasValue ? "PentaFinanceInvoiceRead" : "PentaFinanceOverviewRead", OccurredAtUtc = clock.GetUtcNow().UtcDateTime });
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return new(200, result);
    }
}
