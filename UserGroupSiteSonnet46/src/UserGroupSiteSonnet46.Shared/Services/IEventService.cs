namespace UserGroupSiteSonnet46.Shared.Services;

/// <summary>Represents a full event with its speaker list.</summary>
public record EventDto(
    int Id,
    string Title,
    string Slug,
    string? ShortDescription,
    string? Description,
    DateTimeOffset? EventDateTime,
    string? Location,
    bool IsPublished,
    List<UserDto> Speakers);

/// <summary>DTO used when creating or updating an event.</summary>
public record EventSaveDto(
    int? Id,
    string Title,
    string Slug,
    string? ShortDescription,
    string? Description,
    DateTimeOffset? EventDateTime,
    string? Location,
    bool IsPublished,
    List<int> SpeakerIds);

/// <summary>
/// Service for reading and managing events.
/// Implemented differently on server (direct DB) and client (HTTP).
/// </summary>
public interface IEventService
{
    /// <summary>Returns all published events for the public home page.</summary>
    Task<List<EventDto>> GetPublishedEventsAsync();

    /// <summary>Returns all events (published and draft) for admins and speakers.</summary>
    Task<List<EventDto>> GetAllEventsAsync();

    /// <summary>Returns a single event by slug; null if not found.</summary>
    Task<EventDto?> GetEventBySlugAsync(string slug);

    /// <summary>Returns a single event by ID; null if not found.</summary>
    Task<EventDto?> GetEventByIdAsync(int id);

    /// <summary>Creates or updates an event. Pass <c>null</c> Id to create.</summary>
    Task<EventDto> SaveEventAsync(EventSaveDto dto);

    /// <summary>Returns all users in the Speaker role for the speaker assignment dropdown.</summary>
    Task<List<UserDto>> GetSpeakersAsync();
}