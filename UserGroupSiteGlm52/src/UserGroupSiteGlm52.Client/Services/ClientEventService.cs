using System.Net.Http.Json;

using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Client.Services;

/// <summary>
/// HTTP-backed <see cref="IEventService"/> for WebAssembly. Calls the Minimal API;
/// pre-rendering uses the server (DB-backed) implementation registered in the Server project.
/// </summary>
public sealed class ClientEventService(HttpClient http) : IEventService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSummaryDto>> GetPublishedEventsAsync()
    {
        return await http.GetFromJsonAsync<IReadOnlyList<EventSummaryDto>>("api/events") ?? [];
    }

    /// <inheritdoc />
    public async Task<EventDetailDto?> GetPublishedBySlugAsync(string slug)
    {
        var response = await http.GetAsync($"api/events/{Uri.EscapeDataString(slug)}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventDetailDto>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSummaryDto>> GetEditableListAsync()
    {
        return await http.GetFromJsonAsync<IReadOnlyList<EventSummaryDto>>("api/events/manage") ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpeakerOptionDto>> GetSpeakerOptionsAsync()
    {
        return await http.GetFromJsonAsync<IReadOnlyList<SpeakerOptionDto>>("api/events/speakers") ?? [];
    }

    /// <inheritdoc />
    public async Task<ServiceResult<EventEditDto>> GetForEditAsync(int id)
    {
        var response = await http.GetAsync($"api/events/edit/{id}");
        return await ClientResults.ReadAsync<EventEditDto>(response);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<EventDetailDto>> CreateAsync(EventEditDto dto)
    {
        var response = await http.PostAsJsonAsync("api/events/admin", dto);
        return await ClientResults.ReadAsync<EventDetailDto>(response);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<EventDetailDto>> UpdateAsync(int id, EventEditDto dto)
    {
        var response = await http.PutAsJsonAsync($"api/events/edit/{id}", dto);
        return await ClientResults.ReadAsync<EventDetailDto>(response);
    }
}