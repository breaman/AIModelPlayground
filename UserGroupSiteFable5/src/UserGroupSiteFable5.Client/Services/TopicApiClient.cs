using System.Net.Http.Json;

using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Client.Services;

/// <summary>WASM-side <see cref="ITopicService"/> that calls the server's topic API.</summary>
public class TopicApiClient(HttpClient http) : ITopicService
{
    public async Task<List<TopicSuggestionDto>> GetTopicsAsync()
    {
        return await http.GetFromJsonAsync<List<TopicSuggestionDto>>("api/topics") ?? [];
    }

    public async Task<ServiceResult> CreateTopicAsync(TopicCreateDto topic)
    {
        var response = await http.PostAsJsonAsync("api/topics", topic);

        return await HttpServiceResult.FromResponseAsync(response);
    }

    public async Task<ServiceResult> VoteAsync(int topicId)
    {
        var response = await http.PostAsync($"api/topics/{topicId}/vote", content: null);

        return await HttpServiceResult.FromResponseAsync(response);
    }

    public async Task<ServiceResult> VolunteerAsync(int topicId)
    {
        var response = await http.PostAsync($"api/topics/{topicId}/volunteer", content: null);

        return await HttpServiceResult.FromResponseAsync(response);
    }
}