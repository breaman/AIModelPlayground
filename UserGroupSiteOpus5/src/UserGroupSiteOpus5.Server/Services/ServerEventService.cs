using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Shared.Common;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteOpus5.Server.Services;

/// <summary>
/// Server-side implementation of <see cref="IEventService"/>, running directly against the
/// database. This is the implementation used during pre-rendering and by the API endpoints.
/// </summary>
/// <remarks>
/// Every mutating method re-validates the model and re-checks authorization. The client-side
/// equivalents exist for responsiveness only; this class is where the rules are enforced.
/// </remarks>
public class ServerEventService(
    ApplicationDbContext dbContext,
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ServerEventService> logger) : IEventService
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventListItem>> GetPublishedEventsAsync()
    {
        return await dbContext.Events
            .AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.EventDateTime)
            .Select(x => new EventListItem(
                x.Id,
                x.Title,
                x.Slug,
                x.ShortDescription,
                x.EventDateTime,
                x.Location,
                x.IsPublished,
                x.Speakers
                    .Select(s => new EventSpeakerInfo(s.UserId, s.User.FirstName, s.User.LastName))
                    .ToList()))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventListItem>> GetManageableEventsAsync()
    {
        var userId = CurrentUserId;

        if (userId is null)
        {
            return [];
        }

        var isAdmin = User.IsInRole(RoleNames.Admin);

        if (!isAdmin && !User.IsInRole(RoleNames.Speaker))
        {
            return [];
        }

        var query = dbContext.Events.AsNoTracking();

        // Speakers see only the events they are assigned to; administrators see everything.
        if (!isAdmin)
        {
            query = query.Where(x => x.Speakers.Any(s => s.UserId == userId));
        }

        return await query
            .OrderByDescending(x => x.EventDateTime ?? DateTimeOffset.MaxValue)
            .ThenByDescending(x => x.Id)
            .Select(x => new EventListItem(
                x.Id,
                x.Title,
                x.Slug,
                x.ShortDescription,
                x.EventDateTime,
                x.Location,
                x.IsPublished,
                x.Speakers
                    .Select(s => new EventSpeakerInfo(s.UserId, s.User.FirstName, s.User.LastName))
                    .ToList()))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<EventDetail?> GetEventBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var found = await dbContext.Events
            .AsNoTracking()
            .Where(x => x.Slug == slug)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Slug,
                x.ShortDescription,
                x.Description,
                x.EventDateTime,
                x.Location,
                x.IsPublished,
                Speakers = x.Speakers
                    .Select(s => new EventSpeakerInfo(s.UserId, s.User.FirstName, s.User.LastName))
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (found is null)
        {
            return null;
        }

        var canEdit = await CanEditEventAsync(found.Id);

        // A draft is invisible to everyone but its editors; reporting "not found" rather than
        // "forbidden" avoids confirming that a hidden event exists at that slug.
        if (!found.IsPublished && !canEdit)
        {
            return null;
        }

        return new EventDetail(
            found.Id,
            found.Title,
            found.Slug,
            found.ShortDescription,
            found.Description,
            found.EventDateTime,
            found.Location,
            found.IsPublished,
            found.Speakers,
            canEdit);
    }

    /// <inheritdoc />
    public async Task<EventEditModel?> GetEventForEditAsync(int id)
    {
        if (!await CanEditEventAsync(id))
        {
            return null;
        }

        return await dbContext.Events
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new EventEditModel
            {
                Id = x.Id,
                Title = x.Title,
                Slug = x.Slug,
                ShortDescription = x.ShortDescription,
                Description = x.Description,
                EventDateTime = x.EventDateTime,
                Location = x.Location,
                IsPublished = x.IsPublished,
                SpeakerUserIds = x.Speakers.Select(s => s.UserId).ToList()
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<SaveResult<int>> SaveEventAsync(EventEditModel model)
    {
        // Creating a new event is an administrator action; editing an existing one is open to its
        // assigned speakers as well. Both go through the same resource-based policy.
        if (model.Id == 0)
        {
            if (!User.IsInRole(RoleNames.Admin))
            {
                return SaveResult<int>.Failure("Only an administrator can create an event.");
            }
        }
        else if (!await CanEditEventAsync(model.Id))
        {
            return SaveResult<int>.Failure("You do not have permission to edit this event.");
        }

        if (ValidateModel(model) is { Count: > 0 } validationErrors)
        {
            return SaveResult<int>.Failure(validationErrors);
        }

        // Validation above guarantees a non-empty slug, so this only normalises what the caller
        // sent: a hand-typed "My Slug!" becomes "my-slug" rather than reaching the database as-is.
        var slug = SlugGenerator.Generate(model.Slug);

        if (string.IsNullOrWhiteSpace(slug))
        {
            return SaveResult<int>.Failure(
                "The slug must contain at least one letter or digit.");
        }

        // Only speakers who actually hold the Speaker role may be assigned, so a hand-crafted
        // request cannot list arbitrary user ids as presenters.
        var requestedSpeakerIds = model.SpeakerUserIds.Distinct().ToList();
        var validSpeakerIds = requestedSpeakerIds.Count == 0
            ? []
            : await GetSpeakerUserIdsAsync(requestedSpeakerIds);

        if (validSpeakerIds.Count != requestedSpeakerIds.Count)
        {
            return SaveResult<int>.Failure("One or more selected speakers no longer hold the Speaker role.");
        }

        var entity = model.Id == 0
            ? new Event()
            : await dbContext.Events
                .Include(x => x.Speakers)
                .FirstOrDefaultAsync(x => x.Id == model.Id);

        if (entity is null)
        {
            return SaveResult<int>.Failure("The event no longer exists.");
        }

        entity.Title = model.Title.Trim();
        entity.Slug = slug;
        entity.ShortDescription = string.IsNullOrWhiteSpace(model.ShortDescription) ? null : model.ShortDescription.Trim();
        entity.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description;
        entity.EventDateTime = model.EventDateTime;
        entity.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim();
        entity.IsPublished = model.IsPublished;

        if (model.Id == 0)
        {
            dbContext.Events.Add(entity);
        }

        SynchroniseSpeakers(entity, validSpeakerIds);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // The unique index on Slug is the real guard against a duplicate URL. Two concurrent
            // saves can both pass the availability check, so the constraint violation is expected
            // and surfaced as a friendly message rather than a 500.
            logger.LogWarning(ex, "Saving event {EventId} violated a database constraint.", model.Id);
            return SaveResult<int>.Failure(
                $"The slug '{slug}' is already in use by another event. Choose a different one.");
        }

        return SaveResult<int>.Success(entity.Id);
    }

    /// <inheritdoc />
    public async Task<string> GenerateUniqueSlugAsync(string title, int? excludeEventId)
    {
        var baseSlug = SlugGenerator.Generate(title);

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            return "";
        }

        // Pull the small set of slugs that could collide in one query rather than probing the
        // database once per candidate suffix.
        var taken = await dbContext.Events
            .AsNoTracking()
            .Where(x => (x.Slug == baseSlug || x.Slug.StartsWith(baseSlug + "-"))
                        && (excludeEventId == null || x.Id != excludeEventId))
            .Select(x => x.Slug)
            .ToListAsync();

        if (!taken.Contains(baseSlug))
        {
            return baseSlug;
        }

        var suffix = 2;
        string candidate;
        do
        {
            candidate = SlugGenerator.WithSuffix(baseSlug, suffix);
            suffix++;
        }
        while (taken.Contains(candidate));

        return candidate;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSpeakerInfo>> GetAvailableSpeakersAsync()
    {
        return await (
                from user in dbContext.Users.AsNoTracking()
                join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
                join role in dbContext.Roles on userRole.RoleId equals role.Id
                where role.Name == RoleNames.Speaker
                orderby user.FirstName, user.LastName
                select new EventSpeakerInfo(user.Id, user.FirstName, user.LastName))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> CanEditEventAsync(int eventId)
    {
        var result = await authorizationService.AuthorizeAsync(User, eventId, PolicyNames.EventEditor);
        return result.Succeeded;
    }

    /// <summary>
    /// Runs the model's DataAnnotations and <see cref="IValidatableObject"/> rules server-side.
    /// The client runs the same rules; this call is what actually enforces them.
    /// </summary>
    private static List<string> ValidateModel(EventEditModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.Select(x => x.ErrorMessage ?? "Invalid value.").ToList();
    }

    /// <summary>Returns which of the supplied user ids currently hold the Speaker role.</summary>
    private async Task<List<int>> GetSpeakerUserIdsAsync(List<int> candidateUserIds)
    {
        return await (
                from userRole in dbContext.UserRoles
                join role in dbContext.Roles on userRole.RoleId equals role.Id
                where role.Name == RoleNames.Speaker && candidateUserIds.Contains(userRole.UserId)
                select userRole.UserId)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Adds and removes speaker assignments so the event matches <paramref name="speakerUserIds"/>,
    /// leaving untouched rows alone so their audit history is not churned.
    /// </summary>
    private void SynchroniseSpeakers(Event entity, List<int> speakerUserIds)
    {
        var existing = entity.Speakers.ToList();

        foreach (var removed in existing.Where(x => !speakerUserIds.Contains(x.UserId)))
        {
            dbContext.EventSpeakers.Remove(removed);
            entity.Speakers.Remove(removed);
        }

        foreach (var addedUserId in speakerUserIds.Where(id => existing.All(x => x.UserId != id)))
        {
            entity.Speakers.Add(new EventSpeaker { UserId = addedUserId });
        }
    }
}
