using System.Net.Http.Json;

using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Client.Services;

/// <summary>WASM-side <see cref="IEventService"/> that calls the server's event API.</summary>
public class EventApiClient(HttpClient http) : IEventService
{
    public async Task<List<EventSummaryDto>> GetEditableEventsAsync()
    {
        return await http.GetFromJsonAsync<List<EventSummaryDto>>("api/events") ?? [];
    }

    public async Task<EventEditDto?> GetEventForEditAsync(int id)
    {
        var response = await http.GetAsync($"api/events/{id}");

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<EventEditDto>()
            : null;
    }

    public async Task<ServiceResult> CreateEventAsync(EventEditDto dto)
    {
        var response = await http.PostAsJsonAsync("api/events", dto);

        return await HttpServiceResult.FromResponseAsync(response);
    }

    public async Task<ServiceResult> UpdateEventAsync(EventEditDto dto)
    {
        var response = await http.PutAsJsonAsync($"api/events/{dto.Id}", dto);

        return await HttpServiceResult.FromResponseAsync(response);
    }

    public async Task<List<SpeakerDto>> GetSpeakersAsync()
    {
        return await http.GetFromJsonAsync<List<SpeakerDto>>("api/speakers") ?? [];
    }
}