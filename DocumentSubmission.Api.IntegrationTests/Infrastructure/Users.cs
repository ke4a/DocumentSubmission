using DocumentSubmission.Infrastructure;

namespace DocumentSubmission.Api.IntegrationTests.Infrastructure;

/// <summary>Short names for the seeded users and recipients.</summary>
public static class Users
{
    public static readonly Guid AliceEditorNl = SeedData.AliceEditorNl.Id;
    public static readonly Guid BramEditorNl = SeedData.BramEditorNl.Id;
    public static readonly Guid ClaraReviewerNl = SeedData.ClaraReviewerNl.Id;
    public static readonly Guid DaanReviewerNl = SeedData.DaanReviewerNl.Id;
    public static readonly Guid EmmaEditorBe = SeedData.EmmaEditorBe.Id;
    public static readonly Guid FinnReviewerBe = SeedData.FinnReviewerBe.Id;

    public static readonly Guid RecordsOfficeNl = SeedData.RecordsOfficeNl.Id;
    public static readonly Guid ArchiveAgencyNl = SeedData.ArchiveAgencyNl.Id;
    public static readonly Guid RegistryServiceBe = SeedData.RegistryServiceBe.Id;

    public static readonly IReadOnlyDictionary<string, Guid> ByName = new Dictionary<string, Guid>
    {
        [nameof(AliceEditorNl)] = AliceEditorNl,
        [nameof(BramEditorNl)] = BramEditorNl,
        [nameof(ClaraReviewerNl)] = ClaraReviewerNl,
        [nameof(DaanReviewerNl)] = DaanReviewerNl,
        [nameof(EmmaEditorBe)] = EmmaEditorBe,
        [nameof(FinnReviewerBe)] = FinnReviewerBe,
    };
}
