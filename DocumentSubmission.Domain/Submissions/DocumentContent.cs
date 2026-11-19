namespace DocumentSubmission.Domain.Submissions;

/// <summary>
/// The file bytes of one <see cref="DocumentVersion"/>, kept in their own table so that
/// list and detail queries never load them by accident.
/// </summary>
public sealed class DocumentContent
{
    private DocumentContent() { } // for EF Core

    internal DocumentContent(Guid submissionId, int versionNumber, byte[] content)
    {
        SubmissionId = submissionId;
        VersionNumber = versionNumber;
        Content = content;
    }

    public Guid SubmissionId { get; private set; }
    public int VersionNumber { get; private set; }
    public byte[] Content { get; private set; } = null!;
}
