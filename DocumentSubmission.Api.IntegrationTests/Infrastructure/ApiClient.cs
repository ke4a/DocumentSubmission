using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentSubmission.Api.Contracts;

namespace DocumentSubmission.Api.IntegrationTests.Infrastructure;

/// <summary>Small helpers that keep the tests focused on behaviour rather than on building requests.</summary>
public static class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static async Task<HttpClient> LoginAsync(this ApiFactory factory, Guid userId)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { userId });
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>(Json))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return client;
    }

    public static MultipartFormDataContent FileForm(byte[] content, string fileName, string clientContentType = "application/octet-stream")
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(clientContentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    public static MultipartFormDataContent CreateForm(string? title, Guid? reviewerId, byte[]? content = null, string fileName = "document.pdf", string? comment = null)
    {
        var form = content is null ? new MultipartFormDataContent() : FileForm(content, fileName);
        if (title is not null) form.Add(new StringContent(title), "title");
        if (reviewerId is not null) form.Add(new StringContent(reviewerId.Value.ToString()), "reviewerId");
        if (comment is not null) form.Add(new StringContent(comment), "comment");
        return form;
    }

    public static Task<HttpResponseMessage> PostCreateAsync(this HttpClient client, string title, Guid reviewerId, byte[]? content = null, string fileName = "document.pdf", string? comment = null) =>
        client.PostAsync("/submissions", CreateForm(title, reviewerId, content ?? TestFiles.Pdf(), fileName, comment));

    public static async Task<SubmissionResponse> CreateSubmissionAsync(this HttpClient client, Guid reviewerId, string title = "Test submission")
    {
        var response = await client.PostCreateAsync(title, reviewerId);
        return await ReadAsync<SubmissionResponse>(response, HttpStatusCode.Created);
    }

    public static Task<HttpResponseMessage> PostApproveAsync(this HttpClient client, Guid id, int versionNumber) =>
        client.PostAsJsonAsync($"/submissions/{id}/approve", new { versionNumber });

    public static Task<HttpResponseMessage> PostReturnAsync(this HttpClient client, Guid id, int versionNumber, string comment = "Please revise.") =>
        client.PostAsJsonAsync($"/submissions/{id}/return", new { versionNumber, comment });

    public static Task<HttpResponseMessage> PostResubmitAsync(this HttpClient client, Guid id, byte[]? content = null, string fileName = "revised.pdf", string? comment = null)
    {
        var form = FileForm(content ?? TestFiles.Pdf("revised"), fileName);
        if (comment is not null) form.Add(new StringContent(comment), "comment");
        return client.PostAsync($"/submissions/{id}/resubmit", form);
    }

    public static Task<HttpResponseMessage> PostDistributeAsync(this HttpClient client, Guid id, Guid recipientId) =>
        client.PostAsJsonAsync($"/submissions/{id}/distribute", new { recipientId });

    public static async Task<SubmissionResponse> GetSubmissionAsync(this HttpClient client, Guid id) =>
        await ReadAsync<SubmissionResponse>(await client.GetAsync($"/submissions/{id}"));

    public static async Task<IReadOnlyList<AuditEntryResponse>> GetAuditAsync(this HttpClient client, Guid id) =>
        await ReadAsync<List<AuditEntryResponse>>(await client.GetAsync($"/submissions/{id}/audit"));

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(expected == response.StatusCode, $"Expected {(int)expected} but got {(int)response.StatusCode}: {body}");
        return JsonSerializer.Deserialize<T>(body, Json)!;
    }

    public static async Task EnsureOkAsync(this Task<HttpResponseMessage> request) =>
        await ReadAsync<SubmissionResponse>(await request);
}
