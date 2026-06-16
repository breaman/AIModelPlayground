using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteGpt55.Data.Models;
using UserGroupSiteGpt55.Data.Models.Events;
using UserGroupSiteGpt55.Shared.Authorization;
using UserGroupSiteGpt55.Shared.Events;
using UserGroupSiteGpt55.Shared.Markdown;

namespace UserGroupSiteGpt55.Server.Services.Events;

/// <summary>
/// Implements event operations directly against EF Core for APIs and server prerendering.
/// </summary>
public sealed class ServerEventService(
    ApplicationDbContext dbContext,
    UserManager<User> userManager,
    IHttpContextAccessor httpContextAccessor,
    EventAuthorizationService authorizationService,
    IMarkdownRenderer markdownRenderer) : IEventService
{
    /// <summary>
    /// Gets published events ordered by newest first for anonymous visitors.
    /// </summary>
    public async Task<IReadOnlyList<EventListItem>> GetPublishedEventsAsync(CancellationToken cancellationToken = default)
    {
        var events = await dbContext.Events
            .AsNoTracking()
            .Include(e => e.EventSpeakers)
            .ThenInclude(e => e.User)
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.StartsAt)
            .ThenBy(e => e.Title)
            .ToListAsync(cancellationToken);

        return events.Select(ToListItem).ToList();
    }

    /// <summary>
    /// Gets events the current user can edit.
    /// </summary>
    public async Task<IReadOnlyList<EventListItem>> GetEditableEventsAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return [];
        }

        var isAdmin = await authorizationService.IsAdminAsync(currentUserId.Value);
        var query = dbContext.Events
            .AsNoTracking()
            .Include(e => e.EventSpeakers)
            .ThenInclude(e => e.User)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(e => e.EventSpeakers.Any(s => s.UserId == currentUserId.Value));
        }

        var events = await query
            .OrderByDescending(e => e.StartsAt)
            .ThenBy(e => e.Title)
            .ToListAsync(cancellationToken);

        return events.Select(ToListItem).ToList();
    }

    /// <summary>
    /// Gets an event by slug when it is public or editable by the current user.
    /// </summary>
    public async Task<EventDetail?> GetEventBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = SlugGenerator.Generate(slug);
        var item = await dbContext.Events
            .AsNoTracking()
            .Include(e => e.EventSpeakers)
            .ThenInclude(e => e.User)
            .SingleOrDefaultAsync(e => e.Slug == normalizedSlug, cancellationToken);

        if (item is null)
        {
            return null;
        }

        var currentUserId = GetCurrentUserId();
        var canEdit = currentUserId is not null &&
            await authorizationService.CanEditEventAsync(currentUserId.Value, item.Id, cancellationToken);

        if (!item.IsPublished && !canEdit)
        {
            return null;
        }

        return ToDetail(item, canEdit);
    }

    /// <summary>
    /// Gets an editable event model for the current editor.
    /// </summary>
    public async Task<EventEditModel?> GetEventForEditAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null || !await authorizationService.CanEditEventAsync(currentUserId.Value, eventId, cancellationToken))
        {
            return null;
        }

        var item = await dbContext.Events
            .AsNoTracking()
            .Include(e => e.EventSpeakers)
            .SingleOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        return item is null ? null : ToEditModel(item);
    }

    /// <summary>
    /// Creates a blank draft model for new events.
    /// </summary>
    public Task<EventEditModel> CreateDraftAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EventEditModel());
    }

    /// <summary>
    /// Gets all users in the Speaker role as selectable event speakers.
    /// </summary>
    public async Task<IReadOnlyList<SpeakerOption>> GetSpeakerOptionsAsync(
        IReadOnlyCollection<int> selectedSpeakerIds,
        CancellationToken cancellationToken = default)
    {
        var speakers = await userManager.GetUsersInRoleAsync(ApplicationRoles.Speaker);
        return speakers
            .OrderBy(GetDisplayName)
            .Select(s => new SpeakerOption(s.Id, GetDisplayName(s), s.Email ?? "", selectedSpeakerIds.Contains(s.Id)))
            .ToList();
    }

    /// <summary>
    /// Saves an event after validating and enforcing editor permissions.
    /// </summary>
    public async Task<EventSaveResult> SaveEventAsync(EventEditModel model, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return EventSaveResult.Failure(["You must be logged in to save events."]);
        }

        var isNew = model.Id == 0;
        if (isNew && !await authorizationService.IsAdminAsync(currentUserId.Value))
        {
            return EventSaveResult.Failure(["Only admins can create events."]);
        }

        if (!isNew && !await authorizationService.CanEditEventAsync(currentUserId.Value, model.Id, cancellationToken))
        {
            return EventSaveResult.Failure(["You are not allowed to edit this event."]);
        }

        var errors = await ValidateEventAsync(model, cancellationToken);
        if (errors.Count > 0)
        {
            return EventSaveResult.Failure(errors);
        }

        var item = isNew
            ? new UserGroupEvent()
            : await dbContext.Events.Include(e => e.EventSpeakers).SingleOrDefaultAsync(e => e.Id == model.Id, cancellationToken);

        if (item is null)
        {
            return EventSaveResult.Failure(["The event could not be found."]);
        }

        item.Title = model.Title.Trim();
        item.Slug = SlugGenerator.Generate(model.Slug);
        item.ShortDescription = string.IsNullOrWhiteSpace(model.ShortDescription) ? null : model.ShortDescription.Trim();
        item.MarkdownDescription = string.IsNullOrWhiteSpace(model.MarkdownDescription) ? null : model.MarkdownDescription.Trim();
        item.StartsAt = model.StartsAt;
        item.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim();
        item.IsPublished = model.IsPublished;

        if (isNew)
        {
            dbContext.Events.Add(item);
        }

        item.EventSpeakers.RemoveAll(s => !model.SpeakerIds.Contains(s.UserId));
        foreach (var speakerId in model.SpeakerIds.Where(id => item.EventSpeakers.All(s => s.UserId != id)))
        {
            item.EventSpeakers.Add(new EventSpeaker { Event = item, UserId = speakerId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return EventSaveResult.Success(item.Id, item.Slug);
    }

    /// <summary>
    /// Validates event draft and publishing requirements.
    /// </summary>
    public async Task<IReadOnlyList<string>> ValidateEventAsync(EventEditModel model, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var slug = SlugGenerator.Generate(model.Slug);

        errors.AddRange(EventValidation.Validate(model));

        if (!string.IsNullOrWhiteSpace(slug) &&
            await dbContext.Events.AnyAsync(e => e.Slug == slug && e.Id != model.Id, cancellationToken))
        {
            errors.Add("Slug must be unique.");
        }

        return errors;
    }

    private int? GetCurrentUserId()
    {
        return httpContextAccessor.HttpContext?.User.GetUserId();
    }

    private EventDetail ToDetail(UserGroupEvent item, bool canEdit)
    {
        return new EventDetail(
            item.Id,
            item.Title,
            item.Slug,
            item.ShortDescription,
            item.MarkdownDescription,
            markdownRenderer.Render(item.MarkdownDescription),
            item.StartsAt,
            item.Location,
            item.IsPublished,
            canEdit,
            item.EventSpeakers.Select(s => new SpeakerOption(s.UserId, GetDisplayName(s.User), s.User.Email ?? "", true)).ToList());
    }

    private static EventEditModel ToEditModel(UserGroupEvent item)
    {
        return new EventEditModel
        {
            Id = item.Id,
            Title = item.Title,
            Slug = item.Slug,
            ShortDescription = item.ShortDescription,
            MarkdownDescription = item.MarkdownDescription,
            StartsAt = item.StartsAt,
            Location = item.Location,
            IsPublished = item.IsPublished,
            SpeakerIds = item.EventSpeakers.Select(s => s.UserId).ToList()
        };
    }

    private static EventListItem ToListItem(UserGroupEvent item)
    {
        return new EventListItem(
            item.Id,
            item.Title,
            item.Slug,
            item.ShortDescription,
            item.StartsAt,
            item.Location,
            item.IsPublished,
            item.EventSpeakers.Select(s => GetDisplayName(s.User)).ToList());
    }

    private static string GetDisplayName(User user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email ?? user.UserName ?? $"User {user.Id}" : fullName;
    }
}
