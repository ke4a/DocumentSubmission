using DocumentSubmission.Domain.Submissions;
using DocumentSubmission.Domain.Users;

namespace DocumentSubmission.Domain.Tests;

/// <summary>Users, recipients and files shared by the domain tests.</summary>
internal static class TestData
{
    public static readonly DateTime Now = new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

    public static readonly User Editor = new(Guid.NewGuid(), "Test Editor", Role.Editor, "AA");
    public static readonly User OtherEditor = new(Guid.NewGuid(), "Other Editor", Role.Editor, "AA");
    public static readonly User Reviewer = new(Guid.NewGuid(), "Test Reviewer", Role.Reviewer, "AA");
    public static readonly User OtherReviewer = new(Guid.NewGuid(), "Other Reviewer", Role.Reviewer, "AA");
    public static readonly User ForeignReviewer = new(Guid.NewGuid(), "Foreign Reviewer", Role.Reviewer, "BB");

    public static readonly Recipient Recipient = new(Guid.NewGuid(), "Test Recipient", "AA");
    public static readonly Recipient ForeignRecipient = new(Guid.NewGuid(), "Foreign Recipient", "BB");

    public static DocumentFile File(string name = "report.pdf") => new(name, "application/pdf", [1, 2, 3]);

    public static Submission Submitted() =>
        Submission.Create(Editor, Reviewer, "Annual report", File("v1.pdf"), null, Now);

    public static Submission Returned()
    {
        var submission = Submitted();
        submission.Return(Reviewer, 1, "Please fix the summary.", Now.AddMinutes(1));
        return submission;
    }

    public static Submission Approved()
    {
        var submission = Submitted();
        submission.Approve(Reviewer, 1, Now.AddMinutes(1));
        return submission;
    }

    public static Submission Distributed()
    {
        var submission = Approved();
        submission.Distribute(Editor, Recipient, Now.AddMinutes(2));
        return submission;
    }
}
