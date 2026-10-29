using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaTeacherCreationFieldsTests
{
    private static string Payload(object fields,string tool=PentaTeacherCreationFields.Tool,int revision=1)=>
        JsonSerializer.Serialize(new{tool_key=tool,tool_revision=revision,fields});

    [Fact]
    public void Minimal_and_partial_drafts_keep_only_actual_required_fields()
    {
        Assert.True(PentaTeacherCreationFields.TryRead(Payload(new{}),out var partial));Assert.Equal("",partial!.FirstName);
        Assert.False(PentaTeacherCreationFields.HasRequiredFields(Payload(new{})));
        var payload=Payload(new{FirstName=new{kind="text",value="Synthetic"},LastName=new{kind="text",value="Teacher"}});
        Assert.True(PentaTeacherCreationFields.HasRequiredFields(payload));Assert.True(PentaTeacherCreationFields.TryRead(payload,out var result));
        Assert.Null(result!.Email);Assert.Null(result.DateOfBirth);Assert.Null(result.BranchId);Assert.Null(result.CompensationJson);Assert.Null(result.AvailabilityJson);Assert.Null(result.CertificationsJson);
    }

    [Theory]
    [InlineData("Role")]
    [InlineData("AcademyId")]
    [InlineData("UserName")]
    [InlineData("TemporaryPassword")]
    [InlineData("CompensationJson")]
    [InlineData("AvailabilityJson")]
    [InlineData("CertificationsJson")]
    [InlineData("CallbackUrl")]
    [InlineData("IsActive")]
    [InlineData("Unknown")]
    public void Unsupported_or_sensitive_fields_reject_the_entire_request_instead_of_silently_dropping(string key)
    {
        var fields=new Dictionary<string,object>{{key,new{kind="text",value="synthetic"}}};
        Assert.False(PentaTeacherCreationFields.TryRead(Payload(fields),out var request));Assert.Null(request);
    }

    [Theory]
    [InlineData("DateOfBirth","2026-02-30","date")]
    [InlineData("JoiningDate","2026-10-26T10:00:00Z","date")]
    [InlineData("DateOfBirth","10/26/2000","date")]
    [InlineData("BranchId","00000000-0000-0000-0000-000000000000","reference")]
    [InlineData("BranchId","11111111-1111-4111-8111-AAAAAAAAAAAA","reference")]
    [InlineData("FirstName","synthetic\nname","text")]
    [InlineData("FirstName","Synthetic","date")]
    public void Invalid_typed_values_reject(string key,string value,string kind)=>
        Assert.False(PentaTeacherCreationFields.TryRead(Payload(new Dictionary<string,object>{{key,new{kind,value}}}),out _));

    [Fact]
    public void Actual_allowed_field_types_round_trip_without_finance_or_account_data()
    {
        const string branch="11111111-1111-4111-8111-111111111111";
        var payload=Payload(new{FirstName=new{kind="text",value="Synthetic"},LastName=new{kind="text",value="Teacher"},
            DateOfBirth=new{kind="date",value="2000-01-02"},JoiningDate=new{kind="date",value="2026-11-01"},BranchId=new{kind="reference",value=branch},Qualifications=new{kind="text",value="Synthetic qualification"}});
        Assert.True(PentaTeacherCreationFields.TryRead(payload,out var request));Assert.Equal(new DateOnly(2000,1,2),request!.DateOfBirth);
        Assert.Equal(Guid.Parse(branch),request.BranchId);Assert.Equal(new DateOnly(2026,11,1),request.JoiningDate);Assert.Equal("Synthetic qualification",request.Qualifications);
    }

    [Theory]
    [InlineData("FirstName",121)]
    [InlineData("Phone",31)]
    [InlineData("Qualifications",1001)]
    [InlineData("Email",321)]
    public void Database_bounded_field_lengths_are_enforced(string key,int length)=>
        Assert.False(PentaTeacherCreationFields.TryRead(Payload(new Dictionary<string,object>{{key,new{kind="text",value=new string('x',length)}}}),out _));

    [Fact]
    public void Wrong_tool_revision_duplicate_keys_null_and_unwrapped_values_reject()
    {
        foreach(var payload in new[]{Payload(new{},"AcademyDesk.StudentOnboarding.Create"),Payload(new{},revision:2),"null","{}",
            "{\"tool_key\":\"AcademyDesk.Teacher.Create\",\"tool_revision\":1,\"fields\":{\"FirstName\":null}}",
            "{\"tool_key\":\"AcademyDesk.Teacher.Create\",\"tool_revision\":1,\"fields\":{\"FirstName\":{\"kind\":\"text\",\"value\":\"a\"},\"FirstName\":{\"kind\":\"text\",\"value\":\"b\"}}}",
            "{\"tool_key\":\"AcademyDesk.Teacher.Create\",\"tool_revision\":1,\"fields\":{\"FirstName\":\"Synthetic\"}}"})
            Assert.False(PentaTeacherCreationFields.TryRead(payload,out _));
    }
}
