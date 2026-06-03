using System.Net.Http.Json;

using UserGroupSiteSonnet46.Shared.Services;

namespace UserGroupSiteSonnet46.Client.Services;

/// <summary>
/// WebAssembly-side event service that calls the /api/events HTTP endpoints.
/// </summary>
public class ClientEventService(HttpClient http) : IEventService
{
    /// <inheritdoc />
    public async Task<List<EventDto>> GetPublishedEventsAsync()
    {
        return await http.GetFromJsonAsync<List<EventDto>>("api/events/published") ?? [];
    }

    /// <inheritdoc />
    public async Task<List<EventDto>> GetAllEventsAsync()
    {
        return await http.GetFromJsonAsync<List<EventDto>>("api/events") ?? [];
    }

    /// <inheritdoc />
    public async Task<EventDto?> GetEventBySlugAsync(string slug)
    {
        return await http.GetFromJsonAsync<EventDto>($"api/events/by-slug/{slug}");
    }

    /// <inheritdoc />
    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        return await http.GetFromJsonAsync<EventDto>($"api/events/{id}");
    }

    /// <inheritdoc />
    public async Task<EventDto> SaveEventAsync(EventSaveDto dto)
    {
        HttpResponseMessage response;

        if (dto.Id is null)
        {
            response = await http.PostAsJsonAsync("api/events", dto);
        }
        else
        {
            response = await http.PutAsJsonAsync($"api/events/{dto.Id}", dto);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventDto>()
            ?? throw new InvalidOperationException("Empty response from save event.");
    }

    /// <inheritdoc />
    public async Task<List<UserDto>> GetSpeakersAsync()
    {
        return await http.GetFromJsonAsync<List<UserDto>>("api/events/speakers") ?? [];
    }
}
