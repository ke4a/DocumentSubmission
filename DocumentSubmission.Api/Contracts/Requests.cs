using System.ComponentModel.DataAnnotations;
using DocumentSubmission.Domain.Submissions;

namespace DocumentSubmission.Api.Contracts;

// Value types are nullable so that [Required] can tell "missing" from a default value.

public sealed class LoginRequest
{
    [Required]
    public Guid? UserId { get; init; }
}

public sealed class CreateSubmissionRequest
{
    [Required, StringLength(Submission.MaxTitleLength)]
    public string? Title { get; init; }

    [Required]
    public Guid? ReviewerId { get; init; }

    [Required]
    public IFormFile? File { get; init; }

    [StringLength(Submission.MaxCommentLength)]
    public string? Comment { get; init; }
}

public sealed class ResubmitRequest
{
    [Required]
    public IFormFile? File { get; init; }

    [StringLength(Submission.MaxCommentLength)]
    public string? Comment { get; init; }
}

public sealed class ApproveRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? VersionNumber { get; init; }
}

public sealed class ReturnRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? VersionNumber { get; init; }

    [Required, StringLength(Submission.MaxCommentLength)]
    public string? Comment { get; init; }
}

public sealed class DistributeRequest
{
    [Required]
    public Guid? RecipientId { get; init; }
}
