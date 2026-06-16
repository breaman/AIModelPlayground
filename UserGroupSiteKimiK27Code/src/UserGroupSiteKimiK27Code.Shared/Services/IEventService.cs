using UserGroupSiteKimiK27Code.Shared.Dtos;

namespace UserGroupSiteKimiK27Code.Shared.Services;

public interface IEventService
{
    Task<List<EventListItemDto>> GetPublishedEventsAsync();
    Task<EventDetailDto?> GetEventBySlugAsync(string slug, bool includeUnpublished = false);
    Task<EventEditDto?> GetEventForEditAsync(int id);
    Task<EventEditResult> CreateEventAsync(EventEditDto dto);
    Task<EventEditResult> UpdateEventAsync(int id, EventEditDto dto);
    Task<List<SpeakerDto>> GetSpeakersAsync();
    Task<List<EventListItemDto>> GetEditableEventsAsync();
}

public sealed class EventEditResult
{
    public bool Success { get; set; }
    public int? EventId { get; set; }
    public List<string> Errors { get; set; } = [];

    public static EventEditResult Ok(int eventId) => new() { Success = true, EventId = eventId };
    public static EventEditResult Fail(IEnumerable<string> errors) => new() { Success = false, Errors = errors.ToList() };
    public static EventEditResult Fail(string error) => new() { Success = false, Errors = [error] };
}