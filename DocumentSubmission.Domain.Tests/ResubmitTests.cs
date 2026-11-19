using DocumentSubmission.Domain.Exceptions;
using DocumentSubmission.Domain.Submissions;
using static DocumentSubmission.Domain.Tests.TestData;

namespace DocumentSubmission.Domain.Tests;

public class ResubmitTests
{
    [Fact]
    public void Resubmit_WhenReturned_AddsNextVersionAndAudits()
    {
        var submission = Returned();
        var tokenBefore = submission.ConcurrencyToken;

        submission.Resubmit(Editor, File("v2.pdf"), null, Now.AddHours(1));

        Assert.Equal(SubmissionStatus.Submitted, submission.Status);
        Assert.Equal(Reviewer.Id, submission.ReviewerId);
        Assert.Equal([1, 2], submission.Versions.Select(v => v.Number));
        Assert.Equal(2, submission.CurrentVersionNumber);
        Assert.Equal("v2.pdf", submission.Versions.Single(v => v.Number == 2).FileName);
        var audit = submission.AuditEntries.Last();
        Assert.Equal(AuditAction.Resubmitted, audit.Action);
        Assert.Equal(2, audit.VersionNumber);
        Assert.Equal(Editor.Id, audit.ActorId);
        Assert.Equal(tokenBefore + 1, submission.ConcurrencyToken);
    }

    [Fact]
    public void Resubmit_KeepsEarlierVersionsUnchanged()
    {
        var submission = Returned();
        var version1 = submission.Versions.Single();

        submission.Resubmit(Editor, File("v2.pdf"), null, Now.AddHours(1));
        submission.Return(Reviewer, 2, "Again", Now.AddHours(2));
        submission.Resubmit(Editor, File("v3.pdf"), null, Now.AddHours(3));

        Assert.Equal([1, 2, 3], submission.Versions.Select(v => v.Number));
        Assert.Same(version1, submission.Versions.First());
        Assert.Equal("v1.pdf", version1.FileName);
        Assert.Equal(Now, version1.CreatedAt);
    }

    [Fact]
    public void Resubmit_ByOtherEditor_Throws()
    {
        Assert.Throws<NotAllowedException>(() => Returned().Resubmit(OtherEditor, File(), null, Now));
    }

    [Fact]
    public void Resubmit_ByReviewer_Throws()
    {
        Assert.Throws<NotAllowedException>(() => Returned().Resubmit(Reviewer, File(), null, Now));
    }

    [Fact]
    public void Resubmit_WhenNotReturned_Throws()
    {
        Assert.Throws<InvalidStateException>(() => Submitted().Resubmit(Editor, File(), null, Now));
        Assert.Throws<InvalidStateException>(() => Approved().Resubmit(Editor, File(), null, Now));
        Assert.Throws<InvalidStateException>(() => Distributed().Resubmit(Editor, File(), null, Now));
    }

    [Fact]
    public void Resubmit_WithComment_AuditsComment()
    {
        var submission = Returned();
        submission.Resubmit(Editor, File("v2.pdf"), "Added the summary table.", Now.AddHours(1));
        Assert.Equal("Added the summary table.", submission.AuditEntries.Last().Comment);
    }

    [Fact]
    public void Resubmit_CommentTooLong_Throws()
    {
        var comment = new string('c', Submission.MaxCommentLength + 1);
        Assert.Throws<InvalidInputException>(() => Returned().Resubmit(Editor, File(), comment, Now));
    }
}
