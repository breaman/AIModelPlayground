using System.Net.Http.Json;

using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;
using UserGroupSiteGlm51.Shared.Models;

namespace UserGroupSiteGlm51.Client.Services;

/// <summary>
/// Client-side implementation of <see cref="IEventService"/> that calls
/// the server's minimal API endpoints via HttpClient.
/// </summary>
public class EventService(HttpClient http) : IEventService
{
    public async Task<IEnumerable<Event>> GetPublishedEventsAsync()
    {
        // Client uses DTOs from the API, but the interface returns Event entities.
        // We'll map DTOs back to Event objects.
        var dtos = await http.GetFromJsonAsync<IEnumerable<EventDto>>("api/events") ?? [];
        return dtos.Select(MapFromDto);
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        // This endpoint doesn't exist on the public API; admins use the same endpoint with a flag
        // For now, we'll call a future admin endpoint. Using the same published endpoint for now.
        var dtos = await http.GetFromJsonAsync<IEnumerable<EventDto>>("api/events/all") ?? [];
        return dtos.Select(MapFromDto);
    }

    public async Task<Event?> GetEventBySlugAsync(string slug)
    {
        var dto = await http.GetFromJsonAsync<EventDto>($"api/events/{slug}");
        return dto is null ? null : MapFromDto(dto);
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        var dto = await http.GetFromJsonAsync<EventDto>($"api/events/id/{id}");
        return dto is null ? null : MapFromDto(dto);
    }

    public async Task<Event> CreateEventAsync(Event eventEntity)
    {
        var request = new CreateEventRequest(
            eventEntity.Title,
            eventEntity.Slug,
            eventEntity.ShortDescription,
            eventEntity.Description,
            eventEntity.EventDate,
            eventEntity.Location,
            eventEntity.IsPublished,
            eventEntity.EventSpeakers.Select(es => es.UserId).ToList()
        );

        var response = await http.PostAsJsonAsync("api/events", request);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<EventDto>();
        return MapFromDto(dto!);
    }

    public async Task<Event> UpdateEventAsync(Event eventEntity)
    {
        var request = new UpdateEventRequest(
            eventEntity.Title,
            eventEntity.Slug,
            eventEntity.ShortDescription,
            eventEntity.Description,
            eventEntity.EventDate,
            eventEntity.Location,
            eventEntity.IsPublished,
            eventEntity.EventSpeakers.Select(es => es.UserId).ToList()
        );

        var response = await http.PutAsJsonAsync($"api/events/{eventEntity.Id}", request);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<EventDto>();
        return MapFromDto(dto!);
    }

    public async Task DeleteEventAsync(int eventId)
    {
        var response = await http.DeleteAsync($"api/events/{eventId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task AddSpeakerToEventAsync(int eventId, int userId)
    {
        var response = await http.PostAsJsonAsync($"api/events/{eventId}/speakers", new { UserId = userId });
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveSpeakerFromEventAsync(int eventId, int userId)
    {
        var response = await http.DeleteAsync($"api/events/{eventId}/speakers/{userId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsEditorAsync(int eventId, int userId)
    {
        return await http.GetFromJsonAsync<bool>($"api/events/{eventId}/is-editor/{userId}");
    }

    public Task<bool> CanPublishAsync(Event eventEntity)
    {
        // Client-side validation — same logic as server but without DB check for speakers
        var canPublish = !string.IsNullOrWhiteSpace(eventEntity.Title)
                         && !string.IsNullOrWhiteSpace(eventEntity.Slug)
                         && !string.IsNullOrWhiteSpace(eventEntity.Description)
                         && eventEntity.EventDate.HasValue
                         && !string.IsNullOrWhiteSpace(eventEntity.Location);

        return Task.FromResult(canPublish);
    }

    private static Event MapFromDto(EventDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Slug = dto.Slug,
        ShortDescription = dto.ShortDescription,
        Description = dto.Description,
        EventDate = dto.EventDate,
        Location = dto.Location,
        IsPublished = dto.IsPublished,
        EventSpeakers = dto.Speakers.Select(s => new EventSpeaker
        {
            UserId = s.UserId,
            EventId = dto.Id,
            User = new User { Id = s.UserId, FirstName = s.FirstName, LastName = s.LastName, Email = s.Email }
        }).ToList()
    };
}