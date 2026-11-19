using System.Net;
using DocumentSubmission.Api.Contracts;
using DocumentSubmission.Api.IntegrationTests.Infrastructure;
using DocumentSubmission.Domain.Submissions;
using static DocumentSubmission.Api.IntegrationTests.Infrastructure.Users;

namespace DocumentSubmission.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class WorkflowTests(ApiFactory factory)
{
    [Fact]
    public async Task FullWorkflow_CreateReturnResubmitApproveDistribute()
    {
        var editor = await factory.LoginAsync(AliceEditorNl);
        var reviewer = await factory.LoginAsync(ClaraReviewerNl);
        var version1 = TestFiles.Pdf("first draft");
        var version2 = TestFiles.Docx();

        // Create: 201 + Location, version 1, caller is the editor.
        var createResponse = await editor.PostCreateAsync("Quarterly statistics", ClaraReviewerNl, version1, "draft.pdf", "First draft for review.");
        var created = await ApiClient.ReadAsync<SubmissionResponse>(createResponse, HttpStatusCode.Created);
        Assert.Equal($"/submissions/{created.Id}", createResponse.Headers.Location?.AbsolutePath);
        Assert.Equal(SubmissionStatus.Submitted, created.Status);
        Assert.Equal("NL", created.CountryCode);
        Assert.Equal(AliceEditorNl, created.Editor.Id);
        Assert.Equal(ClaraReviewerNl, created.Reviewer.Id);
        Assert.Equal(1, Assert.Single(created.Versions).Number);

        var returned = await ApiClient.ReadAsync<SubmissionResponse>(await reviewer.PostReturnAsync(created.Id, 1, "Add the summary table."));
        Assert.Equal(SubmissionStatus.Returned, returned.Status);

        var resubmitted = await ApiClient.ReadAsync<SubmissionResponse>(await editor.PostResubmitAsync(created.Id, version2, "final.docx", "Added the summary table."));
        Assert.Equal(SubmissionStatus.Submitted, resubmitted.Status);
        Assert.Equal(ClaraReviewerNl, resubmitted.Reviewer.Id);

        var approved = await ApiClient.ReadAsync<SubmissionResponse>(await reviewer.PostApproveAsync(created.Id, 2));
        Assert.Equal(SubmissionStatus.Approved, approved.Status);
        Assert.Equal(2, approved.ApprovedVersionNumber);

        var distributed = await ApiClient.ReadAsync<SubmissionResponse>(await editor.PostDistributeAsync(created.Id, RecordsOfficeNl));
        Assert.Equal(SubmissionStatus.Distributed, distributed.Status);

        // Details (R25): all versions, approved version, recipient.
        var details = await reviewer.GetSubmissionAsync(created.Id);
        Assert.Equal(SubmissionStatus.Distributed, details.Status);
        Assert.Equal([1, 2], details.Versions.Select(v => v.Number));
        Assert.Equal(["draft.pdf", "final.docx"], details.Versions.Select(v => v.FileName));
        Assert.Equal([TestFiles.PdfContentType, TestFiles.DocxContentType], details.Versions.Select(v => v.ContentType));
        Assert.Equal([version1.LongLength, version2.LongLength], details.Versions.Select(v => v.Size));
        Assert.Equal(2, details.ApprovedVersionNumber);
        Assert.Equal(RecordsOfficeNl, details.Recipient?.Id);
        Assert.NotNull(details.DistributedAt);

        // Downloads (R15): original bytes and headers for every version.
        await AssertDownloadAsync(editor, created.Id, 1, version1, TestFiles.PdfContentType, "draft.pdf");
        await AssertDownloadAsync(reviewer, created.Id, 2, version2, TestFiles.DocxContentType, "final.docx");

        // Audit trail (R21, R23): chronological, with actor, version, comment and recipient.
        var audit = await editor.GetAuditAsync(created.Id);
        Assert.Equal(
            [AuditAction.Submitted, AuditAction.Returned, AuditAction.Resubmitted, AuditAction.Approved, AuditAction.Distributed],
            audit.Select(a => a.Action));
        Assert.Equal([1, 1, 2, 2, 2], audit.Select(a => a.VersionNumber));
        Assert.Equal([AliceEditorNl, ClaraReviewerNl, AliceEditorNl, ClaraReviewerNl, AliceEditorNl], audit.Select(a => a.Actor.Id));
        Assert.Equal(["First draft for review.", "Add the summary table.", "Added the summary table.", null, null], audit.Select(a => a.Comment));
        Assert.Equal(RecordsOfficeNl, audit[4].Recipient?.Id);
        Assert.All(audit.Take(4), a => Assert.Null(a.Recipient));
        Assert.Equal(audit.OrderBy(a => a.OccurredAt).Select(a => a.Action), audit.Select(a => a.Action));
    }

    public static TheoryData<string> WrongStateCases => new()
    {
        "approve while returned",
        "return while approved",
        "resubmit while submitted",
        "resubmit while approved",
        "distribute while submitted",
        "approve stale version",
        "return stale version",
        "approve unknown version",
        "approve after distribution",
        "return after distribution",
        "resubmit after distribution",
        "distribute after distribution",
    };

    [Theory]
    [MemberData(nameof(WrongStateCases))]
    public async Task WrongState_Returns409(string wrongStateCase)
    {
        var editor = await factory.LoginAsync(AliceEditorNl);
        var reviewer = await factory.LoginAsync(ClaraReviewerNl);
        var id = (await editor.CreateSubmissionAsync(ClaraReviewerNl)).Id;

        async Task Return() => await reviewer.PostReturnAsync(id, 1).EnsureOkAsync();
        async Task Approve() => await reviewer.PostApproveAsync(id, 1).EnsureOkAsync();
        async Task Resubmit() { await Return(); await editor.PostResubmitAsync(id).EnsureOkAsync(); }
        async Task Distribute() { await Approve(); await editor.PostDistributeAsync(id, RecordsOfficeNl).EnsureOkAsync(); }

        HttpResponseMessage response;
        switch (wrongStateCase)
        {
            case "approve while returned": await Return(); response = await reviewer.PostApproveAsync(id, 1); break;
            case "return while approved": await Approve(); response = await reviewer.PostReturnAsync(id, 1); break;
            case "resubmit while submitted": response = await editor.PostResubmitAsync(id); break;
            case "resubmit while approved": await Approve(); response = await editor.PostResubmitAsync(id); break;
            case "distribute while submitted": response = await editor.PostDistributeAsync(id, RecordsOfficeNl); break;
            case "approve stale version": await Resubmit(); response = await reviewer.PostApproveAsync(id, 1); break;
            case "return stale version": await Resubmit(); response = await reviewer.PostReturnAsync(id, 1); break;
            case "approve unknown version": response = await reviewer.PostApproveAsync(id, 2); break;
            case "approve after distribution": await Distribute(); response = await reviewer.PostApproveAsync(id, 1); break;
            case "return after distribution": await Distribute(); response = await reviewer.PostReturnAsync(id, 1); break;
            case "resubmit after distribution": await Distribute(); response = await editor.PostResubmitAsync(id); break;
            case "distribute after distribution": await Distribute(); response = await editor.PostDistributeAsync(id, ArchiveAgencyNl); break;
            default: throw new ArgumentOutOfRangeException(nameof(wrongStateCase));
        }

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Upload_ValidDocx_Succeeds()
    {
        var editor = await factory.LoginAsync(AliceEditorNl);
        var content = TestFiles.Docx();

        // The client's content type is ignored; the server decides it from extension + signature.
        var form = ApiClient.CreateForm("Word document", ClaraReviewerNl);
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new("text/plain");
        form.Add(file, "file", "Report.DOCX");
        var created = await ApiClient.ReadAsync<SubmissionResponse>(await editor.PostAsync("/submissions", form), HttpStatusCode.Created);

        var version = Assert.Single(created.Versions);
        Assert.Equal("Report.DOCX", version.FileName);
        Assert.Equal(TestFiles.DocxContentType, version.ContentType);
        await AssertDownloadAsync(editor, created.Id, 1, content, TestFiles.DocxContentType, "Report.DOCX");
    }

    [Fact]
    public async Task DownloadVersion_UnknownVersion_Returns404()
    {
        var editor = await factory.LoginAsync(AliceEditorNl);
        var id = (await editor.CreateSubmissionAsync(ClaraReviewerNl)).Id;

        var response = await editor.GetAsync($"/submissions/{id}/versions/2/content");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound);
    }

    private static async Task AssertDownloadAsync(HttpClient client, Guid id, int versionNumber, byte[] expected, string contentType, string fileName)
    {
        var response = await client.GetAsync($"/submissions/{id}/versions/{versionNumber}/content");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal(fileName, response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
    }
}
