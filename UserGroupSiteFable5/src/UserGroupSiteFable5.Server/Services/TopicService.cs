using Microsoft.EntityFrameworkCore;

using UserGroupSiteFable5.Data.Interfaces;
using UserGroupSiteFable5.Data.Models;
using UserGroupSiteFable5.Shared.Dtos;
using UserGroupSiteFable5.Shared.Services;

namespace UserGroupSiteFable5.Server.Services;

/// <summary>
/// Server-side <see cref="ITopicService"/> backing the topic suggestions page
/// and its API endpoints.
/// </summary>
public class TopicService(ApplicationDbContext dbContext, IUserService currentUser) : ITopicService
{
    public async Task<List<TopicSuggestionDto>> GetTopicsAsync()
    {
        var userId = currentUser.UserId;

        return await dbContext.TopicSuggestions
            .OrderByDescending(t => t.Votes.Count)
            .ThenBy(t => t.Title)
            .Select(t => new TopicSuggestionDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                // Inline name expressions so EF can translate them to SQL.
                SuggestedByName = t.SuggestedByUser.FirstName == null && t.SuggestedByUser.LastName == null
                    ? t.SuggestedByUser.Email ?? ""
                    : ((t.SuggestedByUser.FirstName ?? "") + " " + (t.SuggestedByUser.LastName ?? "")).Trim(),
                VoteCount = t.Votes.Count,
                CurrentUserVoted = t.Votes.Any(v => v.UserId == userId),
                VolunteerName = t.VolunteerUser == null
                    ? null
                    : t.VolunteerUser.FirstName == null && t.VolunteerUser.LastName == null
                        ? t.VolunteerUser.Email ?? ""
                        : ((t.VolunteerUser.FirstName ?? "") + " " + (t.VolunteerUser.LastName ?? "")).Trim()
            })
            .ToListAsync();
    }

    public async Task<ServiceResult> CreateTopicAsync(TopicCreateDto topic)
    {
        var validationError = ValidationHelper.Validate(topic);
        if (validationError is not null)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, validationError);
        }

        dbContext.TopicSuggestions.Add(new TopicSuggestion
        {
            Title = topic.Title.Trim(),
            Description = topic.Description?.Trim(),
            SuggestedByUserId = currentUser.UserId
        });
        await dbContext.SaveChangesAsync();

        return ServiceResult.Ok;
    }

    public async Task<ServiceResult> VoteAsync(int topicId)
    {
        var topicExists = await dbContext.TopicSuggestions.AnyAsync(t => t.Id == topicId);
        if (!topicExists)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "Topic not found.");
        }

        var alreadyVoted = await dbContext.TopicVotes
            .AnyAsync(v => v.TopicSuggestionId == topicId && v.UserId == currentUser.UserId);
        if (alreadyVoted)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "You have already voted for this topic.");
        }

        dbContext.TopicVotes.Add(new TopicVote { TopicSuggestionId = topicId, UserId = currentUser.UserId });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The composite primary key makes a concurrent duplicate vote fail at the
            // database level — surface it as the same friendly conflict.
            return ServiceResult.Fail(ServiceErrorType.Conflict, "You have already voted for this topic.");
        }

        return ServiceResult.Ok;
    }

    public async Task<ServiceResult> VolunteerAsync(int topicId)
    {
        var topic = await dbContext.TopicSuggestions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == topicId);
        if (topic is null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "Topic not found.");
        }

        if (topic.VolunteerUserId is not null)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "Someone has already volunteered for this topic.");
        }

        // Guard against a concurrent volunteer race: only claim the slot if it is still empty.
        var claimed = await dbContext.TopicSuggestions
            .Where(t => t.Id == topicId && t.VolunteerUserId == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.VolunteerUserId, currentUser.UserId));

        return claimed == 1
            ? ServiceResult.Ok
            : ServiceResult.Fail(ServiceErrorType.Conflict, "Someone has already volunteered for this topic.");
    }
}