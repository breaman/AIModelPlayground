using System.Net.Http.Json;

using UserGroupSiteKimiK26.Shared.Dtos;
using UserGroupSiteKimiK26.Shared.Services;

namespace UserGroupSiteKimiK26.Client.Services;

public class EventsService(HttpClient httpClient) : IEventsService
{
    public async Task<List<EventListItemDto>> GetPublishedEventsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<EventListItemDto>>("api/events") ?? [];
    }

    public async Task<EventListItemDto?> GetEventBySlugAsync(string slug)
    {
        return await httpClient.GetFromJsonAsync<EventListItemDto>($"api/events/{slug}");
    }

    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<EventDto>($"api/events/manage/{id}");
    }

    public async Task<List<EventListItemDto>> GetEditableEventsForUserAsync()
    {
        return await httpClient.GetFromJsonAsync<List<EventListItemDto>>("api/events/manage") ?? [];
    }

    public async Task<(bool Success, List<string> Errors)> CreateEventAsync(EventDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("api/events", dto);
        if (response.IsSuccessStatusCode)
            return (true, []);

        var errors = await response.Content.ReadFromJsonAsync<List<string>>();
        return (false, errors ?? ["An error occurred while creating the event."]);
    }

    public async Task<(bool Success, List<string> Errors)> UpdateEventAsync(int id, EventDto dto)
    {
        var response = await httpClient.PutAsJsonAsync($"api/events/{id}", dto);
        if (response.IsSuccessStatusCode)
            return (true, []);

        var errors = await response.Content.ReadFromJsonAsync<List<string>>();
        return (false, errors ?? ["An error occurred while updating the event."]);
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"api/events/{id}");
        return response.IsSuccessStatusCode;
    }
}