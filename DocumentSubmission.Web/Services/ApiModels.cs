namespace DocumentSubmission.Web.Services;

// The JSON shapes of the API (see DocumentSubmission.Api/Contracts). Only the fields the UI uses.

public sealed record LoginResponse(string Token, DateTime ExpiresAt);

public sealed record Person(Guid Id, string Name);

public enum SubmissionStatus { Submitted, Returned, Approved, Distributed }

public enum AuditAction { Submitted, Resubmitted, Returned, Approved, Distributed }

public sealed record SubmissionSummary(
    Guid Id,
    string Title,
    SubmissionStatus Status,
    Person Editor,
    Person Reviewer,
    int CurrentVersionNumber,
    DateTime CreatedAt);

public sealed record Submission(
    Guid Id,
    string Title,
    string CountryCode,
    SubmissionStatus Status,
    Person Editor,
    Person Reviewer,
    List<DocumentVersion> Versions,
    int? ApprovedVersionNumber,
    Person? Recipient,
    DateTime CreatedAt,
    DateTime? DistributedAt)
{
    public int CurrentVersionNumber => Versions.Max(v => v.Number);
}

public sealed record DocumentVersion(int Number, string FileName, long Size, DateTime CreatedAt);

public sealed record AuditEntry(
    AuditAction Action,
    int VersionNumber,
    Person Actor,
    string? Comment,
    Person? Recipient,
    DateTime OccurredAt);
