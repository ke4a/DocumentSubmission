using System.Net.Http.Json;
using System.Text.Json;

namespace DocumentSubmission.Web.Services;

/// <summary>An error from the API, taken from its ProblemDetails response.</summary>
public sealed class ApiException(string title, string? detail = null, IReadOnlyList<string>? errors = null)
    : Exception(detail is null ? title : $"{title} {detail}")
{
    public string Title => title;
    public string? Detail => detail;

    /// <summary>Field errors of a validation ProblemDetails (400).</summary>
    public IReadOnlyList<string> Errors => errors ?? [];

    public static async Task<ApiException> FromResponseAsync(HttpResponseMessage response)
    {
        Problem? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<Problem>();
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Not a ProblemDetails body; fall back to the status code.
        }

        return new ApiException(
            problem?.Title ?? $"{(int)response.StatusCode} {response.ReasonPhrase}",
            problem?.Detail,
            problem?.Errors?.SelectMany(e => e.Value).ToList());
    }

    private sealed record Problem(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
