using System.Net.Http.Json;

using UserGroupSiteKimiK26.Shared.Dtos;
using UserGroupSiteKimiK26.Shared.Services;

namespace UserGroupSiteKimiK26.Client.Services;

public class TopicSuggestionsService(HttpClient httpClient) : ITopicSuggestionsService
{
    public async Task<List<TopicSuggestionDto>> GetAllSuggestionsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<TopicSuggestionDto>>("api/topics") ?? [];
    }

    public async Task<TopicSuggestionDto> CreateSuggestionAsync(CreateSuggestionDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("api/topics", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TopicSuggestionDto>()
               ?? throw new InvalidOperationException("Failed to parse created suggestion.");
    }

    public async Task<VoteResultDto> VoteAsync(int topicId)
    {
        var response = await httpClient.PostAsync($"api/topics/{topicId}/vote", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VoteResultDto>()
               ?? throw new InvalidOperationException("Failed to parse vote result.");
    }

    public async Task<TopicSuggestionDto> VolunteerAsync(int topicId)
    {
        var response = await httpClient.PostAsync($"api/topics/{topicId}/volunteer", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TopicSuggestionDto>()
               ?? throw new InvalidOperationException("Failed to parse volunteer result.");
    }

    public async Task<bool> DeleteSuggestionAsync(int topicId)
    {
        var response = await httpClient.DeleteAsync($"api/topics/{topicId}");
        return response.IsSuccessStatusCode;
    }
}
