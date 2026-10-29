using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaTeacherConversationInput(Guid RequestId,long DraftRevision,string PayloadHash,string Text)
{
    public static bool TryRead(JsonElement input,out PentaTeacherConversationInput? result)
    {
        result=null;
        if(input.ValueKind!=JsonValueKind.Object||input.GetRawText().Length>12000)return false;
        var keys=new HashSet<string>(StringComparer.Ordinal);foreach(var field in input.EnumerateObject())
            if(!keys.Add(field.Name)||field.Name is not ("requestId" or "draftRevision" or "payloadHash" or "text"))return false;
        if(keys.Count!=4 || input.GetProperty("requestId").ValueKind!=JsonValueKind.String ||
            !Guid.TryParseExact(input.GetProperty("requestId").GetString(),"D",out var request)||request==Guid.Empty||request.ToString("D")!=input.GetProperty("requestId").GetString()||
            input.GetProperty("draftRevision").ValueKind!=JsonValueKind.Number||!input.GetProperty("draftRevision").TryGetInt64(out var revision)||revision is <1 or >=100 ||
            input.GetProperty("payloadHash").ValueKind!=JsonValueKind.String||input.GetProperty("text").ValueKind!=JsonValueKind.String)return false;
        var hash=input.GetProperty("payloadHash").GetString()!;var text=input.GetProperty("text").GetString()!;
        if(hash.Length!=64||hash.Any(c=>c is not (>= '0' and <= '9' or >= 'a' and <= 'f'))||text.Length is <1 or >2000||string.IsNullOrWhiteSpace(text)||text.Any(c=>char.IsControl(c)&&c is not '\n' and not '\t'))return false;
        result=new(request,revision,hash,text);return true;
    }
}
