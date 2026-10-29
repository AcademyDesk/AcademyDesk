using System.Security.Claims;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AcademyDesk.Api.Tests;

public sealed class PentaTeacherCreationWorkflowTests
{
    [Theory]
    [InlineData("Production",true)]
    [InlineData("Development",true)]
    [InlineData("Staging",true)]
    [InlineData("Testing",false)]
    public async Task Every_workflow_port_refuses_outside_independent_testing_gate_before_store_access(string name,bool flag)
    {
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Penta:TeacherCreationContractsEnabled"]=flag.ToString()}).Build();
        var workflow=new PentaTeacherCreationWorkflow(null!,null!,null!,TimeProvider.System,config,new Host(name),null!);
        var user=new ClaimsPrincipal();var id=Guid.NewGuid();var binding=new PentaTeacherCreationBinding(1,new string('a',64),id);
        foreach(var operation in new Func<Task<PentaTeacherCreationOutcome>>[]{
            ()=>workflow.SaveAsync(user,id,id,id,0,0,"{}",default),()=>workflow.ReadAsync(user,id,id,id,default),
            ()=>workflow.ReviewAsync(user,id,id,id,binding,default),()=>workflow.ApproveAsync(user,id,id,id,binding,PentaTeacherCreationWorkflow.CreationIntent,default),
            ()=>workflow.ExecuteAsync(user,id,id,id,binding,default),()=>workflow.CancelAsync(user,id,id,id,binding,default)})
        {var result=await operation();Assert.Equal(404,result.HttpStatus);Assert.Null(result.Draft);}
    }
    [Fact]
    public async Task Enabled_workflow_still_requires_real_idle_same_store_SQL_contexts()
    {
        await using var db=new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await using var identity=new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Penta:TeacherCreationContractsEnabled"]="true"}).Build();
        var workflow=new PentaTeacherCreationWorkflow(db,identity,new EphemeralDataProtectionProvider(),TimeProvider.System,config,new Host("Testing"),new TeacherCreationService(db,identity,null!));
        var id=Guid.NewGuid();var result=await workflow.ReadAsync(new ClaimsPrincipal(),id,id,id,default);
        Assert.Equal(503,result.HttpStatus);Assert.Empty(db.ChangeTracker.Entries());Assert.Empty(identity.ChangeTracker.Entries());
    }
    [Theory]
    [InlineData(-1,0)] [InlineData(100,0)] [InlineData(0,-1)] [InlineData(0,9007199254740992)]
    public async Task Invalid_revision_and_context_refuse_before_database_access(long revision,long context)
    {
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Penta:TeacherCreationContractsEnabled"]="true"}).Build();
        var workflow=new PentaTeacherCreationWorkflow(null!,null!,null!,TimeProvider.System,config,new Host("Testing"),null!);
        var id=Guid.NewGuid();var result=await workflow.SaveAsync(new ClaimsPrincipal(),id,id,id,revision,context,"{}",default);
        Assert.Equal(400,result.HttpStatus);
    }
    private sealed class Host(string name):IHostEnvironment
    {
        public string EnvironmentName{get;set;}=name;public string ApplicationName{get;set;}="Synthetic QA";
        public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
    }
}
