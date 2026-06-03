using System.Net.Http.Json;

using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Client.Services;

public class TopicSuggestionClientService : ITopicSuggestionClientService
{
    private readonly HttpClient _httpClient;

    public TopicSuggestionClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<TopicSuggestion>> GetAllSuggestionsAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<TopicSuggestion>>("/api/topics") ?? Enumerable.Empty<TopicSuggestion>();
    }

    public async Task<TopicSuggestion?> GetByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<TopicSuggestion>($"/api/topics/{id}");
    }

    public async Task<TopicSuggestion> CreateSuggestionAsync(TopicSuggestion suggestion)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/topics", suggestion);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TopicSuggestion>() ?? suggestion;
    }

    public async Task<bool> VoteAsync(int topicId)
    {
        var response = await _httpClient.PostAsync($"/api/topics/{topicId}/vote", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveVoteAsync(int topicId)
    {
        var response = await _httpClient.DeleteAsync($"/api/topics/{topicId}/vote");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> HasVotedAsync(int topicId)
    {
        return await _httpClient.GetFromJsonAsync<bool>($"/api/topics/{topicId}/voted");
    }

    public async Task<bool> VolunteerAsync(int topicId)
    {
        var response = await _httpClient.PostAsync($"/api/topics/{topicId}/volunteer", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveVolunteerAsync(int topicId)
    {
        var response = await _httpClient.DeleteAsync($"/api/topics/{topicId}/volunteer");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> IsUserVolunteerAsync(int topicId)
    {
        return await _httpClient.GetFromJsonAsync<bool>($"/api/topics/{topicId}/volunteer");
    }
}
