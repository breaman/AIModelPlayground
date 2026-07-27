using System.Net.Http.Json;
using System.Text.Json.Serialization;

using UserGroupSiteOpus5.Shared.Models;

namespace UserGroupSiteOpus5.Client.Services;

/// <summary>
/// The subset of an RFC 9457 problem details body this client needs.
/// </summary>
/// <remarks>
/// Declared here rather than reusing <c>Microsoft.AspNetCore.Mvc.ValidationProblemDetails</c>,
/// which lives in a server-side assembly that has no business being pulled into the WebAssembly
/// payload.
/// </remarks>
internal sealed record ApiProblemDetails
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; init; }
}

/// <summary>
/// Turns an API response back into the <see cref="SaveResult"/> shape the server produced.
/// </summary>
/// <remarks>
/// Failures arrive as RFC 9457 problem details rather than a serialised <see cref="SaveResult"/>,
/// so the messages have to be lifted back out of the <c>errors</c> dictionary. Doing that here
/// keeps every client service reporting failures identically to its server counterpart.
/// </remarks>
public static class ApiResponseReader
{
    /// <summary>Reads a valueless result from a response.</summary>
    public static async Task<SaveResult> ReadSaveResultAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return SaveResult.Success();
        }

        return SaveResult.Failure(await ReadErrorsAsync(response));
    }

    /// <summary>Reads a value-carrying result from a response.</summary>
    public static async Task<SaveResult<T>> ReadSaveResultAsync<T>(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<SaveResult<T>>();
            return body ?? SaveResult<T>.Failure("The server returned an empty response.");
        }

        return SaveResult<T>.Failure(await ReadErrorsAsync(response));
    }

    /// <summary>
    /// Extracts human-readable messages from a problem details body, falling back to a message
    /// derived from the status code when the body is missing or unparseable.
    /// </summary>
    private static async Task<List<string>> ReadErrorsAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();

            if (problem?.Errors is { Count: > 0 } errors)
            {
                return errors.SelectMany(x => x.Value).ToList();
            }

            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                return [problem.Detail];
            }

            if (!string.IsNullOrWhiteSpace(problem?.Title))
            {
                return [problem.Title];
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or NotSupportedException or System.Text.Json.JsonException)
        {
            // A non-JSON error body (a proxy error page, for example) is not worth surfacing raw.
        }

        return [DescribeStatus(response)];
    }

    /// <summary>Produces a message a member can act on from a bare status code.</summary>
    private static string DescribeStatus(HttpResponseMessage response)
    {
        return (int)response.StatusCode switch
        {
            401 => "You need to sign in to do that.",
            403 => "You do not have permission to do that.",
            404 => "That item no longer exists.",
            _ => $"The request failed ({(int)response.StatusCode})."
        };
    }
}
