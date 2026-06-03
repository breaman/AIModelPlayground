using System.ComponentModel.DataAnnotations;

using UserGroupSiteOpus48.Data.Interfaces;
using UserGroupSiteOpus48.Data.Models;
using UserGroupSiteOpus48.Shared.Authorization;
using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Markdown;
using UserGroupSiteOpus48.Shared.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteOpus48.Server.Services;

/// <summary>
/// Server-side <see cref="IEventService"/> backed by <see cref="ApplicationDbContext"/>. This is the
/// authoritative implementation: it re-validates all input and enforces the business/authorization
/// rules regardless of what the client sent. Used both by the API endpoints and by Blazor pre-render.
/// </summary>
public class ServerEventService(
    ApplicationDbContext db,
    IUserService userService,
    UserManager<User> userManager,
    IHttpContextAccessor httpContextAccessor) : IEventService
{
    public async Task<EventListItemDto[]> GetPublishedEventsAsync()
    {
        var entities = await db.Events
            .Include(e => e.Speakers)
            .ThenInclude(s => s.User)
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.EventDateTime)
            .ToListAsync();

        return entities.Select(ToListItem).ToArray();
    }

    public async Task<EventListItemDto[]> GetAllEventsAsync()
    {
        var entities = await db.Events
            .Include(e => e.Speakers)
            .ThenInclude(s => s.User)
            .OrderByDescending(e => e.EventDateTime)
            .ToListAsync();

        return entities.Select(ToListItem).ToArray();
    }

    public async Task<EventDto?> GetEventBySlugAsync(string slug)
    {
        // The public detail page only exposes published events.
        var entity = await db.Events
            .Include(e => e.Speakers)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(e => e.Slug == slug && e.IsPublished);

        if (entity is null)
        {
            return null;
        }

        var speakers = entity.Speakers
            .Select(s => new SpeakerDto(s.UserId, s.User.FirstName, s.User.LastName, s.User.Email))
            .ToList();

        return new EventDto(
            entity.Id,
            entity.Title,
            entity.Slug,
            entity.ShortDescription,
            MarkdownRenderer.ToHtml(entity.Description),
            entity.EventDateTime,
            entity.Location,
            entity.IsPublished,
            speakers);
    }

    public async Task<EventEditDto?> GetEventForEditAsync(int id)
    {
        var entity = await db.Events
            .Include(e => e.Speakers)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null)
        {
            return null;
        }

        return new EventEditDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Slug = entity.Slug,
            ShortDescription = entity.ShortDescription,
            Description = entity.Description,
            EventDateTime = entity.EventDateTime,
            Location = entity.Location,
            IsPublished = entity.IsPublished,
            SpeakerUserIds = entity.Speakers.Select(s => s.UserId).ToList()
        };
    }

    public async Task<OperationResult<SavedEventDto>> CreateEventAsync(EventEditDto dto)
    {
        // Only admins may create events.
        if (!IsAdmin())
        {
            return OperationResult<SavedEventDto>.Fail("Only administrators can create events.");
        }

        var validationErrors = Validate(dto);
        if (validationErrors.Count > 0)
        {
            return OperationResult<SavedEventDto>.Fail([.. validationErrors]);
        }

        if (await db.Events.AnyAsync(e => e.Slug == dto.Slug))
        {
            return OperationResult<SavedEventDto>.Fail($"The slug '{dto.Slug}' is already in use.");
        }

        var entity = new Event();
        ApplyEditableFields(entity, dto);
        await ReconcileSpeakersAsync(entity, dto.SpeakerUserIds);

        db.Events.Add(entity);
        await db.SaveChangesAsync();

        return OperationResult<SavedEventDto>.Ok(new SavedEventDto(entity.Id, entity.Slug));
    }

    public async Task<OperationResult<SavedEventDto>> UpdateEventAsync(EventEditDto dto)
    {
        var entity = await db.Events
            .Include(e => e.Speakers)
            .FirstOrDefaultAsync(e => e.Id == dto.Id);

        if (entity is null)
        {
            return OperationResult<SavedEventDto>.Fail("Event not found.");
        }

        // Editor = any admin, or a speaker currently assigned to this event.
        if (!IsAdmin() && !entity.Speakers.Any(s => s.UserId == userService.UserId))
        {
            return OperationResult<SavedEventDto>.Fail("You are not allowed to edit this event.");
        }

        var validationErrors = Validate(dto);
        if (validationErrors.Count > 0)
        {
            return OperationResult<SavedEventDto>.Fail([.. validationErrors]);
        }

        if (await db.Events.AnyAsync(e => e.Slug == dto.Slug && e.Id != dto.Id))
        {
            return OperationResult<SavedEventDto>.Fail($"The slug '{dto.Slug}' is already in use.");
        }

        ApplyEditableFields(entity, dto);
        await ReconcileSpeakersAsync(entity, dto.SpeakerUserIds);

        await db.SaveChangesAsync();

        return OperationResult<SavedEventDto>.Ok(new SavedEventDto(entity.Id, entity.Slug));
    }

    public async Task<SpeakerDto[]> GetAssignableSpeakersAsync()
    {
        var speakers = await userManager.GetUsersInRoleAsync(RoleNames.Speaker);
        return speakers
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new SpeakerDto(u.Id, u.FirstName, u.LastName, u.Email))
            .ToArray();
    }

    /// <summary>Maps an event entity to a list DTO (EF translates this projection to SQL).</summary>
    private static EventListItemDto ToListItem(Event e) => new(
        e.Id,
        e.Title,
        e.Slug,
        e.ShortDescription,
        e.EventDateTime,
        e.Location,
        e.IsPublished,
        e.Speakers
            .Select(s => (s.User.FirstName + " " + s.User.LastName).Trim())
            .ToList());

    /// <summary>Runs DataAnnotations + the publish-guard (IValidatableObject) and returns messages.</summary>
    private static List<string> Validate(EventEditDto dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
        return results.Select(r => r.ErrorMessage ?? "Invalid value.").ToList();
    }

    /// <summary>Copies scalar editable fields from the DTO onto the entity.</summary>
    private static void ApplyEditableFields(Event entity, EventEditDto dto)
    {
        entity.Title = dto.Title.Trim();
        entity.Slug = dto.Slug.Trim();
        entity.ShortDescription = dto.ShortDescription;
        entity.Description = dto.Description;
        entity.EventDateTime = dto.EventDateTime;
        entity.Location = dto.Location;
        entity.IsPublished = dto.IsPublished;
    }

    /// <summary>
    /// Adds/removes <see cref="EventSpeaker"/> rows so the entity's speakers match the requested ids,
    /// ignoring ids that are not actually in the Speaker role.
    /// </summary>
    private async Task ReconcileSpeakersAsync(Event entity, List<int> requestedUserIds)
    {
        var validSpeakerIds = (await userManager.GetUsersInRoleAsync(RoleNames.Speaker))
            .Select(u => u.Id)
            .ToHashSet();
        var desired = requestedUserIds.Where(validSpeakerIds.Contains).ToHashSet();

        // Remove speakers no longer desired.
        foreach (var existing in entity.Speakers.Where(s => !desired.Contains(s.UserId)).ToList())
        {
            entity.Speakers.Remove(existing);
        }

        // Add newly desired speakers.
        var currentIds = entity.Speakers.Select(s => s.UserId).ToHashSet();
        foreach (var userId in desired.Where(id => !currentIds.Contains(id)))
        {
            entity.Speakers.Add(new EventSpeaker { UserId = userId });
        }
    }

    /// <summary>True when the current request's user is in the Admin role.</summary>
    private bool IsAdmin() =>
        httpContextAccessor.HttpContext?.User.IsInRole(RoleNames.Admin) ?? false;
}
