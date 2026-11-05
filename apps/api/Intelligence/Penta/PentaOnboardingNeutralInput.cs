using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaOnboardingNeutralInput(long Revision,JsonElement Scope,string Kind,string[] Questions)
{
    public static bool TryRead(JsonElement root,Guid request,out PentaOnboardingNeutralInput? result)
    {
        result=null;
        try {
            string[] keys=["request_ref","scope","base_revision","catalog_revision","kind","candidates","questions","invalidates_review"];
            if(root.ValueKind!=JsonValueKind.Object||root.GetRawText().Length>2048)return false;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var property in root.EnumerateObject())if(!seen.Add(property.Name)||!keys.Contains(property.Name,StringComparer.Ordinal))return false;
            if(seen.Count!=keys.Length||request==Guid.Empty||root.GetProperty("request_ref").GetString()!=request.ToString("D")||
                root.GetProperty("catalog_revision").GetInt64()!=1||root.GetProperty("invalidates_review").ValueKind!=JsonValueKind.False||
                root.GetProperty("candidates").ValueKind!=JsonValueKind.Array||root.GetProperty("candidates").GetArrayLength()!=0)return false;
            var kind=root.GetProperty("kind").GetString();var revision=root.GetProperty("base_revision").GetInt64();
            if(kind is not ("unsupported" or "clarify")||revision is <1 or >=100)return false;
            var questions=root.GetProperty("questions");
            if(questions.ValueKind!=JsonValueKind.Array||(kind=="unsupported"?questions.GetArrayLength()!=0:questions.GetArrayLength() is <1 or >3))return false;
            var names=new List<string>();
            foreach(var question in questions.EnumerateArray()) {
                if(question.ValueKind!=JsonValueKind.String||!PentaOnboardingFields.Names.Contains(question.GetString()!,StringComparer.Ordinal)||names.Contains(question.GetString()!,StringComparer.Ordinal))return false;
                names.Add(question.GetString()!);
            }
            var scope=root.GetProperty("scope");string[] scopeKeys=["tenant_id","actor_id","connection_id","conversation_id","authority_version","context_version"];
            if(scope.ValueKind!=JsonValueKind.Object)return false;seen.Clear();
            foreach(var property in scope.EnumerateObject())if(!seen.Add(property.Name)||!scopeKeys.Contains(property.Name,StringComparer.Ordinal))return false;
            if(seen.Count!=scopeKeys.Length)return false;
            foreach(var name in scopeKeys.Take(4))if(scope.GetProperty(name).ValueKind!=JsonValueKind.String||scope.GetProperty(name).GetString()!.Length is <1 or >128)return false;
            if(scope.GetProperty("authority_version").GetInt64()!=1||scope.GetProperty("context_version").GetInt64() is <1 or >9007199254740991)return false;
            result=new(revision,scope.Clone(),kind,names.ToArray());return true;
        }catch(Exception ex)when(ex is ArgumentException or InvalidOperationException or KeyNotFoundException or JsonException or FormatException){return false;}
    }
}
