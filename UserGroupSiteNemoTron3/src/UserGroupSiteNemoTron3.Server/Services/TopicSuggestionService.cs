using UserGroupSiteNemoTron3.Data.Interfaces;
using UserGroupSiteNemoTron3.Data.Models;
using UserGroupSiteNemoTron3.Shared.DTOs;
using UserGroupSiteNemoTron3.Shared.Services;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteNemoTron3.Server.Services;

public class TopicSuggestionService : ITopicSuggestionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserService _userService;

    public TopicSuggestionService(ApplicationDbContext dbContext, IUserService userService)
    {
        _dbContext = dbContext;
        _userService = userService;
    }

    public async Task<TopicSuggestionDto[]> GetAllAsync()
    {
        var currentUserId = _userService.UserId;
        var topics = await _dbContext.TopicSuggestions
            .OrderByDescending(ts => ts.CreatedOn)
            .Select(ts => new TopicSuggestionDto
            {
                Id = ts.Id,
                Title = ts.Title,
                Description = ts.Description,
                VolunteerSpeakerId = ts.VolunteerSpeakerId,
                VolunteerSpeakerName = ts.VolunteerSpeaker != null ? $"{ts.VolunteerSpeaker.FirstName} {ts.VolunteerSpeaker.LastName}".Trim() : null,
                VoteCount = ts.Votes.Count,
                HasCurrentUserVoted = ts.Votes.Any(v => v.UserId == currentUserId),
                CreatedOn = ts.CreatedOn ?? DateTime.MinValue,
                CreatedBy = ts.CreatedBy.ToString()
            })
            .ToArrayAsync();

        return topics;
    }

    public async Task<TopicSuggestionDto?> GetByIdAsync(int id)
    {
        var currentUserId = _userService.UserId;
        var ts = await _dbContext.TopicSuggestions
            .Where(t => t.Id == id)
            .Select(t => new TopicSuggestionDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                VolunteerSpeakerId = t.VolunteerSpeakerId,
                VolunteerSpeakerName = t.VolunteerSpeaker != null ? $"{t.VolunteerSpeaker.FirstName} {t.VolunteerSpeaker.LastName}".Trim() : null,
                VoteCount = t.Votes.Count,
                HasCurrentUserVoted = t.Votes.Any(v => v.UserId == currentUserId),
                CreatedOn = t.CreatedOn ?? DateTime.MinValue,
                CreatedBy = t.CreatedBy.ToString()
            })
            .FirstOrDefaultAsync();

        return ts;
    }

    public async Task<TopicSuggestionDto> CreateAsync(CreateTopicSuggestionDto dto, int userId)
    {
        var topic = new TopicSuggestion
        {
            Title = dto.Title,
            Description = dto.Description
        };

        _dbContext.TopicSuggestions.Add(topic);
        await _dbContext.SaveChangesAsync();

        return await GetByIdAsync(topic.Id) ?? throw new InvalidOperationException("Failed to create topic suggestion");
    }

    public async Task VoteAsync(int topicId, int userId)
    {
        // Check if already voted
        if (await HasUserVotedAsync(topicId, userId))
        {
            return; // Already voted, no-op
        }

        var vote = new TopicVote
        {
            TopicSuggestionId = topicId,
            UserId = userId,
            VotedOn = DateTime.UtcNow
        };

        _dbContext.TopicVotes.Add(vote);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveVoteAsync(int topicId, int userId)
    {
        var vote = await _dbContext.TopicVotes
            .FirstOrDefaultAsync(v => v.TopicSuggestionId == topicId && v.UserId == userId);

        if (vote != null)
        {
            _dbContext.TopicVotes.Remove(vote);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task VolunteerAsync(int topicId, int userId)
    {
        var topic = await _dbContext.TopicSuggestions.FindAsync(topicId) ?? throw new InvalidOperationException("Topic not found");

        if (topic.VolunteerSpeakerId != null)
        {
            throw new InvalidOperationException("Topic already has a volunteer speaker");
        }

        topic.VolunteerSpeakerId = userId;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> HasUserVotedAsync(int topicId, int userId)
    {
        return await _dbContext.TopicVotes
            .AnyAsync(v => v.TopicSuggestionId == topicId && v.UserId == userId);
    }
}