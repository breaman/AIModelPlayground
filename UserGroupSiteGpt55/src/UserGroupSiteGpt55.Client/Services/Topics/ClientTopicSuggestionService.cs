using System.Net.Http.Json;

using UserGroupSiteGpt55.Shared.Topics;

namespace UserGroupSiteGpt55.Client.Services.Topics;

/// <summary>
/// Implements topic suggestion operations for WebAssembly by calling server APIs.
/// </summary>
public sealed class ClientTopicSuggestionService(HttpClient httpClient) : ITopicSuggestionService
{
    public async Task<IReadOnlyList<TopicSuggestionItem>> GetSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<TopicSuggestionItem>>("api/topics", cancellationToken) ?? [];
    }

    public async Task<TopicActionResult> CreateSuggestionAsync(TopicSuggestionCreateModel model, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/topics", model, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TopicActionResult>(cancellationToken) ??
               TopicActionResult.Failure("The server returned an empty response.");
    }

    public async Task<TopicActionResult> VoteAsync(int topicSuggestionId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"api/topics/{topicSuggestionId}/vote", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TopicActionResult>(cancellationToken) ??
               TopicActionResult.Failure("The server returned an empty response.");
    }

    public async Task<TopicActionResult> RemoveVoteAsync(int topicSuggestionId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/topics/{topicSuggestionId}/vote", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TopicActionResult>(cancellationToken) ??
               TopicActionResult.Failure("The server returned an empty response.");
    }

    public async Task<TopicActionResult> VolunteerAsync(int topicSuggestionId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"api/topics/{topicSuggestionId}/volunteer", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TopicActionResult>(cancellationToken) ??
               TopicActionResult.Failure("The server returned an empty response.");
    }
}