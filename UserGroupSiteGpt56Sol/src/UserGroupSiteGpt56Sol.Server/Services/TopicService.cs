using System.Data;

using UserGroupSiteGpt56Sol.Data.Models;
using UserGroupSiteGpt56Sol.Shared.Models;
using UserGroupSiteGpt56Sol.Shared.Services;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteGpt56Sol.Server.Services;

/// <summary>Implements authenticated topic operations against EF Core.</summary>
public sealed class TopicService(
    ApplicationDbContext dbContext,
    CurrentUserAccessor currentUser,
    ILogger<TopicService> logger) : ITopicService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TopicSuggestionDto>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var userId = currentUser.UserId;
        return await dbContext.TopicSuggestions.AsNoTracking()
            .OrderByDescending(item => item.Votes.Count)
            .ThenByDescending(item => item.CreatedOn)
            .Select(item => new TopicSuggestionDto(item.Id, item.Title, item.SupportingDetails,
                ((item.Creator.FirstName ?? string.Empty) + " " + (item.Creator.LastName ?? string.Empty)).Trim() !=
                string.Empty
                    ? ((item.Creator.FirstName ?? string.Empty) + " " +
                       (item.Creator.LastName ?? string.Empty)).Trim()
                    : item.Creator.Email ?? item.Creator.UserName ?? "Member",
                item.CreatedOn ?? DateTime.MinValue, item.Votes.Count,
                item.Votes.Any(vote => vote.UserId == userId), item.VolunteerUser == null
                    ? null
                    : ((item.VolunteerUser.FirstName ?? string.Empty) + " " +
                       (item.VolunteerUser.LastName ?? string.Empty)).Trim() != string.Empty
                        ? ((item.VolunteerUser.FirstName ?? string.Empty) + " " +
                           (item.VolunteerUser.LastName ?? string.Empty)).Trim()
                        : item.VolunteerUser.Email ?? item.VolunteerUser.UserName ?? "Member"))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<int>> CreateAsync(CreateTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var errors = ValidationUtility.Validate(request);
        if (errors.Count > 0)
        {
            return ServiceResult<int>.Invalid(errors);
        }

        var topic = new TopicSuggestion
        {
            Title = request.Title.Trim(),
            SupportingDetails = string.IsNullOrWhiteSpace(request.SupportingDetails)
                ? null
                : request.SupportingDetails.Trim()
        };
        dbContext.TopicSuggestions.Add(topic);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Topic {TopicId} created by user {UserId}", topic.Id, currentUser.UserId);
        return ServiceResult<int>.Success(topic.Id, "Topic suggested.");
    }

    /// <inheritdoc />
    public async Task<ServiceResult> VoteAsync(int topicId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (!await dbContext.TopicSuggestions.AnyAsync(item => item.Id == topicId, cancellationToken))
        {
            return ServiceResult.Failure("That topic no longer exists.");
        }

        if (await dbContext.TopicVotes.AnyAsync(
                item => item.TopicSuggestionId == topicId && item.UserId == currentUser.UserId,
                cancellationToken))
        {
            return ServiceResult.Success("Your vote is already recorded.");
        }

        dbContext.TopicVotes.Add(new TopicVote
        {
            TopicSuggestionId = topicId,
            UserId = currentUser.UserId,
            CreatedOnUtc = DateTime.UtcNow
        });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success("Vote recorded.");
        }
        catch (DbUpdateException exception)
        {
            logger.LogInformation(exception, "Concurrent duplicate vote ignored for topic {TopicId}", topicId);
            dbContext.ChangeTracker.Clear();
            return ServiceResult.Success("Your vote is already recorded.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UnvoteAsync(int topicId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var vote = await dbContext.TopicVotes.SingleOrDefaultAsync(
            item => item.TopicSuggestionId == topicId && item.UserId == currentUser.UserId,
            cancellationToken);
        if (vote is null)
        {
            return ServiceResult.Success("No vote was recorded.");
        }

        dbContext.TopicVotes.Remove(vote);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Vote removed.");
    }

    /// <inheritdoc />
    public async Task<ServiceResult> VolunteerAsync(int topicId,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var topic = await dbContext.TopicSuggestions.SingleOrDefaultAsync(item => item.Id == topicId,
            cancellationToken);
        if (topic is null)
        {
            return ServiceResult.Failure("That topic no longer exists.");
        }

        if (topic.VolunteerUserId.HasValue)
        {
            return ServiceResult.Failure("Someone has already volunteered for this topic.");
        }

        topic.VolunteerUserId = currentUser.UserId;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("User {UserId} volunteered for topic {TopicId}", currentUser.UserId, topicId);
            return ServiceResult.Success("Thanks for volunteering!");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ServiceResult.Failure("Someone else just volunteered for this topic.");
        }
    }

    /// <summary>Throws when no authenticated request user is available.</summary>
    private void EnsureAuthenticated()
    {
        if (currentUser.UserId == 0)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }
    }
}