using System.Net.Http.Json;

using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Client.Services;

public class ClientEventService(HttpClient http) : IEventService
{
    public async Task<List<EventListItemDto>> GetPublishedEventsAsync()
    {
        return await http.GetFromJsonAsync<List<EventListItemDto>>("api/events/published") ?? [];
    }

    public async Task<List<EventListItemDto>> GetAllEventsAsync()
    {
        return await http.GetFromJsonAsync<List<EventListItemDto>>("api/events") ?? [];
    }

    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        return await http.GetFromJsonAsync<EventDto>($"api/events/{id}");
    }

    public async Task<EventDto?> GetEventBySlugAsync(string slug)
    {
        return await http.GetFromJsonAsync<EventDto>($"api/events/slug/{slug}");
    }

    public async Task<EventDto> CreateEventAsync(EventDto dto)
    {
        var response = await http.PostAsJsonAsync("api/events", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public async Task<EventDto> UpdateEventAsync(EventDto dto)
    {
        var response = await http.PutAsJsonAsync($"api/events/{dto.Id}", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public async Task DeleteEventAsync(int id)
    {
        var response = await http.DeleteAsync($"api/events/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<UserDto>> GetAvailableSpeakersAsync()
    {
        return await http.GetFromJsonAsync<List<UserDto>>("api/events/speakers/available") ?? [];
    }
}
