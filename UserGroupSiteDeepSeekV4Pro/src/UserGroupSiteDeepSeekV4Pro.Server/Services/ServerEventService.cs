using Microsoft.EntityFrameworkCore;

using UserGroupSiteDeepSeekV4Pro.Data.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Server.Services;

public class ServerEventService(ApplicationDbContext context) : IEventService
{
    public async Task<List<EventListItemDto>> GetPublishedEventsAsync()
    {
        return await context.Events
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.EventDateTime)
            .Select(e => new EventListItemDto
            {
                Id = e.Id,
                Title = e.Title,
                Slug = e.Slug,
                ShortDescription = e.ShortDescription,
                EventDateTime = e.EventDateTime,
                Location = e.Location,
                IsPublished = e.IsPublished,
                CreatedOn = e.CreatedOn,
                Speakers = e.EventSpeakers.Select(es => new UserDto
                {
                    Id = es.User.Id,
                    FirstName = es.User.FirstName,
                    LastName = es.User.LastName,
                    Email = es.User.Email ?? ""
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<List<EventListItemDto>> GetAllEventsAsync()
    {
        return await context.Events
            .OrderByDescending(e => e.EventDateTime)
            .Select(e => new EventListItemDto
            {
                Id = e.Id,
                Title = e.Title,
                Slug = e.Slug,
                ShortDescription = e.ShortDescription,
                EventDateTime = e.EventDateTime,
                Location = e.Location,
                IsPublished = e.IsPublished,
                CreatedOn = e.CreatedOn,
                Speakers = e.EventSpeakers.Select(es => new UserDto
                {
                    Id = es.User.Id,
                    FirstName = es.User.FirstName,
                    LastName = es.User.LastName,
                    Email = es.User.Email ?? ""
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        return await context.Events
            .Include(e => e.EventSpeakers)
                .ThenInclude(es => es.User)
            .Where(e => e.Id == id)
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
                CreatedOn = e.CreatedOn,
                ModifiedOn = e.ModifiedOn,
                SpeakerUserIds = e.EventSpeakers.Select(es => es.UserId).ToList(),
                Speakers = e.EventSpeakers.Select(es => new UserDto
                {
                    Id = es.User.Id,
                    FirstName = es.User.FirstName,
                    LastName = es.User.LastName,
                    Email = es.User.Email ?? ""
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<EventDto?> GetEventBySlugAsync(string slug)
    {
        return await context.Events
            .Include(e => e.EventSpeakers)
                .ThenInclude(es => es.User)
            .Where(e => e.Slug == slug)
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
                CreatedOn = e.CreatedOn,
                ModifiedOn = e.ModifiedOn,
                SpeakerUserIds = e.EventSpeakers.Select(es => es.UserId).ToList(),
                Speakers = e.EventSpeakers.Select(es => new UserDto
                {
                    Id = es.User.Id,
                    FirstName = es.User.FirstName,
                    LastName = es.User.LastName,
                    Email = es.User.Email ?? ""
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<EventDto> CreateEventAsync(EventDto dto)
    {
        var entity = new Event
        {
            Title = dto.Title,
            Slug = dto.Slug,
            ShortDescription = dto.ShortDescription,
            Description = dto.Description,
            EventDateTime = dto.EventDateTime,
            Location = dto.Location,
            IsPublished = dto.IsPublished
        };

        context.Events.Add(entity);
        await context.SaveChangesAsync();

        if (dto.SpeakerUserIds.Count > 0)
        {
            foreach (var userId in dto.SpeakerUserIds)
            {
                context.EventSpeakers.Add(new EventSpeaker
                {
                    EventId = entity.Id,
                    UserId = userId
                });
            }
            await context.SaveChangesAsync();
        }

        return await GetEventByIdAsync(entity.Id) ?? throw new InvalidOperationException("Event not found after creation.");
    }

    public async Task<EventDto> UpdateEventAsync(EventDto dto)
    {
        var entity = await context.Events
            .Include(e => e.EventSpeakers)
            .FirstOrDefaultAsync(e => e.Id == dto.Id)
            ?? throw new InvalidOperationException("Event not found.");

        entity.Title = dto.Title;
        entity.Slug = dto.Slug;
        entity.ShortDescription = dto.ShortDescription;
        entity.Description = dto.Description;
        entity.EventDateTime = dto.EventDateTime;
        entity.Location = dto.Location;
        entity.IsPublished = dto.IsPublished;

        // Update speakers
        context.EventSpeakers.RemoveRange(entity.EventSpeakers);
        foreach (var userId in dto.SpeakerUserIds)
        {
            context.EventSpeakers.Add(new EventSpeaker
            {
                EventId = entity.Id,
                UserId = userId
            });
        }

        await context.SaveChangesAsync();
        return await GetEventByIdAsync(entity.Id) ?? throw new InvalidOperationException("Event not found after update.");
    }

    public async Task DeleteEventAsync(int id)
    {
        var entity = await context.Events.FindAsync(id);
        if (entity is not null)
        {
            context.Events.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<UserDto>> GetAvailableSpeakersAsync()
    {
        return await context.Set<Role>()
            .Where(r => r.Name == "Speaker")
            .SelectMany(r => context.UserRoles
                .Where(ur => ur.RoleId == r.Id)
                .Join(context.Users, ur => ur.UserId, u => u.Id, (ur, u) => new UserDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email ?? ""
                })
            )
            .Distinct()
            .ToListAsync();
    }
}