using System.Net.Http.Json;

using UserGroupSiteKimiK27Code.Shared.Dtos;
using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Client.Services;

public class ClientTopicSuggestionService : ITopicSuggestionService
{
    private readonly HttpClient _httpClient;

    public ClientTopicSuggestionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TopicSuggestionDto>> GetSuggestionsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<TopicSuggestionDto>>("api/topics") ?? [];
    }

    public async Task<TopicSuggestionDto?> CreateSuggestionAsync(CreateTopicSuggestionRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/topics", request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TopicSuggestionDto>();
    }

    public async Task<TopicVoteDto?> VoteAsync(int topicId)
    {
        var response = await _httpClient.PostAsync($"api/topics/{topicId}/vote", null);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TopicVoteDto>();
    }

    public async Task<TopicSuggestionDto?> VolunteerAsync(int topicId)
    {
        var response = await _httpClient.PostAsync($"api/topics/{topicId}/volunteer", null);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TopicSuggestionDto>();
    }
}