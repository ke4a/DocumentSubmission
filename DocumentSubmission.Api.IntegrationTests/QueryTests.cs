using DocumentSubmission.Api.Contracts;
using DocumentSubmission.Api.IntegrationTests.Infrastructure;
using static DocumentSubmission.Api.IntegrationTests.Infrastructure.Users;

namespace DocumentSubmission.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class QueryTests(ApiFactory factory)
{
    [Fact]
    public async Task ReferenceLists_ReturnOnlyCallersCountry()
    {
        var editorNl = await factory.LoginAsync(AliceEditorNl);
        var editorBe = await factory.LoginAsync(EmmaEditorBe);

        var reviewersNl = await ApiClient.ReadAsync<List<PersonResponse>>(await editorNl.GetAsync("/reviewers"));
        var recipientsNl = await ApiClient.ReadAsync<List<PersonResponse>>(await editorNl.GetAsync("/recipients"));
        var reviewersBe = await ApiClient.ReadAsync<List<PersonResponse>>(await editorBe.GetAsync("/reviewers"));
        var recipientsBe = await ApiClient.ReadAsync<List<PersonResponse>>(await editorBe.GetAsync("/recipients"));

        Assert.Equal([ClaraReviewerNl, DaanReviewerNl], reviewersNl.Select(r => r.Id).Order());
        Assert.Equal([RecordsOfficeNl, ArchiveAgencyNl], recipientsNl.Select(r => r.Id).Order());
        Assert.Equal([FinnReviewerBe], reviewersBe.Select(r => r.Id));
        Assert.Equal([RegistryServiceBe], recipientsBe.Select(r => r.Id));
    }

    [Fact]
    public async Task ListSubmissions_EditorSeesOwn_ReviewerSeesAssigned_NewestFirst()
    {
        var alice = await factory.LoginAsync(AliceEditorNl);
        var bram = await factory.LoginAsync(BramEditorNl);
        var clara = await factory.LoginAsync(ClaraReviewerNl);
        var first = await alice.CreateSubmissionAsync(ClaraReviewerNl, "Alice to Clara");
        var second = await alice.CreateSubmissionAsync(DaanReviewerNl, "Alice to Daan");
        var third = await bram.CreateSubmissionAsync(ClaraReviewerNl, "Bram to Clara");

        var aliceList = await ApiClient.ReadAsync<List<SubmissionSummaryResponse>>(await alice.GetAsync("/submissions"));
        var claraList = await ApiClient.ReadAsync<List<SubmissionSummaryResponse>>(await clara.GetAsync("/submissions"));

        // Other tests share the database, so check membership and order rather than exact contents.
        Assert.All(aliceList, s => Assert.Equal(AliceEditorNl, s.Editor.Id));
        Assert.All(claraList, s => Assert.Equal(ClaraReviewerNl, s.Reviewer.Id));
        Assert.Equal(aliceList.OrderByDescending(s => s.CreatedAt).Select(s => s.Id), aliceList.Select(s => s.Id));
        Assert.Equal(claraList.OrderByDescending(s => s.CreatedAt).Select(s => s.Id), claraList.Select(s => s.Id));

        Assert.Equal([second.Id, first.Id], aliceList.Select(s => s.Id).Where(id => id == first.Id || id == second.Id));
        Assert.DoesNotContain(aliceList, s => s.Id == third.Id);
        Assert.Equal([third.Id, first.Id], claraList.Select(s => s.Id).Where(id => id == first.Id || id == third.Id));
        Assert.DoesNotContain(claraList, s => s.Id == second.Id);

        var summary = aliceList.Single(s => s.Id == first.Id);
        Assert.Equal("Alice to Clara", summary.Title);
        Assert.Equal("Submitted", summary.Status.ToString());
        Assert.Equal(1, summary.CurrentVersionNumber);
    }
}
