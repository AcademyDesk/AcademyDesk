using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaEnrollmentBrowserViewTests
{
    [Theory]
    [InlineData("Prepared",DateTimeKind.Unspecified)] [InlineData("Authorized",DateTimeKind.Unspecified)] [InlineData("Succeeded",DateTimeKind.Unspecified)]
    [InlineData("Prepared",DateTimeKind.Utc)] [InlineData("Authorized",DateTimeKind.Utc)] [InlineData("Succeeded",DateTimeKind.Utc)]
    public void Browser_JSON_retains_UTC_ticks_and_exact_approval_and_receipt(string status,DateTimeKind kind)
    {
        var expiry=new DateTime(2027,1,1,10,0,0,kind);var admission=new EnrollmentCreatePreview(Guid.NewGuid(),Guid.NewGuid(),new DateOnly(2027,1,1),"Active");
        var enrollment=new EnrollmentSummary(Guid.NewGuid(),admission.StudentId,admission.BatchId,admission.StartDate,null,admission.Status);
        var receipt=status=="Succeeded"?new PentaEnrollmentReceipt(Guid.NewGuid(),Guid.NewGuid(),enrollment,expiry.AddMinutes(-1)):null;
        var draft=new PentaEnrollmentDraftView(Guid.NewGuid(),1,0,status,expiry,admission,new string('a',64),status=="Prepared"?null:Guid.NewGuid(),receipt);
        var original=new PentaEnrollmentDraftOutcome(status=="Prepared"?201:200,draft);
        var value=PentaEnrollmentCreationWorkflow.BrowserView(original);
        Assert.Equal(original.HttpStatus,value.HttpStatus);Assert.Equal(draft.PayloadHash,value.Draft!.PayloadHash);Assert.Equal(draft.ApprovalId,value.Draft.ApprovalId);
        Assert.Same(admission,value.Draft.Admission);Assert.Equal(expiry.Ticks,value.Draft.ExpiresAtUtc.Ticks);Assert.Equal(DateTimeKind.Utc,value.Draft.ExpiresAtUtc.Kind);
        Assert.Equal(kind,original.Draft!.ExpiresAtUtc.Kind);
        using var json=JsonDocument.Parse(JsonSerializer.Serialize(value.Draft,new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.EndsWith("Z",json.RootElement.GetProperty("expiresAtUtc").GetString());
        if(receipt is not null){Assert.Same(enrollment,value.Draft.Receipt!.Enrollment);Assert.Equal(receipt.ReceiptId,value.Draft.Receipt.ReceiptId);Assert.Equal(receipt.AuditId,value.Draft.Receipt.AuditId);Assert.Equal(receipt.CompletedAtUtc.Ticks,value.Draft.Receipt.CompletedAtUtc.Ticks);Assert.EndsWith("Z",json.RootElement.GetProperty("receipt").GetProperty("completedAtUtc").GetString());}
        Assert.Equal(value,PentaEnrollmentCreationWorkflow.BrowserView(value));
    }
    [Theory][InlineData(403)][InlineData(409)][InlineData(410)][InlineData(503)]
    public void Error_outcomes_are_not_reinterpreted_as_drafts(int code)
    {
        var outcome=new PentaEnrollmentDraftOutcome(code,Error:"Synthetic refusal");Assert.Same(outcome,PentaEnrollmentCreationWorkflow.BrowserView(outcome));
    }
}
