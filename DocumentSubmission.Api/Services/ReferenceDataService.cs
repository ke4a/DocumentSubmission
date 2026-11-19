using DocumentSubmission.Api.Contracts;
using DocumentSubmission.Domain.Users;
using DocumentSubmission.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DocumentSubmission.Api.Services;

/// <summary>Lists the reviewers and recipients an editor can choose from (R8, R17).</summary>
public sealed class ReferenceDataService(AppDbContext db)
{
    public async Task<IReadOnlyList<PersonResponse>> ListReviewersAsync(string countryCode, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking()
            .Where(u => u.Role == Role.Reviewer && u.CountryCode == countryCode)
            .OrderBy(u => u.Name)
            .Select(u => new PersonResponse(u.Id, u.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PersonResponse>> ListRecipientsAsync(string countryCode, CancellationToken cancellationToken) =>
        await db.Recipients.AsNoTracking()
            .Where(r => r.CountryCode == countryCode)
            .OrderBy(r => r.Name)
            .Select(r => new PersonResponse(r.Id, r.Name))
            .ToListAsync(cancellationToken);
}
