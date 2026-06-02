using System.Net.Http.Json;

using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Client.Services;

public class ClientTopicSuggestionService(HttpClient http) : ITopicSuggestionService
{
    public async Task<List<TopicSuggestionListItemDto>> GetAllAsync()
    {
        return await http.GetFromJsonAsync<List<TopicSuggestionListItemDto>>("api/topics") ?? [];
    }

    public async Task<TopicSuggestionDto> CreateAsync(TopicSuggestionDto dto)
    {
        var response = await http.PostAsJsonAsync("api/topics", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TopicSuggestionDto>())!;
    }

    public async Task VoteAsync(int topicId)
    {
        var response = await http.PostAsync($"api/topics/{topicId}/vote", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveVoteAsync(int topicId)
    {
        var response = await http.DeleteAsync($"api/topics/{topicId}/vote");
        response.EnsureSuccessStatusCode();
    }

    public async Task VolunteerAsync(int topicId)
    {
        var response = await http.PostAsync($"api/topics/{topicId}/volunteer", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveVolunteerAsync(int topicId)
    {
        var response = await http.DeleteAsync($"api/topics/{topicId}/volunteer");
        response.EnsureSuccessStatusCode();
    }
}
