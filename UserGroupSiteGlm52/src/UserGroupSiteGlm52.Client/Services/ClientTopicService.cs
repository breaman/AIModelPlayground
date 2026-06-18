using System.Net.Http.Json;

using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Client.Services;

/// <summary>HTTP-backed <see cref="ITopicService"/> for WebAssembly.</summary>
public sealed class ClientTopicService(HttpClient http) : ITopicService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TopicDto>> GetTopicsAsync()
    {
        return await http.GetFromJsonAsync<IReadOnlyList<TopicDto>>("api/topics") ?? [];
    }

    /// <inheritdoc />
    public async Task<ServiceResult<TopicDto>> SuggestAsync(TopicInputDto dto)
    {
        var response = await http.PostAsJsonAsync("api/topics", dto);
        return await ClientResults.ReadAsync<TopicDto>(response);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> VoteAsync(int id)
    {
        var response = await http.PostAsync($"api/topics/{id}/vote", content: null);
        return await ClientResults.ReadVoidAsync(response);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UnvoteAsync(int id)
    {
        var response = await http.DeleteAsync($"api/topics/{id}/vote");
        return await ClientResults.ReadVoidAsync(response);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> VolunteerAsync(int id)
    {
        var response = await http.PostAsync($"api/topics/{id}/volunteer", content: null);
        return await ClientResults.ReadVoidAsync(response);
    }
}