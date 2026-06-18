using UserGroupSiteFable5.Shared.Dtos;

namespace UserGroupSiteFable5.Shared.Services;

/// <summary>
/// Event management operations for the interactive pages. Implemented over HTTP in the
/// Client project and directly against the database in the Server project so the same
/// components work during pre-rendering and after WASM hydration.
/// </summary>
public interface IEventService
{
    /// <summary>Events the current user may edit: all for Admins, assigned ones for Speakers.</summary>
    Task<List<EventSummaryDto>> GetEditableEventsAsync();

    /// <summary>Loads an event for editing, or null when missing or not editable by the caller.</summary>
    Task<EventEditDto?> GetEventForEditAsync(int id);

    Task<ServiceResult> CreateEventAsync(EventEditDto dto);

    Task<ServiceResult> UpdateEventAsync(EventEditDto dto);

    /// <summary>Users in the Speaker role, for the event speaker picker.</summary>
    Task<List<SpeakerDto>> GetSpeakersAsync();
}