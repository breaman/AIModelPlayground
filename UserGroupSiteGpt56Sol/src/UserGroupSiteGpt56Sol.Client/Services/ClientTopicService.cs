using System.Net.Http.Json;

using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

namespace UserGroupSiteGpt56Sol.Client.Services;

/// <summary>Calls the topic API from WebAssembly.</summary>
public sealed class ClientTopicService(HttpClient httpClient, AntiforgeryHttpClient writeClient) : ITopicService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TopicSuggestionDto>> GetAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<TopicSuggestionDto>>("api/topics", cancellationToken) ?? [];

    /// <inheritdoc />
    public async Task<ServiceResult<int>> CreateAsync(CreateTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await writeClient.SendAsync(HttpMethod.Post, "api/topics", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceResult<int>>(cancellationToken) ??
               ServiceResult<int>.Failure("The server returned an empty response.");
    }

    /// <inheritdoc />
    public Task<ServiceResult> VoteAsync(int topicId, CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Post, $"api/topics/{topicId}/vote", cancellationToken);

    /// <inheritdoc />
    public Task<ServiceResult> UnvoteAsync(int topicId, CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Delete, $"api/topics/{topicId}/vote", cancellationToken);

    /// <inheritdoc />
    public Task<ServiceResult> VolunteerAsync(int topicId, CancellationToken cancellationToken = default) =>
        SendCommandAsync(HttpMethod.Post, $"api/topics/{topicId}/volunteer", cancellationToken);

    /// <summary>Sends a topic command and deserializes its common result.</summary>
    private async Task<ServiceResult> SendCommandAsync(HttpMethod method, string uri,
        CancellationToken cancellationToken)
    {
        using var response = await writeClient.SendAsync<object>(method, uri, null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken) ??
               ServiceResult.Failure("The server returned an empty response.");
    }
}