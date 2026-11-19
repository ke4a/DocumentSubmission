using DocumentSubmission.Domain.Exceptions;
using DocumentSubmission.Domain.Submissions;
using static DocumentSubmission.Domain.Tests.TestData;

namespace DocumentSubmission.Domain.Tests;

public class ReturnTests
{
    [Fact]
    public void Return_WithComment_ReturnsAndAuditsComment()
    {
        var submission = Submitted();
        var tokenBefore = submission.ConcurrencyToken;

        submission.Return(Reviewer, 1, "Please fix the summary.", Now.AddHours(1));

        Assert.Equal(SubmissionStatus.Returned, submission.Status);
        Assert.Null(submission.ApprovedVersionNumber);
        var audit = submission.AuditEntries.Last();
        Assert.Equal(AuditAction.Returned, audit.Action);
        Assert.Equal(1, audit.VersionNumber);
        Assert.Equal(Reviewer.Id, audit.ActorId);
        Assert.Equal("Please fix the summary.", audit.Comment);
        Assert.Equal(Now.AddHours(1), audit.OccurredAt);
        Assert.Equal(tokenBefore + 1, submission.ConcurrencyToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Return_WithoutComment_Throws(string comment)
    {
        Assert.Throws<InvalidInputException>(() => Submitted().Return(Reviewer, 1, comment, Now));
    }

    [Fact]
    public void Return_CommentTooLong_Throws()
    {
        var comment = new string('c', Submission.MaxCommentLength + 1);
        Assert.Throws<InvalidInputException>(() => Submitted().Return(Reviewer, 1, comment, Now));
    }

    [Fact]
    public void Return_ByOtherReviewer_Throws()
    {
        Assert.Throws<NotAllowedException>(() => Submitted().Return(OtherReviewer, 1, "No", Now));
    }

    [Fact]
    public void Return_ByEditor_Throws()
    {
        Assert.Throws<NotAllowedException>(() => Submitted().Return(Editor, 1, "No", Now));
    }

    [Fact]
    public void Return_VersionNumberBelowOne_Throws()
    {
        Assert.Throws<InvalidInputException>(() => Submitted().Return(Reviewer, 0, "No", Now));
    }

    [Fact]
    public void Return_StaleVersion_Throws()
    {
        var submission = Returned();
        submission.Resubmit(Editor, File("v2.pdf"), null, Now.AddHours(1));

        Assert.Throws<InvalidStateException>(() => submission.Return(Reviewer, 1, "No", Now.AddHours(2)));
    }

    [Fact]
    public void Return_WhenNotSubmitted_Throws()
    {
        Assert.Throws<InvalidStateException>(() => Returned().Return(Reviewer, 1, "No", Now));
        Assert.Throws<InvalidStateException>(() => Approved().Return(Reviewer, 1, "No", Now));
        Assert.Throws<InvalidStateException>(() => Distributed().Return(Reviewer, 1, "No", Now));
    }
}
