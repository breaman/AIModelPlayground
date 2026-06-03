using System.Net;
using System.Net.Http.Json;

using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Services;

namespace UserGroupSiteOpus48.Client.Services;

/// <summary>
/// WebAssembly <see cref="IEventService"/> that calls the server's <c>/api/events</c> endpoints.
/// Used after the component hydrates on the client (pre-render uses the Server implementation).
/// </summary>
public class ClientEventService(HttpClient http) : IEventService
{
    public async Task<EventListItemDto[]> GetPublishedEventsAsync() =>
        await http.GetFromJsonAsync<EventListItemDto[]>("api/events") ?? [];

    public async Task<EventListItemDto[]> GetAllEventsAsync() =>
        await http.GetFromJsonAsync<EventListItemDto[]>("api/events/all") ?? [];

    public async Task<EventDto?> GetEventBySlugAsync(string slug)
    {
        var response = await http.GetAsync($"api/events/{slug}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventDto>();
    }

    public async Task<EventEditDto?> GetEventForEditAsync(int id)
    {
        var response = await http.GetAsync($"api/events/edit/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventEditDto>();
    }

    public async Task<OperationResult<SavedEventDto>> CreateEventAsync(EventEditDto dto)
    {
        var response = await http.PostAsJsonAsync("api/events", dto);
        return await ReadResultAsync(response);
    }

    public async Task<OperationResult<SavedEventDto>> UpdateEventAsync(EventEditDto dto)
    {
        var response = await http.PutAsJsonAsync("api/events", dto);
        return await ReadResultAsync(response);
    }

    public async Task<SpeakerDto[]> GetAssignableSpeakersAsync() =>
        await http.GetFromJsonAsync<SpeakerDto[]>("api/events/speakers") ?? [];

    /// <summary>Reads an <see cref="OperationResult{T}"/> body, mapping auth/other failures to a message.</summary>
    private static async Task<OperationResult<SavedEventDto>> ReadResultAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<OperationResult<SavedEventDto>>()
                ?? OperationResult<SavedEventDto>.Fail("Unexpected empty response.");
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return OperationResult<SavedEventDto>.Fail("You are not authorized to perform this action.");
        }

        return OperationResult<SavedEventDto>.Fail("The server could not process the request.");
    }
}
