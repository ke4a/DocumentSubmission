using DocumentSubmission.Api.Contracts;
using DocumentSubmission.Domain.Exceptions;
using DocumentSubmission.Domain.Submissions;
using DocumentSubmission.Domain.Users;
using DocumentSubmission.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DocumentSubmission.Api.Services;

/// <summary>
/// Thin use cases: load what is needed, call the <see cref="Submission"/> method that holds the rule,
/// save. Queries are no-tracking projections that never touch the file bytes (N5).
/// </summary>
public sealed class SubmissionService(AppDbContext db)
{
    public async Task<IReadOnlyList<SubmissionSummaryResponse>> ListAsync(Guid callerId, CancellationToken cancellationToken) =>
        await db.Submissions.AsNoTracking()
            .Where(s => s.EditorId == callerId || s.ReviewerId == callerId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SubmissionSummaryResponse(
                s.Id,
                s.Title,
                s.Status,
                new PersonResponse(s.Editor.Id, s.Editor.Name),
                new PersonResponse(s.Reviewer.Id, s.Reviewer.Name),
                s.Versions.Max(v => v.Number),
                s.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<SubmissionResponse> GetAsync(Guid id, Guid callerId, CancellationToken cancellationToken) =>
        await Visible(db.Submissions.AsNoTracking(), id, callerId)
            .Select(s => new SubmissionResponse(
                s.Id,
                s.Title,
                s.CountryCode,
                s.Status,
                new PersonResponse(s.Editor.Id, s.Editor.Name),
                new PersonResponse(s.Reviewer.Id, s.Reviewer.Name),
                s.Versions
                    .OrderBy(v => v.Number)
                    .Select(v => new DocumentVersionResponse(v.Number, v.FileName, v.ContentType, v.Size, v.CreatedAt))
                    .ToList(),
                s.ApprovedVersionNumber,
                s.Recipient == null ? null : new PersonResponse(s.Recipient.Id, s.Recipient.Name),
                s.CreatedAt,
                s.DistributedAt))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw SubmissionNotFound();

    public async Task<Guid> CreateAsync(Guid callerId, string title, Guid reviewerId, DocumentFile file, string? comment, CancellationToken cancellationToken)
    {
        var editor = await GetCallerAsync(callerId, cancellationToken);
        var reviewer = await db.Users.FindAsync([reviewerId], cancellationToken)
            ?? throw new InvalidInputException("The selected reviewer does not exist.");

        var submission = Submission.Create(editor, reviewer, title, file, comment, DateTime.UtcNow);
        db.Submissions.Add(submission);
        await db.SaveChangesAsync(cancellationToken);
        return submission.Id;
    }

    public async Task ResubmitAsync(Guid id, Guid callerId, DocumentFile file, string? comment, CancellationToken cancellationToken)
    {
        var submission = await LoadForUpdateAsync(id, callerId, cancellationToken);
        var editor = await GetCallerAsync(callerId, cancellationToken);

        submission.Resubmit(editor, file, comment, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(Guid id, Guid callerId, int versionNumber, CancellationToken cancellationToken)
    {
        var submission = await LoadForUpdateAsync(id, callerId, cancellationToken);
        var reviewer = await GetCallerAsync(callerId, cancellationToken);

        submission.Approve(reviewer, versionNumber, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReturnAsync(Guid id, Guid callerId, int versionNumber, string comment, CancellationToken cancellationToken)
    {
        var submission = await LoadForUpdateAsync(id, callerId, cancellationToken);
        var reviewer = await GetCallerAsync(callerId, cancellationToken);

        submission.Return(reviewer, versionNumber, comment, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DistributeAsync(Guid id, Guid callerId, Guid recipientId, CancellationToken cancellationToken)
    {
        var submission = await LoadForUpdateAsync(id, callerId, cancellationToken);
        var editor = await GetCallerAsync(callerId, cancellationToken);
        var recipient = await db.Recipients.FindAsync([recipientId], cancellationToken)
            ?? throw new InvalidInputException("The selected recipient does not exist.");

        submission.Distribute(editor, recipient, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DocumentDownload> GetContentAsync(Guid id, Guid callerId, int versionNumber, CancellationToken cancellationToken)
    {
        await EnsureVisibleAsync(id, callerId, cancellationToken);

        return await db.DocumentVersions.AsNoTracking()
                .Where(v => v.SubmissionId == id && v.Number == versionNumber)
                .Select(v => new DocumentDownload(v.FileName, v.ContentType, v.DocumentContent.Content))
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Version {versionNumber} was not found.");
    }

    public async Task<IReadOnlyList<AuditEntryResponse>> GetAuditAsync(Guid id, Guid callerId, CancellationToken cancellationToken)
    {
        await EnsureVisibleAsync(id, callerId, cancellationToken);

        return await db.AuditEntries.AsNoTracking()
            .Where(a => a.SubmissionId == id)
            .OrderBy(a => a.OccurredAt)
            .Select(a => new AuditEntryResponse(
                a.Action,
                a.VersionNumber,
                new PersonResponse(a.Actor.Id, a.Actor.Name),
                a.Comment,
                a.Recipient == null ? null : new PersonResponse(a.Recipient.Id, a.Recipient.Name),
                a.OccurredAt))
            .ToListAsync(cancellationToken);
    }

    // Visibility rule (R4): only the creating editor and the assigned reviewer see a submission.
    // Everyone else gets the same 404 as for an id that does not exist.
    private static IQueryable<Submission> Visible(IQueryable<Submission> submissions, Guid id, Guid callerId) =>
        submissions.Where(s => s.Id == id && (s.EditorId == callerId || s.ReviewerId == callerId));

    private async Task EnsureVisibleAsync(Guid id, Guid callerId, CancellationToken cancellationToken)
    {
        if (!await Visible(db.Submissions, id, callerId).AnyAsync(cancellationToken))
            throw SubmissionNotFound();
    }

    // Tracked, with version metadata (needed for the current version number), but without file bytes.
    private async Task<Submission> LoadForUpdateAsync(Guid id, Guid callerId, CancellationToken cancellationToken) =>
        await Visible(db.Submissions, id, callerId)
            .Include(s => s.Versions)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw SubmissionNotFound();

    private async Task<User> GetCallerAsync(Guid callerId, CancellationToken cancellationToken) =>
        await db.Users.FindAsync([callerId], cancellationToken)
        ?? throw new NotAllowedException("The signed-in user does not exist.");

    private static NotFoundException SubmissionNotFound() => new("The submission was not found.");
}
