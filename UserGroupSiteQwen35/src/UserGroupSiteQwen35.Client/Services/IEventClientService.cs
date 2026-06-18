using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Client.Services;

public interface IEventClientService
{
    Task<IEnumerable<Event>> GetAllEventsAsync();
    Task<IEnumerable<Event>> GetPublishedEventsAsync();
    Task<Event?> GetEventByIdAsync(int id);
    Task<Event?> GetEventBySlugAsync(string slug);
    Task<Event> CreateEventAsync(Event evt);
    Task<Event> UpdateEventAsync(Event evt);
    Task DeleteEventAsync(int id);
    Task<bool> CheckSlugAvailabilityAsync(string slug, int? excludeId = null);
    Task<IEnumerable<Speaker>> GetApprovedSpeakersAsync();
}