using System.Security.Claims;

namespace DocumentSubmission.Api.Auth;

/// <summary>
/// The claim names we issue and read. Inbound claim mapping is switched off (MapInboundClaims = false),
/// so these short JWT names reach <see cref="ClaimsPrincipal"/> unchanged.
/// </summary>
public static class AuthClaims
{
    public const string UserId = "sub";
    public const string Role = "role";
    public const string Country = "country";

    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(UserId) ?? throw new InvalidOperationException("The token has no 'sub' claim."));
}
