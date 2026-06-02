using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteMiniMaxM3.Data.Models;
using UserGroupSiteMiniMaxM3.Shared.Models.Topics;
using UserGroupSiteMiniMaxM3.Shared.Services;

namespace UserGroupSiteMiniMaxM3.Data.Services;

/// <summary>Server-side topic operations. Uses the database directly.</summary>
public class TopicService(
    ApplicationDbContext db,
    UserManager<User> users) : ITopicService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TopicSummaryDto>> ListAsync(string? currentUserId, CancellationToken cancellationToken = default)
    {
        // Single query for topics + suggester + counts.
        var topics = await db.TopicSuggestions
            .AsNoTracking()
            .Include(t => t.SuggestedByUser)
            .Include(t => t.Votes)
            .Include(t => t.Volunteers)
            .OrderByDescending(t => t.CreatedOn)
            .ThenByDescending(t => t.Id)
            .ToListAsync(cancellationToken);

        // Resolve the current user's id once.
        var currentUserDbId = await ResolveUserIdAsync(currentUserId, cancellationToken);

        return topics.Select(t => new TopicSummaryDto(
            t.Id,
            t.Title,
            t.Description,
            t.SuggestedByUserId,
            DisplayName(t.SuggestedByUser),
            t.CreatedOn ?? DateTime.UtcNow,
            t.Votes.Count,
            t.Volunteers.Count,
            currentUserDbId is not null && t.Votes.Any(v => v.UserId == currentUserDbId),
            currentUserDbId is not null && t.Volunteers.Any(v => v.UserId == currentUserDbId))).ToList();
    }

    /// <inheritdoc />
    public async Task<TopicSummaryDto> CreateAsync(TopicCreateDto input, string currentUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Validate inline. Title is required, max 200 (matches the entity column).
        var context = new ValidationContext(input);
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, context, errors, validateAllProperties: true))
        {
            throw new TopicValidationException(errors
                .GroupBy(e => e.MemberNames.FirstOrDefault() ?? "_")
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? "Invalid").ToArray()));
        }

        var userDbId = await ResolveUserIdAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user could not be resolved.");

        var topic = new TopicSuggestion
        {
            Title = input.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            SuggestedByUserId = userDbId,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = userDbId,
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = userDbId,
        };

        db.TopicSuggestions.Add(topic);
        await db.SaveChangesAsync(cancellationToken);

        var user = await db.Users.FindAsync([userDbId], cancellationToken);
        return new TopicSummaryDto(
            topic.Id,
            topic.Title,
            topic.Description,
            topic.SuggestedByUserId,
            DisplayName(user),
            topic.CreatedOn ?? DateTime.UtcNow,
            VoteCount: 0,
            VolunteerCount: 0,
            UserHasVoted: false,
            UserHasVolunteered: false);
    }

    /// <inheritdoc />
    public async Task<bool> ToggleVoteAsync(int topicId, string currentUserId, CancellationToken cancellationToken = default)
    {
        var userDbId = await ResolveUserIdAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user could not be resolved.");

        var existing = await db.TopicVotes
            .FirstOrDefaultAsync(v => v.TopicSuggestionId == topicId && v.UserId == userDbId, cancellationToken);

        if (existing is not null)
        {
            db.TopicVotes.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        // Ensure the topic exists before voting.
        var exists = await db.TopicSuggestions.AnyAsync(t => t.Id == topicId, cancellationToken);
        if (!exists) throw new KeyNotFoundException($"Topic {topicId} not found.");

        db.TopicVotes.Add(new TopicVote
        {
            TopicSuggestionId = topicId,
            UserId = userDbId,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ToggleVolunteerAsync(int topicId, string currentUserId, CancellationToken cancellationToken = default)
    {
        var userDbId = await ResolveUserIdAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user could not be resolved.");

        var existing = await db.TopicVolunteers
            .FirstOrDefaultAsync(v => v.TopicSuggestionId == topicId && v.UserId == userDbId, cancellationToken);

        if (existing is not null)
        {
            db.TopicVolunteers.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        var exists = await db.TopicSuggestions.AnyAsync(t => t.Id == topicId, cancellationToken);
        if (!exists) throw new KeyNotFoundException($"Topic {topicId} not found.");

        db.TopicVolunteers.Add(new TopicVolunteer
        {
            TopicSuggestionId = topicId,
            UserId = userDbId,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int topicId, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var userDbId = await ResolveUserIdAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user could not be resolved.");

        var topic = await db.TopicSuggestions.FindAsync([topicId], cancellationToken)
            ?? throw new KeyNotFoundException($"Topic {topicId} not found.");

        if (!isAdmin && topic.SuggestedByUserId != userDbId)
        {
            throw new UnauthorizedAccessException("Only the suggester or an admin can delete a topic.");
        }

        db.TopicSuggestions.Remove(topic);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Resolves the ASP.NET Identity user id (string) to the int primary key.</summary>
    private async Task<int?> ResolveUserIdAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId)) return null;
        var user = await users.FindByIdAsync(userId);
        return user?.Id;
    }

    private static string DisplayName(User? u)
    {
        if (u is null) return "Unknown";
        var name = $"{u.FirstName} {u.LastName}".Trim();
        return string.IsNullOrEmpty(name) ? (u.UserName ?? "User") : name;
    }
}

/// <summary>Validation failure for topic create.</summary>
public class TopicValidationException(Dictionary<string, string[]> errors) : Exception("Topic validation failed.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}