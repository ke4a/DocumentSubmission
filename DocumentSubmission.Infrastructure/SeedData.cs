using DocumentSubmission.Domain.Users;

namespace DocumentSubmission.Infrastructure;

/// <summary>
/// Fixed, fictional reference data (R1, N6). The Ids never change so they can be used to log in
/// and are documented in the README.
/// </summary>
public static class SeedData
{
    public const string CountryNl = "NL";
    public const string CountryBe = "BE";

    public static readonly User AliceEditorNl = new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Alice Ashford", Role.Editor, CountryNl);
    public static readonly User BramEditorNl = new(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Bram Brink", Role.Editor, CountryNl);
    public static readonly User ClaraReviewerNl = new(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Clara Cole", Role.Reviewer, CountryNl);
    public static readonly User DaanReviewerNl = new(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Daan Dekker", Role.Reviewer, CountryNl);
    public static readonly User EmmaEditorBe = new(Guid.Parse("10000000-0000-0000-0000-000000000005"), "Emma Evers", Role.Editor, CountryBe);
    public static readonly User FinnReviewerBe = new(Guid.Parse("10000000-0000-0000-0000-000000000006"), "Finn Fontaine", Role.Reviewer, CountryBe);

    public static readonly Recipient RecordsOfficeNl = new(Guid.Parse("20000000-0000-0000-0000-000000000001"), "Example Records Office", CountryNl);
    public static readonly Recipient ArchiveAgencyNl = new(Guid.Parse("20000000-0000-0000-0000-000000000002"), "Sample Archive Agency", CountryNl);
    public static readonly Recipient RegistryServiceBe = new(Guid.Parse("20000000-0000-0000-0000-000000000003"), "Fictional Registry Service", CountryBe);

    public static readonly User[] Users = [AliceEditorNl, BramEditorNl, ClaraReviewerNl, DaanReviewerNl, EmmaEditorBe, FinnReviewerBe];
    public static readonly Recipient[] Recipients = [RecordsOfficeNl, ArchiveAgencyNl, RegistryServiceBe];
}
