using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOnboardingConversationTests
{
    private static JsonObject Input(bool claim) {
        var value=JsonSerializer.SerializeToNode(new { requestId=Guid.NewGuid(),expectedVersion=0,draftRevision=1,payloadHash=new string('a',64) })!.AsObject();
        if (claim) value["text"]="Continue this onboarding"; else value["draftId"]=Guid.NewGuid().ToString("D");
        return value;
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Exact_host_admission_has_no_model_authority_or_approval(bool claim)
    {
        Assert.True(PentaOnboardingConversationInput.TryRead(JsonSerializer.SerializeToElement(Input(claim)),claim,out var parsed));
        Assert.Equal(0,parsed!.ExpectedVersion); Assert.Equal(claim,parsed.Text is not null); Assert.Equal(!claim,parsed.DraftId is not null);
    }
    [Theory]
    [InlineData("authority")]
    [InlineData("tenant")]
    [InlineData("approval")]
    [InlineData("empty-id")]
    [InlineData("bad-id")]
    [InlineData("negative")]
    [InlineData("exhausted")]
    [InlineData("fractional")]
    [InlineData("revision")]
    [InlineData("hash")]
    [InlineData("empty-text")]
    [InlineData("long-text")]
    [InlineData("control-text")]
    [InlineData("extra-draft")]
    public void Unsafe_or_ambiguous_admission_is_rejected(string mutation)
    {
        var value=Input(true);
        switch(mutation) {
            case "authority":value["role"]="Owner";break;
            case "tenant":value["tenantId"]=Guid.NewGuid().ToString("D");break;
            case "approval":value["approved"]=true;break;
            case "empty-id":value["requestId"]=Guid.Empty.ToString("D");break;
            case "bad-id":value["requestId"]=Guid.NewGuid().ToString("N");break;
            case "negative":value["expectedVersion"]=-1;break;
            case "exhausted":value["expectedVersion"]=100;break;
            case "fractional":value["expectedVersion"]=0.5;break;
            case "revision":value["draftRevision"]=0;break;
            case "hash":value["payloadHash"]=new string('A',64);break;
            case "empty-text":value["text"]="  ";break;
            case "long-text":value["text"]=new string('x',2001);break;
            case "control-text":value["text"]="a\0b";break;
            case "extra-draft":value["draftId"]=Guid.NewGuid().ToString("D");break;
        }
        Assert.False(PentaOnboardingConversationInput.TryRead(JsonSerializer.SerializeToElement(value),true,out _));
    }
    [Fact]
    public void Duplicate_keys_and_wrong_mode_cannot_be_silently_adopted()
    {
        using var duplicate=JsonDocument.Parse(Input(true).ToJsonString().Insert(1,"\"expectedVersion\":1,"));
        Assert.False(PentaOnboardingConversationInput.TryRead(duplicate.RootElement,true,out _));
        Assert.False(PentaOnboardingConversationInput.TryRead(JsonSerializer.SerializeToElement(Input(true)),false,out _));
        Assert.False(PentaOnboardingConversationInput.TryRead(JsonSerializer.SerializeToElement(Input(false)),true,out _));
    }
    [Fact]
    public void Offline_migration_only_adds_nullable_protected_column_and_preserves_read_state()
    {
        using var db=new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=NeverConnected;Integrated Security=true;Connect Timeout=1").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var assembly=db.GetService<IMigrationsAssembly>();
        var migration=assembly.CreateMigration(assembly.Migrations.Single(x=>x.Key.EndsWith("_AddPentaOnboardingConversationState",StringComparison.Ordinal)).Value,"Microsoft.EntityFrameworkCore.SqlServer");
        var operation=Assert.IsType<AddColumnOperation>(Assert.Single(migration.UpOperations));
        Assert.Equal("PentaMiniSessions",operation.Table); Assert.Equal("ProtectedOnboardingState",operation.Name);
        Assert.True(operation.IsNullable); Assert.Equal(4000,operation.MaxLength);
        Assert.IsType<DropColumnOperation>(Assert.Single(migration.DownOperations));
    }
}
