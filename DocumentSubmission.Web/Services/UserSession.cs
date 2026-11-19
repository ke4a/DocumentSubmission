namespace DocumentSubmission.Web.Services;

/// <summary>
/// The logged-in user and their API token. Registered as scoped, and in Blazor Server a scope lives as
/// long as the circuit (one browser tab), so the token stays in server memory. Reloading the page logs out.
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

/// <summary>A seeded user offered on the login page (configured in appsettings.Development.json).</summary>
public sealed record LoginUser(Guid Id, string Name, string Role, string Country);
