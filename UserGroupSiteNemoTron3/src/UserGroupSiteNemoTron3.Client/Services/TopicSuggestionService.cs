using System.Net.Http.Json;

using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

namespace UserGroupSiteNemoTron3.Client.Services;

public class TopicSuggestionService : ITopicSuggestionService
{
    private readonly HttpClient _httpClient;

    public TopicSuggestionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TopicSuggestionDto[]> GetAllAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<TopicSuggestionDto[]>("/api/topics");
        return result ?? [];
    }

    public async Task<TopicSuggestionDto?> GetByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<TopicSuggestionDto>($"/api/topics/{id}");
    }

    public async Task<TopicSuggestionDto> CreateAsync(CreateTopicSuggestionDto dto, int userId)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/topics", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TopicSuggestionDto>())!;
    }

    public async Task VoteAsync(int topicId, int userId)
    {
        var response = await _httpClient.PostAsync($"/api/topics/{topicId}/vote", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveVoteAsync(int topicId, int userId)
    {
        var response = await _httpClient.DeleteAsync($"/api/topics/{topicId}/vote");
        response.EnsureSuccessStatusCode();
    }

    public async Task VolunteerAsync(int topicId, int userId)
    {
        var response = await _httpClient.PostAsync($"/api/topics/{topicId}/volunteer", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> HasUserVotedAsync(int topicId, int userId)
    {
        // This would need a dedicated endpoint; for now, we'll check via GetByIdAsync
        var topic = await GetByIdAsync(topicId);
        return topic?.HasCurrentUserVoted ?? false;
    }
}