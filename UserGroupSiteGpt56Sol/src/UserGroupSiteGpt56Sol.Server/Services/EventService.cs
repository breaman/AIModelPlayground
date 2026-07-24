using System.Data;

using UserGroupSiteGpt56Sol.Data.Models;
using UserGroupSiteGpt56Sol.Shared.Authorization;
using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;
using UserGroupSiteGpt56Sol.Shared.Utilities;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteGpt56Sol.Server.Services;

/// <summary>Implements event queries and commands against EF Core.</summary>
public sealed class EventService(
    ApplicationDbContext dbContext,
    CurrentUserAccessor currentUser,
    UserManager<User> userManager,
    ILogger<EventService> logger) : IEventService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSummaryDto>> GetPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Events.AsNoTracking()
            .Where(item => item.IsPublished && item.StartsAtUtc.HasValue)
            .OrderByDescending(item => item.StartsAtUtc)
            .Select(item => new EventSummaryDto(item.Id, item.Title, item.Slug, item.ShortDescription,
                item.StartsAtUtc!.Value, item.Location!, item.EventSpeakers
                    .OrderBy(speaker => speaker.User.FirstName).ThenBy(speaker => speaker.User.LastName)
                    .Select(speaker =>
                        ((speaker.User.FirstName ?? string.Empty) + " " +
                         (speaker.User.LastName ?? string.Empty)).Trim() != string.Empty
                            ? ((speaker.User.FirstName ?? string.Empty) + " " +
                               (speaker.User.LastName ?? string.Empty)).Trim()
                            : speaker.User.Email ?? speaker.User.UserName ?? "Member").ToList()))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EventDetailDto?> GetPublishedBySlugAsync(string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = SlugUtility.Normalize(slug);
        return await dbContext.Events.AsNoTracking()
            .Where(item => item.IsPublished && item.NormalizedSlug == normalizedSlug && item.StartsAtUtc.HasValue)
            .Select(item => new EventDetailDto(item.Id, item.Title, item.Slug, item.ShortDescription,
                MarkdownRenderer.ToSafeHtml(item.DescriptionMarkdown), item.StartsAtUtc!.Value, item.Location!,
                item.EventSpeakers.OrderBy(speaker => speaker.User.FirstName)
                    .ThenBy(speaker => speaker.User.LastName)
                    .Select(speaker =>
                        ((speaker.User.FirstName ?? string.Empty) + " " +
                         (speaker.User.LastName ?? string.Empty)).Trim() != string.Empty
                            ? ((speaker.User.FirstName ?? string.Empty) + " " +
                               (speaker.User.LastName ?? string.Empty)).Trim()
                            : speaker.User.Email ?? speaker.User.UserName ?? "Member").ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventAdminListItemDto>> GetManageListAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var query = dbContext.Events.AsNoTracking();
        if (!currentUser.IsAdmin)
        {
            var userId = currentUser.UserId;
            query = query.Where(item => item.EventSpeakers.Any(speaker => speaker.UserId == userId));
        }

        return await query.OrderByDescending(item => item.StartsAtUtc)
            .ThenBy(item => item.Title)
            .Select(item => new EventAdminListItemDto(item.Id, item.Title, item.Slug, item.StartsAtUtc,
                item.IsPublished, currentUser.IsAdmin))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EventEditRequest?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        await EnsureCanEditAsync(id, cancellationToken);
        return await dbContext.Events.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new EventEditRequest
            {
                Title = item.Title,
                Slug = item.Slug,
                ShortDescription = item.ShortDescription,
                DescriptionMarkdown = item.DescriptionMarkdown,
                StartsAtUtc = item.StartsAtUtc,
                Location = item.Location,
                IsPublished = item.IsPublished,
                SpeakerIds = item.EventSpeakers.Select(speaker => speaker.UserId).ToList(),
                RowVersion = Convert.ToBase64String(item.RowVersion)
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpeakerOptionDto>> GetSpeakerOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var users = await userManager.GetUsersInRoleAsync(AppRoles.Speaker);
        return users.OrderBy(DisplayName)
            .Select(user => new SpeakerOptionDto(user.Id, DisplayName(user), user.Email ?? string.Empty))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ServiceResult<int>> SaveAsync(int? id, EventEditRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var errors = ValidationUtility.Validate(request);
        if (errors.Count > 0)
        {
            return ServiceResult<int>.Invalid(errors);
        }

        request.Slug = SlugUtility.Normalize(request.Slug);
        var normalizedSlug = request.Slug;
        if (await dbContext.Events.AnyAsync(item => item.NormalizedSlug == normalizedSlug && item.Id != id,
                cancellationToken))
        {
            return ServiceResult<int>.Invalid(new Dictionary<string, string[]>
            {
                [nameof(request.Slug)] = ["That slug is already in use."]
            });
        }

        if (!id.HasValue && !currentUser.IsAdmin)
        {
            throw new UnauthorizedAccessException("Only administrators can create events.");
        }

        Event entity;
        if (id.HasValue)
        {
            await EnsureCanEditAsync(id.Value, cancellationToken);
            entity = await dbContext.Events.Include(item => item.EventSpeakers)
                .SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("The event no longer exists.");
            if (string.IsNullOrWhiteSpace(request.RowVersion))
            {
                return ServiceResult<int>.Failure("The event has changed. Reload it and try again.");
            }

            dbContext.Entry(entity).Property(item => item.RowVersion).OriginalValue =
                Convert.FromBase64String(request.RowVersion);
        }
        else
        {
            entity = new Event
            {
                Title = request.Title.Trim(),
                Slug = request.Slug,
                NormalizedSlug = normalizedSlug
            };
            dbContext.Events.Add(entity);
        }

        entity.Title = request.Title.Trim();
        entity.Slug = request.Slug;
        entity.NormalizedSlug = normalizedSlug;
        entity.ShortDescription = NullIfWhiteSpace(request.ShortDescription);
        entity.DescriptionMarkdown = NullIfWhiteSpace(request.DescriptionMarkdown);
        entity.StartsAtUtc = request.StartsAtUtc.HasValue
            ? DateTime.SpecifyKind(request.StartsAtUtc.Value, DateTimeKind.Utc)
            : null;
        entity.Location = NullIfWhiteSpace(request.Location);
        entity.IsPublished = request.IsPublished;

        if (currentUser.IsAdmin)
        {
            var distinctSpeakerIds = request.SpeakerIds.Distinct().ToArray();
            var eligibleIds = await GetEligibleSpeakerIdsAsync(distinctSpeakerIds, cancellationToken);
            if (eligibleIds.Count != distinctSpeakerIds.Length)
            {
                return ServiceResult<int>.Invalid(new Dictionary<string, string[]>
                {
                    [nameof(request.SpeakerIds)] = ["Every selected user must have the Speaker role."]
                });
            }

            entity.EventSpeakers.RemoveWhere(speaker => !eligibleIds.Contains(speaker.UserId));
            foreach (var speakerId in eligibleIds.Where(speakerId =>
                         entity.EventSpeakers.All(existing => existing.UserId != speakerId)))
            {
                entity.EventSpeakers.Add(new EventSpeaker { UserId = speakerId });
            }
        }
        else if (!entity.EventSpeakers.Select(speaker => speaker.UserId)
                     .Order().SequenceEqual(request.SpeakerIds.Distinct().Order()))
        {
            return ServiceResult<int>.Failure("Only administrators can change event speakers.");
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Event {EventId} saved by user {UserId}", entity.Id, currentUser.UserId);
            return ServiceResult<int>.Success(entity.Id, "Event saved.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<int>.Failure("Someone else changed this event. Reload it and try again.");
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Database rejected event {EventId}", entity.Id);
            return ServiceResult<int>.Failure("The event could not be saved. Its slug may already be in use.");
        }
    }

    /// <summary>Checks whether the current user is an administrator or an assigned speaker.</summary>
    private async Task EnsureCanEditAsync(int eventId, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        if (currentUser.IsAdmin)
        {
            return;
        }

        if (!await dbContext.EventSpeakers.AnyAsync(
                item => item.EventId == eventId && item.UserId == currentUser.UserId, cancellationToken))
        {
            throw new UnauthorizedAccessException("You are not assigned to edit this event.");
        }
    }

    /// <summary>Returns the selected IDs that are currently members of the Speaker role.</summary>
    private async Task<HashSet<int>> GetEligibleSpeakerIdsAsync(int[] ids, CancellationToken cancellationToken)
    {
        if (ids.Length == 0)
        {
            return [];
        }

        var speakerRoleId = await dbContext.Roles.Where(role => role.NormalizedName == AppRoles.Speaker.ToUpperInvariant())
            .Select(role => (int?)role.Id).SingleOrDefaultAsync(cancellationToken);
        if (speakerRoleId is null)
        {
            return [];
        }

        return await dbContext.UserRoles.Where(item => item.RoleId == speakerRoleId && ids.Contains(item.UserId))
            .Select(item => item.UserId).ToHashSetAsync(cancellationToken);
    }

    /// <summary>Throws when no authenticated request user is available.</summary>
    private void EnsureAuthenticated()
    {
        if (currentUser.UserId == 0)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }
    }

    /// <summary>Builds a useful display name with an email fallback.</summary>
    private static string DisplayName(User user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.Email ?? user.UserName ?? "Member" : name;
    }

    /// <summary>Converts blank optional form values to null.</summary>
    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class CollectionExtensions
{
    /// <summary>Removes every collection item that matches a predicate.</summary>
    public static void RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate)
    {
        foreach (var item in collection.Where(predicate).ToArray())
        {
            collection.Remove(item);
        }
    }
}