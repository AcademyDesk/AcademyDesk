using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

internal sealed record PentaTeacherCoreDecision(Guid RequestId,long BaseRevision,JsonElement Scope,string Payload,string Kind)
{
    internal static bool TryRead(JsonElement root,Guid request,bool partial,out PentaTeacherCoreDecision? result)
    {
        result=null;
        try{
            Exact(root,"request_ref","scope","base_revision","catalog_revision","kind","candidates","questions","invalidates_review");
            if(root.GetRawText().Length>16000 || request==Guid.Empty || root.GetProperty("request_ref").GetString()!=request.ToString("D") ||
                root.GetProperty("catalog_revision").GetInt64()!=1 || !root.GetProperty("invalidates_review").GetBoolean() ||
                root.GetProperty("kind").GetString()!=(partial?"clarify":"host_prepare"))return false;
            var revision=root.GetProperty("base_revision").GetInt64();if(revision is <1 or >=100)return false;
            var scope=root.GetProperty("scope");ScopeShape(scope);
            var questions=root.GetProperty("questions");if(questions.ValueKind!=JsonValueKind.Array || (partial?questions.GetArrayLength() is <1 or >3:questions.GetArrayLength()!=0))return false;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var q in questions.EnumerateArray())if(q.ValueKind!=JsonValueKind.String || !PentaTeacherCreationFields.Names.Contains(q.GetString()!,StringComparer.Ordinal)||!seen.Add(q.GetString()!))return false;
            var candidates=root.GetProperty("candidates");if(candidates.ValueKind!=JsonValueKind.Array || candidates.GetArrayLength()!=1)return false;
            var step=candidates[0];Exact(step,"tool_key","edit");if(step.GetProperty("tool_key").GetString()!=PentaTeacherCreationFields.Tool)return false;
            var edit=step.GetProperty("edit");Exact(edit,"mode","set","remove");
            if(edit.GetProperty("mode").GetString()!="replace" || edit.GetProperty("remove").ValueKind!=JsonValueKind.Array || edit.GetProperty("remove").GetArrayLength()!=0)return false;
            var payload=PentaOperationalPayload.Canonicalize(JsonSerializer.Serialize(new{tool_key=PentaTeacherCreationFields.Tool,tool_revision=1,fields=edit.GetProperty("set")}));
            if(!PentaTeacherCreationFields.TryRead(payload,out _))return false;
            result=new(request,revision,scope.Clone(),payload,partial?"partial":"prepare");return true;
        }catch(Exception ex)when(ex is ArgumentException or InvalidOperationException or KeyNotFoundException or JsonException or FormatException){return false;}
    }
    internal static bool TryBinding(JsonElement root,bool execute,out JsonElement scope,out string operation,out PentaTeacherCreationBinding? binding)
    {
        scope=default;operation="";binding=null;
        try{
            Exact(root,execute?["scope","operation_ref","revision","payload_hash","approval_ref"]:["scope","operation_ref","revision","payload_hash"]);
            scope=root.GetProperty("scope");ScopeShape(scope);operation=root.GetProperty("operation_ref").GetString()!;
            if(operation is null || operation.Length is <1 or >128)return false;
            var rev=root.GetProperty("revision").GetInt64();var hash=root.GetProperty("payload_hash").GetString();
            if(rev is <1 or >100 || hash is not{Length:64} || hash.Any(c=>c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))return false;
            var approval=Guid.Empty;if(execute && (root.GetProperty("approval_ref").ValueKind!=JsonValueKind.String ||
                !Guid.TryParseExact(root.GetProperty("approval_ref").GetString(),"D",out approval)||approval==Guid.Empty))return false;
            scope=scope.Clone();binding=new(rev,hash,approval);return true;
        }catch(Exception ex)when(ex is ArgumentException or InvalidOperationException or KeyNotFoundException or JsonException or FormatException){return false;}
    }
    private static void ScopeShape(JsonElement scope)
    {
        Exact(scope,"tenant_id","actor_id","connection_id","conversation_id","authority_version","context_version");
        foreach(var name in new[]{"tenant_id","actor_id","connection_id","conversation_id"})
            if(scope.GetProperty(name).ValueKind!=JsonValueKind.String || scope.GetProperty(name).GetString()!.Length is <1 or >128)throw new ArgumentException("Invalid teacher scope.");
        if(scope.GetProperty("authority_version").GetInt64()!=1 || scope.GetProperty("context_version").GetInt64() is <1 or >9007199254740991)throw new ArgumentException("Invalid teacher scope.");
    }
    private static void Exact(JsonElement root,params string[] fields)
    {
        if(root.ValueKind!=JsonValueKind.Object)throw new ArgumentException("Invalid teacher Core input.");var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var p in root.EnumerateObject())if(!seen.Add(p.Name)||!fields.Contains(p.Name,StringComparer.Ordinal))throw new ArgumentException("Invalid teacher Core input.");
        if(seen.Count!=fields.Length)throw new ArgumentException("Invalid teacher Core input.");
    }
}
