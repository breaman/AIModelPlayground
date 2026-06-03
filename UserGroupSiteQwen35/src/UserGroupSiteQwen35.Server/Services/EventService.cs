using UserGroupSiteQwen35.Data.Interfaces;
using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Server.Services;

public class EventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ISpeakerRepository _speakerRepository;

    public EventService(IEventRepository eventRepository, ISpeakerRepository speakerRepository)
    {
        _eventRepository = eventRepository;
        _speakerRepository = speakerRepository;
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        return await _eventRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Event>> GetPublishedEventsAsync()
    {
        return await _eventRepository.GetPublishedAsync();
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        return await _eventRepository.GetByIdAsync(id);
    }

    public async Task<Event?> GetEventBySlugAsync(string slug)
    {
        return await _eventRepository.GetBySlugAsync(slug);
    }

    public async Task<Event> CreateEventAsync(Event evt)
    {
        return await _eventRepository.CreateAsync(evt);
    }

    public async Task<Event> UpdateEventAsync(Event evt)
    {
        return await _eventRepository.UpdateAsync(evt);
    }

    public async Task DeleteEventAsync(int id)
    {
        await _eventRepository.DeleteAsync(id);
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, int? excludeId = null)
    {
        return await _eventRepository.ExistsBySlugAsync(slug, excludeId);
    }

    public async Task<IEnumerable<Speaker>> GetApprovedSpeakersAsync()
    {
        return await _speakerRepository.GetApprovedSpeakersAsync();
    }

    public async Task AddSpeakerToEventAsync(int eventId, int speakerId)
    {
        var evt = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new ArgumentException("Event not found", nameof(eventId));

        // Note: This would need the EventSpeaker join table to be properly implemented
        // For now, this is a placeholder
    }

    public bool ValidateEventForPublish(Event evt, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(evt.Title))
        {
            errorMessage = "Title is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(evt.Slug))
        {
            errorMessage = "Slug is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(evt.Description))
        {
            errorMessage = "Description is required for publishing";
            return false;
        }

        if (evt.DateTime == default)
        {
            errorMessage = "Date/Time is required for publishing";
            return false;
        }

        if (string.IsNullOrWhiteSpace(evt.Location))
        {
            errorMessage = "Location is required for publishing";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
