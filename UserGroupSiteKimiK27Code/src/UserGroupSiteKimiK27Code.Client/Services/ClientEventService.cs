using System.Net.Http.Json;

using UserGroupSiteKimiK27Code.Shared.Dtos;
using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Client.Services;

public class ClientEventService : IEventService
{
    private readonly HttpClient _httpClient;

    public ClientEventService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<EventListItemDto>> GetPublishedEventsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<EventListItemDto>>("api/events/published") ?? [];
    }

    public async Task<EventDetailDto?> GetEventBySlugAsync(string slug, bool includeUnpublished = false)
    {
        return await _httpClient.GetFromJsonAsync<EventDetailDto>($"api/events/{Uri.EscapeDataString(slug)}");
    }

    public async Task<EventEditDto?> GetEventForEditAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<EventEditDto>($"api/events/{id}/edit");
    }

    public async Task<EventEditResult> CreateEventAsync(EventEditDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/events", dto);
        return await ReadResultAsync(response);
    }

    public async Task<EventEditResult> UpdateEventAsync(int id, EventEditDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/events/{id}", dto);
        return await ReadResultAsync(response);
    }

    public async Task<List<SpeakerDto>> GetSpeakersAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<SpeakerDto>>("api/speakers") ?? [];
    }

    public async Task<List<EventListItemDto>> GetEditableEventsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<EventListItemDto>>("api/events/editable") ?? [];
    }

    private static async Task<EventEditResult> ReadResultAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<EventEditResult>();
            return result ?? EventEditResult.Fail("Unexpected response from server.");
        }

        var content = await response.Content.ReadAsStringAsync();
        return EventEditResult.Fail(string.IsNullOrWhiteSpace(content) ? "Request failed." : content);
    }
}