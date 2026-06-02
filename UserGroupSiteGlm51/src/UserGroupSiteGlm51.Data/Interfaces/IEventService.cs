using UserGroupSiteGlm51.Data.Models;

namespace UserGroupSiteGlm51.Data.Interfaces;

/// <summary>
/// Service for managing user group events — listing, creating, editing, and assigning speakers.
/// </summary>
public interface IEventService
{
    /// <summary>Gets all published events, ordered by event date descending.</summary>
    Task<IEnumerable<Event>> GetPublishedEventsAsync();

    /// <summary>Gets all events (published and unpublished), ordered by event date descending.</summary>
    Task<IEnumerable<Event>> GetAllEventsAsync();

    /// <summary>Gets a single event by its URL slug. Returns null if not found.</summary>
    Task<Event?> GetEventBySlugAsync(string slug);

    /// <summary>Gets a single event by its ID. Returns null if not found.</summary>
    Task<Event?> GetEventByIdAsync(int id);

    /// <summary>Creates a new event.</summary>
    Task<Event> CreateEventAsync(Event eventEntity);

    /// <summary>Updates an existing event.</summary>
    Task<Event> UpdateEventAsync(Event eventEntity);

    /// <summary>Deletes an event by ID.</summary>
    Task DeleteEventAsync(int eventId);

    /// <summary>Assigns a user as a speaker to an event.</summary>
    Task AddSpeakerToEventAsync(int eventId, int userId);

    /// <summary>Removes a user as a speaker from an event.</summary>
    Task RemoveSpeakerFromEventAsync(int eventId, int userId);

    /// <summary>
    /// Checks whether the given user is an editor for the event
    /// (either an Admin or an assigned speaker).
    /// </summary>
    Task<bool> IsEditorAsync(int eventId, int userId);

    /// <summary>
    /// Checks whether an event can be published — requires Title, Slug,
    /// Description, EventDate, Location, and at least one speaker.
    /// </summary>
    Task<bool> CanPublishAsync(Event eventEntity);
}