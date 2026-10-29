using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaEnrollmentFieldsTests
{
    private static readonly Guid Student=Guid.NewGuid(),Batch=Guid.NewGuid(),Enrollment=Guid.NewGuid();
    private static JsonObject Payload(string action)=>JsonSerializer.SerializeToNode(new{tool_key=action=="create"?PentaEnrollmentFields.CreateTool:action=="update"?PentaEnrollmentFields.UpdateTool:PentaEnrollmentFields.TransferTool,tool_revision=1,
        fields=action=="create"?new Dictionary<string,object>{["StudentId"]=new{kind="reference",value=Student.ToString("D")},["BatchId"]=new{kind="reference",value=Batch.ToString("D")}}:
            action=="update"?new Dictionary<string,object>{["EnrollmentId"]=new{kind="reference",value=Enrollment.ToString("D")},["Status"]=new{kind="text",value="Active"}}:
            new Dictionary<string,object>{["EnrollmentId"]=new{kind="reference",value=Enrollment.ToString("D")},["TargetBatchId"]=new{kind="reference",value=Batch.ToString("D")}}})!.AsObject();
    private static bool Read(string action,string raw)=>action=="create"?PentaEnrollmentFields.TryCreate(raw,out _):action=="update"?PentaEnrollmentFields.TryUpdate(raw,out _,out _):PentaEnrollmentFields.TryTransfer(raw,out _,out _);
    [Fact]
    public void Minimal_optional_dates_absent_and_explicit_lifecycle_reason_match_native_requests(){
        Assert.True(PentaEnrollmentFields.TryCreate(Payload("create").ToJsonString(),out var create));Assert.Equal(Student,create!.StudentId);Assert.Equal(Batch,create.BatchId);Assert.Null(create.StartDate);Assert.Equal("Active",create.Status);
        var update=Payload("update");update["fields"]!["Status"]!["value"]="Paused";Assert.False(PentaEnrollmentFields.TryUpdate(update.ToJsonString(),out _,out _));
        update["fields"]!["LifecycleReason"]=JsonSerializer.SerializeToNode(new{kind="text",value=" Synthetic reason "});Assert.True(PentaEnrollmentFields.TryUpdate(update.ToJsonString(),out var id,out var change));Assert.Equal(Enrollment,id);Assert.Equal("Synthetic reason",change!.LifecycleReason);
        Assert.True(PentaEnrollmentFields.TryTransfer(Payload("transfer").ToJsonString(),out id,out var transfer));Assert.Equal(Enrollment,id);Assert.Equal(Batch,transfer!.TargetBatchId);Assert.Null(transfer.TransferDate);
    }
    public static IEnumerable<object[]> InvalidCases()=>from action in new[]{"create","update","transfer"}from change in new[]{"authority","wrong_tool","revision","empty_id","missing_id","wrong_kind","null_value","invalid_date","duplicate"}select new object[]{action,change};
    [Theory] [MemberData(nameof(InvalidCases))]
    public void Unknown_privilege_ambiguous_or_malformed_payloads_rejected(string action,string change){
        var json=Payload(action);var fields=json["fields"]!.AsObject();var key=action=="create"?"StudentId":"EnrollmentId";
        switch(change){case "authority":fields["Role"]=JsonSerializer.SerializeToNode(new{kind="text",value="Owner"});break;case "wrong_tool":json["tool_key"]="ExecuteSql";break;case "revision":json["tool_revision"]=2;break;
            case "empty_id":fields[key]!["value"]=Guid.Empty.ToString("D");break;case "missing_id":fields.Remove(key);break;case "wrong_kind":fields[key]!["kind"]="text";break;case "null_value":fields[key]!["value"]=null;break;
            case "invalid_date":fields[action=="create"?"StartDate":action=="update"?"EndDate":"TransferDate"]=JsonSerializer.SerializeToNode(new{kind="date",value="2026-02-30"});break;}
        var raw=json.ToJsonString();if(change=="duplicate")raw=raw.Insert(1,"\"tool_revision\":1,");Assert.False(Read(action,raw));
    }
    [Theory] [InlineData("Paused")] [InlineData("Completed")] [InlineData("Unknown")] [InlineData("active")]
    public void Conversational_create_refuses_unrecognized_admission_intent_instead_of_silently_activating(string status){var p=Payload("create");p["fields"]!["Status"]=JsonSerializer.SerializeToNode(new{kind="text",value=status});Assert.False(PentaEnrollmentFields.TryCreate(p.ToJsonString(),out _));}
    [Theory] [InlineData("")] [InlineData("   ")] [InlineData("bad\nreason")]
    public void Lifecycle_change_requires_a_bounded_nonblank_reason(string reason){var p=Payload("update");p["fields"]!["Status"]!["value"]="Cancelled";p["fields"]!["LifecycleReason"]=JsonSerializer.SerializeToNode(new{kind="text",value=reason});Assert.False(PentaEnrollmentFields.TryUpdate(p.ToJsonString(),out _,out _));}
}
