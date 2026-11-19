using System.Net;
using System.Text.Json;

namespace DocumentSubmission.Api.IntegrationTests.Infrastructure;

/// <summary>Shared assertion that an error response is RFC 9457 ProblemDetails with the expected status (N2).</summary>
public static class ProblemAssert
{
    public const string ProblemContentType = "application/problem+json";

    public static async Task IsProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(expectedStatus == response.StatusCode,
            $"Expected {(int)expectedStatus} {expectedStatus} but got {(int)response.StatusCode} {response.StatusCode}: {body}");
        IsProblem((int)response.StatusCode, response.Content.Headers.ContentType?.MediaType, body);
    }

    public static void IsProblem(int statusCode, string? contentType, string body)
    {
        Assert.Equal(ProblemContentType, contentType);
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("status", out var status), $"ProblemDetails has no 'status': {body}");
        Assert.Equal(statusCode, status.GetInt32());
        Assert.True(json.RootElement.TryGetProperty("title", out _), $"ProblemDetails has no 'title': {body}");
    }
}
