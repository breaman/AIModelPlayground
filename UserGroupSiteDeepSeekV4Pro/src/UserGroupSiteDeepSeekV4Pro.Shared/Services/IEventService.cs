using UserGroupSiteDeepSeekV4Pro.Shared.Models;

namespace UserGroupSiteDeepSeekV4Pro.Shared.Services;

public interface IEventService
{
    Task<List<EventListItemDto>> GetPublishedEventsAsync();
    Task<List<EventListItemDto>> GetAllEventsAsync();
    Task<EventDto?> GetEventByIdAsync(int id);
    Task<EventDto?> GetEventBySlugAsync(string slug);
    Task<EventDto> CreateEventAsync(EventDto dto);
    Task<EventDto> UpdateEventAsync(EventDto dto);
    Task DeleteEventAsync(int id);
    Task<List<UserDto>> GetAvailableSpeakersAsync();
}