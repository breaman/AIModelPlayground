using UserGroupSiteNemoTron3.Shared.DTOs;

namespace UserGroupSiteNemoTron3.Shared.Services;

public interface IEventService
{
    Task<EventDto[]> GetPublishedEventsAsync();
    Task<EventDto?> GetEventBySlugAsync(string slug);
    Task<EventDto?> GetEventByIdAsync(int id);
    Task<EventDto> CreateEventAsync(CreateEventDto dto);
    Task<EventDto> UpdateEventAsync(int id, UpdateEventDto dto);
    Task DeleteEventAsync(int id);
    Task<bool> CanUserEditEventAsync(int eventId, int userId);
    Task<string> GenerateSlugAsync(string title);
}