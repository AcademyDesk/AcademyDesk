using System.Security.Claims;
using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaOnboardingInterpretation(Guid RequestId,string Text,JsonElement Snapshot,
    [property:System.Text.Json.Serialization.JsonIgnore] Func<CancellationToken,Task<JsonElement>>? Current=null);
/// <summary>Private interpretation only; implementations have no host execution
/// capability. No production interpreter is registered or qualified by this seam.</summary>
public interface IPentaOnboardingInterpreter
{
    Task<JsonElement> InterpretAsync(PentaOnboardingInterpretation input,CancellationToken token);
}
/// <summary>Explicit disposable-QA marker, never implemented by a real model
/// adapter. It cannot qualify a model or enable a production conversation.</summary>
public interface IPentaOnboardingSyntheticInterpreter : IPentaOnboardingInterpreter { }

public sealed partial class PentaOnboardingDraftService
{
    private static readonly SemaphoreSlim GatewayCapacity=new(2,2);
    private bool BrowserContractReady => CorePortsEnabled &&
        configuration.GetValue<bool>("Penta:OperationalOnboardingGatewayContractsEnabled") &&
        configuration.GetValue<bool>("Penta:OperationalOnboardingBrowserContractsEnabled") &&
        interpreter is IPentaOnboardingSyntheticInterpreter;
    public Task<PentaOnboardingCoreOutcome> CoreBrowserGatewayAsync(ClaimsPrincipal principal,Guid academy,
        Guid sessionId,JsonElement input,CancellationToken token) => !CorePortsEnabled
        ? Task.FromResult(new PentaOnboardingCoreOutcome(404)) : !BrowserContractReady
        ? Task.FromResult(new PentaOnboardingCoreOutcome(503)) : CoreGatewayAsync(principal,academy,sessionId,input,token);
    public async Task<PentaOnboardingCoreOutcome> CoreGatewayAsync(ClaimsPrincipal principal,Guid academy,
        Guid sessionId,JsonElement input,CancellationToken token)
    {
        if(!CorePortsEnabled||!configuration.GetValue<bool>("Penta:OperationalOnboardingGatewayContractsEnabled"))return new(404);
        if(!PentaOnboardingConversationInput.TryRead(input,true,out var claim))return new(400);
        // No runtime/interpreter selected by client/model/tier. Default is absent.
        if(interpreter is null)return new(503,Error:"Private conversational interpretation is not qualified or connected. Use the manual workspace.");
        if(!await GatewayCapacity.WaitAsync(0,token))return new(503,Error:"PENTA is occupied. Reconcile pending work before trying a new request.");
        Task<JsonElement>? task=null;
        try {
            var admitted=await CoreConversationAsync(principal,academy,sessionId,"claim",input,token);
            if(admitted.HttpStatus!=202||admitted.PlanningSnapshot is not {} snapshot)return admitted;
            // All claim/Identity transactions have ended before private planning.
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var planningToken=deadline.Token;
            JsonElement decision=default;string? failure=null;
            try {
                var actor=new ClaimsPrincipal(principal);
                async Task<JsonElement> Current(CancellationToken callbackToken) {
                    planningToken.ThrowIfCancellationRequested();
                    if(gatewayScopes is null)throw new HttpRequestException("Private context unavailable.");
                    using var linked=CancellationTokenSource.CreateLinkedTokenSource(callbackToken,planningToken);
                    await using var scope=gatewayScopes.CreateAsyncScope();
                    return await scope.ServiceProvider.GetRequiredService<PentaOnboardingDraftService>()
                        .ClaimedContextAsync(actor,academy,sessionId,claim!,snapshot,linked.Token);
                }
                task=interpreter.InterpretAsync(new(claim!.RequestId,claim.Text!,snapshot,Current),deadline.Token);
                decision=await task.WaitAsync(deadline.Token);
            }catch(Exception ex)when(ex is HttpRequestException or IOException or OperationCanceledException or JsonException) {
                token.ThrowIfCancellationRequested();
                failure=ex is JsonException?"invalid_response":"unavailable";
            }
            token.ThrowIfCancellationRequested();
            var envelope=JsonSerializer.SerializeToElement(new{claim=input,decision=decision.ValueKind==JsonValueKind.Undefined?(JsonElement?)null:decision});
            if(failure is null && !PentaOnboardingCompletionInput.TryRead(envelope,out _))failure="invalid_response";
            return await CoreTurnBoundAsync(principal,academy,sessionId,claim!.RequestId,
                failure is null?envelope:input,false,token,snapshot,failure);
        }finally {
            // A timed-out/abandoned planner retains capacity until it really exits.
            // Late results are observed/discarded, never completed or redispatched.
            if(task is {IsCompleted:false})_ = task.ContinueWith(done=>{_ = done.Exception;GatewayCapacity.Release();},
                CancellationToken.None,TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
            else {if(task is {IsFaulted:true})_ = task.Exception;GatewayCapacity.Release();}
        }
    }
    private static bool GatewayContextUnchanged(JsonElement before,JsonElement after)=>
        new[]{"scope","catalog_revision","tools","draft","evidence","availability"}.All(key=>
            before.TryGetProperty(key,out var left)&&after.TryGetProperty(key,out var right)&&JsonElement.DeepEquals(left,right))&&
        after.GetProperty("availability").GetString()=="ready";
}
