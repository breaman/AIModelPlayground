using Markdig;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK27Code.Data.Models;
using UserGroupSiteKimiK27Code.Shared;
using UserGroupSiteKimiK27Code.Shared.Dtos;
using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Server.Services;

public class ServerEventService : IEventService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;

    public ServerEventService(ApplicationDbContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<List<EventListItemDto>> GetPublishedEventsAsync()
    {
        var events = await _dbContext.Events
            .AsNoTracking()
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.EventDate)
            .Include(e => e.EventSpeakers)
            .ThenInclude(es => es.User)
            .ToListAsync();

        return events.Select(MapToListItem).ToList();
    }

    public async Task<EventDetailDto?> GetEventBySlugAsync(string slug, bool includeUnpublished = false)
    {
        var query = _dbContext.Events
            .AsNoTracking()
            .Where(e => e.Slug == slug);

        if (!includeUnpublished)
        {
            query = query.Where(e => e.IsPublished);
        }

        var evt = await query
            .Include(e => e.EventSpeakers)
            .ThenInclude(es => es.User)
            .FirstOrDefaultAsync();

        if (evt is null) return null;

        return MapToDetail(evt);
    }

    public async Task<EventEditDto?> GetEventForEditAsync(int id)
    {
        var evt = await _dbContext.Events
            .AsNoTracking()
            .Include(e => e.EventSpeakers)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt is null) return null;

        return new EventEditDto
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

    public async Task<EventEditResult> CreateEventAsync(EventEditDto dto)
    {
        var validationErrors = ValidateForSave(dto);
        if (validationErrors.Count > 0) return EventEditResult.Fail(validationErrors);

        if (await SlugExistsAsync(dto.Slug))
        {
            return EventEditResult.Fail("Slug is already in use.");
        }

        var evt = new Event
        {
            Title = dto.Title.Trim(),
            Slug = dto.Slug.Trim(),
            ShortDescription = dto.ShortDescription?.Trim(),
            Description = dto.Description.Trim(),
            EventDate = dto.EventDate,
            Location = dto.Location?.Trim(),
            IsPublished = dto.IsPublished
        };

        evt.EventSpeakers = await BuildEventSpeakersAsync(dto.SpeakerIds);

        _dbContext.Events.Add(evt);
        await _dbContext.SaveChangesAsync();

        return EventEditResult.Ok(evt.Id);
    }

    public async Task<EventEditResult> UpdateEventAsync(int id, EventEditDto dto)
    {
        var validationErrors = ValidateForSave(dto);
        if (validationErrors.Count > 0) return EventEditResult.Fail(validationErrors);

        var evt = await _dbContext.Events
            .Include(e => e.EventSpeakers)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt is null) return EventEditResult.Fail("Event not found.");

        if (evt.Slug != dto.Slug && await SlugExistsAsync(dto.Slug))
        {
            return EventEditResult.Fail("Slug is already in use.");
        }

        evt.Title = dto.Title.Trim();
        evt.Slug = dto.Slug.Trim();
        evt.ShortDescription = dto.ShortDescription?.Trim();
        evt.Description = dto.Description.Trim();
        evt.EventDate = dto.EventDate;
        evt.Location = dto.Location?.Trim();
        evt.IsPublished = dto.IsPublished;

        // Refresh speakers
        _dbContext.EventSpeakers.RemoveRange(evt.EventSpeakers);
        evt.EventSpeakers = await BuildEventSpeakersAsync(dto.SpeakerIds);

        await _dbContext.SaveChangesAsync();

        return EventEditResult.Ok(evt.Id);
    }

    public async Task<List<SpeakerDto>> GetSpeakersAsync()
    {
        var userIdsInRole = await _dbContext.Set<IdentityUserRole<int>>()
            .AsNoTracking()
            .Where(ur => _dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == Roles.Speaker))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync();

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIdsInRole.Contains(u.Id))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        return users.Select(u => new SpeakerDto
        {
            Id = u.Id,
            FullName = $"{u.FirstName} {u.LastName}".Trim()
        }).ToList();
    }

    public async Task<List<EventListItemDto>> GetEditableEventsAsync()
    {
        var events = await _dbContext.Events
            .AsNoTracking()
            .OrderByDescending(e => e.EventDate)
            .Include(e => e.EventSpeakers)
            .ThenInclude(es => es.User)
            .ToListAsync();

        return events.Select(MapToListItem).ToList();
    }

    private static List<string> ValidateForSave(EventEditDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Title)) errors.Add("Title is required.");
        if (string.IsNullOrWhiteSpace(dto.Slug)) errors.Add("Slug is required.");

        if (dto.IsPublished && !dto.CanPublish())
        {
            errors.Add("A published event requires a description, event date/time, location, and at least one speaker.");
        }

        return errors;
    }

    private async Task<bool> SlugExistsAsync(string slug)
    {
        return await _dbContext.Events
            .AsNoTracking()
            .AnyAsync(e => e.Slug == slug);
    }

    private async Task<ICollection<EventSpeaker>> BuildEventSpeakersAsync(List<int> speakerIds)
    {
        var speakers = await _dbContext.Users
            .Where(u => speakerIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        return speakers.Select(id => new EventSpeaker { UserId = id }).ToList();
    }

    private static string RenderMarkdown(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
        return Markdown.ToHtml(markdown, pipeline);
    }

    private static EventListItemDto MapToListItem(Event evt)
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
                Id = es.User.Id,
                FullName = $"{es.User.FirstName} {es.User.LastName}".Trim()
            }).ToList()
        };
    }

    private static EventDetailDto MapToDetail(Event evt)
    {
        return new EventDetailDto
        {
            Id = evt.Id,
            Title = evt.Title,
            Slug = evt.Slug,
            ShortDescription = evt.ShortDescription,
            Description = evt.Description,
            DescriptionHtml = RenderMarkdown(evt.Description),
            EventDate = evt.EventDate,
            Location = evt.Location,
            IsPublished = evt.IsPublished,
            Speakers = evt.EventSpeakers.Select(es => new SpeakerDto
            {
                Id = es.User.Id,
                FullName = $"{es.User.FirstName} {es.User.LastName}".Trim()
            }).ToList()
        };
    }
}