using System.Net;
using System.Net.Http.Json;

using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

namespace UserGroupSiteOpus48.Client.Services;

/// <summary>
/// WebAssembly <see cref="ITopicService"/> that calls the server's <c>/api/topics</c> endpoints.
/// </summary>
public class ClientTopicService(HttpClient http) : ITopicService
{
    public async Task<TopicSuggestionDto[]> GetTopicsAsync() =>
        await http.GetFromJsonAsync<TopicSuggestionDto[]>("api/topics") ?? [];

    public async Task<OperationResult> SuggestTopicAsync(TopicCreateDto dto)
    {
        var response = await http.PostAsJsonAsync("api/topics", dto);
        return await ReadResultAsync(response);
    }

    public async Task<OperationResult> VoteAsync(int topicId)
    {
        var response = await http.PostAsync($"api/topics/{topicId}/vote", null);
        return await ReadResultAsync(response);
    }

    public async Task<OperationResult> VolunteerAsync(int topicId)
    {
        var response = await http.PostAsync($"api/topics/{topicId}/volunteer", null);
        return await ReadResultAsync(response);
    }

    /// <summary>Reads an <see cref="OperationResult"/> body, mapping auth/other failures to a message.</summary>
    private static async Task<OperationResult> ReadResultAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<OperationResult>()
                ?? OperationResult.Fail("Unexpected empty response.");
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return OperationResult.Fail("You must be signed in to do that.");
        }

        return OperationResult.Fail("The server could not process the request.");
    }
}