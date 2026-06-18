using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Client.Services;

/// <summary>
/// Translates HTTP responses from the server API into <see cref="ServiceResult"/>s,
/// extracting a user-displayable message from RFC 9457 problem details on failure.
/// </summary>
public static class HttpServiceResult
{
    public static async Task<ServiceResult> FromResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return ServiceResult.Ok;
        }

        string? error = null;
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
            error = problem?.Errors is { Count: > 0 }
                ? string.Join(" ", problem.Errors.SelectMany(e => e.Value))
                : problem?.Detail ?? problem?.Title;
        }
        catch
        {
            // Not a problem-details body — fall through to the raw text below.
        }

        if (string.IsNullOrWhiteSpace(error))
        {
            error = $"Request failed ({(int)response.StatusCode}).";
        }

        var errorType = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => ServiceErrorType.Validation,
            HttpStatusCode.Unauthorized => ServiceErrorType.Unauthorized,
            HttpStatusCode.Forbidden => ServiceErrorType.Forbidden,
            HttpStatusCode.NotFound => ServiceErrorType.NotFound,
            HttpStatusCode.Conflict => ServiceErrorType.Conflict,
            _ => ServiceErrorType.Validation
        };

        return ServiceResult.Fail(errorType, error);
    }

    private sealed class ProblemPayload
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        [JsonPropertyName("errors")]
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}