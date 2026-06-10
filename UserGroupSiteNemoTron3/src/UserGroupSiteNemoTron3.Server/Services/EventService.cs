using System.Text.RegularExpressions;

using UserGroupSiteNemoTron3.Data.Interfaces;
using UserGroupSiteNemoTron3.Data.Models;
using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteNemoTron3.Server.Services;

public class EventService : IEventService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserService _userService;

    public EventService(ApplicationDbContext dbContext, IUserService userService)
    {
        _dbContext = dbContext;
        _userService = userService;
    }

    public async Task<EventDto[]> GetPublishedEventsAsync()
    {
        var events = await _dbContext.Events
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.EventDateTime)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Slug = e.Slug,
                ShortDescription = e.ShortDescription,
                Description = e.Description,
                EventDateTime = e.EventDateTime,
                Location = e.Location,
                IsPublished = e.IsPublished,
                Speakers = e.EventSpeakers
                    .OrderBy(es => !es.IsPrimary)
                    .ThenBy(es => es.User.FirstName)
                    .Select(es => new EventSpeakerDto
                    {
                        UserId = es.UserId,
                        FullName = $"{es.User.FirstName} {es.User.LastName}".Trim(),
                        IsPrimary = es.IsPrimary
                    })
                    .ToArray(),
                CreatedOn = e.CreatedOn ?? DateTime.MinValue,
                CreatedBy = e.CreatedBy.ToString(),
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy.ToString()
            })
            .ToArrayAsync();

        return events;
    }

    public async Task<EventDto?> GetEventBySlugAsync(string slug)
    {
        var e = await _dbContext.Events
            .Where(ev => ev.Slug == slug)
            .Select(ev => new EventDto
            {
                Id = ev.Id,
                Title = ev.Title,
                Slug = ev.Slug,
                ShortDescription = ev.ShortDescription,
                Description = ev.Description,
                EventDateTime = ev.EventDateTime,
                Location = ev.Location,
                IsPublished = ev.IsPublished,
                Speakers = ev.EventSpeakers
                    .OrderBy(es => !es.IsPrimary)
                    .ThenBy(es => es.User.FirstName)
                    .Select(es => new EventSpeakerDto
                    {
                        UserId = es.UserId,
                        FullName = $"{es.User.FirstName} {es.User.LastName}".Trim(),
                        IsPrimary = es.IsPrimary
                    })
                    .ToArray(),
                CreatedOn = ev.CreatedOn ?? DateTime.MinValue,
                CreatedBy = ev.CreatedBy.ToString(),
                ModifiedOn = ev.ModifiedOn,
                ModifiedBy = ev.ModifiedBy.ToString()
            })
            .FirstOrDefaultAsync();

        return e;
    }

    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        var e = await _dbContext.Events
            .Where(ev => ev.Id == id)
            .Select(ev => new EventDto
            {
                Id = ev.Id,
                Title = ev.Title,
                Slug = ev.Slug,
                ShortDescription = ev.ShortDescription,
                Description = ev.Description,
                EventDateTime = ev.EventDateTime,
                Location = ev.Location,
                IsPublished = ev.IsPublished,
                Speakers = ev.EventSpeakers
                    .OrderBy(es => !es.IsPrimary)
                    .ThenBy(es => es.User.FirstName)
                    .Select(es => new EventSpeakerDto
                    {
                        UserId = es.UserId,
                        FullName = $"{es.User.FirstName} {es.User.LastName}".Trim(),
                        IsPrimary = es.IsPrimary
                    })
                    .ToArray(),
                CreatedOn = ev.CreatedOn ?? DateTime.MinValue,
                CreatedBy = ev.CreatedBy.ToString(),
                ModifiedOn = ev.ModifiedOn,
                ModifiedBy = ev.ModifiedBy.ToString()
            })
            .FirstOrDefaultAsync();

        return e;
    }

    public async Task<EventDto> CreateEventAsync(CreateEventDto dto)
    {
        var slug = await GenerateSlugAsync(dto.Title);
        if (!string.IsNullOrWhiteSpace(dto.Slug))
        {
            // Check if custom slug is unique
            if (await _dbContext.Events.AnyAsync(e => e.Slug == dto.Slug))
            {
                throw new InvalidOperationException("Slug already exists");
            }
            slug = dto.Slug;
        }

        var eventEntity = new Event
        {
            Title = dto.Title,
            Slug = slug,
            ShortDescription = dto.ShortDescription,
            Description = dto.Description,
            EventDateTime = dto.EventDateTime,
            Location = dto.Location,
            IsPublished = dto.IsPublished
        };

        _dbContext.Events.Add(eventEntity);
        await _dbContext.SaveChangesAsync();

        // Add speakers
        if (dto.SpeakerUserIds.Length > 0)
        {
            var speakers = dto.SpeakerUserIds.Select((userId, index) => new EventSpeaker
            {
                EventId = eventEntity.Id,
                UserId = userId,
                IsPrimary = index == 0
            });
            _dbContext.EventSpeakers.AddRange(speakers);
            await _dbContext.SaveChangesAsync();
        }

        return await GetEventByIdAsync(eventEntity.Id) ?? throw new InvalidOperationException("Failed to create event");
    }

    public async Task<EventDto> UpdateEventAsync(int id, UpdateEventDto dto)
    {
        var eventEntity = await _dbContext.Events.FindAsync(id) ?? throw new InvalidOperationException("Event not found");

        // Validate published requirements
        if (dto.IsPublished)
        {
            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new InvalidOperationException("Published events require a description");
            if (string.IsNullOrWhiteSpace(dto.Location))
                throw new InvalidOperationException("Published events require a location");
            if (dto.EventDateTime == default)
                throw new InvalidOperationException("Published events require a date/time");
            if (dto.SpeakerUserIds.Length == 0)
                throw new InvalidOperationException("Published events require at least one speaker");
        }

        // Handle slug
        var slug = dto.Slug;
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = await GenerateSlugAsync(dto.Title, id);
        }
        else
        {
            if (await _dbContext.Events.AnyAsync(e => e.Slug == slug && e.Id != id))
            {
                throw new InvalidOperationException("Slug already exists");
            }
        }

        eventEntity.Title = dto.Title;
        eventEntity.Slug = slug;
        eventEntity.ShortDescription = dto.ShortDescription;
        eventEntity.Description = dto.Description;
        eventEntity.EventDateTime = dto.EventDateTime;
        eventEntity.Location = dto.Location;
        eventEntity.IsPublished = dto.IsPublished;

        // Update speakers
        var existingSpeakers = await _dbContext.EventSpeakers.Where(es => es.EventId == id).ToListAsync();
        _dbContext.EventSpeakers.RemoveRange(existingSpeakers);

        if (dto.SpeakerUserIds.Length > 0)
        {
            var speakers = dto.SpeakerUserIds.Select((userId, index) => new EventSpeaker
            {
                EventId = eventEntity.Id,
                UserId = userId,
                IsPrimary = index == 0
            });
            _dbContext.EventSpeakers.AddRange(speakers);
        }

        await _dbContext.SaveChangesAsync();

        return await GetEventByIdAsync(id) ?? throw new InvalidOperationException("Failed to update event");
    }

    public async Task DeleteEventAsync(int id)
    {
        var eventEntity = await _dbContext.Events.FindAsync(id) ?? throw new InvalidOperationException("Event not found");
        _dbContext.Events.Remove(eventEntity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> CanUserEditEventAsync(int eventId, int userId)
    {
        // Check if user is Admin
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            // Check if user has Admin role
            var userRoles = await _dbContext.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync();
            var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN");
            if (adminRole != null && userRoles.Contains(adminRole.Id))
            {
                return true;
            }
        }

        // Check if user is a speaker for this event
        return await _dbContext.EventSpeakers.AnyAsync(es => es.EventId == eventId && es.UserId == userId);
    }

    public async Task<string> GenerateSlugAsync(string title)
    {
        return await GenerateSlugAsync(title, null);
    }

    public async Task<string> GenerateSlugAsync(string title, int? excludeEventId)
    {
        var slug = title.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        // Ensure uniqueness
        var baseSlug = slug;
        var counter = 1;
        while (await _dbContext.Events.AnyAsync(e => e.Slug == slug && (!excludeEventId.HasValue || e.Id != excludeEventId.Value)))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }
}