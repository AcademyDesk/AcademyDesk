using System.Security.Claims;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed class PentaFinancePolicy(PentaPilotPolicy pilot, AcademyDeskDbContext db)
{
    public async Task<bool> AllowsAsync(ClaimsPrincipal principal, Guid academyId, CancellationToken token) =>
        await pilot.AllowsAsync(principal, academyId, token) && SubscriptionPlanCatalog.Allows(
            await db.Academies.AsNoTracking().Where(x => x.Id == academyId && x.IsActive)
                .Select(x => x.EnabledModulesJson).SingleOrDefaultAsync(token), "Finance");
}
public sealed record ConnectorOutcome(string Kind, string Message, MiniState State, OutstandingResult? Result = null);

/// <summary>Thin generic-tool adapter; finance calculation belongs to the shared service.</summary>
public sealed class AcademyDeskConnector(PentaFinancePolicy policy, OutstandingFeesService fees)
{
    public async Task<ConnectorOutcome> ExecuteAsync(ClaimsPrincipal principal, Guid academyId, MiniToolCall call,
        MiniState trusted, CancellationToken token)
    {
        if (!await policy.AllowsAsync(principal, academyId, token)) return new("REFUSAL", "Finance access is not available.", MiniState.Empty);
        MiniState next;
        Guid? selected = null;
        if (call.Name == "SearchLearners")
        {
            if (!PentaMiniTools.TrySearch(call, out next)) return Invalid(trusted);
        }
        else if (call.Name == "GetLearner")
        {
            if (call.Arguments.Count != 1 || !call.Arguments.TryGetValue("learner_id", out var id) ||
                id.ValueKind != System.Text.Json.JsonValueKind.String || !Guid.TryParse(id.GetString(), out var value)) return Invalid(trusted);
            // Model IDs have no authority. Only a current displayed, private result may be opened.
            if (!trusted.CurrentResultIds.Contains(value.ToString("D")))
                return new("CLARIFICATION_REQUIRED", "Search first, then choose a student from the displayed results.", trusted);
            selected = value;
            next = trusted with { CurrentLearnerId = value.ToString("D") };
        }
        else return Invalid(trusted);
        try
        {
            var result = await fees.ReadAsync(academyId, next.Filters, next.SortBy, selected, token);
            if (selected is not null)
            {
                if (result.Rows.Length != 1) return new("CLARIFICATION_REQUIRED", "That student is no longer in these results. Search again.", MiniState.Empty);
                next = next with { CurrentResultIds = [result.Rows[0].SourceId.ToString("D")] };
            }
            else next = next with { CurrentResultIds = result.Rows.Select(x => x.SourceId.ToString("D")).ToArray(), CurrentLearnerId = null };
            return new("RESULT", selected is not null ? "Here is the selected student's verified fee summary." :
                result.Count == 0 ? "No students match these filters." : $"Found {result.Count} matching student{(result.Count == 1 ? "" : "s")}. Showing {result.Rows.Length}.", next, result);
        }
        catch (OutstandingFeesException ex) { return new("CLARIFICATION_REQUIRED", ex.Message, trusted); }
    }
    private static ConnectorOutcome Invalid(MiniState state) => new("ERROR", "The proposed read could not be validated. No tool was run.", state);
}
