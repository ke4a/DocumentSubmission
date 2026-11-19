using DocumentSubmission.Domain.Exceptions;
using DocumentSubmission.Domain.Submissions;
using static DocumentSubmission.Domain.Tests.TestData;

namespace DocumentSubmission.Domain.Tests;

public class CreateTests
{
    [Fact]
    public void Create_ValidInput_SubmitsVersion1AndAudits()
    {
        var submission = Submission.Create(Editor, Reviewer, "  Annual report  ", File("v1.pdf"), null, Now);

        Assert.Equal(SubmissionStatus.Submitted, submission.Status);
        Assert.Equal("Annual report", submission.Title);
        Assert.Equal(Editor.Id, submission.EditorId);
        Assert.Equal(Reviewer.Id, submission.ReviewerId);
        Assert.Equal(Editor.CountryCode, submission.CountryCode);
        Assert.Equal(Now, submission.CreatedAt);
        Assert.Null(submission.ApprovedVersionNumber);

        var version = Assert.Single(submission.Versions);
        Assert.Equal(1, version.Number);
        Assert.Equal("v1.pdf", version.FileName);
        Assert.Equal(3, version.Size);
        Assert.Equal([1, 2, 3], version.DocumentContent.Content);

        var audit = Assert.Single(submission.AuditEntries);
        Assert.Equal(AuditAction.Submitted, audit.Action);
        Assert.Equal(1, audit.VersionNumber);
        Assert.Equal(Editor.Id, audit.ActorId);
        Assert.Equal(Now, audit.OccurredAt);
        Assert.Equal(1, submission.ConcurrencyToken);
    }

    [Fact]
    public void Create_ByReviewer_Throws()
    {
        Assert.Throws<NotAllowedException>(() => Submission.Create(Reviewer, OtherReviewer, "Title", File(), null, Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingTitle_Throws(string title)
    {
        Assert.Throws<InvalidInputException>(() => Submission.Create(Editor, Reviewer, title, File(), null, Now));
    }

    [Fact]
    public void Create_TitleTooLong_Throws()
    {
        var title = new string('a', Submission.MaxTitleLength + 1);
        Assert.Throws<InvalidInputException>(() => Submission.Create(Editor, Reviewer, title, File(), null, Now));
    }

    [Fact]
    public void Create_TitleAtMaxLength_Succeeds()
    {
        var title = new string('a', Submission.MaxTitleLength);
        Assert.Equal(title, Submission.Create(Editor, Reviewer, title, File(), null, Now).Title);
    }

    [Fact]
    public void Create_ReviewerIsNotAReviewer_Throws()
    {
        Assert.Throws<InvalidInputException>(() => Submission.Create(Editor, OtherEditor, "Title", File(), null, Now));
    }

    [Fact]
    public void Create_ReviewerFromOtherCountry_Throws()
    {
        Assert.Throws<InvalidInputException>(() => Submission.Create(Editor, ForeignReviewer, "Title", File(), null, Now));
    }

    [Fact]
    public void Create_WithComment_AuditsComment()
    {
        var submission = Submission.Create(Editor, Reviewer, "Title", File(), "First draft.", Now);
        Assert.Equal("First draft.", Assert.Single(submission.AuditEntries).Comment);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankComment_AuditsNoComment(string? comment)
    {
        var submission = Submission.Create(Editor, Reviewer, "Title", File(), comment, Now);
        Assert.Null(Assert.Single(submission.AuditEntries).Comment);
    }

    [Fact]
    public void Create_CommentTooLong_Throws()
    {
        var comment = new string('c', Submission.MaxCommentLength + 1);
        Assert.Throws<InvalidInputException>(() => Submission.Create(Editor, Reviewer, "Title", File(), comment, Now));
    }
}
