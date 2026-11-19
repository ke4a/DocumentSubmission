using DocumentSubmission.Domain.Exceptions;
using DocumentSubmission.Domain.Users;

namespace DocumentSubmission.Domain.Submissions;

/// <summary>
/// The workflow record for one document. All state changes go through the methods below,
/// which check the workflow rules, write the audit entry and call <see cref="Transition"/>.
/// </summary>
public sealed class Submission
{
    public const int MaxTitleLength = 200;
    public const int MaxCommentLength = 2000;

    private readonly List<DocumentVersion> _versions = [];
    private readonly List<AuditEntry> _auditEntries = [];

    private Submission() { } // for EF Core

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string CountryCode { get; private set; } = null!;
    public Guid EditorId { get; private set; }
    public User Editor { get; private set; } = null!;
    public Guid ReviewerId { get; private set; }
    public User Reviewer { get; private set; } = null!;
    public SubmissionStatus Status { get; private set; }
    public int? ApprovedVersionNumber { get; private set; }
    public Guid? RecipientId { get; private set; }
    public Recipient? Recipient { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DistributedAt { get; private set; }

    /// <summary>
    /// Optimistic concurrency token: EF Core adds "WHERE ConcurrencyToken = &lt;value when loaded&gt;"
    /// to every UPDATE, so of two requests that loaded the same state only the first can save.
    /// </summary>
    public int ConcurrencyToken { get; private set; }

    public IReadOnlyCollection<DocumentVersion> Versions => _versions;
    public IReadOnlyCollection<AuditEntry> AuditEntries => _auditEntries;

    /// <summary>The highest version number. Requires <see cref="Versions"/> to be loaded.</summary>
    public int CurrentVersionNumber => _versions.Max(v => v.Number);

    public static Submission Create(User editor, User reviewer, string title, DocumentFile file, string? comment, DateTime now)
    {
        if (editor.Role != Role.Editor)
            throw new NotAllowedException("Only editors can create submissions.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > MaxTitleLength)
            throw new InvalidInputException($"Title is required and must be at most {MaxTitleLength} characters.");
        if (reviewer.Role != Role.Reviewer || reviewer.CountryCode != editor.CountryCode)
            throw new InvalidInputException("The selected reviewer is not a reviewer in the editor's country.");
        comment = OptionalComment(comment);

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            CountryCode = editor.CountryCode,
            EditorId = editor.Id,
            Editor = editor,
            ReviewerId = reviewer.Id,
            Reviewer = reviewer,
            CreatedAt = now,
        };
        var version = submission.AddVersion(file, now);
        submission.Transition(SubmissionStatus.Submitted, new AuditEntry(submission.Id, version.Number, AuditAction.Submitted, editor, now, comment: comment));
        return submission;
    }

    public void Resubmit(User editor, DocumentFile file, string? comment, DateTime now)
    {
        EnsureIsCreatingEditor(editor);
        EnsureStatus(SubmissionStatus.Returned, "resubmitted");
        comment = OptionalComment(comment);

        var version = AddVersion(file, now);
        Transition(SubmissionStatus.Submitted, new AuditEntry(Id, version.Number, AuditAction.Resubmitted, editor, now, comment: comment));
    }

    public void Approve(User reviewer, int versionNumber, DateTime now)
    {
        EnsureCanDecide(reviewer, versionNumber);

        ApprovedVersionNumber = versionNumber;
        Transition(SubmissionStatus.Approved, new AuditEntry(Id, versionNumber, AuditAction.Approved, reviewer, now));
    }

    public void Return(User reviewer, int versionNumber, string comment, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(comment) || comment.Length > MaxCommentLength)
            throw new InvalidInputException($"A comment is required and must be at most {MaxCommentLength} characters.");
        EnsureCanDecide(reviewer, versionNumber);

        Transition(SubmissionStatus.Returned, new AuditEntry(Id, versionNumber, AuditAction.Returned, reviewer, now, comment: comment));
    }

    public void Distribute(User editor, Recipient recipient, DateTime now)
    {
        EnsureIsCreatingEditor(editor);
        if (recipient.CountryCode != CountryCode)
            throw new InvalidInputException("The selected recipient does not belong to the submission's country.");
        EnsureStatus(SubmissionStatus.Approved, "distributed");

        RecipientId = recipient.Id;
        Recipient = recipient;
        DistributedAt = now;
        Transition(SubmissionStatus.Distributed,
            new AuditEntry(Id, ApprovedVersionNumber!.Value, AuditAction.Distributed, editor, now, recipient: recipient));
    }

    /// <summary>The single place where the status changes: records the audit entry and bumps the concurrency token.</summary>
    private void Transition(SubmissionStatus newStatus, AuditEntry auditEntry)
    {
        Status = newStatus;
        _auditEntries.Add(auditEntry);
        ConcurrencyToken++;
    }

    private DocumentVersion AddVersion(DocumentFile file, DateTime now)
    {
        var number = _versions.Count == 0 ? 1 : CurrentVersionNumber + 1;
        var version = new DocumentVersion(Id, number, file, now);
        _versions.Add(version);
        return version;
    }

    private void EnsureIsCreatingEditor(User editor)
    {
        if (editor.Id != EditorId)
            throw new NotAllowedException("Only the editor who created the submission can do this.");
    }

    private void EnsureCanDecide(User reviewer, int versionNumber)
    {
        if (reviewer.Id != ReviewerId || reviewer.Role != Role.Reviewer || reviewer.CountryCode != CountryCode)
            throw new NotAllowedException("Only the assigned reviewer can approve or return the submission.");
        if (versionNumber < 1)
            throw new InvalidInputException("The version number must be at least 1.");
        EnsureStatus(SubmissionStatus.Submitted, "approved or returned");
        if (versionNumber != CurrentVersionNumber)
            throw new InvalidStateException(
                $"Version {versionNumber} is not the current version ({CurrentVersionNumber}). Reload the submission and review the current version.");
    }

    private void EnsureStatus(SubmissionStatus required, string action)
    {
        if (Status != required)
            throw new InvalidStateException($"A submission can only be {action} while it is {required}; it is {Status}.");
    }

    /// <summary>An optional comment: blank means none; otherwise at most <see cref="MaxCommentLength"/> characters.</summary>
    private static string? OptionalComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return null;
        if (comment.Length > MaxCommentLength)
            throw new InvalidInputException($"A comment must be at most {MaxCommentLength} characters.");
        return comment;
    }
}
