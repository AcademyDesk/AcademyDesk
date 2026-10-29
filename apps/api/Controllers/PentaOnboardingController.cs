using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

/// <summary>Protected test-only host adapter, never a model-accessible tool endpoint.</summary>
[ApiController]
[Authorize(AuthenticationSchemes = "Identity.Bearer")]
[Route("api/academies/{academyId:guid}/penta/onboarding/conversations/{conversationId:guid}/drafts/{draftId:guid}")]
public sealed partial class PentaOnboardingController(PentaOnboardingDraftService drafts, IConfiguration configuration,
    IHostEnvironment environment, ILogger<PentaOnboardingController> logger) : ControllerBase
{
    private bool Available => environment.IsEnvironment("Testing") && drafts.Enabled &&
        configuration.GetValue<bool>("Penta:OperationalOnboardingHttpContractsEnabled");

    private const string ConversationRoute = "~/api/academies/{academyId:guid}/penta/onboarding/conversations/{conversationId:guid}";

    [HttpGet(ConversationRoute + "/send-readiness")]
    public Task<ActionResult> SendReadiness(Guid academyId,Guid conversationId,CancellationToken token) =>
        Conversation(academyId,conversationId,"send-readiness",default,token);

    [HttpPost(ConversationRoute + "/browser-turns"),RequestSizeLimit(16*1024)]
    public Task<ActionResult> BrowserTurn(Guid academyId,Guid conversationId,[FromBody] JsonElement input,CancellationToken token) =>
        Safely(async()=>{var outcome=await drafts.CoreBrowserGatewayAsync(User,academyId,conversationId,input,token);
            return ToResult(outcome.HttpStatus,outcome.Value,outcome.Error);});

    [HttpGet("~/api/academies/{academyId:guid}/penta/onboarding/readiness")]
    public Task<ActionResult> StartReadiness(Guid academyId, CancellationToken token) => Safely(async () => {
        var outcome = await drafts.CoreStartReadinessAsync(User, academyId, token);
        return ToResult(outcome.HttpStatus, outcome.Value, outcome.Error);
    });

    [HttpPost("~/api/academies/{academyId:guid}/penta/onboarding/conversations"), RequestSizeLimit(1024)]
    public Task<ActionResult> StartConversation(Guid academyId, [FromBody] JsonElement input, CancellationToken token) => Safely(async () => {
        var outcome = await drafts.CoreStartAsync(User, academyId, input, token);
        return ToResult(outcome.HttpStatus, outcome.Value, outcome.Error);
    });

    [HttpGet("~/api/academies/{academyId:guid}/penta/onboarding/starts/{requestId:guid}")]
    public Task<ActionResult> StartResult(Guid academyId, Guid requestId, CancellationToken token) => Safely(async () => {
        var outcome = await drafts.CoreStartResultAsync(User, academyId, requestId, token);
        return ToResult(outcome.HttpStatus, outcome.Value, outcome.Error);
    });

    [HttpPost(ConversationRoute + "/turns"), RequestSizeLimit(16 * 1024)]
    public Task<ActionResult> GatewayTurn(Guid academyId,Guid conversationId,[FromBody] JsonElement input,CancellationToken token)=>
        Safely(async()=>{
            var outcome=await drafts.CoreGatewayAsync(User,academyId,conversationId,input,token);
            return ToResult(outcome.HttpStatus,outcome.Value,outcome.Error);
        });

    [HttpGet(ConversationRoute + "/current")]
    public Task<ActionResult> CurrentConversation(Guid academyId, Guid conversationId, CancellationToken token) =>
        Conversation(academyId, conversationId, "current", default, token);

    [HttpPost(ConversationRoute + "/selection"), RequestSizeLimit(2048)]
    public Task<ActionResult> SelectDraft(Guid academyId, Guid conversationId, [FromBody] JsonElement input, CancellationToken token) =>
        Conversation(academyId, conversationId, "select", input, token);

    [HttpPost(ConversationRoute + "/claims"), RequestSizeLimit(16 * 1024)]
    public Task<ActionResult> ClaimTurn(Guid academyId, Guid conversationId, [FromBody] JsonElement input, CancellationToken token) =>
        Conversation(academyId, conversationId, "claim", input, token);

    [HttpPost(ConversationRoute + "/claims/{requestId:guid}/complete"), RequestSizeLimit(32 * 1024)]
    public Task<ActionResult> CompleteTurn(Guid academyId, Guid conversationId, Guid requestId, [FromBody] JsonElement input, CancellationToken token) =>
        Turn(academyId,conversationId,requestId,input,false,token);

    [HttpGet(ConversationRoute + "/claims/{requestId:guid}/result")]
    public Task<ActionResult> TurnResult(Guid academyId, Guid conversationId, Guid requestId, CancellationToken token) =>
        Turn(academyId,conversationId,requestId,default,true,token);

    private Task<ActionResult> Turn(Guid academy,Guid conversation,Guid request,JsonElement input,bool readOnly,CancellationToken token)=>
        Safely(async()=>{
            var outcome=await drafts.CoreTurnAsync(User,academy,conversation,request,input,readOnly,token);
            return ToResult(outcome.HttpStatus,outcome.Value,outcome.Error);
        });

    private Task<ActionResult> Conversation(Guid academy, Guid conversation, string action, JsonElement input, CancellationToken token) =>
        Safely(async () => {
            var outcome = await drafts.CoreConversationAsync(User, academy, conversation, action, input, token);
            return ToResult(outcome.HttpStatus, outcome.Value, outcome.Error);
        });

    [HttpPut, RequestSizeLimit(16 * 1024)]
    public Task<ActionResult> Save(Guid academyId, Guid conversationId, Guid draftId, [FromBody] JsonElement input, CancellationToken token) =>
        Safely(async () =>
        {
            if (!Keys(input, "expectedRevision", "contextVersion", "fields") ||
                !Integer(input, "expectedRevision", 0, 99, out var revision) ||
                !Integer(input, "contextVersion", 0, 9007199254740991, out var context) ||
                input.GetProperty("fields").ValueKind != JsonValueKind.Object) return BadInput();
            var payload = JsonSerializer.Serialize(new { tool_key = PentaOnboardingDraftService.Tool, tool_revision = 1,
                fields = input.GetProperty("fields") });
            return Draft(await drafts.SaveAsync(User, academyId, conversationId, draftId, revision, context, payload, token));
        });

    [HttpGet]
    public Task<ActionResult> Get(Guid academyId, Guid conversationId, Guid draftId, CancellationToken token) =>
        Safely(async () => Draft(await drafts.GetAsync(User, academyId, conversationId, draftId, token)));

    [HttpGet("core-context")]
    public Task<ActionResult> CoreContext(Guid academyId, Guid conversationId, Guid draftId, CancellationToken token) =>
        Safely(async () => {
            var outcome = await drafts.CoreContextAsync(User, academyId, conversationId, draftId, token);
            return ToResult(outcome.HttpStatus, outcome.Snapshot, outcome.Error);
        });

    [HttpPost("review"), RequestSizeLimit(1024)]
    public Task<ActionResult> Review(Guid academyId, Guid conversationId, Guid draftId, [FromBody] JsonElement input, CancellationToken token) =>
        Safely(async () =>
        {
            if (!Binding(input, false, out var revision, out var hash)) return BadInput();
            return Draft(await drafts.ConfirmAsync(User, academyId, conversationId, draftId, revision, hash, token));
        });

    [HttpPost("core/requests/{requestId:guid}/prepare"), RequestSizeLimit(16 * 1024)]
    public Task<ActionResult> CorePrepare(Guid academyId, Guid conversationId, Guid draftId, Guid requestId, [FromBody] JsonElement input, CancellationToken token) =>
        Safely(async () => {
            var outcome = await drafts.CorePrepareAsync(User, academyId, conversationId, draftId, requestId, input, token);
            // ConnectorSDK.prepare expects a list, even for one bounded operation.
            return ToResult(outcome.HttpStatus, outcome.HttpStatus is 200 or 201 && outcome.Value is { } value ? new[] { value } : outcome.Value, outcome.Error);
        });

    [HttpGet("core/requests/{requestId:guid}/validation")]
    public Task<ActionResult> CoreValidation(Guid academyId, Guid conversationId, Guid draftId, Guid requestId, CancellationToken token) =>
        Safely(async () => {
            var outcome = await drafts.CoreValidationAsync(User, academyId, conversationId, draftId, requestId, token);
            return ToResult(outcome.HttpStatus, outcome.Value, outcome.Error);
        });

    [HttpPost("core/requests/{requestId:guid}/partial"), RequestSizeLimit(16 * 1024)]
    public Task<ActionResult> CorePartial(Guid academyId, Guid conversationId, Guid draftId, Guid requestId, [FromBody] JsonElement input, CancellationToken token) =>
        Safely(async () => {
            var outcome = await drafts.CorePartialAsync(User,academyId,conversationId,draftId,requestId,input,token);
            return ToResult(outcome.HttpStatus,outcome.Value,outcome.Error);
        });

    [HttpPost("core/requests/{requestId:guid}/transition"), RequestSizeLimit(2048)]
    public Task<ActionResult> CoreTransition(Guid academyId, Guid conversationId, Guid draftId, Guid requestId, [FromBody] JsonElement input, CancellationToken token) =>
        Safely(async () => {
            var outcome = await drafts.CoreTransitionAsync(User, academyId, conversationId, draftId, requestId, input, token);
            return ToResult(outcome.HttpStatus, outcome.Value, outcome.Error);
        });

    [HttpPost("authorize-creation"), RequestSizeLimit(1024)]
    public Task<ActionResult> AuthorizeCreation(Guid academyId, Guid conversationId, Guid draftId, [FromBody] JsonElement input, CancellationToken token) =>
        Safely(async () =>
        {
            if (!Binding(input, true, out var revision, out var hash) ||
                input.GetProperty("intent").ValueKind != JsonValueKind.String) return BadInput();
            return Draft(await drafts.ConfirmCreationAsync(User, academyId, conversationId, draftId, revision, hash,
                input.GetProperty("intent").GetString()!, token));
        });

    [HttpPost("core/challenge"), RequestSizeLimit(1024)]
    public Task<ActionResult> IssueChallenge(Guid academyId, Guid conversationId, Guid draftId, [FromBody] JsonElement input, CancellationToken token) =>
        CoreBound(academyId, conversationId, draftId, "challenge", input, token);

    [HttpPost("core/confirm"), RequestSizeLimit(1024)]
    public Task<ActionResult> ConfirmCore(Guid academyId, Guid conversationId, Guid draftId, [FromBody] JsonElement input, CancellationToken token) =>
        CoreBound(academyId, conversationId, draftId, "confirm", input, token);

    [HttpPost("core/operation"), RequestSizeLimit(1024)]
    public Task<ActionResult> Operation(Guid academyId, Guid conversationId, Guid draftId, [FromBody] JsonElement input, CancellationToken token) =>
        CoreBound(academyId, conversationId, draftId, "operation", input, token);

    [HttpPost("core/status"), RequestSizeLimit(1024)]
    public Task<ActionResult> Status(Guid academyId, Guid conversationId, Guid draftId, [FromBody] JsonElement input, CancellationToken token) =>
        CoreBound(academyId, conversationId, draftId, "status", input, token);

    [HttpPost("core/execute"), RequestSizeLimit(1024)]
    public Task<ActionResult> ExecuteCore(Guid academyId, Guid conversationId, Guid draftId, [FromBody] JsonElement input, CancellationToken token) =>
        CoreBound(academyId, conversationId, draftId, "execute", input, token);

    private Task<ActionResult> CoreBound(Guid academy, Guid conversation, Guid draft, string action, JsonElement input, CancellationToken token) =>
        Safely(async () => {
            if (!PentaOnboardingOperationBinding.TryRead(input, action is "confirm" or "execute", out var binding)) return BadInput();
            var outcome = action == "execute"
                ? await drafts.CoreExecuteAsync(User, academy, conversation, draft, binding!, token)
                : await drafts.CoreOperationAsync(User, academy, conversation, draft, action, binding!, token);
            return ToResult(outcome.HttpStatus, outcome.Value, outcome.Error);
        });

    [HttpPost("execute"), RequestSizeLimit(1024)]
    public Task<ActionResult> Execute(Guid academyId, Guid conversationId, Guid draftId, [FromBody] JsonElement input, CancellationToken token) =>
        Safely(async () =>
        {
            if (!Binding(input, false, out var revision, out var hash)) return BadInput();
            var outcome = await drafts.ExecuteAsync(User, academyId, conversationId, draftId, revision, hash, token);
            return ToResult(outcome.HttpStatus, outcome.Receipt, outcome.Error);
        });

    private async Task<ActionResult> Safely(Func<Task<ActionResult>> operation, string subject = "onboarding")
    {
        // Gate before adapter/store work. Authorization and the academy filter still
        // run independently; a flag never grants an actor or model permissions.
        if (!Available) return NotFound();
        HttpContext.Response.Headers.CacheControl = "no-store";
        HttpContext.Response.Headers.Pragma = "no-cache";
        try { return await operation(); }
        catch (Exception ex) when (ex is DbException or DbUpdateException or CryptographicException or InvalidOperationException)
        {
            // No payload, SQL, tokens, exception text or private values in the log.
            logger.LogWarning("PENTA {Workflow} could not verify completion ({FailureType}); trace {TraceId}",
                subject, ex.GetType().Name, HttpContext.TraceIdentifier);
            return StatusCode(503, new { message = $"PENTA could not verify this {subject} outcome. Do not create another request; reconcile the same draft or use the manual workspace." });
        }
    }
    private ActionResult Draft(PentaOnboardingDraftOutcome outcome) => ToResult(outcome.HttpStatus, outcome.Draft, outcome.Error);
    private ActionResult ToResult(int status, object? value, string? error) => status switch {
        403 => Forbid(IdentityConstants.BearerScheme), 404 => NotFound(),
        _ => StatusCode(status, value ?? new { message = error ?? "The onboarding request could not be completed." }) };
    private ActionResult BadInput() => BadRequest(new { message = "Enter only the expected onboarding fields and current draft revision." });

    private static bool Keys(JsonElement input, params string[] expected)
    {
        if (input.ValueKind != JsonValueKind.Object || input.GetRawText().Length > 12000) return false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in input.EnumerateObject())
            if (!seen.Add(item.Name) || !expected.Contains(item.Name, StringComparer.Ordinal)) return false;
        return seen.Count == expected.Length;
    }
    private static bool Integer(JsonElement input, string field, long minimum, long maximum, out long result)
    {
        result = 0;
        return input.GetProperty(field).ValueKind == JsonValueKind.Number &&
            input.GetProperty(field).TryGetInt64(out result) && result >= minimum && result <= maximum;
    }
    private static bool Binding(JsonElement input, bool creation, out long revision, out string hash)
    {
        revision = 0; hash = "";
        if (!Keys(input, creation ? ["revision", "payloadHash", "intent"] : ["revision", "payloadHash"]) ||
            !Integer(input, "revision", 1, 100, out revision) || input.GetProperty("payloadHash").ValueKind != JsonValueKind.String) return false;
        hash = input.GetProperty("payloadHash").GetString()!;
        return hash.Length == 64 && hash.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    }
}
