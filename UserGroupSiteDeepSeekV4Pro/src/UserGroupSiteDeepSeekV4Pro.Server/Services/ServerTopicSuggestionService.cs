using Microsoft.EntityFrameworkCore;

using UserGroupSiteDeepSeekV4Pro.Data.Interfaces;
using UserGroupSiteDeepSeekV4Pro.Data.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Models;
using UserGroupSiteDeepSeekV4Pro.Shared.Services;

namespace UserGroupSiteDeepSeekV4Pro.Server.Services;

public class ServerTopicSuggestionService(ApplicationDbContext context, IUserService userService) : ITopicSuggestionService
{
    public async Task<List<TopicSuggestionListItemDto>> GetAllAsync()
    {
        var currentUserId = userService.UserId;

        return await context.TopicSuggestions
            .Include(ts => ts.SuggestedByUser)
            .Include(ts => ts.VolunteerUser)
            .Include(ts => ts.Votes)
            .OrderByDescending(ts => ts.Votes.Count)
            .ThenByDescending(ts => ts.CreatedOn)
            .Select(ts => new TopicSuggestionListItemDto
            {
                Id = ts.Id,
                Title = ts.Title,
                Description = ts.Description,
                VoteCount = ts.Votes.Count,
                CurrentUserHasVoted = ts.Votes.Any(v => v.UserId == currentUserId),
                CurrentUserIsVolunteer = ts.VolunteerUserId == currentUserId,
                CreatedOn = ts.CreatedOn,
                SuggestedByUser = ts.SuggestedByUser != null ? new UserDto
                {
                    Id = ts.SuggestedByUser.Id,
                    FirstName = ts.SuggestedByUser.FirstName,
                    LastName = ts.SuggestedByUser.LastName,
                    Email = ts.SuggestedByUser.Email ?? ""
                } : null,
                VolunteerUser = ts.VolunteerUser != null ? new UserDto
                {
                    Id = ts.VolunteerUser.Id,
                    FirstName = ts.VolunteerUser.FirstName,
                    LastName = ts.VolunteerUser.LastName,
                    Email = ts.VolunteerUser.Email ?? ""
                } : null
            })
            .ToListAsync();
    }

    public async Task<TopicSuggestionDto> CreateAsync(TopicSuggestionDto dto)
    {
        var currentUserId = userService.UserId;

        var entity = new TopicSuggestion
        {
            Title = dto.Title,
            Description = dto.Description,
            SuggestedByUserId = currentUserId
        };

        context.TopicSuggestions.Add(entity);
        await context.SaveChangesAsync();

        return new TopicSuggestionDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            SuggestedByUserId = entity.SuggestedByUserId,
            CreatedOn = entity.CreatedOn
        };
    }

    public async Task VoteAsync(int topicId)
    {
        var currentUserId = userService.UserId;
        var existingVote = await context.TopicVotes
            .FirstOrDefaultAsync(tv => tv.TopicSuggestionId == topicId && tv.UserId == currentUserId);

        if (existingVote is not null)
            return; // Already voted

        context.TopicVotes.Add(new TopicVote
        {
            TopicSuggestionId = topicId,
            UserId = currentUserId
        });
        await context.SaveChangesAsync();
    }

    public async Task RemoveVoteAsync(int topicId)
    {
        var currentUserId = userService.UserId;
        var existingVote = await context.TopicVotes
            .FirstOrDefaultAsync(tv => tv.TopicSuggestionId == topicId && tv.UserId == currentUserId);

        if (existingVote is not null)
        {
            context.TopicVotes.Remove(existingVote);
            await context.SaveChangesAsync();
        }
    }

    public async Task VolunteerAsync(int topicId)
    {
        var currentUserId = userService.UserId;
        var topic = await context.TopicSuggestions.FindAsync(topicId)
            ?? throw new InvalidOperationException("Topic not found.");

        if (topic.VolunteerUserId is not null)
            throw new InvalidOperationException("Someone has already volunteered for this topic.");

        topic.VolunteerUserId = currentUserId;
        await context.SaveChangesAsync();
    }

    public async Task RemoveVolunteerAsync(int topicId)
    {
        var currentUserId = userService.UserId;
        var topic = await context.TopicSuggestions.FindAsync(topicId)
            ?? throw new InvalidOperationException("Topic not found.");

        if (topic.VolunteerUserId == currentUserId)
        {
            topic.VolunteerUserId = null;
            await context.SaveChangesAsync();
        }
    }
}