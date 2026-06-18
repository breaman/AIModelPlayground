using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using UserGroupSiteGlm52.Data.Models;
using UserGroupSiteGlm52.Shared.Models;
using UserGroupSiteGlm52.Shared.Services;

namespace UserGroupSiteGlm52.Server.Services;

/// <summary>DB-backed <see cref="ITopicService"/>. All actions require a logged-in user (enforced by the API endpoints).</summary>
public sealed class ServerTopicService(
    ApplicationDbContext db,
    IHttpContextAccessor httpContextAccessor) : ITopicService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TopicDto>> GetTopicsAsync()
    {
        var currentUserId = CurrentUserId();
        var topics = await db.TopicSuggestions
            .AsNoTracking()
            .Include(t => t.SuggestedByUser)
            .Include(t => t.Votes)
            .Include(t => t.Volunteer).ThenInclude(v => v!.User)
            .OrderByDescending(t => t.Votes.Count)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        return topics.Select(t => Map(t, currentUserId)).ToList();
    }

    /// <inheritdoc />
    public async Task<ServiceResult<TopicDto>> SuggestAsync(TopicInputDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return ServiceResult<TopicDto>.Failed(
                new Dictionary<string, string[]> { [nameof(TopicInputDto.Title)] = ["A title is required."] });
        }

        var topic = new TopicSuggestion
        {
            Title = dto.Title,
            Description = dto.Description,
            SuggestedByUserId = CurrentUserId()
        };

        db.TopicSuggestions.Add(topic);
        await db.SaveChangesAsync();

        // Re-load with relationships for the mapping.
        var created = await db.TopicSuggestions
            .AsNoTracking()
            .Include(t => t.SuggestedByUser)
            .Include(t => t.Votes)
            .Include(t => t.Volunteer).ThenInclude(v => v!.User)
            .FirstAsync(t => t.Id == topic.Id);

        return ServiceResult<TopicDto>.Success(Map(created, CurrentUserId()));
    }

    /// <inheritdoc />
    public async Task<ServiceResult> VoteAsync(int id)
    {
        var currentUserId = CurrentUserId();
        if (currentUserId == 0)
        {
            return ServiceResult.Failed("You must be signed in to vote.");
        }

        var exists = await db.TopicSuggestions.AnyAsync(t => t.Id == id);
        if (!exists)
        {
            return ServiceResult.Failed("Topic not found.");
        }

        // Server check; the composite unique index is the DB-level backstop.
        var alreadyVoted = await db.TopicVotes.AnyAsync(v => v.TopicSuggestionId == id && v.UserId == currentUserId);
        if (alreadyVoted)
        {
            return ServiceResult.Failed("You have already voted on this topic.");
        }

        db.TopicVotes.Add(new TopicVote
        {
            TopicSuggestionId = id,
            UserId = currentUserId,
            VotedOn = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Race: another request inserted the same (topic, user) vote first.
            return ServiceResult.Failed("You have already voted on this topic.");
        }

        return ServiceResult.Success();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UnvoteAsync(int id)
    {
        var currentUserId = CurrentUserId();
        var vote = await db.TopicVotes
            .FirstOrDefaultAsync(v => v.TopicSuggestionId == id && v.UserId == currentUserId);

        if (vote is null)
        {
            return ServiceResult.Failed("You have not voted on this topic.");
        }

        db.TopicVotes.Remove(vote);
        await db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> VolunteerAsync(int id)
    {
        var currentUserId = CurrentUserId();
        if (currentUserId == 0)
        {
            return ServiceResult.Failed("You must be signed in to volunteer.");
        }

        var topic = await db.TopicSuggestions
            .Include(t => t.Volunteer)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic is null)
        {
            return ServiceResult.Failed("Topic not found.");
        }

        if (topic.Volunteer is not null)
        {
            return ServiceResult.Failed("Someone has already volunteered for this topic.");
        }

        topic.Volunteer = new TopicVolunteer
        {
            TopicSuggestionId = id,
            UserId = currentUserId,
            VolunteeredOn = DateTime.UtcNow
        };

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Race: another user became the volunteer first.
            return ServiceResult.Failed("Someone has already volunteered for this topic.");
        }

        return ServiceResult.Success();
    }

    // --- helpers ---

    private int CurrentUserId()
    {
        var nameId = httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(nameId, out var id) ? id : 0;
    }

    private static TopicDto Map(TopicSuggestion t, int currentUserId)
    {
        return new TopicDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            SuggestedByName = DisplayName(t.SuggestedByUser),
            VoteCount = t.Votes.Count,
            HasCurrentUserVoted = t.Votes.Any(v => v.UserId == currentUserId),
            VolunteerName = t.Volunteer is null ? null : DisplayName(t.Volunteer.User),
            HasCurrentUserVolunteered = t.Volunteer is not null && t.Volunteer.UserId == currentUserId
        };
    }

    private static string DisplayName(User user)
    {
        var name = (user.FirstName + " " + user.LastName).Trim();
        return string.IsNullOrWhiteSpace(name) ? (user.Email ?? "Unknown") : name;
    }
}