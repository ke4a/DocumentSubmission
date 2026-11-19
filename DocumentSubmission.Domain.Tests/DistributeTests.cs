using DocumentSubmission.Domain.Exceptions;
using DocumentSubmission.Domain.Submissions;
using static DocumentSubmission.Domain.Tests.TestData;

namespace DocumentSubmission.Domain.Tests;

public class DistributeTests
{
    [Fact]
    public void Distribute_WhenApproved_CompletesAndAudits()
    {
        var submission = Approved();
        var tokenBefore = submission.ConcurrencyToken;

        submission.Distribute(Editor, Recipient, Now.AddHours(1));

        Assert.Equal(SubmissionStatus.Distributed, submission.Status);
        Assert.Equal(Recipient.Id, submission.RecipientId);
        Assert.Equal(Now.AddHours(1), submission.DistributedAt);
        var audit = submission.AuditEntries.Last();
        Assert.Equal(AuditAction.Distributed, audit.Action);
        Assert.Equal(submission.ApprovedVersionNumber, audit.VersionNumber);
        Assert.Equal(Editor.Id, audit.ActorId);
        Assert.Equal(Recipient.Id, audit.RecipientId);
        Assert.Equal(tokenBefore + 1, submission.ConcurrencyToken);
    }

    [Fact]
    public void Distribute_ByOtherEditor_Throws()
    {
        Assert.Throws<NotAllowedException>(() => Approved().Distribute(OtherEditor, Recipient, Now));
    }

    [Fact]
    public void Distribute_ByReviewer_Throws()
    {
        Assert.Throws<NotAllowedException>(() => Approved().Distribute(Reviewer, Recipient, Now));
    }

    [Fact]
    public void Distribute_RecipientFromOtherCountry_Throws()
    {
        Assert.Throws<InvalidInputException>(() => Approved().Distribute(Editor, ForeignRecipient, Now));
    }

    [Fact]
    public void Distribute_WhenNotApproved_Throws()
    {
        Assert.Throws<InvalidStateException>(() => Submitted().Distribute(Editor, Recipient, Now));
        Assert.Throws<InvalidStateException>(() => Returned().Distribute(Editor, Recipient, Now));
    }

    [Fact]
    public void Distribute_AfterDistribution_NoFurtherActionsAllowed()
    {
        var submission = Distributed();

        Assert.Throws<InvalidStateException>(() => submission.Distribute(Editor, Recipient, Now));
        Assert.Throws<InvalidStateException>(() => submission.Resubmit(Editor, File(), null, Now));
        Assert.Throws<InvalidStateException>(() => submission.Approve(Reviewer, 1, Now));
        Assert.Throws<InvalidStateException>(() => submission.Return(Reviewer, 1, "No", Now));
    }
}
