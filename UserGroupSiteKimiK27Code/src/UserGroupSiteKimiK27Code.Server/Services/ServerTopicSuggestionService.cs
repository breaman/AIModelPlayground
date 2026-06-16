using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK27Code.Data.Interfaces;
using UserGroupSiteKimiK27Code.Data.Models;
using UserGroupSiteKimiK27Code.Shared.Dtos;
using UserGroupSiteKimiK27Code.Shared.Services;

namespace UserGroupSiteKimiK27Code.Server.Services;

public class ServerTopicSuggestionService : ITopicSuggestionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserService _userService;
    private readonly UserManager<User> _userManager;

    public ServerTopicSuggestionService(ApplicationDbContext dbContext, IUserService userService, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userService = userService;
        _userManager = userManager;
    }

    public async Task<List<TopicSuggestionDto>> GetSuggestionsAsync()
    {
        var userId = _userService.UserId;

        var suggestions = await _dbContext.TopicSuggestions
            .AsNoTracking()
            .Include(t => t.SuggestedBy)
            .Include(t => t.VolunteerSpeaker)
            .OrderByDescending(t => t.Votes.Count)
            .ThenBy(t => t.Title)
            .ToListAsync();

        return suggestions.Select(t => MapToDto(t, userId)).ToList();
    }

    public async Task<TopicSuggestionDto?> CreateSuggestionAsync(CreateTopicSuggestionRequest request)
    {
        var userId = _userService.UserId;
        if (userId == 0) return null;

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        var suggestion = new TopicSuggestion
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            SuggestedById = userId
        };

        _dbContext.TopicSuggestions.Add(suggestion);
        await _dbContext.SaveChangesAsync();

        return MapToDto(suggestion, userId);
    }

    public async Task<TopicVoteDto?> VoteAsync(int topicId)
    {
        var userId = _userService.UserId;
        if (userId == 0) return null;

        var existingVote = await _dbContext.TopicVotes
            .FirstOrDefaultAsync(v => v.TopicSuggestionId == topicId && v.UserId == userId);

        if (existingVote is null)
        {
            _dbContext.TopicVotes.Add(new TopicVote
            {
                TopicSuggestionId = topicId,
                UserId = userId
            });
        }
        else
        {
            _dbContext.TopicVotes.Remove(existingVote);
        }

        await _dbContext.SaveChangesAsync();

        var count = await _dbContext.TopicVotes
            .AsNoTracking()
            .CountAsync(v => v.TopicSuggestionId == topicId);

        return new TopicVoteDto
        {
            TopicSuggestionId = topicId,
            VoteCount = count,
            HasVoted = existingVote is null
        };
    }

    public async Task<TopicSuggestionDto?> VolunteerAsync(int topicId)
    {
        var userId = _userService.UserId;
        if (userId == 0) return null;

        var suggestion = await _dbContext.TopicSuggestions
            .Include(t => t.VolunteerSpeaker)
            .FirstOrDefaultAsync(t => t.Id == topicId);

        if (suggestion is null || suggestion.VolunteerSpeakerId.HasValue) return null;

        suggestion.VolunteerSpeakerId = userId;
        await _dbContext.SaveChangesAsync();

        return MapToDto(suggestion, userId);
    }

    private static TopicSuggestionDto MapToDto(TopicSuggestion suggestion, int currentUserId)
    {
        return new TopicSuggestionDto
        {
            Id = suggestion.Id,
            Title = suggestion.Title,
            Description = suggestion.Description,
            SuggestedById = suggestion.SuggestedById,
            SuggestedByName = $"{suggestion.SuggestedBy.FirstName} {suggestion.SuggestedBy.LastName}".Trim(),
            VolunteerSpeakerId = suggestion.VolunteerSpeakerId,
            VolunteerSpeakerName = suggestion.VolunteerSpeaker is not null
                ? $"{suggestion.VolunteerSpeaker.FirstName} {suggestion.VolunteerSpeaker.LastName}".Trim()
                : null,
            VoteCount = suggestion.Votes.Count,
            HasVoted = suggestion.Votes.Any(v => v.UserId == currentUserId)
        };
    }
}