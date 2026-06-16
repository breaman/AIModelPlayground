using Microsoft.EntityFrameworkCore;

using UserGroupSiteGpt55.Data.Models;
using UserGroupSiteGpt55.Data.Models.Topics;
using UserGroupSiteGpt55.Shared.Topics;

namespace UserGroupSiteGpt55.Server.Services.Topics;

/// <summary>
/// Implements authenticated topic suggestions, voting, and volunteering.
/// </summary>
public sealed class ServerTopicSuggestionService(
    ApplicationDbContext dbContext,
    IHttpContextAccessor httpContextAccessor) : ITopicSuggestionService
{
    /// <summary>
    /// Lists topic suggestions with current-user voting state.
    /// </summary>
    public async Task<IReadOnlyList<TopicSuggestionItem>> GetSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var suggestions = await dbContext.TopicSuggestions
            .AsNoTracking()
            .Include(t => t.SuggestedByUser)
            .Include(t => t.VolunteerSpeakerUser)
            .Include(t => t.Votes)
            .OrderByDescending(t => t.CreatedOn)
            .ToListAsync(cancellationToken);

        return suggestions.Select(t => new TopicSuggestionItem(
            t.Id,
            t.Title,
            t.Description,
            GetDisplayName(t.SuggestedByUser),
            t.VolunteerSpeakerUser is null ? null : GetDisplayName(t.VolunteerSpeakerUser),
            t.Votes.Count,
            currentUserId is not null && t.Votes.Any(v => v.UserId == currentUserId.Value),
            currentUserId is not null && t.VolunteerSpeakerUserId == currentUserId.Value,
            t.Status,
            t.CreatedOn)).ToList();
    }

    /// <summary>
    /// Creates a new topic suggestion for the current authenticated user.
    /// </summary>
    public async Task<TopicActionResult> CreateSuggestionAsync(TopicSuggestionCreateModel model, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return TopicActionResult.Failure("You must be logged in to suggest topics.");
        }

        if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Description))
        {
            return TopicActionResult.Failure("Title and description are required.");
        }

        dbContext.TopicSuggestions.Add(new TopicSuggestion
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            SuggestedByUserId = currentUserId.Value,
            CreatedOn = DateTimeOffset.UtcNow,
            Status = TopicSuggestionStatus.Open
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return TopicActionResult.Success();
    }

    /// <summary>
    /// Adds a current-user vote when one does not already exist.
    /// </summary>
    public async Task<TopicActionResult> VoteAsync(int topicSuggestionId, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return TopicActionResult.Failure("You must be logged in to vote.");
        }

        var exists = await dbContext.TopicSuggestions.AnyAsync(t => t.Id == topicSuggestionId, cancellationToken);
        if (!exists)
        {
            return TopicActionResult.Failure("The topic suggestion could not be found.");
        }

        if (await dbContext.TopicVotes.AnyAsync(v => v.TopicSuggestionId == topicSuggestionId && v.UserId == currentUserId.Value, cancellationToken))
        {
            return TopicActionResult.Failure("You have already voted for this topic.");
        }

        dbContext.TopicVotes.Add(new TopicVote
        {
            TopicSuggestionId = topicSuggestionId,
            UserId = currentUserId.Value,
            CreatedOn = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return TopicActionResult.Success();
    }

    /// <summary>
    /// Removes the current user's vote when present.
    /// </summary>
    public async Task<TopicActionResult> RemoveVoteAsync(int topicSuggestionId, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return TopicActionResult.Failure("You must be logged in to change votes.");
        }

        var vote = await dbContext.TopicVotes.FindAsync([topicSuggestionId, currentUserId.Value], cancellationToken);
        if (vote is null)
        {
            return TopicActionResult.Failure("You have not voted for this topic.");
        }

        dbContext.TopicVotes.Remove(vote);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TopicActionResult.Success();
    }

    /// <summary>
    /// Assigns the current user as the volunteer speaker when the slot is open.
    /// </summary>
    public async Task<TopicActionResult> VolunteerAsync(int topicSuggestionId, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return TopicActionResult.Failure("You must be logged in to volunteer.");
        }

        var topic = await dbContext.TopicSuggestions.SingleOrDefaultAsync(t => t.Id == topicSuggestionId, cancellationToken);
        if (topic is null)
        {
            return TopicActionResult.Failure("The topic suggestion could not be found.");
        }

        if (topic.VolunteerSpeakerUserId is not null)
        {
            return TopicActionResult.Failure("A volunteer speaker is already assigned.");
        }

        topic.VolunteerSpeakerUserId = currentUserId.Value;
        await dbContext.SaveChangesAsync(cancellationToken);
        return TopicActionResult.Success();
    }

    private int? GetCurrentUserId()
    {
        return httpContextAccessor.HttpContext?.User.GetUserId();
    }

    private static string GetDisplayName(User user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email ?? user.UserName ?? $"User {user.Id}" : fullName;
    }
}
