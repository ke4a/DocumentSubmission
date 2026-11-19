namespace DocumentSubmission.Domain.Users;

/// <summary>Seeded, read-only reference data: who an approved document can be distributed to.</summary>
public sealed class Recipient(Guid id, string name, string countryCode)
{
    public Guid Id { get; private set; } = id;
    public string Name { get; private set; } = name;
    public string CountryCode { get; private set; } = countryCode;
}
