using DocumentSubmission.Api.Auth;
using DocumentSubmission.Api.Contracts;
using DocumentSubmission.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentSubmission.Api.Controllers;

/// <summary>
/// Request pipeline order: authentication (401) → role policy (403) → request validation (415/400,
/// done by [ApiController] and the upload validator) → visibility (404) → eligibility (400) → state (409).
/// </summary>
[ApiController]
[Route("submissions")]
[Authorize(Policy = Policies.Participant)]
public sealed class SubmissionsController(SubmissionService submissions) : ControllerBase
{
    /// <summary>Own (Editor) or assigned (Reviewer) submissions, newest first (R24).</summary>
    [HttpGet]
    public Task<IReadOnlyList<SubmissionSummaryResponse>> List(CancellationToken cancellationToken) =>
        submissions.ListAsync(User.GetUserId(), cancellationToken);

    /// <summary>Creates a submission and submits version 1 to the chosen reviewer, with an optional comment (R5–R7, R26).</summary>
    [HttpPost]
    [Authorize(Policy = Policies.Editor)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<SubmissionResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SubmissionResponse>> Create([FromForm] CreateSubmissionRequest request, CancellationToken cancellationToken)
    {
        var file = await DocumentUploadValidator.ReadAsync(request.File!, cancellationToken);
        var id = await submissions.CreateAsync(User.GetUserId(), request.Title!, request.ReviewerId!.Value, file, request.Comment, cancellationToken);
        var created = await submissions.GetAsync(id, User.GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, created);
    }

    /// <summary>Details including all version metadata (R25).</summary>
    [HttpGet("{id:guid}")]
    public Task<SubmissionResponse> Get(Guid id, CancellationToken cancellationToken) =>
        submissions.GetAsync(id, User.GetUserId(), cancellationToken);

    /// <summary>Uploads a new version of a returned submission, with an optional comment (R12, R26).</summary>
    [HttpPost("{id:guid}/resubmit")]
    [Authorize(Policy = Policies.Editor)]
    [Consumes("multipart/form-data")]
    public async Task<SubmissionResponse> Resubmit(Guid id, [FromForm] ResubmitRequest request, CancellationToken cancellationToken)
    {
        var file = await DocumentUploadValidator.ReadAsync(request.File!, cancellationToken);
        await submissions.ResubmitAsync(id, User.GetUserId(), file, request.Comment, cancellationToken);
        return await submissions.GetAsync(id, User.GetUserId(), cancellationToken);
    }

    /// <summary>Approves the named (current) version (R9, R10, R14).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Policies.Reviewer)]
    public async Task<SubmissionResponse> Approve(Guid id, ApproveRequest request, CancellationToken cancellationToken)
    {
        await submissions.ApproveAsync(id, User.GetUserId(), request.VersionNumber!.Value, cancellationToken);
        return await submissions.GetAsync(id, User.GetUserId(), cancellationToken);
    }

    /// <summary>Returns the named (current) version for revision, with a comment (R9–R11).</summary>
    [HttpPost("{id:guid}/return")]
    [Authorize(Policy = Policies.Reviewer)]
    public async Task<SubmissionResponse> Return(Guid id, ReturnRequest request, CancellationToken cancellationToken)
    {
        await submissions.ReturnAsync(id, User.GetUserId(), request.VersionNumber!.Value, request.Comment!, cancellationToken);
        return await submissions.GetAsync(id, User.GetUserId(), cancellationToken);
    }

    /// <summary>Records the recipient and completes the workflow (R18–R20).</summary>
    [HttpPost("{id:guid}/distribute")]
    [Authorize(Policy = Policies.Editor)]
    public async Task<SubmissionResponse> Distribute(Guid id, DistributeRequest request, CancellationToken cancellationToken)
    {
        await submissions.DistributeAsync(id, User.GetUserId(), request.RecipientId!.Value, cancellationToken);
        return await submissions.GetAsync(id, User.GetUserId(), cancellationToken);
    }

    /// <summary>Downloads the original bytes of a version (R15).</summary>
    [HttpGet("{id:guid}/versions/{versionNumber:int}/content")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public async Task<IActionResult> DownloadVersion(Guid id, int versionNumber, CancellationToken cancellationToken)
    {
        var download = await submissions.GetContentAsync(id, User.GetUserId(), versionNumber, cancellationToken);
        Response.Headers.XContentTypeOptions = "nosniff";
        // File(..., fileDownloadName) sets "Content-Disposition: attachment; filename=...".
        return File(download.Content, download.ContentType, download.FileName);
    }

    /// <summary>The audit trail in chronological order (R21, R23).</summary>
    [HttpGet("{id:guid}/audit")]
    public Task<IReadOnlyList<AuditEntryResponse>> Audit(Guid id, CancellationToken cancellationToken) =>
        submissions.GetAuditAsync(id, User.GetUserId(), cancellationToken);
}
