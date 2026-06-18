using System.Net.Http.Json;

using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Client.Services;

public class EventClientService : IEventClientService
{
    private readonly HttpClient _httpClient;

    public EventClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Event>>("/api/events/all") ?? Enumerable.Empty<Event>();
    }

    public async Task<IEnumerable<Event>> GetPublishedEventsAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Event>>("/api/events") ?? Enumerable.Empty<Event>();
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<Event>($"/api/events/{id}");
    }

    public async Task<Event?> GetEventBySlugAsync(string slug)
    {
        return await _httpClient.GetFromJsonAsync<Event>($"/api/events/slug/{slug}");
    }

    public async Task<Event> CreateEventAsync(Event evt)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/events", evt);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Event>() ?? evt;
    }

    public async Task<Event> UpdateEventAsync(Event evt)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/events/{evt.Id}", evt);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Event>() ?? evt;
    }

    public async Task DeleteEventAsync(int id)
    {
        await _httpClient.DeleteAsync($"/api/events/{id}");
    }

    public async Task<bool> CheckSlugAvailabilityAsync(string slug, int? excludeId = null)
    {
        var url = excludeId.HasValue
            ? $"/api/events/slug/{slug}/check?excludeId={excludeId}"
            : $"/api/events/slug/{slug}/check";
        return await _httpClient.GetFromJsonAsync<bool>(url);
    }

    public async Task<IEnumerable<Speaker>> GetApprovedSpeakersAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Speaker>>("/api/events/speakers") ?? Enumerable.Empty<Speaker>();
    }
}