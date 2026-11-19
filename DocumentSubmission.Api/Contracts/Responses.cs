using DocumentSubmission.Domain.Submissions;
using DocumentSubmission.Domain.Users;

namespace DocumentSubmission.Api.Contracts;

public sealed record LoginResponse(string Token, DateTime ExpiresAt);

public sealed record LoginUserResponse(Guid Id, string Name, Role Role, string CountryCode);

public sealed record PersonResponse(Guid Id, string Name);

public sealed record SubmissionSummaryResponse(
    Guid Id,
    string Title,
    SubmissionStatus Status,
    PersonResponse Editor,
    PersonResponse Reviewer,
    int CurrentVersionNumber,
    DateTime CreatedAt);

public sealed record SubmissionResponse(
    Guid Id,
    string Title,
    string CountryCode,
    SubmissionStatus Status,
    PersonResponse Editor,
    PersonResponse Reviewer,
    IReadOnlyList<DocumentVersionResponse> Versions,
    int? ApprovedVersionNumber,
    PersonResponse? Recipient,
    DateTime CreatedAt,
    DateTime? DistributedAt);

public sealed record DocumentVersionResponse(int Number, string FileName, string ContentType, long Size, DateTime CreatedAt);

public sealed record AuditEntryResponse(
    AuditAction Action,
    int VersionNumber,
    PersonResponse Actor,
    string? Comment,
    PersonResponse? Recipient,
    DateTime OccurredAt);

public sealed record DocumentDownload(string FileName, string ContentType, byte[] Content);
