using Microsoft.EntityFrameworkCore;

using UserGroupSiteGlm51.Data.Interfaces;
using UserGroupSiteGlm51.Data.Models;

namespace UserGroupSiteGlm51.Server.Services;

/// <summary>
/// Server-side implementation of <see cref="ITopicSuggestionService"/> using EF Core and ApplicationDbContext.
/// </summary>
public class TopicSuggestionService(ApplicationDbContext dbContext) : ITopicSuggestionService
{
    public async Task<IEnumerable<TopicSuggestion>> GetAllTopicsAsync()
    {
        return await dbContext.TopicSuggestions
            .Include(ts => ts.SuggestedBy)
            .Include(ts => ts.Volunteer)
            .Include(ts => ts.Votes)
            .OrderByDescending(ts => ts.Votes.Count)
            .ThenByDescending(ts => ts.CreatedOn)
            .ToListAsync();
    }

    public async Task<TopicSuggestion?> GetTopicByIdAsync(int id)
    {
        return await dbContext.TopicSuggestions
            .Include(ts => ts.SuggestedBy)
            .Include(ts => ts.Volunteer)
            .Include(ts => ts.Votes)
            .FirstOrDefaultAsync(ts => ts.Id == id);
    }

    public async Task<TopicSuggestion> CreateTopicAsync(TopicSuggestion topic)
    {
        dbContext.TopicSuggestions.Add(topic);
        await dbContext.SaveChangesAsync();
        return topic;
    }

    public async Task VoteAsync(int topicSuggestionId, int userId)
    {
        // Enforce one-vote-per-user constraint
        var alreadyVoted = await dbContext.TopicVotes
            .AnyAsync(tv => tv.TopicSuggestionId == topicSuggestionId && tv.UserId == userId);
        if (alreadyVoted)
        {
            return;
        }

        dbContext.TopicVotes.Add(new TopicVote { TopicSuggestionId = topicSuggestionId, UserId = userId });
        await dbContext.SaveChangesAsync();
    }

    public async Task UnvoteAsync(int topicSuggestionId, int userId)
    {
        var vote = await dbContext.TopicVotes
            .FirstOrDefaultAsync(tv => tv.TopicSuggestionId == topicSuggestionId && tv.UserId == userId);
        if (vote is not null)
        {
            dbContext.TopicVotes.Remove(vote);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task VolunteerAsync(int topicSuggestionId, int userId)
    {
        var topic = await dbContext.TopicSuggestions.FindAsync(topicSuggestionId);
        if (topic is null)
        {
            return;
        }

        // Only allow volunteering if no one has volunteered yet (first-come-first-served)
        if (topic.VolunteerId is null)
        {
            topic.VolunteerId = userId;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task UnvolunteerAsync(int topicSuggestionId, int userId)
    {
        var topic = await dbContext.TopicSuggestions.FindAsync(topicSuggestionId);
        if (topic is null)
        {
            return;
        }

        // Only the current volunteer can unvolunteer
        if (topic.VolunteerId == userId)
        {
            topic.VolunteerId = null;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> HasVotedAsync(int topicSuggestionId, int userId)
    {
        return await dbContext.TopicVotes
            .AnyAsync(tv => tv.TopicSuggestionId == topicSuggestionId && tv.UserId == userId);
    }
}