using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using UserGroupSiteOpus5.Data.Models;
using UserGroupSiteOpus5.Shared.Models;
using UserGroupSiteOpus5.Shared.Services;

using Microsoft.EntityFrameworkCore;

namespace UserGroupSiteOpus5.Server.Services;

/// <summary>
/// Server-side implementation of <see cref="ITopicSuggestionService"/>, running directly against
/// the database.
/// </summary>
public class ServerTopicSuggestionService(
    ApplicationDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ServerTopicSuggestionService> logger) : ITopicSuggestionService
{
    private int? CurrentUserId =>
        int.TryParse(
            httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var id)
            ? id
            : null;

    /// <inheritdoc />
    public async Task<IReadOnlyList<TopicSuggestionListItem>> GetSuggestionsAsync()
    {
        var userId = CurrentUserId;

        // Vote count and the current user's vote state are both resolved inside the projection, so
        // the list costs one query regardless of how many suggestions come back. Names are
        // projected as raw columns and composed afterwards, because the fallback logic in
        // BuildDisplayName has no SQL translation.
        var rows = await dbContext.TopicSuggestions
            .AsNoTracking()
            .OrderByDescending(x => x.Votes.Count)
            .ThenBy(x => x.Title)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                SuggestedByFirstName = x.SuggestedByUser.FirstName,
                SuggestedByLastName = x.SuggestedByUser.LastName,
                SuggestedByEmail = x.SuggestedByUser.Email,
                x.VolunteerUserId,
                VolunteerFirstName = x.VolunteerUser!.FirstName,
                VolunteerLastName = x.VolunteerUser.LastName,
                VolunteerEmail = x.VolunteerUser.Email,
                VoteCount = x.Votes.Count,
                HasCurrentUserVoted = userId != null && x.Votes.Any(v => v.UserId == userId)
            })
            .ToListAsync();

        return rows
            .Select(x => new TopicSuggestionListItem(
                x.Id,
                x.Title,
                x.Description,
                BuildDisplayName(x.SuggestedByFirstName, x.SuggestedByLastName, x.SuggestedByEmail),
                x.VolunteerUserId,
                x.VolunteerUserId is null
                    ? null
                    : BuildDisplayName(x.VolunteerFirstName, x.VolunteerLastName, x.VolunteerEmail),
                x.VoteCount,
                x.HasCurrentUserVoted))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<SaveResult<int>> CreateSuggestionAsync(TopicSuggestionCreateModel model)
    {
        var userId = CurrentUserId;

        if (userId is null)
        {
            return SaveResult<int>.Failure("You must be signed in to suggest a topic.");
        }

        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
        {
            return SaveResult<int>.Failure(results.Select(x => x.ErrorMessage ?? "Invalid value."));
        }

        var entity = new TopicSuggestion
        {
            Title = model.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            SuggestedByUserId = userId.Value
        };

        dbContext.TopicSuggestions.Add(entity);
        await dbContext.SaveChangesAsync();

        return SaveResult<int>.Success(entity.Id);
    }

    /// <inheritdoc />
    public async Task<SaveResult> VoteAsync(int suggestionId)
    {
        var userId = CurrentUserId;

        if (userId is null)
        {
            return SaveResult.Failure("You must be signed in to vote.");
        }

        if (!await dbContext.TopicSuggestions.AnyAsync(x => x.Id == suggestionId))
        {
            return SaveResult.Failure("That topic suggestion no longer exists.");
        }

        // A friendlier error than a constraint violation, but not the guarantee: the unique index
        // on (TopicSuggestionId, UserId) is what actually prevents a double vote when two requests
        // arrive at once.
        if (await dbContext.TopicSuggestionVotes.AnyAsync(x => x.TopicSuggestionId == suggestionId && x.UserId == userId))
        {
            return SaveResult.Failure("You have already voted for this topic.");
        }

        dbContext.TopicSuggestionVotes.Add(new TopicSuggestionVote
        {
            TopicSuggestionId = suggestionId,
            UserId = userId.Value
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Duplicate vote for suggestion {SuggestionId} by user {UserId}.", suggestionId, userId);
            return SaveResult.Failure("You have already voted for this topic.");
        }

        return SaveResult.Success();
    }

    /// <inheritdoc />
    public async Task<SaveResult> VolunteerAsync(int suggestionId)
    {
        var userId = CurrentUserId;

        if (userId is null)
        {
            return SaveResult.Failure("You must be signed in to volunteer.");
        }

        var suggestion = await dbContext.TopicSuggestions.FirstOrDefaultAsync(x => x.Id == suggestionId);

        if (suggestion is null)
        {
            return SaveResult.Failure("That topic suggestion no longer exists.");
        }

        if (suggestion.VolunteerUserId is { } existingVolunteer)
        {
            // Two members can click Volunteer at the same moment; the one whose write lands second
            // needs to be told why nothing happened rather than silently seeing someone else's name.
            return existingVolunteer == userId
                ? SaveResult.Failure("You have already volunteered for this topic.")
                : SaveResult.Failure("Someone else has already volunteered to present this topic.");
        }

        suggestion.VolunteerUserId = userId;
        await dbContext.SaveChangesAsync();

        return SaveResult.Success();
    }

    /// <summary>
    /// Builds a member's display name inside a LINQ projection, falling back through last name and
    /// email so a member who never supplied a name still shows something meaningful.
    /// </summary>
    private static string BuildDisplayName(string? firstName, string? lastName, string? email)
    {
        if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
        {
            return $"{firstName} {lastName}";
        }

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            return firstName;
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            return lastName;
        }

        return email ?? "Unnamed member";
    }
}
