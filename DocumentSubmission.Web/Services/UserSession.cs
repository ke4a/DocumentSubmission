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

/// <summary>A user that can log in (from the Development-only <c>GET /auth/users</c>).</summary>
public sealed record LoginUser(Guid Id, string Name, string Role, string CountryCode);
