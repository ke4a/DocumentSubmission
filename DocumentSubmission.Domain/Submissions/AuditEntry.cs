using DocumentSubmission.Domain.Users;

namespace DocumentSubmission.Domain.Submissions;

/// <summary>Append-only record of one workflow action. Created only by <see cref="Submission"/>.</summary>
public sealed class AuditEntry
{
    private AuditEntry() { } // for EF Core

    internal AuditEntry(Guid submissionId, int versionNumber, AuditAction action, User actor,
        DateTime occurredAt, string? comment = null, Recipient? recipient = null)
    {
        Id = Guid.NewGuid();
        SubmissionId = submissionId;
        VersionNumber = versionNumber;
        Action = action;
        ActorId = actor.Id;
        Actor = actor;
        Comment = comment;
        RecipientId = recipient?.Id;
        Recipient = recipient;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public Guid SubmissionId { get; private set; }
    public int VersionNumber { get; private set; }
    public AuditAction Action { get; private set; }
    public Guid ActorId { get; private set; }
    public User Actor { get; private set; } = null!;
    public string? Comment { get; private set; }
    public Guid? RecipientId { get; private set; }
    public Recipient? Recipient { get; private set; }
    public DateTime OccurredAt { get; private set; }
}
