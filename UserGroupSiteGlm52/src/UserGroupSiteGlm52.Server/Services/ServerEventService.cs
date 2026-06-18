using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteGlm52.Data.Models;
using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Server.Services;

/// <summary>
/// DB-backed <see cref="IEventService"/>. Authorization for edit/create is enforced
/// here (the API endpoint's <c>RequireAuthorization</c> is the coarse gate; this
/// service applies the admin-or-assigned-speaker rule for edits).
/// </summary>
public sealed class ServerEventService(
    ApplicationDbContext db,
    UserManager<User> userManager,
    IHttpContextAccessor httpContextAccessor) : IEventService
{
    private const string AdminRole = "Admin";
    private const string SpeakerRole = "Speaker";

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSummaryDto>> GetPublishedEventsAsync()
    {
        var events = await db.Events
            .AsNoTracking()
            .Include(e => e.Speakers).ThenInclude(es => es.User)
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();

        return events.Select(MapSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<EventDetailDto?> GetPublishedBySlugAsync(string slug)
    {
        var evt = await db.Events
            .AsNoTracking()
            .Include(e => e.Speakers).ThenInclude(es => es.User)
            .FirstOrDefaultAsync(e => e.Slug == slug);

        if (evt is null)
        {
            return null;
        }

        // Unpublished events are hidden from anonymous users; only an admin or an
        // assigned speaker may preview them.
        if (!evt.IsPublished && !await CanViewUnpublishedAsync(evt))
        {
            return null;
        }

        return MapDetail(evt);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSummaryDto>> GetEditableListAsync()
    {
        // The endpoint gates this to Admin, but speakers may also edit their own
        // events; listing everything keeps the manage page simple and the per-event
        // edit check enforces the boundary.
        var currentUserId = CurrentUserId();
        var isAdmin = await IsAdminAsync();

        var query = db.Events
            .AsNoTracking()
            .Include(e => e.Speakers).ThenInclude(es => es.User)
            .OrderByDescending(e => e.EventDate);

        var events = await query.ToListAsync();

        // Non-admins only see events they are assigned to speak at.
        if (!isAdmin)
        {
            events = events.Where(e => e.Speakers.Any(es => es.UserId == currentUserId)).ToList();
        }

        return events.Select(MapSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpeakerOptionDto>> GetSpeakerOptionsAsync()
    {
        var speakers = await userManager.GetUsersInRoleAsync(SpeakerRole);
        return speakers
            .OrderBy(u => (u.FirstName + " " + u.LastName).Trim())
            .Select(u => new SpeakerOptionDto
            {
                Id = u.Id,
                DisplayName = DisplayName(u),
                Email = u.Email ?? string.Empty,
                IsAssigned = false
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ServiceResult<EventEditDto>> GetForEditAsync(int id)
    {
        var evt = await db.Events
            .AsNoTracking()
            .Include(e => e.Speakers)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt is null)
        {
            return ServiceResult<EventEditDto>.NotFound("Event not found.");
        }

        if (!await CanEditAsync(evt))
        {
            return ServiceResult<EventEditDto>.Forbidden("You are not authorized to edit this event.");
        }

        var speakerOptions = await GetSpeakerOptionsForEventAsync(evt);

        return ServiceResult<EventEditDto>.Success(new EventEditDto
        {
            Id = evt.Id,
            Title = evt.Title,
            Slug = evt.Slug,
            ShortDescription = evt.ShortDescription,
            Description = evt.Description,
            EventDate = evt.EventDate,
            Location = evt.Location,
            IsPublished = evt.IsPublished,
            SpeakerIds = evt.Speakers.Select(es => es.UserId).ToList(),
            SpeakerOptions = speakerOptions
        });
    }

    /// <inheritdoc />
    public async Task<ServiceResult<EventDetailDto>> CreateAsync(EventEditDto dto)
    {
        // Create is Admin-only at the endpoint; this guards against misuse.
        if (!await IsAdminAsync())
        {
            return ServiceResult<EventDetailDto>.Forbidden("Only administrators may create events.");
        }

        var validation = Validate(dto);
        if (validation is not null)
        {
            return ServiceResult<EventDetailDto>.Failed(validation);
        }

        if (await db.Events.AnyAsync(e => e.Slug == dto.Slug))
        {
            return ServiceResult<EventDetailDto>.Failed(
                new Dictionary<string, string[]> { [nameof(EventEditDto.Slug)] = ["That slug is already in use."] });
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

        foreach (var userId in dto.SpeakerIds.Distinct())
        {
            evt.Speakers.Add(new EventSpeaker { UserId = userId });
        }

        var publishError = CheckPublishable(evt);
        if (publishError is not null)
        {
            return ServiceResult<EventDetailDto>.Failed(publishError);
        }

        db.Events.Add(evt);
        await db.SaveChangesAsync();

        // Re-load with speaker users for the detail mapping.
        var created = await db.Events
            .Include(e => e.Speakers).ThenInclude(es => es.User)
            .AsNoTracking()
            .FirstAsync(e => e.Id == evt.Id);

        return ServiceResult<EventDetailDto>.Success(MapDetail(created));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<EventDetailDto>> UpdateAsync(int id, EventEditDto dto)
    {
        var evt = await db.Events
            .Include(e => e.Speakers)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt is null)
        {
            return ServiceResult<EventDetailDto>.NotFound("Event not found.");
        }

        if (!await CanEditAsync(evt))
        {
            return ServiceResult<EventDetailDto>.Forbidden("You are not authorized to edit this event.");
        }

        var validation = Validate(dto);
        if (validation is not null)
        {
            return ServiceResult<EventDetailDto>.Failed(validation);
        }

        if (await db.Events.AnyAsync(e => e.Slug == dto.Slug && e.Id != id))
        {
            return ServiceResult<EventDetailDto>.Failed(
                new Dictionary<string, string[]> { [nameof(EventEditDto.Slug)] = ["That slug is already in use."] });
        }

        evt.Title = dto.Title;
        evt.Slug = dto.Slug;
        evt.ShortDescription = dto.ShortDescription;
        evt.Description = dto.Description;
        evt.EventDate = dto.EventDate;
        evt.Location = dto.Location;
        evt.IsPublished = dto.IsPublished;

        // Reconcile assigned speakers against the submitted id list.
        var desired = dto.SpeakerIds.Distinct().ToHashSet();
        var toRemove = evt.Speakers.Where(es => !desired.Contains(es.UserId)).ToList();
        foreach (var removed in toRemove)
        {
            evt.Speakers.Remove(removed);
            db.EventSpeakers.Remove(removed);
        }

        var existing = evt.Speakers.Select(es => es.UserId).ToHashSet();
        foreach (var userId in desired.Where(uid => !existing.Contains(uid)))
        {
            evt.Speakers.Add(new EventSpeaker { UserId = userId });
        }

        var publishError = CheckPublishable(evt);
        if (publishError is not null)
        {
            return ServiceResult<EventDetailDto>.Failed(publishError);
        }

        await db.SaveChangesAsync();

        var updated = await db.Events
            .Include(e => e.Speakers).ThenInclude(es => es.User)
            .AsNoTracking()
            .FirstAsync(e => e.Id == evt.Id);

        return ServiceResult<EventDetailDto>.Success(MapDetail(updated));
    }

    // --- helpers ---

    private static Dictionary<string, string[]>? Validate(EventEditDto dto)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            errors[nameof(EventEditDto.Title)] = ["Title is required."];
        }

        if (string.IsNullOrWhiteSpace(dto.Slug))
        {
            errors[nameof(EventEditDto.Slug)] = ["Slug is required."];
        }

        return errors.Count == 0 ? null : errors;
    }

    /// <summary>Publishing requires a description, date, location, and at least one speaker.</summary>
    private static Dictionary<string, string[]>? CheckPublishable(Event evt)
    {
        if (!evt.IsPublished)
        {
            return null;
        }

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(evt.Description))
        {
            errors[nameof(EventEditDto.Description)] = ["A description is required before publishing."];
        }

        if (!evt.EventDate.HasValue)
        {
            errors[nameof(EventEditDto.EventDate)] = ["An event date is required before publishing."];
        }

        if (string.IsNullOrWhiteSpace(evt.Location))
        {
            errors[nameof(EventEditDto.Location)] = ["A location is required before publishing."];
        }

        if (evt.Speakers.Count == 0)
        {
            errors[nameof(EventEditDto.SpeakerIds)] = ["At least one speaker is required before publishing."];
        }

        return errors.Count == 0 ? null : errors;
    }

    private async Task<bool> CanEditAsync(Event evt)
    {
        if (await IsAdminAsync())
        {
            return true;
        }

        var currentUserId = CurrentUserId();
        return currentUserId != 0 && evt.Speakers.Any(es => es.UserId == currentUserId);
    }

    private Task<bool> CanViewUnpublishedAsync(Event evt)
    {
        // Admins see everything; assigned speakers may preview their own unpublished events.
        return CanEditAsync(evt);
    }

    private async Task<IReadOnlyList<SpeakerOptionDto>> GetSpeakerOptionsForEventAsync(Event evt)
    {
        var options = await GetSpeakerOptionsAsync();
        var assigned = evt.Speakers.Select(es => es.UserId).ToHashSet();
        return options
            .Select(o => o with { IsAssigned = assigned.Contains(o.Id) })
            .ToList();
    }

    private Task<bool> IsAdminAsync()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return Task.FromResult(user?.IsInRole(AdminRole) ?? false);
    }

    private int CurrentUserId()
    {
        var nameId = httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(nameId, out var id) ? id : 0;
    }

    private static EventSummaryDto MapSummary(Event evt)
    {
        return new EventSummaryDto
        {
            Id = evt.Id,
            Title = evt.Title,
            Slug = evt.Slug,
            ShortDescription = evt.ShortDescription,
            EventDate = evt.EventDate,
            Location = evt.Location,
            IsPublished = evt.IsPublished,
            SpeakerNames = evt.Speakers.Select(es => DisplayName(es.User)).ToList()
        };
    }

    private static EventDetailDto MapDetail(Event evt)
    {
        return new EventDetailDto
        {
            Id = evt.Id,
            Title = evt.Title,
            Slug = evt.Slug,
            ShortDescription = evt.ShortDescription,
            Description = evt.Description,
            EventDate = evt.EventDate,
            Location = evt.Location,
            IsPublished = evt.IsPublished,
            Speakers = evt.Speakers.Select(es => new SpeakerDto
            {
                Id = es.UserId,
                DisplayName = DisplayName(es.User),
                Email = es.User.Email ?? string.Empty
            }).ToList()
        };
    }

    /// <summary>"First Last" when available, otherwise the email, so names are always meaningful.</summary>
    private static string DisplayName(User user)
    {
        var name = (user.FirstName + " " + user.LastName).Trim();
        return string.IsNullOrWhiteSpace(name) ? (user.Email ?? "Unknown") : name;
    }
}