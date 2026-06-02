using UserGroupSiteKimiK26.Shared.Dtos;

namespace UserGroupSiteKimiK26.Shared.Services;

public interface IEventsService
{
    Task<List<EventListItemDto>> GetPublishedEventsAsync();
    Task<EventListItemDto?> GetEventBySlugAsync(string slug);
    Task<EventDto?> GetEventByIdAsync(int id);
    Task<List<EventListItemDto>> GetEditableEventsForUserAsync();
    Task<(bool Success, List<string> Errors)> CreateEventAsync(EventDto dto);
    Task<(bool Success, List<string> Errors)> UpdateEventAsync(int id, EventDto dto);
    Task<bool> DeleteEventAsync(int id);
}
