using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaOnboardingCompletionInput(PentaOnboardingConversationInput Claim,string Kind,
    long BaseRevision,JsonElement Scope,PentaOnboardingPreparationInput? Preparation,
    PentaOnboardingLifecycleInput? Lifecycle,string Digest,string[]? Questions=null)
{
    public static bool TryRead(JsonElement root,out PentaOnboardingCompletionInput? result)
    {
        result=null;
        if(root.ValueKind!=JsonValueKind.Object || root.GetRawText().Length>24000) return false;
        var keys=new HashSet<string>(StringComparer.Ordinal);
        foreach(var property in root.EnumerateObject())
            if(!keys.Add(property.Name)||property.Name is not ("claim" or "decision"))return false;
        if(keys.Count!=2 || !PentaOnboardingConversationInput.TryRead(root.GetProperty("claim"),true,out var claim))return false;
        var decision=root.GetProperty("decision");
        if(decision.ValueKind!=JsonValueKind.Object || !decision.TryGetProperty("kind",out var kindValue)||kindValue.ValueKind!=JsonValueKind.String)return false;
        var kind=kindValue.GetString()!;
        PentaOnboardingPreparationInput? prep=null; PentaOnboardingLifecycleInput? lifecycle=null;
        if(PentaOnboardingNeutralInput.TryRead(decision,claim!.RequestId,out var neutral)) {
            if(neutral!.Revision!=claim.DraftRevision)return false;
            result=new(claim,neutral.Kind,neutral.Revision,neutral.Scope,null,null,
                NeutralDigest(neutral.Kind,neutral.Revision,neutral.Questions),neutral.Questions);return true;
        }
        var valid=kind switch {
            "clarify"=>PentaOnboardingPreparationInput.TryReadPartial(decision,claim!.RequestId,out prep),
            "host_prepare"=>PentaOnboardingPreparationInput.TryRead(decision,claim!.RequestId,out prep),
            "request_pause" or "request_resume" or "request_cancel"=>PentaOnboardingLifecycleInput.TryRead(decision,claim!.RequestId,out lifecycle),
            _=>false };
        if(!valid)return false;
        var revision=prep?.BaseRevision??lifecycle!.BaseRevision;
        if(revision!=claim!.DraftRevision)return false;
        var digest=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            kind,revision,payload=prep?.Payload,questions=prep?.Questions??[] }))));
        result=new(claim,kind,revision,prep?.Scope??lifecycle!.Scope,prep,lifecycle,digest,prep?.Questions);return true;
    }
    internal static string NeutralDigest(string kind,long revision,string[] questions)=>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{kind,revision,questions}))));
}
