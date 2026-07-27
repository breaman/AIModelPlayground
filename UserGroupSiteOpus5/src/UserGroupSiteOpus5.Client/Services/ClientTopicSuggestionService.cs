using System.Net.Http.Json;

using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

namespace UserGroupSiteOpus5.Client.Services;

/// <summary>
/// WebAssembly implementation of <see cref="ITopicSuggestionService"/>, calling the server's HTTP
/// API.
/// </summary>
public class ClientTopicSuggestionService(HttpClient http) : ITopicSuggestionService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TopicSuggestionListItem>> GetSuggestionsAsync()
    {
        return await http.GetFromJsonAsync<List<TopicSuggestionListItem>>("api/topics") ?? [];
    }

    /// <inheritdoc />
    public async Task<SaveResult<int>> CreateSuggestionAsync(TopicSuggestionCreateModel model)
    {
        var response = await http.PostAsJsonAsync("api/topics", model);
        return await response.ReadSaveResultAsync<int>();
    }

    /// <inheritdoc />
    public async Task<SaveResult> VoteAsync(int suggestionId)
    {
        var response = await http.PostAsync($"api/topics/{suggestionId}/vote", content: null);
        return await response.ReadSaveResultAsync();
    }

    /// <inheritdoc />
    public async Task<SaveResult> VolunteerAsync(int suggestionId)
    {
        var response = await http.PostAsync($"api/topics/{suggestionId}/volunteer", content: null);
        return await response.ReadSaveResultAsync();
    }
}
