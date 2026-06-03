using System.Net.Http.Json;

using UserGroupSiteSonnet46.Shared.Services;

namespace UserGroupSiteSonnet46.Client.Services;

/// <summary>
/// WebAssembly-side topic service that calls the /api/topics HTTP endpoints.
/// </summary>
public class ClientTopicService(HttpClient http) : ITopicService
{
    /// <inheritdoc />
    public async Task<List<TopicSuggestionDto>> GetTopicsAsync()
    {
        return await http.GetFromJsonAsync<List<TopicSuggestionDto>>("api/topics") ?? [];
    }

    /// <inheritdoc />
    public async Task<TopicSuggestionDto> SuggestTopicAsync(string title, string? description)
    {
        var response = await http.PostAsJsonAsync("api/topics", new { title, description });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TopicSuggestionDto>()
            ?? throw new InvalidOperationException("Empty response from suggest topic.");
    }

    /// <inheritdoc />
    public async Task VoteAsync(int topicId)
    {
        var response = await http.PostAsync($"api/topics/{topicId}/vote", null);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task VolunteerAsync(int topicId)
    {
        var response = await http.PostAsync($"api/topics/{topicId}/volunteer", null);
        response.EnsureSuccessStatusCode();
    }
}
