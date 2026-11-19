namespace DocumentSubmission.Domain.Users;

/// <summary>Seeded, read-only reference data. Each user has exactly one role and one country.</summary>
public sealed class User(Guid id, string name, Role role, string countryCode)
{
    public Guid Id { get; private set; } = id;
    public string Name { get; private set; } = name;
    public Role Role { get; private set; } = role;
    public string CountryCode { get; private set; } = countryCode;
}
