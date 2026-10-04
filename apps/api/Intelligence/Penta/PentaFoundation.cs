using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>The five product experiences share one server-side authority boundary.</summary>
public static class PentaCapabilities
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        "pulse", "executor", "navigator", "twin", "autopilot"
    };
}

public sealed record PentaTurnRequest(string? Text, string? Capability)
{
    // Reject command properties we have not explicitly authorized, including
    // model-supplied user IDs, academy IDs, tool names and URLs.
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record PentaSyntheticResult(
    Guid CorrelationId, string Capability, string Tool, string Message, bool Synthetic);

public interface IPentaSyntheticProvider
{
    Task<PentaSyntheticResult> ExecuteAsync(string capability, CancellationToken token);
}

/// <summary>No network, domain data, user text echo or model credentials.</summary>
public sealed class FixedPentaSyntheticProvider : IPentaSyntheticProvider
{
    public Task<PentaSyntheticResult> ExecuteAsync(string capability, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(new PentaSyntheticResult(
            Guid.NewGuid(), capability, PentaSyntheticDispatcher.ToolName,
            "Synthetic PENTA diagnostic only. No academy information was read or changed.", true));
    }
}

public sealed class PentaSyntheticDispatcher(IPentaSyntheticProvider provider)
{
    public const string ToolName = "diagnostic.synthetic.v1";

    public async Task<PentaSyntheticResult?> RunAsync(string capability, CancellationToken token)
    {
        // Code-owned registry. There is no request/model-controlled tool dispatch.
        var result = await provider.ExecuteAsync(capability, token);
        if (result is null || result.CorrelationId == Guid.Empty ||
            !string.Equals(result.Capability, capability, StringComparison.Ordinal) ||
            !string.Equals(result.Tool, ToolName, StringComparison.Ordinal) ||
            !result.Synthetic || string.IsNullOrWhiteSpace(result.Message) || result.Message.Length > 250)
            return null;
        return result;
    }
}

public sealed class PentaPilotPolicy(
    UserManager<ApplicationUser> users, AcademyDeskDbContext academyDb,
    IConfiguration configuration, IHostEnvironment environment)
{
    public bool IsAvailable =>
        (environment.IsDevelopment() || environment.IsEnvironment("Testing")) &&
        configuration.GetValue<bool>("Penta:Enabled");

    public async Task<bool> AllowsAsync(System.Security.Claims.ClaimsPrincipal principal, Guid academyId, CancellationToken token)
    {
        if (!IsAvailable || academyId == Guid.Empty) return false;
        var user = await users.GetUserAsync(principal);
        if (user is null || !user.IsActive || user.IsPlatformOwner || user.AcademyId != academyId) return false;
        if (!await academyDb.Academies.AsNoTracking().AnyAsync(x => x.Id == academyId && x.IsActive, token)) return false;
        return await users.IsInRoleAsync(user, "Owner") || await users.IsInRoleAsync(user, "AcademyAdmin");
    }
}
