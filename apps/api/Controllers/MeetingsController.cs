using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcademyDesk.Api.Security;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/meetings")]
public sealed class MeetingsController(MeetingReadinessService readiness, MeetingPreviewService preview, MeetingReceiptService receipts,
    MeetingOperationService operations) : ControllerBase
{
    [HttpGet("capabilities")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<MeetingCapabilities>> Capabilities(Guid academyId, CancellationToken token)
    {
        var decision = await readiness.ReadAsync(User, academyId, token);
        return decision.StatusCode switch
        {
            200 => Ok(decision.Capabilities),
            401 => Unauthorized(),
            _ => Forbid()
        };
    }

    // Preparation only: not a signed preview/approval receipt or a provider action.
    [HttpPost("draft-preview")]
    [RequestSizeLimit(32768)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<MeetingDraftPreview>> DraftPreview(Guid academyId, MeetingPreviewRequest request, CancellationToken token)
    {
        var decision = await preview.PrepareAsync(User, academyId, request, token);
        return decision.StatusCode switch
        {
            200 => Ok(decision.Draft),
            401 => Unauthorized(),
            403 => Forbid(),
            _ => BadRequest(new { code = decision.ReasonCode, message = "The meeting draft could not be prepared. Check its details and current academy records." })
        };
    }

    [HttpPost("preview-receipts")]
    [AtomicAcademyMutation(IncludePlatformOwner = true)]
    [RequestSizeLimit(32768)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<MeetingReceiptState>> PrepareReceipt(Guid academyId, MeetingPreviewRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken token)
        => ReceiptResult(await receipts.PrepareAsync(User, academyId, request, key, token));

    [HttpGet("preview-receipts/{receiptId:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<MeetingReceiptState>> GetReceipt(Guid academyId, Guid receiptId, CancellationToken token)
        => ReceiptResult(await receipts.GetAsync(User, academyId, receiptId, token));

    [HttpPost("preview-receipts/{receiptId:guid}/review")]
    [AtomicAcademyMutation(IncludePlatformOwner = true)]
    [RequestSizeLimit(4096)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<MeetingReceiptState>> ReviewReceipt(Guid academyId, Guid receiptId,
        ReviewMeetingDraftRequest request, CancellationToken token)
        => ReceiptResult(await receipts.ReviewAsync(User, academyId, receiptId, request, token));

    [HttpPost]
    [RequestSizeLimit(4096)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<MeetingOperationStatus>> RequestCreation(Guid academyId, MeetingAdmissionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken token)
        => OperationResult(await operations.RequestCreationAsync(User, academyId, request, key, token),
            "Meeting creation is unavailable or the draft is no longer valid. Reviewing a draft does not approve execution. Nothing was queued.");

    [HttpGet("operations/{operationId:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<MeetingOperationStatus>> GetOperation(Guid academyId, Guid operationId, CancellationToken token)
        => OperationResult(await operations.GetAsync(User, academyId, operationId, token),
            "The operation status is unavailable. No action was taken.");

    private ActionResult<MeetingOperationStatus> OperationResult(MeetingOperationOutcome result, string message) => result.StatusCode switch
    {
        200 => Ok(result.Operation),
        401 => Unauthorized(),
        403 => Forbid(),
        _ => StatusCode(result.StatusCode, new { code = result.Code, message, canExecute = false, canRetry = false })
    };

    private ActionResult<MeetingReceiptState> ReceiptResult(MeetingReceiptOutcome result) => result.StatusCode switch
    {
        200 => Ok(result.Receipt),
        201 => StatusCode(201, result.Receipt),
        401 => Unauthorized(),
        403 => Forbid(),
        _ => StatusCode(result.StatusCode, new { code = result.Code, message = "The draft receipt is unavailable or no longer valid. No meeting has been created." })
    };
}
