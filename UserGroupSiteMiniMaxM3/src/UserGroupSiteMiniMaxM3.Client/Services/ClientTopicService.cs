using System.Net.Http.Json;

using UserGroupSiteMiniMaxM3.Shared.Models.Topics;
using UserGroupSiteMiniMaxM3.Shared.Services;

namespace UserGroupSiteMiniMaxM3.Client.Services;

/// <summary>
/// WebAssembly client implementation of <see cref="ITopicService"/>. Calls
/// the HTTP endpoints under <c>/api/topics</c>.
/// </summary>
public class ClientTopicService(HttpClient http) : ITopicService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TopicSummaryDto>> ListAsync(string? currentUserId, CancellationToken cancellationToken = default)
    {
        // Server resolves "me" from the auth cookie; ignore any caller-supplied id.
        return await http.GetFromJsonAsync<List<TopicSummaryDto>>("api/topics", cancellationToken) ?? [];
    }

    /// <inheritdoc />
    public async Task<TopicSummaryDto> CreateAsync(TopicCreateDto input, string currentUserId, CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsJsonAsync("api/topics", input, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadFromJsonAsync<TopicSummaryDto>(cancellationToken))!;
        }

        // Surface RFC 9457 validation errors verbatim for the UI.
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(cancellationToken: cancellationToken);
        var message = problem?.Errors is { Count: > 0 }
            ? string.Join("; ", problem.Errors.SelectMany(kv => kv.Value))
            : $"Create failed ({(int)response.StatusCode}).";
        throw new InvalidOperationException(message);
    }

    /// <inheritdoc />
    public async Task<bool> ToggleVoteAsync(int topicId, string currentUserId, CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsync($"api/topics/{topicId}/vote", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ToggleResult>(cancellationToken: cancellationToken);
        return result?.IsOn ?? false;
    }

    /// <inheritdoc />
    public async Task<bool> ToggleVolunteerAsync(int topicId, string currentUserId, CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsync($"api/topics/{topicId}/volunteer", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ToggleResult>(cancellationToken: cancellationToken);
        return result?.IsOn ?? false;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int topicId, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var response = await http.DeleteAsync($"api/topics/{topicId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private record ToggleResult(bool IsOn);
    private record ValidationProblem(Dictionary<string, string[]>? Errors);
}