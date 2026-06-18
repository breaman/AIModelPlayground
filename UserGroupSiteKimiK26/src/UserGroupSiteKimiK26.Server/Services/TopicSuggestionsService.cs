using Microsoft.EntityFrameworkCore;

using UserGroupSiteKimiK26.Data.Interfaces;
using UserGroupSiteKimiK26.Data.Models;
using UserGroupSiteKimiK26.Shared.Dtos;
using UserGroupSiteKimiK26.Shared.Services;

namespace UserGroupSiteKimiK26.Server.Services;

public class TopicSuggestionsService(
    ApplicationDbContext dbContext,
    IUserService currentUserService) : ITopicSuggestionsService
{
    public async Task<List<TopicSuggestionDto>> GetAllSuggestionsAsync()
    {
        var userId = currentUserService.UserId;
        var suggestions = await dbContext.TopicSuggestions
            .Include(t => t.SuggestedByUser)
            .Include(t => t.VolunteerUser)
            .Include(t => t.Votes)
            .ThenInclude(v => v.User)
            .OrderByDescending(t => t.Votes.Count)
            .ToListAsync();

        return suggestions.Select(t => new TopicSuggestionDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            SuggestedByUserId = t.SuggestedByUserId,
            SuggestedByName = $"{t.SuggestedByUser.FirstName} {t.SuggestedByUser.LastName}",
            VolunteerUserId = t.VolunteerUserId,
            VolunteerName = t.VolunteerUser != null ? $"{t.VolunteerUser.FirstName} {t.VolunteerUser.LastName}" : null,
            VoteCount = t.Votes.Count,
            HasVoted = t.Votes.Any(v => v.UserId == userId)
        }).ToList();
    }

    public async Task<TopicSuggestionDto> CreateSuggestionAsync(CreateSuggestionDto dto)
    {
        var userId = currentUserService.UserId;
        if (userId == default) throw new InvalidOperationException("User must be authenticated.");

        var suggestion = new TopicSuggestion
        {
            Title = dto.Title,
            Description = dto.Description,
            SuggestedByUserId = userId
        };

        dbContext.TopicSuggestions.Add(suggestion);
        await dbContext.SaveChangesAsync();

        return new TopicSuggestionDto
        {
            Id = suggestion.Id,
            Title = suggestion.Title,
            Description = suggestion.Description,
            SuggestedByUserId = userId,
            SuggestedByName = "You",
            VoteCount = 0,
            HasVoted = false
        };
    }

    public async Task<VoteResultDto> VoteAsync(int topicId)
    {
        var userId = currentUserService.UserId;
        if (userId == default) throw new InvalidOperationException("User must be authenticated.");

        var existingVote = await dbContext.TopicVotes
            .FirstOrDefaultAsync(v => v.TopicSuggestionId == topicId && v.UserId == userId);

        if (existingVote is not null)
        {
            dbContext.TopicVotes.Remove(existingVote);
            await dbContext.SaveChangesAsync();
            return new VoteResultDto { HasVoted = false, VoteCount = await GetVoteCountAsync(topicId) };
        }
        else
        {
            dbContext.TopicVotes.Add(new TopicVote { TopicSuggestionId = topicId, UserId = userId });
            await dbContext.SaveChangesAsync();
            return new VoteResultDto { HasVoted = true, VoteCount = await GetVoteCountAsync(topicId) };
        }
    }

    public async Task<TopicSuggestionDto> VolunteerAsync(int topicId)
    {
        var userId = currentUserService.UserId;
        if (userId == default) throw new InvalidOperationException("User must be authenticated.");

        var suggestion = await dbContext.TopicSuggestions
            .Include(t => t.VolunteerUser)
            .Include(t => t.SuggestedByUser)
            .Include(t => t.Votes)
            .FirstOrDefaultAsync(t => t.Id == topicId);

        if (suggestion is null) throw new InvalidOperationException("Topic not found.");
        if (suggestion.VolunteerUserId is not null) throw new InvalidOperationException("This topic already has a volunteer.");

        suggestion.VolunteerUserId = userId;
        await dbContext.SaveChangesAsync();

        var currentUser = await dbContext.Users.FindAsync(userId);

        return new TopicSuggestionDto
        {
            Id = suggestion.Id,
            Title = suggestion.Title,
            Description = suggestion.Description,
            SuggestedByUserId = suggestion.SuggestedByUserId,
            SuggestedByName = $"{suggestion.SuggestedByUser.FirstName} {suggestion.SuggestedByUser.LastName}",
            VolunteerUserId = userId,
            VolunteerName = $"{currentUser?.FirstName} {currentUser?.LastName}",
            VoteCount = suggestion.Votes.Count,
            HasVoted = suggestion.Votes.Any(v => v.UserId == userId)
        };
    }

    public async Task<bool> DeleteSuggestionAsync(int topicId)
    {
        var userId = currentUserService.UserId;
        if (userId == default) return false;

        var suggestion = await dbContext.TopicSuggestions.FindAsync(topicId);
        if (suggestion is null) return false;

        var isAdmin = await dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == userId && dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin"));

        if (suggestion.SuggestedByUserId != userId && !isAdmin)
            return false;

        dbContext.TopicSuggestions.Remove(suggestion);
        await dbContext.SaveChangesAsync();
        return true;
    }

    private async Task<int> GetVoteCountAsync(int topicId)
    {
        return await dbContext.TopicVotes.CountAsync(v => v.TopicSuggestionId == topicId);
    }
}