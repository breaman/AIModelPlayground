using System.Net;
using System.Net.Http.Json;

using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

namespace UserGroupSiteGpt56Sol.Client.Services;

/// <summary>Calls the event API from WebAssembly.</summary>
public sealed class ClientEventService(HttpClient httpClient, AntiforgeryHttpClient writeClient) : IEventService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSummaryDto>> GetPublishedAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<EventSummaryDto>>("api/events", cancellationToken) ?? [];

    /// <inheritdoc />
    public async Task<EventDetailDto?> GetPublishedBySlugAsync(string slug,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/events/{Uri.EscapeDataString(slug)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventDetailDto>(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventAdminListItemDto>> GetManageListAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<EventAdminListItemDto>>("api/events/manage", cancellationToken) ?? [];

    /// <inheritdoc />
    public async Task<EventEditRequest?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/events/manage/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventEditRequest>(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpeakerOptionDto>> GetSpeakerOptionsAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<SpeakerOptionDto>>("api/events/manage/speakers", cancellationToken) ?? [];

    /// <inheritdoc />
    public async Task<ServiceResult<int>> SaveAsync(int? id, EventEditRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await writeClient.SendAsync(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            id.HasValue ? $"api/events/manage/{id.Value}" : "api/events/manage", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceResult<int>>(cancellationToken) ??
               ServiceResult<int>.Failure("The server returned an empty response.");
    }
}