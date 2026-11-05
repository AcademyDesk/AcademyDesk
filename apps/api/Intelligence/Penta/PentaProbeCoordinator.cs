namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>
/// Internal-only synthetic probe boundary. Deliberately not registered as a service
/// or exposed through a route; the provider/privacy decision is still open.
/// </summary>
public sealed class PentaProbeCoordinator(PentaBudgetService budget, IPentaModelProvider provider,
    IConfiguration configuration, IHostEnvironment environment)
{
    public const string ToolName = "penta.synthetic-model-probe.v1";

    public async Task<PentaBudgetResult> RunAsync(Guid academyId, Guid actorId, Guid executionId,
        PentaPricedPolicy policy, CancellationToken token)
    {
        if ((!environment.IsDevelopment() && !environment.IsEnvironment("Testing")) ||
            !configuration.GetValue<bool>("Penta:Enabled") ||
            !configuration.GetValue<bool>("Penta:Provider:Enabled") ||
            !configuration.GetValue<bool>("Penta:Provider:SyntheticProbeApproved") ||
            !configuration.GetValue<bool>("Penta:Provider:OrchestrationEnabled") ||
            policy is null || !string.Equals(provider.ProviderName, policy.Provider, StringComparison.Ordinal) ||
            !string.Equals(provider.ModelName, policy.Model, StringComparison.Ordinal) ||
            policy.MaxInputTokens < 2048 || policy.MaxOutputTokens < 64)
            return PentaBudgetResult.InvalidPolicy;

        var claim = await budget.ReserveAsync(academyId, actorId, executionId, policy, token, ToolName);
        if (claim != PentaBudgetResult.Reserved) return claim;

        PentaProviderProbeResult observation;
        try { observation = await provider.ProbeAsync(token); }
        catch (Exception) { observation = new("OutcomeUnknown"); }
        // Completion failure leaves the durable claim reserved. Never redispatch
        // automatically: a timeout, malformed answer or audit fault may have cost.
        return await budget.CompleteProbeAsync(academyId, actorId, executionId,
            policy, observation, CancellationToken.None);
    }
}
