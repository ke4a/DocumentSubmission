using DocumentSubmission.Domain.Exceptions;
using DocumentSubmission.Domain.Submissions;
using static DocumentSubmission.Domain.Tests.TestData;

namespace DocumentSubmission.Domain.Tests;

public class ApproveTests
{
    [Fact]
    public void Approve_CurrentVersion_PinsVersionAndAudits()
    {
        var submission = Submitted();
        var tokenBefore = submission.ConcurrencyToken;

        submission.Approve(Reviewer, 1, Now.AddHours(1));

        Assert.Equal(SubmissionStatus.Approved, submission.Status);
        Assert.Equal(1, submission.ApprovedVersionNumber);
        var audit = submission.AuditEntries.Last();
        Assert.Equal(AuditAction.Approved, audit.Action);
        Assert.Equal(1, audit.VersionNumber);
        Assert.Equal(Reviewer.Id, audit.ActorId);
        Assert.Equal(Now.AddHours(1), audit.OccurredAt);
        Assert.Equal(tokenBefore + 1, submission.ConcurrencyToken);
    }

    [Fact]
    public void Approve_AfterResubmit_PinsTheNewVersion()
    {
        var submission = Returned();
        submission.Resubmit(Editor, File("v2.pdf"), null, Now.AddHours(1));

        submission.Approve(Reviewer, 2, Now.AddHours(2));

        Assert.Equal(2, submission.ApprovedVersionNumber);
        Assert.Equal(2, submission.AuditEntries.Last().VersionNumber);
    }

    [Fact]
    public void Approve_ApprovedVersionNeverChangesAfterwards()
    {
        var submission = Approved();
        submission.Distribute(Editor, Recipient, Now.AddHours(1));

        Assert.Equal(1, submission.ApprovedVersionNumber);
    }

    [Fact]
    public void Approve_ByOtherReviewer_Throws()
    {
        Assert.Throws<NotAllowedException>(() => Submitted().Approve(OtherReviewer, 1, Now));
    }

    [Fact]
    public void Approve_ByEditor_Throws()
    {
        Assert.Throws<NotAllowedException>(() => Submitted().Approve(Editor, 1, Now));
    }

    [Fact]
    public void Approve_VersionNumberBelowOne_Throws()
    {
        Assert.Throws<InvalidInputException>(() => Submitted().Approve(Reviewer, 0, Now));
    }

    [Fact]
    public void Approve_StaleVersion_Throws()
    {
        var submission = Returned();
        submission.Resubmit(Editor, File("v2.pdf"), null, Now.AddHours(1));

        Assert.Throws<InvalidStateException>(() => submission.Approve(Reviewer, 1, Now.AddHours(2)));
    }

    [Fact]
    public void Approve_UnknownVersion_Throws()
    {
        Assert.Throws<InvalidStateException>(() => Submitted().Approve(Reviewer, 2, Now));
    }

    [Fact]
    public void Approve_WhenNotSubmitted_Throws()
    {
        Assert.Throws<InvalidStateException>(() => Returned().Approve(Reviewer, 1, Now));
        Assert.Throws<InvalidStateException>(() => Approved().Approve(Reviewer, 1, Now));
        Assert.Throws<InvalidStateException>(() => Distributed().Approve(Reviewer, 1, Now));
    }

    [Fact]
    public void Approve_Rejected_ChangesNothing()
    {
        var submission = Returned();
        var tokenBefore = submission.ConcurrencyToken;
        var auditCountBefore = submission.AuditEntries.Count;

        Assert.Throws<InvalidStateException>(() => submission.Approve(Reviewer, 1, Now));

        Assert.Equal(SubmissionStatus.Returned, submission.Status);
        Assert.Null(submission.ApprovedVersionNumber);
        Assert.Equal(tokenBefore, submission.ConcurrencyToken);
        Assert.Equal(auditCountBefore, submission.AuditEntries.Count);
    }
}
