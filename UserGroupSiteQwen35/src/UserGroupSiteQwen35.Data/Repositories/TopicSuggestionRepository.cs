using UserGroupSiteQwen35.Data.Interfaces;
using UserGroupSiteQwen35.Data.Models;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteQwen35.Data.Repositories;

public class TopicSuggestionRepository : ITopicSuggestionRepository
{
    private readonly ApplicationDbContext _context;

    public TopicSuggestionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TopicSuggestion>> GetAllAsync()
    {
        return await _context.TopicSuggestions
            .Include(t => t.SuggestedByUser)
            .Include(t => t.VolunteerSpeaker)
            .ThenInclude(vs => vs != null ? vs.User : null)
            .OrderByDescending(t => t.CreatedOn)
            .ToListAsync();
    }

    public async Task<TopicSuggestion?> GetByIdAsync(int id)
    {
        return await _context.TopicSuggestions
            .Include(t => t.SuggestedByUser)
            .Include(t => t.VolunteerSpeaker)
            .ThenInclude(vs => vs != null ? vs.User : null)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TopicSuggestion> CreateAsync(TopicSuggestion suggestion)
    {
        _context.TopicSuggestions.Add(suggestion);
        await _context.SaveChangesAsync();
        return suggestion;
    }

    public async Task<TopicSuggestion> UpdateAsync(TopicSuggestion suggestion)
    {
        _context.Entry(suggestion).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return suggestion;
    }

    public async Task DeleteAsync(int id)
    {
        var suggestion = await _context.TopicSuggestions.FindAsync(id);
        if (suggestion != null)
        {
            _context.TopicSuggestions.Remove(suggestion);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<TopicSuggestion>> GetWithVoteCountsAsync()
    {
        return await _context.TopicSuggestions
            .Include(t => t.SuggestedByUser)
            .Include(t => t.VolunteerSpeaker)
            .ThenInclude(vs => vs != null ? vs.User : null)
            .Include(t => t.TopicVotes)
            .OrderByDescending(t => t.TopicVotes!.Count)
            .ThenByDescending(t => t.CreatedOn)
            .ToListAsync();
    }

    public async Task<TopicSuggestion?> GetWithVotesAndVolunteerAsync(int id)
    {
        return await _context.TopicSuggestions
            .Include(t => t.SuggestedByUser)
            .Include(t => t.VolunteerSpeaker)
            .ThenInclude(vs => vs != null ? vs.User : null)
            .Include(t => t.TopicVotes)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
