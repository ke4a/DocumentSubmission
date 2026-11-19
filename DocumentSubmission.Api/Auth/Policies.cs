using DocumentSubmission.Domain.Users;

namespace DocumentSubmission.Api.Auth;

/// <summary>Role-based authorization policies, registered in Program.cs.</summary>
public static class Policies
{
    public const string Editor = nameof(Editor);
    public const string Reviewer = nameof(Reviewer);
    public const string Participant = nameof(Participant);

    public static readonly string EditorRole = nameof(Role.Editor);
    public static readonly string ReviewerRole = nameof(Role.Reviewer);
}
