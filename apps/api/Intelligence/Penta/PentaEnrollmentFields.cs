using System.Globalization;
using System.Text.Json;
using AcademyDesk.Api.Controllers;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>Inactive typed enrollment adapters. Parsing is not authority,
/// preparation, approval, duplicate checking or a tool registration.</summary>
public static class PentaEnrollmentFields
{
    public const string CreateTool="AcademyDesk.Enrollment.Create";
    public const string UpdateTool="AcademyDesk.Enrollment.Update";
    public const string TransferTool="AcademyDesk.Enrollment.Transfer";
    public static bool TryCreate(string? payload,out CreateEnrollmentRequest? request)
    {
        request=null;if(!Read(payload,CreateTool,["StudentId","BatchId","StartDate","Status"],out var fields))return false;
        if(!Reference(fields,"StudentId",true,out var student)||!Reference(fields,"BatchId",true,out var batch)||!Date(fields,"StartDate",out var start)||!Text(fields,"Status",30,out var status))return false;
        status??="Active";if(status is not ("Active" or "Waitlisted"))return false;
        request=new(student!.Value,batch!.Value,start,status);return true;
    }
    public static bool TryUpdate(string? payload,out Guid enrollment,out UpdateEnrollmentRequest? request)
    {
        enrollment=Guid.Empty;request=null;if(!Read(payload,UpdateTool,["EnrollmentId","Status","EndDate","LifecycleReason"],out var fields))return false;
        if(!Reference(fields,"EnrollmentId",true,out var id)||!Date(fields,"EndDate",out var end)||!Text(fields,"Status",30,out var status)||!Text(fields,"LifecycleReason",1000,out var reason))return false;
        if(status is not ("Active" or "Waitlisted" or "Completed" or "Withdrawn" or "Cancelled" or "Paused")||status!="Active"&&string.IsNullOrWhiteSpace(reason))return false;
        enrollment=id!.Value;request=new(status,end,reason);return true;
    }
    public static bool TryTransfer(string? payload,out Guid enrollment,out TransferEnrollmentRequest? request)
    {
        enrollment=Guid.Empty;request=null;if(!Read(payload,TransferTool,["EnrollmentId","TargetBatchId","TransferDate"],out var fields))return false;
        if(!Reference(fields,"EnrollmentId",true,out var id)||!Reference(fields,"TargetBatchId",true,out var target)||!Date(fields,"TransferDate",out var date))return false;
        enrollment=id!.Value;request=new(target!.Value,date);return true;
    }
    private static bool Read(string? raw,string tool,string[] allowed,out JsonElement fields)
    {
        fields=default;if(raw is null||raw.Length>8000)return false;
        try{using var doc=JsonDocument.Parse(PentaOperationalPayload.Canonicalize(raw));var root=doc.RootElement;
            if(root.GetProperty("tool_key").GetString()!=tool||root.GetProperty("tool_revision").GetInt64()!=1)return false;
            fields=root.GetProperty("fields").Clone();return fields.EnumerateObject().All(x=>allowed.Contains(x.Name,StringComparer.Ordinal));
        }catch(Exception ex)when(ex is ArgumentException or JsonException or InvalidOperationException or FormatException){return false;}
    }
    private static bool Reference(JsonElement fields,string key,bool required,out Guid? result)
    {
        result=null;if(!fields.TryGetProperty(key,out var field))return !required;
        if(field.GetProperty("kind").GetString()!="reference"||field.GetProperty("value").ValueKind!=JsonValueKind.String)return false;
        var text=field.GetProperty("value").GetString();if(!Guid.TryParseExact(text,"D",out var id)||id==Guid.Empty||text!=id.ToString("D"))return false;result=id;return true;
    }
    private static bool Date(JsonElement fields,string key,out DateOnly? result)
    {
        result=null;if(!fields.TryGetProperty(key,out var field))return true;
        if(field.GetProperty("kind").GetString()!="date"||field.GetProperty("value").ValueKind!=JsonValueKind.String||!DateOnly.TryParseExact(field.GetProperty("value").GetString(),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date))return false;
        result=date;return true;
    }
    private static bool Text(JsonElement fields,string key,int max,out string? result)
    {
        result=null;if(!fields.TryGetProperty(key,out var field))return true;
        if(field.GetProperty("kind").GetString()!="text"||field.GetProperty("value").ValueKind!=JsonValueKind.String)return false;
        var text=field.GetProperty("value").GetString()!;if(text.Length>max||text.Any(char.IsControl))return false;result=text.Trim();return true;
    }
}
