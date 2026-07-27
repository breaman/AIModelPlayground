using UserGroupSiteOpus5.Shared.Models;

namespace UserGroupSiteOpus5.Shared.Services;

/// <summary>
/// Reads and writes events. Implemented twice: directly against the database on the server (used
/// during pre-rendering) and over HTTP in the WebAssembly client.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Returns published events, most recent first. Available to anonymous visitors.
    /// </summary>
    Task<IReadOnlyList<EventListItem>> GetPublishedEventsAsync();

    /// <summary>
    /// Returns the events the current user may manage: all of them for an administrator, or only
    /// the ones they are assigned to for a speaker.
    /// </summary>
    Task<IReadOnlyList<EventListItem>> GetManageableEventsAsync();

    /// <summary>
    /// Returns an event by slug, or null when it does not exist or the caller may not see it.
    /// Unpublished events are visible only to their editors.
    /// </summary>
    /// <param name="slug">The event's URL slug.</param>
    Task<EventDetail?> GetEventBySlugAsync(string slug);

    /// <summary>
    /// Returns an event in a form suitable for editing, or null when it does not exist or the
    /// caller may not edit it.
    /// </summary>
    /// <param name="id">The event id.</param>
    Task<EventEditModel?> GetEventForEditAsync(int id);

    /// <summary>
    /// Creates or updates an event. Re-validates the model and re-checks authorization; the client
    /// is never trusted.
    /// </summary>
    /// <param name="model">The event to save. An <c>Id</c> of zero creates.</param>
    /// <returns>The saved event's id on success.</returns>
    Task<SaveResult<int>> SaveEventAsync(EventEditModel model);

    /// <summary>
    /// Produces a slug from <paramref name="title"/> that no other event is using, appending a
    /// numeric suffix on collision.
    /// </summary>
    /// <param name="title">The title to derive the slug from.</param>
    /// <param name="excludeEventId">
    /// The event being edited, so that its own existing slug is not treated as a collision.
    /// </param>
    Task<string> GenerateUniqueSlugAsync(string title, int? excludeEventId);

    /// <summary>Returns the members holding the Speaker role, for assignment to an event.</summary>
    Task<IReadOnlyList<EventSpeakerInfo>> GetAvailableSpeakersAsync();

    /// <summary>
    /// Whether the current user may edit the given event. Used to show or hide the edit affordance;
    /// the server-side check on each mutation is the actual enforcement.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    Task<bool> CanEditEventAsync(int eventId);
}
