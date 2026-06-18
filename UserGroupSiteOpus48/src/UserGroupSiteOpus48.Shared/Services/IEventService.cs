using UserGroupSiteOpus48.Shared.Dtos;

namespace UserGroupSiteOpus48.Shared.Services;

/// <summary>
/// Event data operations. Implemented twice (dual-mode): a Client version that calls the HTTP API
/// and a Server version that hits the database directly (used during pre-render).
/// </summary>
public interface IEventService
{
    /// <summary>Published events, newest first by event date/time. Public.</summary>
    Task<EventListItemDto[]> GetPublishedEventsAsync();

    /// <summary>All events including unpublished, newest first. Editors/admins only.</summary>
    Task<EventListItemDto[]> GetAllEventsAsync();

    /// <summary>A single event by its slug for public display, or null if not found/unpublished.</summary>
    Task<EventDto?> GetEventBySlugAsync(string slug);

    /// <summary>The editable payload for an event, or null if not found.</summary>
    Task<EventEditDto?> GetEventForEditAsync(int id);

    /// <summary>Creates a new event. Admins only (enforced server-side).</summary>
    Task<OperationResult<SavedEventDto>> CreateEventAsync(EventEditDto dto);

    /// <summary>Updates an existing event. Editor = any admin or an assigned speaker.</summary>
    Task<OperationResult<SavedEventDto>> UpdateEventAsync(EventEditDto dto);

    /// <summary>Users in the Speaker role that can be assigned to an event.</summary>
    Task<SpeakerDto[]> GetAssignableSpeakersAsync();
}