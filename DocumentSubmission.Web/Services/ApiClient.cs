using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace DocumentSubmission.Web.Services;

/// <summary>
/// All calls to the Document Submission API. Sends the session's bearer token and turns error
/// responses into <see cref="ApiException"/>. A 401 ends the session and goes back to the login page.
/// </summary>
public sealed class ApiClient(HttpClient http, UserSession session, NavigationManager navigation)
{
    public const long MaxFileSize = 10 * 1024 * 1024;

    // The API writes enums as strings ("Submitted", ...).
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Task<LoginResponse> LoginAsync(Guid userId) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "auth/login", JsonContent.Create(new { userId }));

    public Task<List<Person>> GetReviewersAsync() => SendAsync<List<Person>>(HttpMethod.Get, "reviewers");

    public Task<List<Person>> GetRecipientsAsync() => SendAsync<List<Person>>(HttpMethod.Get, "recipients");

    public Task<List<SubmissionSummary>> GetSubmissionsAsync() =>
        SendAsync<List<SubmissionSummary>>(HttpMethod.Get, "submissions");

    public Task<Submission> GetSubmissionAsync(Guid id) => SendAsync<Submission>(HttpMethod.Get, $"submissions/{id}");

    public Task<List<AuditEntry>> GetAuditAsync(Guid id) =>
        SendAsync<List<AuditEntry>>(HttpMethod.Get, $"submissions/{id}/audit");

    public Task<Submission> CreateAsync(string title, Guid reviewerId, IBrowserFile file, string? comment)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(title), "title" },
            { new StringContent(reviewerId.ToString()), "reviewerId" },
        };
        AddFileAndComment(form, file, comment);
        return SendAsync<Submission>(HttpMethod.Post, "submissions", form);
    }

    public Task<Submission> ResubmitAsync(Guid id, IBrowserFile file, string? comment)
    {
        var form = new MultipartFormDataContent();
        AddFileAndComment(form, file, comment);
        return SendAsync<Submission>(HttpMethod.Post, $"submissions/{id}/resubmit", form);
    }

    public Task<Submission> ApproveAsync(Guid id, int versionNumber) =>
        SendAsync<Submission>(HttpMethod.Post, $"submissions/{id}/approve", JsonContent.Create(new { versionNumber }));

    public Task<Submission> ReturnAsync(Guid id, int versionNumber, string comment) =>
        SendAsync<Submission>(HttpMethod.Post, $"submissions/{id}/return", JsonContent.Create(new { versionNumber, comment }));

    public Task<Submission> DistributeAsync(Guid id, Guid recipientId) =>
        SendAsync<Submission>(HttpMethod.Post, $"submissions/{id}/distribute", JsonContent.Create(new { recipientId }));

    public async Task<byte[]> DownloadAsync(Guid id, int versionNumber)
    {
        using var response = await SendAsync(HttpMethod.Get, $"submissions/{id}/versions/{versionNumber}/content");
        return await response.Content.ReadAsByteArrayAsync();
    }

    private static void AddFileAndComment(MultipartFormDataContent form, IBrowserFile file, string? comment)
    {
        if (file.Size > MaxFileSize)
            throw new ApiException("The file is too large.", "The maximum size is 10 MB.");

        // The API detects the content type itself.
        var content = new StreamContent(file.OpenReadStream(MaxFileSize));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(content, "file", file.Name);

        if (!string.IsNullOrWhiteSpace(comment))
            form.Add(new StringContent(comment), "comment");
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, HttpContent? content = null)
    {
        using var response = await SendAsync(method, path, content);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        if (session.Token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException("The API could not be reached.", ex.Message);
        }

        if (response.IsSuccessStatusCode)
            return response;

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized && session.IsLoggedIn)
            {
                // Token expired or rejected: log in again.
                session.LogOut();
                navigation.NavigateTo("login");
            }

            throw await ApiException.FromResponseAsync(response);
        }
    }
}
