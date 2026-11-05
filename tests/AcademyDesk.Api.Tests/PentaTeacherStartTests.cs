using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AcademyDesk.Api.Tests;

public sealed class PentaTeacherStartTests
{
    private static PentaTeacherCreationWorkflow Service(string environment="Testing",bool teacher=true,bool core=true,bool conversation=true,bool start=true)=>
        new(null!,null!,null!,TimeProvider.System,new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            ["Penta:TeacherCreationContractsEnabled"]=teacher.ToString(),["Penta:TeacherCoreContractsEnabled"]=core.ToString(),
            ["Penta:TeacherConversationContractsEnabled"]=conversation.ToString(),["Penta:TeacherStartContractsEnabled"]=start.ToString()}).Build(),new Host(environment),null!);
    [Theory]
    [InlineData("Production",true,true,true,true)] [InlineData("Development",true,true,true,true)] [InlineData("Staging",true,true,true,true)]
    [InlineData("Testing",false,true,true,true)] [InlineData("Testing",true,false,true,true)]
    [InlineData("Testing",true,true,false,true)] [InlineData("Testing",true,true,true,false)]
    public async Task Every_start_port_requires_Testing_and_all_four_flags(string environment,bool teacher,bool core,bool conversation,bool start){
        var service=Service(environment,teacher,core,conversation,start);var id=Guid.NewGuid();
        Assert.Equal(404,(await service.ConversationStartReadinessAsync(new(),id,default)).HttpStatus);
        Assert.Equal(404,(await service.ConversationStartAsync(new(),id,default,default)).HttpStatus);
        Assert.Equal(404,(await service.ConversationStartResultAsync(new(),id,id,default)).HttpStatus);
    }
    [Theory]
    [InlineData("{}")] [InlineData("null")] [InlineData("[]")] [InlineData("{\"requestId\":null}")] [InlineData("{\"requestId\":1}")]
    [InlineData("{\"requestId\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("{\"requestId\":\"AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA\"}")]
    [InlineData("{\"requestId\":\"11111111-1111-1111-1111-111111111111\",\"approved\":true}")]
    [InlineData("{\"requestId\":\"11111111-1111-1111-1111-111111111111\",\"requestId\":\"11111111-1111-1111-1111-111111111111\"}")]
    public async Task Strict_start_input_rejects_untrusted_or_malformed_fields_before_store(string input){
        using var json=JsonDocument.Parse(input);Assert.Equal(400,(await Service().ConversationStartAsync(new(),Guid.NewGuid(),json.RootElement,default)).HttpStatus);
    }
    [Fact]
    public async Task Empty_identifiers_are_rejected_before_store(){
        var service=Service();Assert.Equal(400,(await service.ConversationStartResultAsync(new(),Guid.NewGuid(),Guid.Empty,default)).HttpStatus);
        Assert.Equal(400,(await service.ConversationStartReadinessAsync(new(),Guid.Empty,default)).HttpStatus);
        Assert.Equal(400,(await service.ConversationStartAsync(new(),Guid.Empty,JsonSerializer.SerializeToElement(new{requestId=Guid.NewGuid()}),default)).HttpStatus);
    }
    private sealed class Host(string name):IHostEnvironment{
        public string EnvironmentName{get;set;}=name;public string ApplicationName{get;set;}="TeacherStartTest";
        public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
    }
}
