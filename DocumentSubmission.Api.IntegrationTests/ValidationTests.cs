using System.Net;
using System.Net.Http.Json;
using DocumentSubmission.Api.IntegrationTests.Infrastructure;
using static DocumentSubmission.Api.IntegrationTests.Infrastructure.Users;

namespace DocumentSubmission.Api.IntegrationTests;

/// <summary>Request validation (400) and eligibility of the chosen reviewer or recipient (400).</summary>
[Collection(ApiCollection.Name)]
public class ValidationTests(ApiFactory factory)
{
    private const int TenMegabytes = 10 * 1024 * 1024;

    public static TheoryData<string> InvalidRequests => new()
    {
        "create: title missing",
        "create: title blank",
        "create: title too long",
        "create: reviewerId missing",
        "create: file missing",
        "create: file type not allowed",
        "create: pdf extension with docx signature",
        "create: docx extension with pdf signature",
        "create: file empty",
        "create: file too large",
        "create: file name too long",
        "create: comment too long",
        "resubmit: file signature wrong",
        "resubmit: comment too long",
        "approve: versionNumber missing",
        "approve: versionNumber zero",
        "return: versionNumber missing",
        "return: comment missing",
        "return: comment blank",
        "return: comment too long",
        "distribute: recipientId missing",
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task InvalidRequest_Returns400(string invalidCase)
    {
        var editor = await factory.LoginAsync(AliceEditorNl);
        var reviewer = await factory.LoginAsync(ClaraReviewerNl);
        var id = (await editor.CreateSubmissionAsync(ClaraReviewerNl)).Id;

        var response = invalidCase switch
        {
            "create: title missing" => await editor.PostAsync("/submissions", ApiClient.CreateForm(null, ClaraReviewerNl, TestFiles.Pdf())),
            "create: title blank" => await editor.PostCreateAsync("   ", ClaraReviewerNl),
            "create: title too long" => await editor.PostCreateAsync(new string('t', 201), ClaraReviewerNl),
            "create: reviewerId missing" => await editor.PostAsync("/submissions", ApiClient.CreateForm("Title", null, TestFiles.Pdf())),
            "create: file missing" => await editor.PostAsync("/submissions", ApiClient.CreateForm("Title", ClaraReviewerNl)),
            "create: file type not allowed" => await editor.PostCreateAsync("Title", ClaraReviewerNl, TestFiles.Pdf(), "notes.txt"),
            "create: pdf extension with docx signature" => await editor.PostCreateAsync("Title", ClaraReviewerNl, TestFiles.Docx(), "report.pdf"),
            "create: docx extension with pdf signature" => await editor.PostCreateAsync("Title", ClaraReviewerNl, TestFiles.Pdf(), "report.docx"),
            "create: file empty" => await editor.PostCreateAsync("Title", ClaraReviewerNl, [], "empty.pdf"),
            "create: file too large" => await editor.PostCreateAsync("Title", ClaraReviewerNl, TestFiles.PdfOfSize(TenMegabytes + 1)),
            "create: file name too long" => await editor.PostCreateAsync("Title", ClaraReviewerNl, TestFiles.Pdf(), new string('n', 252) + ".pdf"),
            "create: comment too long" => await editor.PostCreateAsync("Title", ClaraReviewerNl, comment: new string('c', 2001)),
            "resubmit: file signature wrong" => await editor.PostResubmitAsync(id, TestFiles.Docx(), "revised.pdf"),
            "resubmit: comment too long" => await editor.PostResubmitAsync(id, comment: new string('c', 2001)),
            "approve: versionNumber missing" => await reviewer.PostAsJsonAsync($"/submissions/{id}/approve", new { }),
            "approve: versionNumber zero" => await reviewer.PostApproveAsync(id, 0),
            "return: versionNumber missing" => await reviewer.PostAsJsonAsync($"/submissions/{id}/return", new { comment = "Fix it" }),
            "return: comment missing" => await reviewer.PostAsJsonAsync($"/submissions/{id}/return", new { versionNumber = 1 }),
            "return: comment blank" => await reviewer.PostReturnAsync(id, 1, "  "),
            "return: comment too long" => await reviewer.PostReturnAsync(id, 1, new string('c', 2001)),
            "distribute: recipientId missing" => await editor.PostAsJsonAsync($"/submissions/{id}/distribute", new { }),
            _ => throw new ArgumentOutOfRangeException(nameof(invalidCase)),
        };

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Submitted", (await editor.GetSubmissionAsync(id)).Status.ToString());
    }

    [Fact]
    public async Task Upload_LimitsAreInclusive()
    {
        var editor = await factory.LoginAsync(AliceEditorNl);

        var largest = await editor.PostCreateAsync(new string('t', 200), ClaraReviewerNl, TestFiles.PdfOfSize(TenMegabytes), new string('n', 251) + ".pdf");

        Assert.Equal(HttpStatusCode.Created, largest.StatusCode);
    }

    public static TheoryData<string> IneligibleReferences => new()
    {
        "reviewer unknown",
        "reviewer is an editor",
        "reviewer from other country",
        "recipient unknown",
        "recipient from other country",
    };

    [Theory]
    [MemberData(nameof(IneligibleReferences))]
    public async Task IneligibleReference_Returns400(string ineligibleCase)
    {
        var editor = await factory.LoginAsync(AliceEditorNl);
        var reviewer = await factory.LoginAsync(ClaraReviewerNl);
        var approved = await editor.CreateSubmissionAsync(ClaraReviewerNl);
        await reviewer.PostApproveAsync(approved.Id, 1).EnsureOkAsync();

        var response = ineligibleCase switch
        {
            "reviewer unknown" => await editor.PostCreateAsync("Title", Guid.NewGuid()),
            "reviewer is an editor" => await editor.PostCreateAsync("Title", BramEditorNl),
            "reviewer from other country" => await editor.PostCreateAsync("Title", FinnReviewerBe),
            "recipient unknown" => await editor.PostDistributeAsync(approved.Id, Guid.NewGuid()),
            "recipient from other country" => await editor.PostDistributeAsync(approved.Id, RegistryServiceBe),
            _ => throw new ArgumentOutOfRangeException(nameof(ineligibleCase)),
        };

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Approved", (await editor.GetSubmissionAsync(approved.Id)).Status.ToString());
    }

    [Fact]
    public async Task WrongContentType_Returns415()
    {
        var editor = await factory.LoginAsync(AliceEditorNl);

        var response = await editor.PostAsJsonAsync("/submissions", new { title = "Title", reviewerId = ClaraReviewerNl });

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.UnsupportedMediaType);
    }
}
