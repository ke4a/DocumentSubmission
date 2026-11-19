namespace DocumentSubmission.Web.Services;

/// <summary>
/// The logged-in user and their API token, kept in memory only (one user per browser tab).
/// Reloading the page logs out.
/// </summary>
public sealed class UserSession
{
    public LoginUser? User { get; private set; }
    public string? Token { get; private set; }

    public bool IsLoggedIn => Token is not null;
    public bool IsEditor => User?.Role == "Editor";

    public void LogIn(LoginUser user, string token) => (User, Token) = (user, token);

    public void LogOut() => (User, Token) = (null, null);
}

public sealed record LoginUser(Guid Id, string Name, string Role, string Country)
{
    /// <summary>The seeded users (DocumentSubmission.Infrastructure/SeedData.cs) offered on the login page.</summary>
    public static readonly LoginUser[] Seeded =
    [
        new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Alice Ashford", "Editor", "NL"),
        new(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Bram Brink", "Editor", "NL"),
        new(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Clara Cole", "Reviewer", "NL"),
        new(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Daan Dekker", "Reviewer", "NL"),
        new(Guid.Parse("10000000-0000-0000-0000-000000000005"), "Emma Evers", "Editor", "BE"),
        new(Guid.Parse("10000000-0000-0000-0000-000000000006"), "Finn Fontaine", "Reviewer", "BE"),
    ];
}
