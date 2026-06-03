using UserGroupSiteSonnet46.Data.Interfaces;
using UserGroupSiteSonnet46.Data.Models;
using UserGroupSiteSonnet46.Shared.Services;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteSonnet46.Server.Services;

/// <summary>
/// Server-side topic service that queries <see cref="ApplicationDbContext"/> directly.
/// Uses <see cref="IUserService"/> for the current user's ID in vote/volunteer checks.
/// </summary>
public class ServerTopicService(
    ApplicationDbContext db,
    IUserService userService) : ITopicService
{
    /// <inheritdoc />
    public async Task<List<TopicSuggestionDto>> GetTopicsAsync()
    {
        var currentUserId = userService.UserId;

        // Materialize first; the DTO constructor with nested UserDto cannot be translated to SQL
        var topics = await db.TopicSuggestions
            .AsNoTracking()
            .Include(t => t.SuggestedBy)
            .Include(t => t.Volunteer)
            .Include(t => t.Votes)
            .ToListAsync();

        return topics
            .Select(t => new TopicSuggestionDto(
                t.Id,
                t.Title,
                t.Description,
                new UserDto(t.SuggestedBy.Id, t.SuggestedBy.FirstName, t.SuggestedBy.LastName, t.SuggestedBy.Email, false, false),
                t.Votes.Count,
                t.Votes.Any(v => v.UserId == currentUserId),
                t.Volunteer is null
                    ? null
                    : new UserDto(t.Volunteer.Id, t.Volunteer.FirstName, t.Volunteer.LastName, t.Volunteer.Email, false, false)))
            .OrderByDescending(t => t.VoteCount)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<TopicSuggestionDto> SuggestTopicAsync(string title, string? description)
    {
        var topic = new TopicSuggestion
        {
            Title = title,
            Description = description,
            SuggestedById = userService.UserId
        };

        db.TopicSuggestions.Add(topic);
        await db.SaveChangesAsync();

        var loaded = await db.TopicSuggestions
            .AsNoTracking()
            .Include(t => t.SuggestedBy)
            .FirstAsync(t => t.Id == topic.Id);

        return new TopicSuggestionDto(
            loaded.Id,
            loaded.Title,
            loaded.Description,
            new UserDto(loaded.SuggestedBy.Id, loaded.SuggestedBy.FirstName, loaded.SuggestedBy.LastName, loaded.SuggestedBy.Email, false, false),
            VoteCount: 0,
            HasCurrentUserVoted: false,
            Volunteer: null);
    }

    /// <inheritdoc />
    public async Task VoteAsync(int topicId)
    {
        var currentUserId = userService.UserId;
        var alreadyVoted = await db.TopicVotes
            .AnyAsync(v => v.TopicSuggestionId == topicId && v.UserId == currentUserId);

        if (alreadyVoted)
        {
            return;
        }

        db.TopicVotes.Add(new TopicVote { TopicSuggestionId = topicId, UserId = currentUserId });
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task VolunteerAsync(int topicId)
    {
        var topic = await db.TopicSuggestions.FindAsync(topicId)
            ?? throw new InvalidOperationException($"Topic {topicId} not found.");

        if (topic.VolunteerUserId.HasValue)
        {
            throw new InvalidOperationException("This topic already has a volunteer.");
        }

        topic.VolunteerUserId = userService.UserId;
        await db.SaveChangesAsync();
    }
}
