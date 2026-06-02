using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using UserGroupSiteMiniMaxM3.Data.Models;
using UserGroupSiteMiniMaxM3.Shared.Models.Events;

namespace UserGroupSiteMiniMaxM3.Data.Services;

/// <summary>Contract for the event management service.</summary>
public interface IEventService
{
    /// <summary>Lists events visible to anonymous users (published only), newest first.</summary>
    Task<IReadOnlyList<EventSummaryDto>> ListPublishedAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists events visible to the user: all events for admins, only their own for assigned speakers.</summary>
    Task<IReadOnlyList<EventSummaryDto>> ListForUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>Gets an event by slug. Returns null when not found. Unpublished events are only returned to editors.</summary>
    Task<EventDto?> GetBySlugAsync(string slug, ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>Creates a new event. Returns the new event's id.</summary>
    /// <exception cref="EventValidationException">When validation fails.</exception>
    Task<int> CreateAsync(EventEditDto input, ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing event. Requires editor (admin or assigned speaker) authorization.</summary>
    /// <exception cref="EventValidationException">When validation fails.</exception>
    /// <exception cref="UnauthorizedAccessException">When the user cannot edit the event.</exception>
    /// <exception cref="KeyNotFoundException">When the event does not exist.</exception>
    Task UpdateAsync(int id, EventEditDto input, ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>Deletes an event. Admin only.</summary>
    /// <exception cref="UnauthorizedAccessException">When the user is not an admin.</exception>
    /// <exception cref="KeyNotFoundException">When the event does not exist.</exception>
    Task DeleteAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>Replaces the speaker set for an event. Editor only.</summary>
    Task SetSpeakersAsync(int id, IEnumerable<int> userIds, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}

/// <summary>Thrown when an event fails validation.</summary>
public class EventValidationException : Exception
{
    /// <summary>The per-field validation errors.</summary>
    public EventValidationResult Result { get; }

    public EventValidationException(EventValidationResult result)
        : base("Event validation failed.")
    {
        Result = result;
    }
}

/// <summary>Server-side <see cref="IEventService"/> implementation.</summary>
public class EventService(
    ApplicationDbContext db,
    EventValidator validator) : IEventService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSummaryDto>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        // Anonymous home: published only, descending date.
        var events = await db.Events
            .AsNoTracking()
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.EventDateTime)
            .Include(e => e.Speakers)
            .ToListAsync(cancellationToken);

        return events.Select(ToSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSummaryDto>> ListForUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var isAdmin = user.IsInRole(Roles.Admin);
        var currentUserId = int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        IQueryable<GroupEvent> query = db.Events.AsNoTracking();
        if (!isAdmin)
        {
            // Speakers see only events they speak at.
            if (currentUserId == 0)
            {
                return [];
            }
            query = query.Where(e => e.Speakers.Any(s => s.Id == currentUserId));
        }

        var events = await query
            .OrderByDescending(e => e.EventDateTime)
            .Include(e => e.Speakers)
            .ToListAsync(cancellationToken);

        return events.Select(ToSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<EventDto?> GetBySlugAsync(string slug, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentNullException.ThrowIfNull(user);

        var ev = await db.Events
            .AsNoTracking()
            .Include(e => e.Speakers)
            .FirstOrDefaultAsync(e => e.Slug == slug, cancellationToken);

        if (ev is null)
        {
            return null;
        }

        // Unpublished events are only visible to editors (admin or assigned speaker).
        if (!ev.IsPublished && !CanViewUnpublished(ev, user))
        {
            return null;
        }

        return new EventDto(
            ev.Id,
            ev.Title,
            ev.Slug,
            ev.ShortDescription,
            ev.Description,
            ev.EventDateTime,
            ev.Location,
            ev.IsPublished,
            ev.Speakers.Select(s => new EventSpeakerDto(s.Id, DisplayName(s))).ToList());
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(EventEditDto input, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(user);

        // Anyone signed in can suggest an event; admins/speakers are auto-editors.
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("You must be signed in to create an event.");
        }

        var result = await validator.ValidateAsync(
            input,
            (slug, _) => db.Events.AllAsync(e => e.Slug != slug, cancellationToken),
            DateTime.UtcNow,
            cancellationToken);

        if (!result.IsValid)
        {
            throw new EventValidationException(result);
        }

        var ev = new GroupEvent
        {
            Title = input.Title.Trim(),
            Slug = input.Slug.Trim(),
            ShortDescription = input.ShortDescription?.Trim(),
            Description = input.Description,
            EventDateTime = DateTime.SpecifyKind(input.EventDateTime, DateTimeKind.Utc),
            Location = input.Location?.Trim(),
            IsPublished = input.IsPublished,
        };

        if (input.SpeakerIds.Count > 0)
        {
            var speakers = await db.Users.Where(u => input.SpeakerIds.Contains(u.Id)).ToListAsync(cancellationToken);
            ev.Speakers = speakers;
        }

        db.Events.Add(ev);
        await db.SaveChangesAsync(cancellationToken);
        return ev.Id;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(int id, EventEditDto input, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(user);

        var ev = await db.Events.Include(e => e.Speakers).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (ev is null)
        {
            throw new KeyNotFoundException($"Event with id {id} not found.");
        }

        if (!CanEdit(ev, user))
        {
            throw new UnauthorizedAccessException("You are not authorized to edit this event.");
        }

        var result = await validator.ValidateAsync(
            input,
            (slug, currentId) => db.Events.AllAsync(e => e.Slug != slug || e.Id == currentId, cancellationToken),
            DateTime.UtcNow,
            cancellationToken);

        if (!result.IsValid)
        {
            throw new EventValidationException(result);
        }

        ev.Title = input.Title.Trim();
        ev.Slug = input.Slug.Trim();
        ev.ShortDescription = input.ShortDescription?.Trim();
        ev.Description = input.Description;
        ev.EventDateTime = DateTime.SpecifyKind(input.EventDateTime, DateTimeKind.Utc);
        ev.Location = input.Location?.Trim();
        ev.IsPublished = input.IsPublished;

        // Replace speakers.
        if (input.SpeakerIds.Count > 0)
        {
            var speakers = await db.Users.Where(u => input.SpeakerIds.Contains(u.Id)).ToListAsync(cancellationToken);
            ev.Speakers = speakers;
        }
        else
        {
            ev.Speakers.Clear();
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsInRole(Roles.Admin))
        {
            throw new UnauthorizedAccessException("Only admins can delete events.");
        }

        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (ev is null)
        {
            throw new KeyNotFoundException($"Event with id {id} not found.");
        }

        db.Events.Remove(ev);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetSpeakersAsync(int id, IEnumerable<int> userIds, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        ArgumentNullException.ThrowIfNull(user);

        var ev = await db.Events.Include(e => e.Speakers).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (ev is null)
        {
            throw new KeyNotFoundException($"Event with id {id} not found.");
        }

        if (!CanEdit(ev, user))
        {
            throw new UnauthorizedAccessException("You are not authorized to edit this event.");
        }

        var speakers = await db.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);
        ev.Speakers = speakers;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static EventSummaryDto ToSummary(GroupEvent e) => new(
        e.Id,
        e.Title,
        e.Slug,
        e.ShortDescription,
        e.EventDateTime,
        e.Location,
        e.IsPublished,
        e.Speakers.Select(DisplayName).ToList());

    private static string DisplayName(User u)
    {
        var name = $"{u.FirstName} {u.LastName}".Trim();
        return string.IsNullOrEmpty(name) ? (u.UserName ?? "User") : name;
    }

    private static bool CanViewUnpublished(GroupEvent ev, ClaimsPrincipal user)
    {
        if (user.IsInRole(Roles.Admin))
        {
            return true;
        }
        var userId = int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
        return ev.Speakers.Any(s => s.Id == userId);
    }

    private static bool CanEdit(GroupEvent ev, ClaimsPrincipal user)
    {
        if (user.IsInRole(Roles.Admin))
        {
            return true;
        }
        var userId = int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
        return ev.Speakers.Any(s => s.Id == userId);
    }
}