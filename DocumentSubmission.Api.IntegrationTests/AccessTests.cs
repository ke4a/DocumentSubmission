using System.Net;
using System.Net.Http.Json;
using DocumentSubmission.Api.IntegrationTests.Infrastructure;
using static DocumentSubmission.Api.IntegrationTests.Infrastructure.Users;

namespace DocumentSubmission.Api.IntegrationTests;

/// <summary>Authentication (401), role policies (403) and the visibility rule (404).</summary>
[Collection(ApiCollection.Name)]
public class AccessTests(ApiFactory factory)
{
    public static TheoryData<string, string> AllEndpoints => new()
    {
        { "GET", "/reviewers" },
        { "GET", "/recipients" },
        { "GET", "/submissions" },
        { "POST", "/submissions" },
        { "GET", "/submissions/{id}" },
        { "POST", "/submissions/{id}/resubmit" },
        { "POST", "/submissions/{id}/approve" },
        { "POST", "/submissions/{id}/return" },
        { "POST", "/submissions/{id}/distribute" },
        { "GET", "/submissions/{id}/versions/1/content" },
        { "GET", "/submissions/{id}/audit" },
    };

    [Theory]
    [MemberData(nameof(AllEndpoints))]
    public async Task ProtectedEndpoints_WithoutToken_Return401(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path.Replace("{id}", Guid.NewGuid().ToString()));

        var response = await factory.CreateClient().SendAsync(request);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    public static TheoryData<string, string> WrongRoleCases => new()
    {
        { "reviewers", "Reviewer" },
        { "recipients", "Reviewer" },
        { "create", "Reviewer" },
        { "resubmit", "Reviewer" },
        { "distribute", "Reviewer" },
        { "approve", "Editor" },
        { "return", "Editor" },
    };

    [Theory]
    [MemberData(nameof(WrongRoleCases))]
    public async Task WrongRole_Returns403(string endpoint, string role)
    {
        // The caller is a participant of the submission, so only the role decides the outcome.
        var editor = await factory.LoginAsync(AliceEditorNl);
        var submission = await editor.CreateSubmissionAsync(ClaraReviewerNl);
        var caller = role == "Editor" ? editor : await factory.LoginAsync(ClaraReviewerNl);

        var response = endpoint switch
        {
            "reviewers" => await caller.GetAsync("/reviewers"),
            "recipients" => await caller.GetAsync("/recipients"),
            "create" => await caller.PostCreateAsync("Title", ClaraReviewerNl),
            "resubmit" => await caller.PostResubmitAsync(submission.Id),
            "distribute" => await caller.PostDistributeAsync(submission.Id, RecordsOfficeNl),
            "approve" => await caller.PostApproveAsync(submission.Id, 1),
            "return" => await caller.PostReturnAsync(submission.Id, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint)),
        };

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WrongRole_Returns403_WhateverTheId()
    {
        var reviewer = await factory.LoginAsync(ClaraReviewerNl);

        var response = await reviewer.PostDistributeAsync(Guid.NewGuid(), RecordsOfficeNl);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Forbidden);
    }

    public static TheoryData<string, string> NonParticipantCases
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var user in new[] { "BramEditorNl", "DaanReviewerNl", "EmmaEditorBe", "FinnReviewerBe" })
            {
                data.Add("details", user);
                data.Add("content", user);
                data.Add("audit", user);
            }
            data.Add("resubmit", "BramEditorNl");
            data.Add("resubmit", "EmmaEditorBe");
            data.Add("distribute", "BramEditorNl");
            data.Add("approve", "DaanReviewerNl");
            data.Add("approve", "FinnReviewerBe");
            data.Add("return", "DaanReviewerNl");
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(NonParticipantCases))]
    public async Task SubmissionRoutes_NonParticipant_Return404(string route, string user)
    {
        var editor = await factory.LoginAsync(AliceEditorNl);
        var submission = await editor.CreateSubmissionAsync(ClaraReviewerNl);
        var caller = await factory.LoginAsync(Users.ByName[user]);
        var id = submission.Id;

        var response = route switch
        {
            "details" => await caller.GetAsync($"/submissions/{id}"),
            "content" => await caller.GetAsync($"/submissions/{id}/versions/1/content"),
            "audit" => await caller.GetAsync($"/submissions/{id}/audit"),
            "resubmit" => await caller.PostResubmitAsync(id),
            "distribute" => await caller.PostDistributeAsync(id, RecordsOfficeNl),
            "approve" => await caller.PostApproveAsync(id, 1),
            "return" => await caller.PostReturnAsync(id, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(route)),
        };

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmissionRoutes_UnknownId_Return404()
    {
        var editor = await factory.LoginAsync(AliceEditorNl);

        var response = await editor.GetAsync($"/submissions/{Guid.NewGuid()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound);
    }
}
