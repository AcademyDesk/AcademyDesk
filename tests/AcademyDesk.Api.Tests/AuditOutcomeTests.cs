using AcademyDesk.Api.Security;
using AcademyDesk.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace AcademyDesk.Api.Tests;

public sealed class AuditOutcomeTests
{
    [Theory]
    [InlineData(200, true)]
    [InlineData(201, true)]
    [InlineData(204, true)]
    [InlineData(400, false)]
    [InlineData(401, false)]
    [InlineData(403, false)]
    [InlineData(404, false)]
    [InlineData(409, false)]
    [InlineData(422, false)]
    [InlineData(429, false)]
    [InlineData(500, false)]
    [InlineData(503, false)]
    public void Pending_object_status_overrides_unexecuted_response(int status, bool expected)
    {
        var context = Context(new ObjectResult(new { message = "Synthetic outcome" }) { StatusCode = status });
        Assert.Equal(200, context.HttpContext.Response.StatusCode);
        Assert.Equal(expected, AcademyAccessFilter.ShouldAudit(context));
    }

    [Fact]
    public void Specialized_rejections_are_not_success_audits()
    {
        foreach (var result in new IActionResult[] { new BadRequestResult(), new NotFoundResult(), new ConflictResult(),
            new UnauthorizedResult(), new ForbidResult(), new ChallengeResult(), new ObjectResult(new ProblemDetails { Status = 503 }) })
            Assert.False(AcademyAccessFilter.ShouldAudit(Context(result)));
    }

    [Fact]
    public void Canceled_exception_and_written_failure_are_not_success_audits()
    {
        var canceled = Context(new OkResult()); canceled.Canceled = true;
        Assert.False(AcademyAccessFilter.ShouldAudit(canceled));
        var thrown = Context(new OkResult()); thrown.Exception = new InvalidOperationException("Synthetic");
        Assert.False(AcademyAccessFilter.ShouldAudit(thrown));
        thrown.ExceptionHandled = true;
        Assert.False(AcademyAccessFilter.ShouldAudit(thrown));
        var written = Context(new OkResult()); written.HttpContext.Response.StatusCode = 500;
        Assert.False(AcademyAccessFilter.ShouldAudit(written));
    }

    [Fact]
    public void Successful_empty_and_implicit_object_results_remain_auditable()
    {
        Assert.True(AcademyAccessFilter.ShouldAudit(Context(new EmptyResult())));
        Assert.True(AcademyAccessFilter.ShouldAudit(Context(new ObjectResult(new { id = Guid.NewGuid() }))));
        Assert.True(AcademyAccessFilter.ShouldAudit(Context(new NoContentResult())));
    }

    private static ActionExecutedContext Context(IActionResult result) => new(
        new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()), [], new object()) { Result = result };

    [Theory]
    [InlineData(typeof(StudentsController), "Create", true)]
    [InlineData(typeof(TeachersController), "Create", true)]
    [InlineData(typeof(StudentsController), "Update", true)]
    [InlineData(typeof(TeachersController), "Update", true)]
    [InlineData(typeof(ClassMediaUploadsController), "Complete", false)]
    [InlineData(typeof(InvoicesController), "Create", true)]
    public void Atomic_scope_is_action_level_not_controller_wide(Type controller, string method, bool expected)
    {
        var descriptor = new ControllerActionDescriptor { MethodInfo = controller.GetMethod(method)! };
        Assert.NotNull(descriptor.MethodInfo);
        Assert.Equal(expected, AcademyAccessFilter.UsesAtomicBoundary(controller.Name, descriptor));
    }

    [Fact]
    public void Unknown_action_descriptor_does_not_opt_in_non_finance_controller() =>
        Assert.False(AcademyAccessFilter.UsesAtomicBoundary(nameof(StudentsController), new ActionDescriptor()));

    [Fact]
    public void Opt_in_marker_cannot_be_applied_to_a_whole_controller()
    {
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(typeof(AtomicAcademyMutationAttribute), typeof(AttributeUsageAttribute))!;
        Assert.Equal(AttributeTargets.Method, usage.ValidOn);
        Assert.False(usage.Inherited);
        Assert.False(usage.AllowMultiple);
    }

    [Theory]
    [InlineData(typeof(StudentsController), "Create", false)]
    [InlineData(typeof(TeachersController), "Create", false)]
    [InlineData(typeof(StudentsController), "Update", true)]
    [InlineData(typeof(TeachersController), "Update", true)]
    [InlineData(typeof(ClassMediaUploadsController), "Complete", false)]
    [InlineData(typeof(InvoicesController), "Create", false)]
    public void Identity_enlistment_requires_explicit_action_opt_in(Type controller, string method, bool expected) =>
        Assert.Equal(expected, AcademyAccessFilter.IncludesIdentity(new ControllerActionDescriptor { MethodInfo = controller.GetMethod(method)! }));

    [Fact]
    public void Unknown_descriptor_does_not_enlist_Identity() => Assert.False(AcademyAccessFilter.IncludesIdentity(new ActionDescriptor()));
}
