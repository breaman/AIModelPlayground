using UserGroupSiteGlm52.Shared.Models;

namespace UserGroupSiteGlm52.Shared.Services;

/// <summary>
/// Event features: public listing/detail plus admin/speaker authoring.
/// Implementations: <c>ServerEventService</c> (DB-backed, in Server) and
/// <c>ClientEventService</c> (HTTP to the Minimal API, in Client).
/// </summary>
public interface IEventService
{
    /// <summary>Published events ordered by EventDate descending, for the public home page.</summary>
    Task<IReadOnlyList<EventSummaryDto>> GetPublishedEventsAsync();

    /// <summary>Published event by slug, or null. Admins/assigned speakers may also retrieve unpublished events.</summary>
    Task<EventDetailDto?> GetPublishedBySlugAsync(string slug);

    /// <summary>All events (published + drafts) for the admin/speaker manage list.</summary>
    Task<IReadOnlyList<EventSummaryDto>> GetEditableListAsync();

    /// <summary>Users in the Speaker role, for the event editor's speaker picker (create mode has no event yet).</summary>
    Task<IReadOnlyList<SpeakerOptionDto>> GetSpeakerOptionsAsync();

    /// <summary>An event for editing. Fails with 403 when not authorized, 404 when not found.</summary>
    Task<ServiceResult<EventEditDto>> GetForEditAsync(int id);

    /// <summary>Create a new event (Admin only).</summary>
    Task<ServiceResult<EventDetailDto>> CreateAsync(EventEditDto dto);

    /// <summary>Update an existing event (Admin or assigned speaker). Enforces publish guards server-side.</summary>
    Task<ServiceResult<EventDetailDto>> UpdateAsync(int id, EventEditDto dto);
}