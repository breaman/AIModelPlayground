using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using UserGroupSiteOpus48.Data.Interfaces;
using UserGroupSiteOpus48.Data.Models;
using UserGroupSiteOpus48.Shared.Dtos;
using UserGroupSiteOpus48.Shared.Markdown;
using UserGroupSiteOpus48.Shared.Services;

namespace UserGroupSiteOpus48.Server.Services;

/// <summary>
/// Server-side <see cref="ITopicService"/> backed by <see cref="ApplicationDbContext"/>. Enforces the
/// one-vote-per-user and single-volunteer rules authoritatively.
/// </summary>
public class ServerTopicService(ApplicationDbContext db, IUserService userService) : ITopicService
{
    public async Task<TopicSuggestionDto[]> GetTopicsAsync()
    {
        var currentUserId = userService.UserId;

        var topics = await db.TopicSuggestions
            .Include(t => t.SuggestedBy)
            .Include(t => t.Volunteer)
            .Include(t => t.Votes)
            .OrderByDescending(t => t.Votes.Count)
            .ThenByDescending(t => t.CreatedOn)
            .ToListAsync();

        return topics.Select(t => new TopicSuggestionDto(
            t.Id,
            t.Title,
            t.Description,
            MarkdownRenderer.ToHtml(t.Description),
            t.Votes.Count,
            t.Votes.Any(v => v.UserId == currentUserId),
            t.VolunteerUserId,
            t.Volunteer is null ? null : $"{t.Volunteer.FirstName} {t.Volunteer.LastName}".Trim(),
            $"{t.SuggestedBy.FirstName} {t.SuggestedBy.LastName}".Trim()))
            .ToArray();
    }

    public async Task<OperationResult> SuggestTopicAsync(TopicCreateDto dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, context, results, validateAllProperties: true))
        {
            return OperationResult.Fail([.. results.Select(r => r.ErrorMessage ?? "Invalid value.")]);
        }

        var topic = new TopicSuggestion
        {
            Title = dto.Title.Trim(),
            Description = dto.Description,
            SuggestedByUserId = userService.UserId
        };

        db.TopicSuggestions.Add(topic);
        await db.SaveChangesAsync();

        return OperationResult.Ok();
    }

    public async Task<OperationResult> VoteAsync(int topicId)
    {
        var currentUserId = userService.UserId;

        if (!await db.TopicSuggestions.AnyAsync(t => t.Id == topicId))
        {
            return OperationResult.Fail("Topic not found.");
        }

        // Reject a duplicate vote (also guarded by the unique index as a safety net).
        if (await db.TopicVotes.AnyAsync(v => v.TopicSuggestionId == topicId && v.UserId == currentUserId))
        {
            return OperationResult.Fail("You have already voted for this topic.");
        }

        db.TopicVotes.Add(new TopicVote { TopicSuggestionId = topicId, UserId = currentUserId });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Lost the race against a concurrent vote from the same user; treat as already voted.
            return OperationResult.Fail("You have already voted for this topic.");
        }

        return OperationResult.Ok();
    }

    public async Task<OperationResult> VolunteerAsync(int topicId)
    {
        var topic = await db.TopicSuggestions.FirstOrDefaultAsync(t => t.Id == topicId);
        if (topic is null)
        {
            return OperationResult.Fail("Topic not found.");
        }

        // Only allow volunteering when no one has volunteered yet (single occupancy).
        if (topic.VolunteerUserId is not null)
        {
            return OperationResult.Fail("Someone has already volunteered for this topic.");
        }

        topic.VolunteerUserId = userService.UserId;
        await db.SaveChangesAsync();

        return OperationResult.Ok();
    }
}