using DocumentSubmission.Api.Auth;
using DocumentSubmission.Api.Contracts;
using DocumentSubmission.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentSubmission.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(AppDbContext db, TokenService tokenService) : ControllerBase
{
    /// <summary>Development-only login: returns a JWT for a seeded user (R2).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [DevelopmentOnly]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([request.UserId!.Value], cancellationToken);
        if (user is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Unknown user.");

        var (token, expiresAt) = tokenService.CreateToken(user);
        return new LoginResponse(token, expiresAt);
    }
}
