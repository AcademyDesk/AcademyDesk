using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PentaOnboardingController
{
    private const string TeacherAttendanceRoot = "~/api/teacher/penta/attendance";
    private const string TeacherAttendanceDraftRoute = TeacherAttendanceRoot + "/sessions/{sessionId:guid}/drafts/{requestId:guid}";

    [HttpGet(TeacherAttendanceRoot + "/readiness")]
    public Task<ActionResult> TeacherAttendanceReadiness([FromServices] PentaTeacherAttendanceWorkflow workflow,
        CancellationToken token) => Safely(async () =>
        {
            var result = await workflow.ReadinessAsync(User, token);
            return ToResult(result.Status, result.Value, result.Error);
        }, "teacher attendance readiness", workflow.Enabled);

    [HttpGet(TeacherAttendanceRoot + "/sessions")]
    public Task<ActionResult> TeacherAttendanceSessions([FromQuery] string? q, [FromServices] PentaTeacherAttendanceWorkflow workflow,
        CancellationToken token) => Safely(async () =>
        {
            var result = await workflow.SessionsAsync(User, q, token);
            return ToResult(result.Status, result.Value, result.Error);
        }, "teacher attendance session search", workflow.Enabled);

    [HttpGet(TeacherAttendanceRoot + "/sessions/{sessionId:guid}/roster")]
    public Task<ActionResult> TeacherAttendanceRoster(Guid sessionId, [FromQuery] string? q,
        [FromServices] PentaTeacherAttendanceWorkflow workflow, CancellationToken token) => Safely(async () =>
        {
            var result = await workflow.RosterAsync(User, sessionId, q, token);
            return ToResult(result.Status, result.Value, result.Error);
        }, "teacher attendance roster search", workflow.Enabled);

    [HttpPut(TeacherAttendanceDraftRoute), RequestSizeLimit(5 * 1024)]
    public Task<ActionResult> PrepareTeacherAttendance(Guid sessionId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaTeacherAttendanceWorkflow workflow, CancellationToken token) => Safely(async () =>
        {
            if (!Keys(input, "payload") || input.GetProperty("payload").ValueKind != JsonValueKind.Object)
                return BadRequest(new { message = "Enter the expected attendance payload." });
            var result = await workflow.RunAsync(User, sessionId, requestId, input.GetProperty("payload").GetRawText(), null, token);
            return ToResult(result.Status, result.Value, result.Error);
        }, "teacher attendance", workflow.Enabled);

    [HttpGet(TeacherAttendanceDraftRoute)]
    public Task<ActionResult> ReadTeacherAttendance(Guid sessionId, Guid requestId,
        [FromServices] PentaTeacherAttendanceWorkflow workflow, CancellationToken token) => Safely(async () =>
        {
            var result = await workflow.RunAsync(User, sessionId, requestId, null, null, token);
            return ToResult(result.Status, result.Value, result.Error);
        }, "teacher attendance", workflow.Enabled);

    [HttpPost(TeacherAttendanceDraftRoute + "/confirm"), RequestSizeLimit(1024)]
    public Task<ActionResult> ConfirmTeacherAttendance(Guid sessionId, Guid requestId, [FromBody] JsonElement input,
        [FromServices] PentaTeacherAttendanceWorkflow workflow, CancellationToken token) => Safely(async () =>
        {
            if (!Keys(input, "payloadHash", "intent") ||
                input.GetProperty("intent").ValueKind != JsonValueKind.String ||
                input.GetProperty("intent").GetString() != PentaTeacherAttendanceWorkflow.ConfirmIntent ||
                input.GetProperty("payloadHash").ValueKind != JsonValueKind.String ||
                input.GetProperty("payloadHash").GetString() is not { Length: 64 } hash ||
                hash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
                return BadRequest(new { message = "Confirm the exact reviewed attendance and InApp notice." });
            var result = await workflow.RunAsync(User, sessionId, requestId, null, hash, token);
            return ToResult(result.Status, result.Value, result.Error);
        }, "teacher attendance", workflow.Enabled);
}
