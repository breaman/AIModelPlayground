using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using UserGroupSiteFable5.Data.Models;
using UserGroupSiteFable5.Server.Endpoints;
using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Helpers;
using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Server.Services;

/// <summary>
/// Server-side <see cref="IEventService"/> used by the API endpoints and during
/// pre-rendering of the interactive event pages. The current user comes from the
/// HTTP context; editor rules are enforced via <see cref="IEventAuthorizationService"/>.
/// </summary>
public class EventService(
    ApplicationDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IEventAuthorizationService eventAuthorization) : IEventService
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    public async Task<List<EventSummaryDto>> GetEditableEventsAsync()
    {
        var query = dbContext.Events.AsQueryable();

        // Admins manage every event; speakers only see the ones they are assigned to.
        if (!User.IsInRole("Admin"))
        {
            var userId = User.GetUserId();
            query = query.Where(e => e.Speakers.Any(s => s.UserId == userId));
        }

        return await query
            .OrderByDescending(e => e.StartsAt)
            .Select(e => new EventSummaryDto
            {
                Id = e.Id,
                Title = e.Title,
                Slug = e.Slug,
                StartsAt = e.StartsAt,
                Location = e.Location,
                IsPublished = e.IsPublished,
                // Inline expression (not a helper method) so EF can translate it to SQL
                // inside this nested collection projection.
                SpeakerNames = e.Speakers
                    .Select(s => s.User.FirstName == null && s.User.LastName == null
                        ? s.User.Email ?? ""
                        : ((s.User.FirstName ?? "") + " " + (s.User.LastName ?? "")).Trim())
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<EventEditDto?> GetEventForEditAsync(int id)
    {
        if (!await eventAuthorization.CanEditEventAsync(User, id))
        {
            return null;
        }

        return await dbContext.Events
            .Where(e => e.Id == id)
            .Select(e => new EventEditDto
            {
                Id = e.Id,
                Title = e.Title,
                Slug = e.Slug,
                ShortDescription = e.ShortDescription,
                Description = e.Description,
                StartsAt = e.StartsAt,
                Location = e.Location,
                IsPublished = e.IsPublished,
                SpeakerUserIds = e.Speakers.Select(s => s.UserId).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult> CreateEventAsync(EventEditDto dto)
    {
        if (!User.IsInRole("Admin"))
        {
            return ServiceResult.Fail(ServiceErrorType.Forbidden, "Only admins can create events.");
        }

        var validationError = await NormalizeAndValidateAsync(dto);
        if (validationError is not null)
        {
            return validationError;
        }

        var newEvent = new Event
        {
            Title = dto.Title,
            Slug = dto.Slug,
            ShortDescription = dto.ShortDescription,
            Description = dto.Description,
            StartsAt = dto.StartsAt,
            Location = dto.Location,
            IsPublished = dto.IsPublished,
            Speakers = dto.SpeakerUserIds.Distinct().Select(userId => new EventSpeaker { UserId = userId }).ToList()
        };

        dbContext.Events.Add(newEvent);
        await dbContext.SaveChangesAsync();

        return ServiceResult.Ok;
    }

    public async Task<ServiceResult> UpdateEventAsync(EventEditDto dto)
    {
        if (!await eventAuthorization.CanEditEventAsync(User, dto.Id))
        {
            return ServiceResult.Fail(ServiceErrorType.Forbidden, "You are not an editor of this event.");
        }

        var existing = await dbContext.Events
            .Include(e => e.Speakers)
            .FirstOrDefaultAsync(e => e.Id == dto.Id);
        if (existing is null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "Event not found.");
        }

        var isAdmin = User.IsInRole("Admin");
        var existingSpeakerIds = existing.Speakers.Select(s => s.UserId).ToHashSet();
        var requestedSpeakerIds = dto.SpeakerUserIds.Distinct().ToHashSet();

        // Only admins may change the speaker list; a speaker editing the event keeps it as-is.
        if (!isAdmin && !requestedSpeakerIds.SetEquals(existingSpeakerIds))
        {
            return ServiceResult.Fail(ServiceErrorType.Forbidden, "Only admins can modify the speaker list.");
        }

        var effectiveSpeakerIds = isAdmin ? requestedSpeakerIds : existingSpeakerIds;
        dto.SpeakerUserIds = effectiveSpeakerIds.ToList();

        var validationError = await NormalizeAndValidateAsync(dto, excludeEventId: dto.Id);
        if (validationError is not null)
        {
            return validationError;
        }

        existing.Title = dto.Title;
        existing.Slug = dto.Slug;
        existing.ShortDescription = dto.ShortDescription;
        existing.Description = dto.Description;
        existing.StartsAt = dto.StartsAt;
        existing.Location = dto.Location;
        existing.IsPublished = dto.IsPublished;

        foreach (var removed in existing.Speakers.Where(s => !effectiveSpeakerIds.Contains(s.UserId)).ToList())
        {
            existing.Speakers.Remove(removed);
        }

        foreach (var addedId in effectiveSpeakerIds.Except(existingSpeakerIds))
        {
            existing.Speakers.Add(new EventSpeaker { EventId = dto.Id, UserId = addedId });
        }

        await dbContext.SaveChangesAsync();

        return ServiceResult.Ok;
    }

    public async Task<List<SpeakerDto>> GetSpeakersAsync()
    {
        return await (
                from userRole in dbContext.UserRoles
                join role in dbContext.Roles on userRole.RoleId equals role.Id
                join speaker in dbContext.Users on userRole.UserId equals speaker.Id
                where role.Name == "Speaker"
                orderby speaker.FirstName, speaker.LastName
                select new SpeakerDto
                {
                    Id = speaker.Id,
                    DisplayName = speaker.FirstName == null && speaker.LastName == null
                        ? speaker.Email ?? ""
                        : ((speaker.FirstName ?? "") + " " + (speaker.LastName ?? "")).Trim()
                })
            .ToListAsync();
    }

    /// <summary>
    /// Normalizes the slug the same way the client auto-fill does, then runs the shared
    /// DataAnnotations validation plus the server-only slug uniqueness check.
    /// </summary>
    private async Task<ServiceResult?> NormalizeAndValidateAsync(EventEditDto dto, int? excludeEventId = null)
    {
        dto.Slug = SlugHelper.GenerateSlug(dto.Slug);

        var validationError = ValidationHelper.Validate(dto);
        if (validationError is not null)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, validationError);
        }

        var slugTaken = await dbContext.Events
            .AnyAsync(e => e.Slug == dto.Slug && (excludeEventId == null || e.Id != excludeEventId));

        return slugTaken
            ? ServiceResult.Fail(ServiceErrorType.Validation,
                $"The slug \"{dto.Slug}\" is already in use by another event.")
            : null;
    }
}