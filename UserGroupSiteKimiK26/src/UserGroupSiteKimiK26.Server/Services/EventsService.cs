using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK26.Data.Interfaces;
using UserGroupSiteKimiK26.Data.Models;
using UserGroupSiteKimiK26.Shared.Dtos;
using UserGroupSiteKimiK26.Shared.Services;

namespace UserGroupSiteKimiK26.Server.Services;

public class EventsService(
    ApplicationDbContext dbContext,
    IUserService currentUserService) : IEventsService
{
    public async Task<List<EventListItemDto>> GetPublishedEventsAsync()
    {
        var events = await dbContext.Events
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.EventDate)
            .Include(e => e.EventSpeakers)
            .ThenInclude(es => es.User)
            .ToListAsync();

        return events.Select(MapToListItemDto).ToList();
    }

    public async Task<EventListItemDto?> GetEventBySlugAsync(string slug)
    {
        var evt = await dbContext.Events
            .Include(e => e.EventSpeakers)
            .ThenInclude(es => es.User)
            .FirstOrDefaultAsync(e => e.Slug == slug && e.IsPublished);

        return evt is null ? null : MapToListItemDto(evt);
    }

    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        var evt = await dbContext.Events
            .Include(e => e.EventSpeakers)
            .ThenInclude(es => es.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt is null) return null;

        return new EventDto
        {
            Id = evt.Id,
            Title = evt.Title,
            Slug = evt.Slug,
            ShortDescription = evt.ShortDescription,
            Description = evt.Description,
            EventDate = evt.EventDate,
            Location = evt.Location,
            IsPublished = evt.IsPublished,
            SpeakerIds = evt.EventSpeakers.Select(es => es.UserId).ToList()
        };
    }

    public async Task<List<EventListItemDto>> GetEditableEventsForUserAsync()
    {
        var userId = currentUserService.UserId;
        if (userId == default) return [];

        var isAdmin = await dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == userId && dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin"));

        IQueryable<Event> query = dbContext.Events
            .Include(e => e.EventSpeakers)
            .ThenInclude(es => es.User);

        if (!isAdmin)
        {
            query = query.Where(e => e.EventSpeakers.Any(es => es.UserId == userId));
        }

        var events = await query.OrderByDescending(e => e.EventDate).ToListAsync();
        return events.Select(MapToListItemDto).ToList();
    }

    public async Task<(bool Success, List<string> Errors)> CreateEventAsync(EventDto dto)
    {
        var errors = Validate(dto, true);
        if (errors.Count > 0) return (false, errors);

        var slugExists = await dbContext.Events.AnyAsync(e => e.Slug == dto.Slug);
        if (slugExists)
        {
            dto.Slug = await MakeSlugUniqueAsync(dto.Slug);
        }

        var evt = new Event
        {
            Title = dto.Title,
            Slug = dto.Slug,
            ShortDescription = dto.ShortDescription,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location,
            IsPublished = dto.IsPublished
        };

        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();

        foreach (var speakerId in dto.SpeakerIds)
        {
            dbContext.EventSpeakers.Add(new EventSpeaker
            {
                EventId = evt.Id,
                UserId = speakerId
            });
        }

        await dbContext.SaveChangesAsync();
        return (true, []);
    }

    public async Task<(bool Success, List<string> Errors)> UpdateEventAsync(int id, EventDto dto)
    {
        var evt = await dbContext.Events
            .Include(e => e.EventSpeakers)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt is null) return (false, ["Event not found."]);

        var errors = Validate(dto, evt.IsPublished);
        if (errors.Count > 0) return (false, errors);

        if (evt.Slug != dto.Slug)
        {
            var slugExists = await dbContext.Events.AnyAsync(e => e.Slug == dto.Slug && e.Id != id);
            if (slugExists)
            {
                dto.Slug = await MakeSlugUniqueAsync(dto.Slug);
            }
        }

        evt.Title = dto.Title;
        evt.Slug = dto.Slug;
        evt.ShortDescription = dto.ShortDescription;
        evt.Description = dto.Description;
        evt.EventDate = dto.EventDate;
        evt.Location = dto.Location;
        evt.IsPublished = dto.IsPublished;

        // Update speakers
        var existingSpeakers = evt.EventSpeakers.ToList();
        var speakersToRemove = existingSpeakers.Where(es => !dto.SpeakerIds.Contains(es.UserId)).ToList();
        var speakerIdsToAdd = dto.SpeakerIds.Where(sid => !existingSpeakers.Any(es => es.UserId == sid)).ToList();

        dbContext.EventSpeakers.RemoveRange(speakersToRemove);
        foreach (var speakerId in speakerIdsToAdd)
        {
            dbContext.EventSpeakers.Add(new EventSpeaker { EventId = evt.Id, UserId = speakerId });
        }

        await dbContext.SaveChangesAsync();
        return (true, []);
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        var evt = await dbContext.Events.FindAsync(id);
        if (evt is null) return false;

        dbContext.Events.Remove(evt);
        await dbContext.SaveChangesAsync();
        return true;
    }

    private static List<string> Validate(EventDto dto, bool isPublishCheck)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Title))
            errors.Add("Title is required.");

        if (string.IsNullOrWhiteSpace(dto.Slug))
            errors.Add("Slug is required.");

        if (isPublishCheck || dto.IsPublished)
        {
            if (string.IsNullOrWhiteSpace(dto.Description))
                errors.Add("Description is required to publish.");

            if (dto.EventDate is null)
                errors.Add("Event date is required to publish.");

            if (string.IsNullOrWhiteSpace(dto.Location))
                errors.Add("Location is required to publish.");

            if (dto.SpeakerIds.Count == 0)
                errors.Add("At least one speaker is required to publish.");
        }

        return errors;
    }

    private async Task<string> MakeSlugUniqueAsync(string slug)
    {
        var originalSlug = slug;
        var counter = 2;
        while (await dbContext.Events.AnyAsync(e => e.Slug == slug))
        {
            slug = $"{originalSlug}-{counter}";
            counter++;
        }
        return slug;
    }

    private static EventListItemDto MapToListItemDto(Event evt)
    {
        return new EventListItemDto
        {
            Id = evt.Id,
            Title = evt.Title,
            Slug = evt.Slug,
            ShortDescription = evt.ShortDescription,
            EventDate = evt.EventDate,
            Location = evt.Location,
            IsPublished = evt.IsPublished,
            Speakers = evt.EventSpeakers.Select(es => new SpeakerDto
            {
                Id = es.UserId,
                FirstName = es.User.FirstName ?? "",
                LastName = es.User.LastName ?? "",
                Email = es.User.Email
            }).ToList()
        };
    }
}