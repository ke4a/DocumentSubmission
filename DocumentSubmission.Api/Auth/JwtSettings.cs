using System.ComponentModel.DataAnnotations;

namespace DocumentSubmission.Api.Auth;

/// <summary>Bound from the "Jwt" configuration section (appsettings or environment, e.g. Jwt__SigningKey).</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = null!;

    [Required]
    public string Audience { get; init; } = null!;

    /// <summary>HMAC-SHA256 key; at least 32 characters.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; init; } = null!;

    [Range(1, 24 * 60)]
    public int LifetimeMinutes { get; init; } = 60;
}
