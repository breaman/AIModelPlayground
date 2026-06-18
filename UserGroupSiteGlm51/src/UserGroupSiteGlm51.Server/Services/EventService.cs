using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;

namespace UserGroupSiteGlm51.Server.Services;

/// <summary>
/// Server-side implementation of <see cref="IEventService"/> using EF Core and ApplicationDbContext.
/// </summary>
public class EventService(ApplicationDbContext dbContext) : IEventService
{
    public async Task<IEnumerable<Event>> GetPublishedEventsAsync()
    {
        return await dbContext.Events
            .Include(e => e.EventSpeakers)
            .ThenInclude(es => es.User)
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        return await dbContext.Events
            .Include(e => e.EventSpeakers)
            .ThenInclude(es => es.User)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<Event?> GetEventBySlugAsync(string slug)
    {
        return await dbContext.Events
            .Include(e => e.EventSpeakers)
            .ThenInclude(es => es.User)
            .FirstOrDefaultAsync(e => e.Slug == slug);
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        return await dbContext.Events
            .Include(e => e.EventSpeakers)
            .ThenInclude(es => es.User)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Event> CreateEventAsync(Event eventEntity)
    {
        // Auto-generate slug from title if not provided
        if (string.IsNullOrWhiteSpace(eventEntity.Slug))
        {
            eventEntity.Slug = GenerateSlug(eventEntity.Title);
        }

        dbContext.Events.Add(eventEntity);
        await dbContext.SaveChangesAsync();
        return eventEntity;
    }

    public async Task<Event> UpdateEventAsync(Event eventEntity)
    {
        // Remove existing speaker assignments and re-add from the updated entity
        var existingSpeakers = await dbContext.EventSpeakers
            .Where(es => es.EventId == eventEntity.Id)
            .ToListAsync();
        dbContext.EventSpeakers.RemoveRange(existingSpeakers);

        dbContext.Events.Update(eventEntity);
        await dbContext.SaveChangesAsync();
        return eventEntity;
    }

    public async Task DeleteEventAsync(int eventId)
    {
        var eventEntity = await dbContext.Events.FindAsync(eventId);
        if (eventEntity is not null)
        {
            dbContext.Events.Remove(eventEntity);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task AddSpeakerToEventAsync(int eventId, int userId)
    {
        // Check for duplicate to avoid unique constraint violation
        var exists = await dbContext.EventSpeakers
            .AnyAsync(es => es.EventId == eventId && es.UserId == userId);
        if (!exists)
        {
            dbContext.EventSpeakers.Add(new EventSpeaker { EventId = eventId, UserId = userId });
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task RemoveSpeakerFromEventAsync(int eventId, int userId)
    {
        var speaker = await dbContext.EventSpeakers
            .FirstOrDefaultAsync(es => es.EventId == eventId && es.UserId == userId);
        if (speaker is not null)
        {
            dbContext.EventSpeakers.Remove(speaker);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> IsEditorAsync(int eventId, int userId)
    {
        // Check if user is a speaker on the event
        var isSpeaker = await dbContext.EventSpeakers
            .AnyAsync(es => es.EventId == eventId && es.UserId == userId);

        if (isSpeaker)
        {
            return true;
        }

        // Check if user is in Admin role
        return await dbContext.Set<IdentityUserRole<int>>()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == dbContext.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .First());
    }

    public Task<bool> CanPublishAsync(Event eventEntity)
    {
        var canPublish = !string.IsNullOrWhiteSpace(eventEntity.Title)
                         && !string.IsNullOrWhiteSpace(eventEntity.Slug)
                         && !string.IsNullOrWhiteSpace(eventEntity.Description)
                         && eventEntity.EventDate.HasValue
                         && !string.IsNullOrWhiteSpace(eventEntity.Location)
                         && eventEntity.EventSpeakers.Count > 0;

        return Task.FromResult(canPublish);
    }

    /// <summary>
    /// Generates a URL-friendly kebab-case slug from a title string.
    /// </summary>
    private static string GenerateSlug(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');
    }
}