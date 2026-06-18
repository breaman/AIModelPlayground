using System.ComponentModel.DataAnnotations;

namespace UserGroupSiteGpt55.Shared.Events;

/// <summary>
/// Provides a compact event projection for lists and navigation.
/// </summary>
public sealed record EventListItem(
    int Id,
    string Title,
    string Slug,
    string? ShortDescription,
    DateTimeOffset? StartsAt,
    string? Location,
    bool IsPublished,
    IReadOnlyList<string> Speakers);

/// <summary>
/// Provides event detail data for public display and editor preview.
/// </summary>
public sealed record EventDetail(
    int Id,
    string Title,
    string Slug,
    string? ShortDescription,
    string? MarkdownDescription,
    string HtmlDescription,
    DateTimeOffset? StartsAt,
    string? Location,
    bool IsPublished,
    bool CanEdit,
    IReadOnlyList<SpeakerOption> Speakers);

/// <summary>
/// Provides selectable speaker data.
/// </summary>
public sealed record SpeakerOption(int Id, string DisplayName, string Email, bool IsSelected);

/// <summary>
/// Provides editable event data used by create and edit forms.
/// </summary>
public sealed class EventEditModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [Required]
    [StringLength(220)]
    public string Slug { get; set; } = "";

    [StringLength(300)]
    public string? ShortDescription { get; set; }

    public string? MarkdownDescription { get; set; }

    public DateTimeOffset? StartsAt { get; set; }

    [StringLength(300)]
    public string? Location { get; set; }

    public bool IsPublished { get; set; }

    public List<int> SpeakerIds { get; set; } = [];
}

/// <summary>
/// Provides the result of saving an event.
/// </summary>
public sealed record EventSaveResult(bool Succeeded, int? EventId, string? Slug, IReadOnlyList<string> Errors)
{
    public static EventSaveResult Success(int eventId, string slug)
    {
        return new EventSaveResult(true, eventId, slug, []);
    }

    public static EventSaveResult Failure(IReadOnlyList<string> errors)
    {
        return new EventSaveResult(false, null, null, errors);
    }
}

/// <summary>
/// Defines shared event operations for server prerendering and WebAssembly hydration.
/// </summary>
public interface IEventService
{
    Task<IReadOnlyList<EventListItem>> GetPublishedEventsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventListItem>> GetEditableEventsAsync(CancellationToken cancellationToken = default);

    Task<EventDetail?> GetEventBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<EventEditModel?> GetEventForEditAsync(int eventId, CancellationToken cancellationToken = default);

    Task<EventEditModel> CreateDraftAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpeakerOption>> GetSpeakerOptionsAsync(IReadOnlyCollection<int> selectedSpeakerIds, CancellationToken cancellationToken = default);

    Task<EventSaveResult> SaveEventAsync(EventEditModel model, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ValidateEventAsync(EventEditModel model, CancellationToken cancellationToken = default);
}