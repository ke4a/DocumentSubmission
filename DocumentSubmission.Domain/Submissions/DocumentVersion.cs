namespace DocumentSubmission.Domain.Submissions;

/// <summary>Immutable metadata of one uploaded document. The bytes live in <see cref="DocumentContent"/>.</summary>
public sealed class DocumentVersion
{
    private DocumentVersion() { } // for EF Core

    internal DocumentVersion(Guid submissionId, int number, DocumentFile file, DateTime createdAt)
    {
        SubmissionId = submissionId;
        Number = number;
        FileName = file.FileName;
        ContentType = file.ContentType;
        Size = file.Content.LongLength;
        CreatedAt = createdAt;
        DocumentContent = new DocumentContent(submissionId, number, file.Content);
    }

    public Guid SubmissionId { get; private set; }
    public int Number { get; private set; }
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long Size { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DocumentContent DocumentContent { get; private set; } = null!;
}
