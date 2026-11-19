using DocumentSubmission.Api.Auth;
using DocumentSubmission.Api.Contracts;
using DocumentSubmission.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentSubmission.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.Editor)]
public sealed class ReferenceDataController(ReferenceDataService referenceData) : ControllerBase
{
    /// <summary>Reviewers in the caller's country (R8).</summary>
    [HttpGet("reviewers")]
    public Task<IReadOnlyList<PersonResponse>> ListReviewers(CancellationToken cancellationToken) =>
        referenceData.ListReviewersAsync(CallerCountry, cancellationToken);

    /// <summary>Recipients in the caller's country (R17).</summary>
    [HttpGet("recipients")]
    public Task<IReadOnlyList<PersonResponse>> ListRecipients(CancellationToken cancellationToken) =>
        referenceData.ListRecipientsAsync(CallerCountry, cancellationToken);

    private string CallerCountry => User.FindFirst(AuthClaims.Country)?.Value ?? string.Empty;
}
