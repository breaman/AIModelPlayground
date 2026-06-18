using Microsoft.EntityFrameworkCore;

using UserGroupSiteQwen35.Data.Interfaces;
using UserGroupSiteQwen35.Data.Models;

namespace UserGroupSiteQwen35.Data.Repositories;

public class TopicVoteRepository : ITopicVoteRepository
{
    private readonly ApplicationDbContext _context;

    public TopicVoteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TopicVote?> GetByUserAndTopicAsync(int userId, int topicId)
    {
        return await _context.TopicVotes
            .FirstOrDefaultAsync(tv => tv.UserId == userId && tv.TopicSuggestionId == topicId);
    }

    public async Task<TopicVote> CreateAsync(TopicVote vote)
    {
        _context.TopicVotes.Add(vote);
        await _context.SaveChangesAsync();
        return vote;
    }

    public async Task DeleteAsync(TopicVote vote)
    {
        _context.TopicVotes.Remove(vote);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetVoteCountAsync(int topicId)
    {
        return await _context.TopicVotes.CountAsync(tv => tv.TopicSuggestionId == topicId);
    }
}