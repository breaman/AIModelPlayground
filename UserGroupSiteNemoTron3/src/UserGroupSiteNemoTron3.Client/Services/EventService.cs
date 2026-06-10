using System.Net.Http.Json;

using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

namespace UserGroupSiteNemoTron3.Client.Services;

public class EventService : IEventService
{
    private readonly HttpClient _httpClient;

    public EventService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EventDto[]> GetPublishedEventsAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<EventDto[]>("/api/events");
        return result ?? [];
    }

    public async Task<EventDto?> GetEventBySlugAsync(string slug)
    {
        return await _httpClient.GetFromJsonAsync<EventDto>($"/api/events/{slug}");
    }

    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<EventDto>($"/api/admin/events/{id}");
    }

    public async Task<EventDto> CreateEventAsync(CreateEventDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/admin/events", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public async Task<EventDto> UpdateEventAsync(int id, UpdateEventDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/admin/events/{id}", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public async Task DeleteEventAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"/api/admin/events/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> CanUserEditEventAsync(int eventId, int userId)
    {
        // This would need a dedicated endpoint; for now, return false on client
        // The server will enforce authorization
        return false;
    }

    public async Task<string> GenerateSlugAsync(string title)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/admin/events/generate-slug", title);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GenerateSlugResponse>();
        return result?.Slug ?? "";
    }

    private record GenerateSlugResponse(string Slug);
}